using AUS2_S2.DataClasses;
using AUS2_S2.LinearHashFile.MainHF;
using AUS2_S2.LinearHashFile.OverflowHF;
using System.Text;

namespace AUS2_S2.LinearHashFile;

public class LinHashFile<T> where T : IData<T>, new()
{
    private readonly MainHeapFile<T> mainFile;
    private readonly OverflowHeapFile<T> overflowFile;

    private int baseBucketCount;
    private int level;
    private int splitPointer;

    private int mainValidCount;
    private int overflowValidCount;

    private readonly FileStream metadataStream;

    private static string AddMetadataExtension(string filePath) => filePath + ".metadata";

    public LinHashFile(int mainFileBlockSize, string mainFilePath, int overflowFileBlockSize, string overflowFilePath, 
        int initialBucketCount, string metadataPath)
    {
        mainFile = new(mainFileBlockSize, mainFilePath, LinHashFile<T>.AddMetadataExtension(mainFilePath));
        overflowFile = new(overflowFileBlockSize, overflowFilePath, LinHashFile<T>.AddMetadataExtension(overflowFilePath));
        if (File.Exists(metadataPath))
            File.Delete(metadataPath);
        metadataStream = new(metadataPath, FileMode.Create, FileAccess.ReadWrite);

        baseBucketCount = initialBucketCount;
        level = 0;
        splitPointer = 0;
        mainValidCount = 0;
        overflowValidCount = 0;

        MainHFBlock<T> mainBlock = new(mainFile.BlockSize, mainFile.BlockingFactor);
        for (int i = 0; i < baseBucketCount; i++)
            mainFile.WriteBlockAtEnd(mainBlock);
    }

    public LinHashFile(string mainFilePath, string overflowFilePath, string metadataPath)
    {
        mainFile = new(mainFilePath, AddMetadataExtension(mainFilePath));
        overflowFile = new(overflowFilePath, AddMetadataExtension(overflowFilePath));
        metadataStream = new(metadataPath, FileMode.Open, FileAccess.ReadWrite);
        LoadMetadata();
    }

    private void LoadMetadata()
    {
        metadataStream.Position = 0;
        using var br = new BinaryReader(metadataStream, Encoding.Default, leaveOpen: true);
        baseBucketCount = br.ReadInt32();
        level = br.ReadInt32();
        splitPointer = br.ReadInt32();
        mainValidCount = br.ReadInt32();
        overflowValidCount = br.ReadInt32();
    }

    private void SaveMetadata()
    {
        metadataStream.Position = 0;
        metadataStream.SetLength(0);
        using var bw = new BinaryWriter(metadataStream, Encoding.Default, leaveOpen: true);
        bw.Write(baseBucketCount);
        bw.Write(level);
        bw.Write(splitPointer);
        bw.Write(mainValidCount);
        bw.Write(overflowValidCount);
    }

    public void Close()
    {
        mainFile.Close();
        overflowFile.Close();
        SaveMetadata();
        metadataStream.Close();
    }

    private int Hash0(int hashCode) => Math.Abs(hashCode % (baseBucketCount << level));
    private int Hash1(int hashCode) => Math.Abs(hashCode % (baseBucketCount << (level + 1)));

    private int Hash(T data)
    {
        int h = data.GetHashCode();
        int bucket = Hash0(h);

        if (bucket < splitPointer)
            bucket = Hash1(h);

        return bucket;
    }

    private long GetBucketAddress(int bucketIndex) => mainFile.GetBlockAddressByIndex(bucketIndex);

    public void Insert(T data)
    {
        int bucket = Hash(data);
        InsertIntoBucket(bucket, data);

        while (ShouldSplit())
        {
            Split();
        }
            
    }

