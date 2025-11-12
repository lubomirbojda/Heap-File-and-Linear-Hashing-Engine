namespace AUS2_S2.HeapFile;

public class Block<T>
{
    public T[] BlockData { get; private set; }
    public int ValidCount { get; private set; }

    public Block()
    {
        
    }

    public int GetSize()
    {
        throw new NotImplementedException();
    }

    public byte[] GetBytes()
    {
        throw new NotImplementedException();
    }

    public T FromBytes(byte[] dataBytes)
    {
        throw new NotImplementedException();
    }

    public static int GetManageInfoSize()
    {
        return sizeof(int);
    }
}
