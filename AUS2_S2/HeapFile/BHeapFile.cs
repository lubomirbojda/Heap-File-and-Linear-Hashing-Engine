namespace AUS2_S2.HeapFile;

public class BHeapFile<T> where T : IData<T>, new()
{
    private int BlockingFactor;
    private FileStream FileStream;
    public BHeapFile(int dataBlockSize, string filePath)
    {
        var dummyT = new T();
        int availableBytes = dataBlockSize - Block<T>.GetManageInfoSize();

        BlockingFactor = (availableBytes >= dummyT.GetSize()) ? (availableBytes / dummyT.GetSize()) : throw new InvalidOperationException("Block is not big enough)");

        if (File.Exists(filePath))
            FileStream = new(filePath, FileMode.Open, FileAccess.ReadWrite);
        else
            FileStream = new(filePath, FileMode.Create, FileAccess.ReadWrite);
    }

    public int Insert(T insertingData)
    {
        throw new NotImplementedException();
    }

    public T Get(int address, T dummy)
    {
        throw new NotImplementedException();
    }

    public bool Delete(int adress, T dummy)
    {
        throw new NotImplementedException();
    }

    private void Seek(int adress)
    {
        throw new NotImplementedException();
    }

    public bool SetNewFileSize(int newSize)
    {
        throw new NotImplementedException();
    }

    public void Close()
    {
        throw new NotImplementedException();
    }

}