    private void InsertIntoBucket(int bucket, T data)
    {
        long address = GetBucketAddress(bucket);

        var mainBlock = mainFile.ReadBlock(address);

        if (mainBlock.ValidCount < mainBlock.BlockData.Length)
        {
            //insert into block in main file
            mainBlock.BlockData[mainBlock.ValidCount] = data;
            mainBlock.IncrementValidCount();
            mainFile.WriteBlock(address, mainBlock);
            mainValidCount++;
            return;
        }

        //continue to overflow file
        long prevAddress = -1;
        long currentAddress = mainBlock.OverflowAddress;
        OverflowHFBlock<T>? prevBlock = null;

        while (currentAddress >= 0)
        {
            var overflowBlock = overflowFile.ReadBlock(currentAddress);

            if (overflowBlock.ValidCount < overflowBlock.BlockData.Length)
            {
                //insert into existing block in overflow file
                overflowBlock.BlockData[overflowBlock.ValidCount] = data;
                overflowBlock.IncrementValidCount();
                overflowFile.WriteBlock(currentAddress, overflowBlock);
                overflowValidCount++;
                mainBlock.OverflowValidRecordsCount++;
                mainFile.WriteBlock(address, mainBlock);
                return;
            }

            prevAddress = currentAddress;
            prevBlock = overflowBlock;
            currentAddress = overflowBlock.OverflowAddress;
        }

        //have to create new block in overflow file and insert there
        long newAddr = overflowFile.GetNextClearBlockAddress();

        var newBlock = new OverflowHFBlock<T>(overflowFile.BlockSize, overflowFile.BlockingFactor);
        newBlock.BlockData[0] = data;
        overflowValidCount++;
        newBlock.IncrementValidCount();
        overflowFile.WriteBlock(newAddr, newBlock);

        //set overflow address to correct block
        if (prevAddress == -1 || prevBlock == null)
        {
            mainBlock.OverflowAddress = newAddr;      
        }
        else
        {
            prevBlock.OverflowAddress = newAddr;
            overflowFile.WriteBlock(prevAddress, prevBlock);
        }

        mainBlock.OverflowValidRecordsCount++;
        mainBlock.OverflowBlocksCount++;
        mainFile.WriteBlock(address, mainBlock);
    }

