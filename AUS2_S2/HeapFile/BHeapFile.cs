namespace AUS2_S2.HeapFile;

public class BHeapFile<T> where T : IData<T>, new()
{
    private int blockingFactor;
    private int blockSize;
    private int lastBlockAdress;
    private FileStream fileStream;
    private Block<T> actualBlock;
    private T dummyT;
    private List<int> partlyClearBlocks = [];
    private List<int> fullyClearBlocks = [];
    
    //private T actaulData;
    public BHeapFile(int dataBlockSize, string filePath)
    {
        //actaulData = new T();
        dummyT = new T();
        blockSize = dataBlockSize;
        int availableBytes = dataBlockSize - Block<T>.GetManageInfoSize();
        blockingFactor = (availableBytes >= dummyT.GetSize()) ? (availableBytes / dummyT.GetSize()) : throw new InvalidOperationException("Block is not big enough)");
        actualBlock = new Block<T>(blockSize, blockingFactor);
        lastBlockAdress = 0;

        if (File.Exists(filePath))
            fileStream = new(filePath, FileMode.Open, FileAccess.ReadWrite);
        else
            fileStream = new(filePath, FileMode.Create, FileAccess.ReadWrite);
    }

    public int Insert(T insertingData)
    {
        int returningPos;
        if (partlyClearBlocks.Count > 0)
        {
            Seek(partlyClearBlocks[0]);
            returningPos = partlyClearBlocks[0];
            ChangeActualBlockByActualPosition();
            actualBlock.BlockData[actualBlock.ValidCount] = insertingData;
            actualBlock.IncrementValidCount();
            //WriteWithOffset(insertingData.GetBytes(), insertingData.GetSize() * actualBlock.ValidCount + Block<T>.GetManageInfoSize(), insertingData.GetSize());
            fileStream.Write(actualBlock.GetBytes(), 0, actualBlock.GetSize());
            fileStream.Flush();
            if (actualBlock.ValidCount == blockingFactor)
                partlyClearBlocks.RemoveAt(0);
            return returningPos;
        }

        if (fullyClearBlocks.Count > 0)
        {
            Seek(fullyClearBlocks[0]);
            returningPos = fullyClearBlocks[0];
            ChangeActualBlockByActualPosition();
            actualBlock.BlockData[actualBlock.ValidCount] = insertingData;
            actualBlock.IncrementValidCount();
            //WriteWithOffset(insertingData.GetBytes(), insertingData.GetSize() * actualBlock.ValidCount + Block<T>.GetManageInfoSize(), insertingData.GetSize());
            fileStream.Write(actualBlock.GetBytes(), 0, actualBlock.GetSize());
            fileStream.Flush();
            if (actualBlock.ValidCount == blockingFactor)
                fullyClearBlocks.RemoveAt(0);
            return returningPos;
        }

        fileStream.Seek(0, SeekOrigin.End);
        lastBlockAdress = (int)fileStream.Position;
        returningPos = (int)fileStream.Position;
        var newBlock = new Block<T>(blockSize, blockingFactor);
        newBlock.BlockData[0] = insertingData;
        newBlock.IncrementValidCount();
        fileStream.Write(newBlock.GetBytes(), 0, newBlock.GetSize());
        fileStream.Flush();
        partlyClearBlocks.Add(lastBlockAdress);
        return returningPos;
    }

    public T? Get(int address, T dummy)
    {
        Seek(address);
        ChangeActualBlockByActualPosition();
        var p = actualBlock.BlockData.FirstOrDefault(x => x.Equals(dummy));
        return p;
    }

    public bool Delete(int adress, T dummy)
    {
        throw new NotImplementedException();
    }

    private void Seek(int adress)
    {
        //*blocksize?
        fileStream.Seek(adress, SeekOrigin.Begin);
    }

    public bool SetNewFileSize(int newSize)
    {
        throw new NotImplementedException();
    }

    public void Close()
    {
        fileStream.Close();
        //throw new NotImplementedException();
    }

    private void ChangeActualBlockByActualPosition()
    {
        var bytes = new byte[blockSize];
        var positionSave = fileStream.Position;
        fileStream?.Read(bytes, 0, actualBlock.GetSize());
        actualBlock = actualBlock.FromBytes(bytes);
        fileStream?.Seek(positionSave, SeekOrigin.Begin);
    }

    private void WriteWithOffset(byte[] bytes, int offset, int count)
    {
        var positionSave = fileStream.Position;
        fileStream.Position += offset;
        fileStream.Write(bytes, 0, count);
        fileStream.Flush();
        fileStream.Seek(positionSave, SeekOrigin.Begin);
    }
}
