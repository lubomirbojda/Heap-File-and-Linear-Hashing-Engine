namespace AUS2_S2.HeapFile;

public class BHeapFile<T> where T : IData<T>, new()
{
    private int blockingFactor;
    private FileStream fileStream;
    private T actaulData;
    private int actualPosition;
    public BHeapFile(int dataBlockSize, string filePath)
    {
        actualPosition = 0;
        actaulData = new T();
        var dummyT = new T();
        int availableBytes = dataBlockSize - Block<T>.GetManageInfoSize();

        blockingFactor = (availableBytes >= dummyT.GetSize()) ? (availableBytes / dummyT.GetSize()) : throw new InvalidOperationException("Block is not big enough)");

        if (File.Exists(filePath))
            fileStream = new(filePath, FileMode.Open, FileAccess.ReadWrite);
        else
            fileStream = new(filePath, FileMode.Create, FileAccess.ReadWrite);
    }

    public (int adress, int index) Insert(T insertingData)
    {
        throw new NotImplementedException();
    }

    public T Get(int address, int index, T dummy)
    {
        Seek(address);
        byte[] bytes;;
        fileStream.Read(bytes, actualPosition, dummy.GetSize());
    }

    public bool Delete(int adress, T dummy)
    {
        throw new NotImplementedException();
    }

    private void Seek(int adress)
    {
        actualPosition = adress;
        fileStream.Seek(actualPosition, SeekOrigin.Begin);
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