    private void Split()
    {
        var (records, mainBlock, overflowBlocksAndAddresses) = ReadBucket(splitPointer);

        mainValidCount -= mainBlock.ValidCount;
        overflowValidCount -= mainBlock.OverflowValidRecordsCount;

        mainBlock.ResetBlock();
        foreach (var (block, _) in overflowBlocksAndAddresses)
            block.ResetBlock();

        List<T> recordsToMove = [];
        List<T> recordsToStay = [];

        foreach (var rec in records)
        {
            var hashCode = rec.GetHashCode();
            if (Hash1(hashCode) == splitPointer)
                recordsToStay.Add(rec);
            else
                recordsToMove.Add(rec);
        }


        //preparing blocks with staying records
        int index = 0;
        OverflowHFBlock<T>? helpBlock = null;
        foreach (var rec in recordsToStay)
        {
            if (mainBlock.ValidCount < mainBlock.BlockData.Length)
            {
                mainBlock.BlockData[mainBlock.ValidCount] = rec;
                mainBlock.IncrementValidCount();
                mainValidCount++;
                continue;
            }

            if (helpBlock == null || helpBlock.ValidCount == helpBlock.BlockData.Length)
            {
                var prevBlock = helpBlock;
                (helpBlock, var address) = overflowBlocksAndAddresses[index];
                mainBlock.OverflowBlocksCount++;
                if (prevBlock == null)
                    mainBlock.OverflowAddress = address;
                else
                    prevBlock.OverflowAddress = address;
                index++;
            }

            helpBlock.BlockData[helpBlock.ValidCount] = rec;
            helpBlock.IncrementValidCount();
            mainBlock.OverflowValidRecordsCount++;
            overflowValidCount++;
        }

        for (int i = overflowBlocksAndAddresses.Count - 1; i >= index; i--)
        {
            overflowFile.MarkBlockAsFree(overflowBlocksAndAddresses[i].address);
            overflowBlocksAndAddresses.RemoveAt(i);
        }

        //preparing new blocks for moving records
        MainHFBlock<T> newMainBlock = new(mainFile.BlockSize, mainFile.BlockingFactor);
        List<(OverflowHFBlock<T> block, long address)> newOverflowBlocksAndAddresses = [];
        OverflowHFBlock<T>? actualOverflowBlock = null;

        foreach (var rec in recordsToMove)
        {
            if (newMainBlock.ValidCount < newMainBlock.BlockData.Length)
            {
                newMainBlock.BlockData[newMainBlock.ValidCount] = rec;
                newMainBlock.IncrementValidCount();
                mainValidCount++;
                continue;
            }

            if (actualOverflowBlock == null || actualOverflowBlock.ValidCount == actualOverflowBlock.BlockData.Length)
            {
                var prevBlock = actualOverflowBlock;
                actualOverflowBlock = new(overflowFile.BlockSize, overflowFile.BlockingFactor);
                var newAddress = overflowFile.GetNextClearBlockAddress();
                newMainBlock.OverflowBlocksCount++;
                newOverflowBlocksAndAddresses.Add((actualOverflowBlock, newAddress));
                if (prevBlock == null)
                    newMainBlock.OverflowAddress = newAddress;
                else
                    prevBlock.OverflowAddress = newAddress;
            }

            actualOverflowBlock.BlockData[actualOverflowBlock.ValidCount] = rec;
            actualOverflowBlock.IncrementValidCount();
            newMainBlock.OverflowValidRecordsCount++;
            overflowValidCount++;
        }

        //write all new blocks
        var mainBlockAddress = GetBucketAddress(splitPointer);
        mainFile.WriteBlock(mainBlockAddress, mainBlock);
        foreach (var (block, address) in overflowBlocksAndAddresses)
            overflowFile.WriteBlock(address, block);

        var newMainBlockAddress = mainFile.GetNextClearBlockAddress();
        mainFile.WriteBlock(newMainBlockAddress, newMainBlock);
        foreach (var (block, address) in newOverflowBlocksAndAddresses)
            overflowFile.WriteBlock(address, block);

        //setting new splitpointer after split
        splitPointer++;
        if (splitPointer == (baseBucketCount << level))
        {
            splitPointer = 0;
            level++;
        }

        overflowFile.RemoveAllEmptyBlocksFromEnd();
    }

    private (List<T>, 
        MainHFBlock<T>, 
        List<(OverflowHFBlock<T> block, long address)>)
    ReadBucket(int bucket)
    {
        var mainBlockAddress = GetBucketAddress(bucket);
        List<T> records = [];
        List<(OverflowHFBlock<T>, long address)> overflowBlocksAndAddresses = [];
        MainHFBlock<T> mainBlock = mainFile.ReadBlock(mainBlockAddress);

        for (int i = 0; i < mainBlock.ValidCount; i++)
            records.Add(mainBlock.BlockData[i]);

        long overflowAddr = mainBlock.OverflowAddress;

        while (overflowAddr >= 0)
        {
            var overflowBlock = overflowFile.ReadBlock(overflowAddr);
            overflowBlocksAndAddresses.Add((overflowBlock, overflowAddr));

            for (int i = 0; i < overflowBlock.ValidCount; i++)
                records.Add(overflowBlock.BlockData[i]);

            overflowAddr = overflowBlock.OverflowAddress;
        }

        return (records, mainBlock, overflowBlocksAndAddresses);
    }

    private List<T> ReadAndClearBucket(int bucket)
    {
        var address = GetBucketAddress(bucket);
        List<T> list = [];

        var mainBlock = mainFile.ReadBlock(address);

        mainValidCount -= mainBlock.ValidCount;
        overflowValidCount -= mainBlock.OverflowValidRecordsCount;

        for (int i = 0; i < mainBlock.ValidCount; i++)
            list.Add(mainBlock.BlockData[i]);

        long overflowAddr = mainBlock.OverflowAddress;

        mainBlock.ResetBlock();
        mainFile.WriteBlock(address, mainBlock);

        while (overflowAddr >= 0)
        {
            var overflowBlock = overflowFile.ReadBlock(overflowAddr);

            for (int i = 0; i < overflowBlock.ValidCount; i++)
                list.Add(overflowBlock.BlockData[i]);

            long next = overflowBlock.OverflowAddress;

            overflowBlock.ResetBlock();
            overflowFile.MarkBlockAsFree(overflowAddr);

            overflowAddr = next;
        }

        return list;
    }

