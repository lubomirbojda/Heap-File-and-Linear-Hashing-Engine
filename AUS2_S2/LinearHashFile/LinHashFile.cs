using AUS2_S2.HeapFile;

namespace AUS2_S2.LinearHashFile;

public class LinHashFile<T> where T : IData<T>, new()
{
    private readonly BHeapFile<T> mainFile;
    private readonly BHeapFile<T> overflowFile;

    private readonly int baseBucketCount;
    private int level;
    private int splitPointer;

    private int validRecordsCountAll;
    private int overflowBlockCount;
    private int validRecordsCountMain;

    public LinHashFile(int mainFileBlockSize, string mainFilePath, int overflowFileBlockSize, string overflowFilePath, int initialBucketCount)
    {
        mainFile = new(mainFileBlockSize, mainFilePath, mainFilePath + ".metadata");
        overflowFile = new(overflowFileBlockSize, overflowFilePath, overflowFilePath + ".metadata");

        baseBucketCount = initialBucketCount;
        level = 0;
        splitPointer = 0;
        validRecordsCountAll = 0;
        overflowBlockCount = 0;
        validRecordsCountMain = 0;

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
        long addr = GetBucketAddress(bucket);

        Block<T> block = mainFile.ReadBlock(addr);

        if (block.ValidCount < block.BlockData.Length)
        {
            //insert into block in main file
            block.BlockData[block.ValidCount] = data;
            block.IncrementValidCount();
            mainFile.WriteBlock(addr, block);
            validRecordsCountMain++;
            return;
        }

        //continue to overflow file
        long prevAddr = -1;
        long currentAddr = block.OverflowAddress;

        while (currentAddr >= 0)
        {
            Block<T> o = overflowFile.ReadBlock(currentAddr);

            if (o.ValidCount < o.BlockData.Length)
            {
                //insert into existing block in overflow file
                o.BlockData[o.ValidCount] = data;
                o.IncrementValidCount();
                overflowFile.WriteBlock(currentAddr, o);
                return;
            }

            prevAddr = currentAddr;
            currentAddr = o.OverflowAddress;
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
        overflowBlockCount++;

        Block<T> newBlock = new Block<T>(overflowFile.BlockSize, overflowFile.BlockingFactor);
        newBlock.BlockData[0] = data;
        newBlock.IncrementValidCount();
        overflowFile.WriteBlock(newAddr, newBlock);

        //set overflow address to correct block
        if (prevAddr == -1)
        {
            block.OverflowAddress = newAddr;
            mainFile.WriteBlock(addr, block);
        }
        else
        {
            Block<T> prev = overflowFile.ReadBlock(prevAddr);
            prev.OverflowAddress = newAddr;
            overflowFile.WriteBlock(prevAddr, prev);
        }
    }

    private void Split()
    {
        int newBucketIndex = (baseBucketCount << level) + splitPointer;

        mainFile.AllocateNewEmptyBlock();
        long splitAddr = GetBucketAddress(splitPointer);

        List<T> all = ReadAndClearBucket(splitAddr);

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
        List<T> list = new();

        Block<T> block = mainFile.ReadBlock(address);

        for (int i = 0; i < block.ValidCount; i++)
            list.Add(block.BlockData[i]);

        validRecordsCountMain -= block.ValidCount;
        long overflowAddr = block.OverflowAddress;

        block.ResetBlock();
        mainFile.WriteBlock(address, block);

        while (overflowAddr >= 0)
        {
            Block<T> o = overflowFile.ReadBlock(overflowAddr);

            for (int i = 0; i < o.ValidCount; i++)
                list.Add(o.BlockData[i]);

            long next = o.OverflowAddress;

            o.ResetBlock();
            overflowFile.WriteBlock(overflowAddr, o);

            overflowFile.MarkBlockAsFree(overflowAddr);
            overflowBlockCount--;

            overflowAddr = next;
        }

        overflowFile.RemoveAllEmptyBlocksFromEnd();

        return list;
    }

    public double GetFactorTest()
    {
        long addr = GetBucketAddress(splitPointer);
        Block<T> bucket = mainFile.ReadBlock(addr);

        int totalRecordsInBucket = bucket.ValidCount;
        long overflowAddr = bucket.OverflowAddress;

        while (overflowAddr >= 0)
        {
            Block<T> ob = overflowFile.ReadBlock(overflowAddr);
            totalRecordsInBucket += ob.ValidCount;
            overflowAddr = ob.OverflowAddress;
        }

        Console.WriteLine("total rec in bucket: " + totalRecordsInBucket);
        Console.WriteLine("mainFile.BlockingFactor: " + mainFile.BlockingFactor);
        double localLoad = (double)totalRecordsInBucket / mainFile.BlockingFactor;

        return localLoad;
    }

    private bool ShouldSplit()
    {
        long addr = GetBucketAddress(splitPointer);
        Block<T> bucket = mainFile.ReadBlock(addr);

        int totalRecordsInBucket = bucket.ValidCount;
        long overflowAddr = bucket.OverflowAddress;

        while (overflowAddr >= 0)
        {
            Block<T> ob = overflowFile.ReadBlock(overflowAddr);
            totalRecordsInBucket += ob.ValidCount;
            overflowAddr = ob.OverflowAddress;
        }

        double localLoad = (double)totalRecordsInBucket / mainFile.BlockingFactor;

        return localLoad > 0.8;
    }

    public T? Get(T dummy)
    {
        int bucket = Hash(dummy);
        long addr = GetBucketAddress(bucket);

        //check main file
        Block<T> main = mainFile.ReadBlock(addr);
        for (int i = 0; i < main.ValidCount; i++)
        {
            if (main.BlockData[i].Equals(dummy))
                return main.BlockData[i];
        }

        //chcek overflow file
        long overflowAddr = main.OverflowAddress;
        while (overflowAddr >= 0)
        {
            Block<T> ob = overflowFile.ReadBlock(overflowAddr);
            for (int i = 0; i < ob.ValidCount; i++)
            {
                if (ob.BlockData[i].Equals(dummy))
                    return ob.BlockData[i];
            }
            overflowAddr = ob.OverflowAddress;
        }

        return default;
    }
}
