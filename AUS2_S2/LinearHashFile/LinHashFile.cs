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

    private int validRecordsCountAll;

    public int mergersCount = 0;
    public int splitsCOunt = 0;

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
        validRecordsCountAll = 0;

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
        validRecordsCountAll = br.ReadInt32();
    }

    private void SaveMetadata()
    {
        metadataStream.Position = 0;
        metadataStream.SetLength(0);
        using var bw = new BinaryWriter(metadataStream, Encoding.Default, leaveOpen: true);
        bw.Write(baseBucketCount);
        bw.Write(level);
        bw.Write(splitPointer);
        bw.Write(validRecordsCountAll);
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
        validRecordsCountAll++;

        while (ShouldSplit())
        {
            Split();
            splitsCOunt++;
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
            return;
        }

        //continue to overflow file
        long prevAddress = -1;
        long currentAddress = mainBlock.OverflowAddress;

        while (currentAddress >= 0)
        {
            var overflowBlock = overflowFile.ReadBlock(currentAddress);

            if (overflowBlock.ValidCount < overflowBlock.BlockData.Length)
            {
                //insert into existing block in overflow file
                overflowBlock.BlockData[overflowBlock.ValidCount] = data;
                overflowBlock.IncrementValidCount();
                overflowFile.WriteBlock(currentAddress, overflowBlock);
                mainBlock.OverflowValidRecordsCount++;
                mainFile.WriteBlock(address, mainBlock);
                return;
            }

            prevAddress = currentAddress;
            currentAddress = overflowBlock.OverflowAddress;
        }

        //have to create new block in overflow file and insert there
        long newAddr = overflowFile.GetNextClearBlockAddress();

        var newBlock = new OverflowHFBlock<T>(overflowFile.BlockSize, overflowFile.BlockingFactor);
        newBlock.BlockData[0] = data;
        newBlock.IncrementValidCount();
        overflowFile.WriteBlock(newAddr, newBlock);

        //set overflow address to correct block
        if (prevAddress == -1)
        {
            mainBlock.OverflowAddress = newAddr;      
        }
        else
        {
            OverflowHFBlock<T> prev = overflowFile.ReadBlock(prevAddress);
            prev.OverflowAddress = newAddr;
            overflowFile.WriteBlock(prevAddress, prev);
        }

        mainBlock.OverflowValidRecordsCount++;
        mainBlock.OverflowBlocksCount++;
        mainFile.WriteBlock(address, mainBlock);
    }

    private void Split()
    {
        var (records, mainBlock, overflowBlocksAndAddresses) = ReadBucket(splitPointer);

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
                continue;
            }

            if (helpBlock == null || helpBlock.ValidCount == helpBlock.BlockData.Length)
            {
                var prevBlock = helpBlock;
                (helpBlock, var address) = overflowBlocksAndAddresses[index];
                if (index == 0)
                    mainBlock.OverflowAddress = address;
                else
                {
                    if (prevBlock != null)
                        prevBlock.OverflowAddress = address;
                }
                index++;
            }

            helpBlock.BlockData[helpBlock.ValidCount] = rec;
            helpBlock.IncrementValidCount();
            mainBlock.OverflowValidRecordsCount++;
        }


        mainBlock.OverflowBlocksCount = (mainBlock.OverflowValidRecordsCount + overflowFile.BlockingFactor - 1) / overflowFile.BlockingFactor;

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
                continue;
            }

            if (actualOverflowBlock == null || actualOverflowBlock.ValidCount == actualOverflowBlock.BlockData.Length)
            {
                actualOverflowBlock = new(overflowFile.BlockSize, overflowFile.BlockingFactor);
                var newAddress = overflowFile.GetNextClearBlockAddress();
                newOverflowBlocksAndAddresses.Add((actualOverflowBlock, newAddress));
                if (newOverflowBlocksAndAddresses.Count == 1)
                    newMainBlock.OverflowAddress = newAddress;
                else
                    actualOverflowBlock.OverflowAddress = newAddress;
            }

            actualOverflowBlock.BlockData[actualOverflowBlock.ValidCount] = rec;
            actualOverflowBlock.IncrementValidCount();
            newMainBlock.OverflowValidRecordsCount++;
        }

        newMainBlock.OverflowBlocksCount = (newMainBlock.OverflowValidRecordsCount + overflowFile.BlockingFactor - 1) / overflowFile.BlockingFactor;

        overflowFile.RemoveAllEmptyBlocksFromEnd();

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

    private double GetFillFactor()
    {
        int mainBucketsCount = (baseBucketCount << level) + splitPointer;
        int mainBlocksSpace = mainBucketsCount * mainFile.BlockingFactor;
        return (double)validRecordsCountAll / mainBlocksSpace;
    }

    private bool ShouldSplit()
    {
        return GetFillFactor() > 0.75;
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

        validRecordsCountAll--;
        while (ShouldMerge())
        {
            Merge();
            mergersCount++;
        }
            

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
                            mainBlock.OverflowBlocksCount--;
                            //mainFile.WriteBlock(address, mainBlock);
                            overflowFile.WriteBlock(prevAddress, prev);
                        }

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


    private bool ShouldMerge()
    {
        if (level <= 0)
            return false;
        return GetFillFactor() < 0.6;
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

        //prepare all blocks to write
        var (_, mainBlock, overflowBlocksAndAddresses) = ReadBucket(bucketToMerge);
        List<(OverflowHFBlock<T> block, long address)> newOverflowBlocksAndAddresses = [];

        OverflowHFBlock<T>? helpBlock = null;
        int index = -1;
        foreach (var rec in listMergingRecords)
        {
            if (mainBlock.ValidCount < mainBlock.BlockData.Length)
            {
                mainBlock.BlockData[mainBlock.ValidCount] = rec;
                mainBlock.IncrementValidCount();
                continue;
            }

            if (helpBlock == null || helpBlock.ValidCount == helpBlock.BlockData.Length)
            {
                while (true)
                {
                    if (++index < overflowBlocksAndAddresses.Count)
                    {
                        (helpBlock, var address) = overflowBlocksAndAddresses[index];
                        if (helpBlock.ValidCount < helpBlock.BlockData.Length)
                        {
                            newOverflowBlocksAndAddresses.Add((helpBlock, address));
                            break;
                        }
                    }
                    else
                    {
                        var address = overflowFile.GetNextClearBlockAddress();
                        if (helpBlock == null)
                            mainBlock.OverflowAddress = address;
                        else
                            helpBlock.OverflowAddress = address;

                        helpBlock = new(overflowFile.BlockSize, overflowFile.BlockingFactor);
                        newOverflowBlocksAndAddresses.Add((helpBlock, address));
                        break;
                    }
                } 
            }

            helpBlock.BlockData[helpBlock.ValidCount] = rec;
            helpBlock.IncrementValidCount();
            mainBlock.OverflowValidRecordsCount++;
        }

        mainBlock.OverflowBlocksCount = (mainBlock.OverflowValidRecordsCount + overflowFile.BlockingFactor - 1) / overflowFile.BlockingFactor;

        //write all blocks
        var mainBlockAddress = GetBucketAddress(bucketToMerge);
        mainFile.WriteBlock(mainBlockAddress, mainBlock);
        foreach (var (block, address) in newOverflowBlocksAndAddresses)
            overflowFile.WriteBlock(address, block);


        overflowFile.RemoveAllEmptyBlocksFromEnd();
        mainFile.RemoveLastBlock();
    }

    public string GetPrintMainFile() => mainFile.GetAllBlocksAsString();

    public string GetPrintOverflowFile() => overflowFile.GetAllBlocksAsString();
}