    private bool ShouldSplit()
    {
        return (overflowValidCount > mainValidCount * 0.8);
    }

    private bool ShouldMerge()
    {
        if (level <= 0)
            return false;

        return (overflowValidCount < mainValidCount * 0.5);
    }

    public T? Get(T dummy)
    {
        int bucket = Hash(dummy);
        long addr = GetBucketAddress(bucket);

        //check main file
        var main = mainFile.ReadBlock(addr);
        for (int i = 0; i < main.ValidCount; i++)
        {
            if (main.BlockData[i].Equals(dummy))
                return main.BlockData[i];
        }

        //chcek overflow file
        long overflowAddr = main.OverflowAddress;
        while (overflowAddr >= 0)
        {
            var ob = overflowFile.ReadBlock(overflowAddr);
            for (int i = 0; i < ob.ValidCount; i++)
            {
                if (ob.BlockData[i].Equals(dummy))
                    return ob.BlockData[i];
            }
            overflowAddr = ob.OverflowAddress;
        }

        return default;
    }

    public bool Update(T newData)
    {
        int bucket = Hash(newData);
        long address = GetBucketAddress(bucket);

        //check main block
        var mainBlock = mainFile.ReadBlock(address);

        for (int i = 0; i < mainBlock.ValidCount; i++)
        {
            if (mainBlock.BlockData[i].Equals(newData))
            {
                mainBlock.BlockData[i] = newData;
                mainFile.WriteBlock(address, mainBlock);
                return true;
            }
        }

        //check overflow blocks
        long overflowAddress = mainBlock.OverflowAddress;

        while (overflowAddress >= 0)
        {
            var overflowBlock = overflowFile.ReadBlock(overflowAddress);

            for (int i = 0; i < overflowBlock.ValidCount; i++)
            {
                if (overflowBlock.BlockData[i].Equals(newData))
                {
                    overflowBlock.BlockData[i] = newData;
                    overflowFile.WriteBlock(overflowAddress, overflowBlock);
                    return true;
                }
            }

            overflowAddress = overflowBlock.OverflowAddress;
        }

        return false;
    }


    public bool Delete(T dummy)
    {
        int bucket = Hash(dummy);
        var result = DeleteFromBucket(bucket, dummy);

        if (!result)
            return false;

        while (ShouldMerge())
        {
            Merge();
        }

        TryCompactBucket(bucket);

        return true;
    }

    private bool DeleteFromBucket(int bucket, T data)
    {
        var address = GetBucketAddress(bucket);

        //try find in main file and delete
        var mainBlock = mainFile.ReadBlock(address);
        for (int i = 0; i < mainBlock.ValidCount; i++)
        {
            if (mainBlock.BlockData[i].Equals(data))
            {
                for (int j = i + 1; j < mainBlock.ValidCount; j++)
                    mainBlock.BlockData[j - 1] = mainBlock.BlockData[j];
                mainBlock.DecrementValidCount();
                mainValidCount--;
                mainFile.WriteBlock(address, mainBlock);
                return true;
            }
        }

        //try find in overflow file and delete
        long prevAddress = -1;
        long currentAddress = mainBlock.OverflowAddress;
        while (currentAddress >= 0)
        {
            var overflowBLock = overflowFile.ReadBlock(currentAddress);
            for (int i = 0; i < overflowBLock.ValidCount; i++)
            {
                if (overflowBLock.BlockData[i].Equals(data))
                {
                    for (int j = i + 1; j < overflowBLock.ValidCount; j++)
                        overflowBLock.BlockData[j - 1] = overflowBLock.BlockData[j];

                    overflowBLock.DecrementValidCount();
                    mainBlock.OverflowValidRecordsCount--;
                    overflowValidCount--;

                    //if block becomes empty - mark as free and fix addresses
                    if (overflowBLock.ValidCount == 0)
                    {
                        if (prevAddress == -1)
                        {
                            mainBlock.OverflowAddress = overflowBLock.OverflowAddress;
                            //mainFile.WriteBlock(address, mainBlock);
                        }
                        else
                        {
                            var prev = overflowFile.ReadBlock(prevAddress);
                            prev.OverflowAddress = overflowBLock.OverflowAddress;
                            
                            //mainFile.WriteBlock(address, mainBlock);
                            overflowFile.WriteBlock(prevAddress, prev);
                        }

                        mainBlock.OverflowBlocksCount--;
                        overflowFile.MarkBlockAsFree(currentAddress);
                        overflowFile.RemoveAllEmptyBlocksFromEnd();
                    }
                    else
                    {
                        //just write changed overflow block back
                        overflowFile.WriteBlock(currentAddress, overflowBLock);
                    }

                    mainFile.WriteBlock(address, mainBlock);


                    return true;
                }
            }

            prevAddress = currentAddress;
            currentAddress = overflowBLock.OverflowAddress;
        }

        return false;
    }


