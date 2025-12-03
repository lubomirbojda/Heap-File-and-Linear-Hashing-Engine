using AUS2_S2.DataClasses;
using AUS2_S2.LinearHashFile.MainHF;
using AUS2_S2.LinearHashFile.OverflowHF;

namespace AUS2_S2.LinearHashFile;

public class LinHashFile<T> where T : IData<T>, new()
{
    private readonly MainHeapFile<T> mainFile;
    private readonly OverflowHeapFile<T> overflowFile;

    private readonly int baseBucketCount;
    private int level;
    private int splitPointer;

    private int validRecordsCountAll;

    public LinHashFile(int mainFileBlockSize, string mainFilePath, int overflowFileBlockSize, string overflowFilePath, int initialBucketCount)
    {
        mainFile = new(mainFileBlockSize, mainFilePath, mainFilePath + ".metadata");
        overflowFile = new(overflowFileBlockSize, overflowFilePath, overflowFilePath + ".metadata");

        baseBucketCount = initialBucketCount;
        level = 0;
        splitPointer = 0;
        validRecordsCountAll = 0;

        for (int i = 0; i < baseBucketCount; i++)
            mainFile.AllocateNewEmptyBlock();
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
            Split();
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
        long newAddr;

        if (overflowFile.fullyClearBlocks.Count > 0)
        {
            newAddr = overflowFile.fullyClearBlocks[0];
            overflowFile.fullyClearBlocks.RemoveAt(0);
        }
        else
        {
            newAddr = overflowFile.AllocateNewEmptyBlock();
        }

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
        mainFile.AllocateNewEmptyBlock();
        long splitAddress = GetBucketAddress(splitPointer);

        List<T> all = ReadAndClearBucket(splitAddress);

        foreach (var d in all)
        {
            int newIndex = Hash1(d.GetHashCode());
            InsertIntoBucket(newIndex, d);
        }

        splitPointer++;
        if (splitPointer == (baseBucketCount << level))
        {
            splitPointer = 0;
            level++;
        }
    }

    private List<T> ReadAndClearBucket(long address)
    {
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
            overflowFile.WriteBlock(overflowAddr, overflowBlock);

            overflowFile.MarkBlockAsFree(overflowAddr);

            overflowAddr = next;
        }

        overflowFile.RemoveAllEmptyBlocksFromEnd();

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

    public bool Delete(T dummy)
    {
        int bucket = Hash(dummy);
        var result = DeleteFromBucket(bucket, dummy);

        if (!result)
            return false;

        validRecordsCountAll--;
        while (ShouldMerge())
            Merge();

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

        var deleteBucketAddress = mainFile.GetBlockAddressByIndex(bucketToRemove);
        var list = ReadAndClearBucket(deleteBucketAddress);

        foreach (var record in list)
        {
            InsertIntoBucket(bucketToMerge, record);
        }

        mainFile.RemoveLastBlock();
    }
}