    private void Merge()
    {
        int mainBucketsCount = (baseBucketCount << level) + splitPointer;
        int bucketToRemove = mainBucketsCount - 1;

        int bucketToMerge;
        if (splitPointer > 0)
        {
            bucketToMerge = splitPointer - 1;
            splitPointer--;
        }
        else
        {
            bucketToMerge = (baseBucketCount << (level - 1)) - 1;
            level--;
            splitPointer = (baseBucketCount << level) - 1;
        }

        var listMergingRecords = ReadAndClearBucket(bucketToRemove);
        var (_, mainBlock, overflowBlocksAndAddresses) = ReadBucket(bucketToMerge);

        List<long> changedBlocksOverflowAddresses = [];

        OverflowHFBlock<T>? helpBlock = null;
        long currentHelpBlockAddress = -1;
        int index = 0;

        foreach (var rec in listMergingRecords)
        {
            //try insert to main
            if (mainBlock.ValidCount < mainBlock.BlockData.Length)
            {
                mainBlock.BlockData[mainBlock.ValidCount] = rec;
                mainBlock.IncrementValidCount();
                mainValidCount++;
                continue;
            }

            //try find existing overflow block to insert
            if (helpBlock == null && overflowBlocksAndAddresses.Count > 0 && index < overflowBlocksAndAddresses.Count)
            {
                (helpBlock, currentHelpBlockAddress) = overflowBlocksAndAddresses[index];
            }

            while (helpBlock != null && helpBlock.ValidCount == helpBlock.BlockData.Length)
            {
                index++;
                if (index < overflowBlocksAndAddresses.Count)
                {
                    (helpBlock, currentHelpBlockAddress) = overflowBlocksAndAddresses[index];
                }
                else
                {
                    break;
                }
            }

            //need create new block to insert
            if (helpBlock == null || helpBlock.ValidCount == helpBlock.BlockData.Length)
            {
                var newAddress = overflowFile.GetNextClearBlockAddress();

                if (helpBlock != null)
                {
                    helpBlock.OverflowAddress = newAddress;
                    if (!changedBlocksOverflowAddresses.Contains(currentHelpBlockAddress))
                        changedBlocksOverflowAddresses.Add(currentHelpBlockAddress);
                }
                else
                {
                    mainBlock.OverflowAddress = newAddress;
                }

                helpBlock = new(overflowFile.BlockSize, overflowFile.BlockingFactor);
                mainBlock.OverflowBlocksCount++;
                currentHelpBlockAddress = newAddress;

                overflowBlocksAndAddresses.Add((helpBlock, newAddress));
                changedBlocksOverflowAddresses.Add(newAddress);
                
                index = overflowBlocksAndAddresses.Count - 1;
            }

            helpBlock.BlockData[helpBlock.ValidCount] = rec;
            helpBlock.IncrementValidCount();
            mainBlock.OverflowValidRecordsCount++;
            overflowValidCount++;

            if (!changedBlocksOverflowAddresses.Contains(currentHelpBlockAddress))
                changedBlocksOverflowAddresses.Add(currentHelpBlockAddress);      
        }


        var mainBlockAddress = GetBucketAddress(bucketToMerge);
        mainFile.WriteBlock(mainBlockAddress, mainBlock);

        foreach (var (block, address) in overflowBlocksAndAddresses)
        {
            if (changedBlocksOverflowAddresses.Contains(address))
                overflowFile.WriteBlock(address, block);
        }

        overflowFile.RemoveAllEmptyBlocksFromEnd();
        mainFile.RemoveLastBlock();
    }

    private void TryCompactBucket(int bucket)
    {
        var mainAddress = GetBucketAddress(bucket);
        var mainBlock = mainFile.ReadBlock(mainAddress);

        if (mainBlock.OverflowBlocksCount == 0)
            return;

        int totalRecordsInBucket = mainBlock.ValidCount + mainBlock.OverflowValidRecordsCount;

        //calculate how many blocks i need in overflow file
        int recordsNeedingOverflow = totalRecordsInBucket - mainBlock.BlockData.Length;
        int neededOverflowBlocks = (recordsNeedingOverflow > 0)
            ? (recordsNeedingOverflow + overflowFile.BlockingFactor - 1) / overflowFile.BlockingFactor
            : 0;

        if (mainBlock.OverflowBlocksCount <= neededOverflowBlocks)
            return;


        var (_, _, allOverflowBlocksAndAddresses) = ReadBucket(bucket);

        List<long> changedOverflowAddresses = new List<long>();

        var (blockToMoveDataFrom, addressToFree) = allOverflowBlocksAndAddresses.Last();
        allOverflowBlocksAndAddresses.RemoveAt(allOverflowBlocksAndAddresses.Count - 1);

        if (allOverflowBlocksAndAddresses.Count > 0)
        {
            var lastBlockInNewChain = allOverflowBlocksAndAddresses.Last();
            lastBlockInNewChain.block.OverflowAddress = -1;
            changedOverflowAddresses.Add(lastBlockInNewChain.address);
        }
        else
            mainBlock.OverflowAddress = -1;

        mainBlock.OverflowBlocksCount = allOverflowBlocksAndAddresses.Count;
        

        int index = 0;
        for (int i = 0; i < blockToMoveDataFrom.ValidCount; i++)
        {
            var rec = blockToMoveDataFrom.BlockData[i];

            if (mainBlock.ValidCount < mainBlock.BlockData.Length)
            {
                mainBlock.BlockData[mainBlock.ValidCount] = rec;
                mainBlock.IncrementValidCount();
                overflowValidCount--;
                mainValidCount++;
                continue;
            }

            while (true)
            {
                var helpBlockAddress = allOverflowBlocksAndAddresses[index].address;
                var helpBlock = allOverflowBlocksAndAddresses[index].block;
                if (helpBlock.ValidCount < helpBlock.BlockData.Length)
                {
                    helpBlock.BlockData[helpBlock.ValidCount] = rec;
                    helpBlock.IncrementValidCount();
                    if (!changedOverflowAddresses.Contains(helpBlockAddress))
                        changedOverflowAddresses.Add(helpBlockAddress);
                    break;
                }
                index++;
            }
        }

        blockToMoveDataFrom.ResetBlock();
        overflowFile.MarkBlockAsFree(addressToFree);

        mainBlock.OverflowValidRecordsCount = totalRecordsInBucket - mainBlock.ValidCount;

        mainFile.WriteBlock(mainAddress, mainBlock);
        foreach (var (block, address) in allOverflowBlocksAndAddresses)
        {
            if (changedOverflowAddresses.Contains(address))
                overflowFile.WriteBlock(address, block);
        }

        overflowFile.RemoveAllEmptyBlocksFromEnd();
    }

    public string GetPrintMainFile() => mainFile.GetAllBlocksAsString();

    public string GetPrintOverflowFile() => overflowFile.GetAllBlocksAsString();
}
