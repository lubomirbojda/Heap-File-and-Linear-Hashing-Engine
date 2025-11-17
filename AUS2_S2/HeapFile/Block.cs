namespace AUS2_S2.HeapFile;

public class Block<T> where T : IData<T>, new()
{
    public T[] BlockData { get; private set; }
    public int ValidCount { get; private set; }

    private int blockSize;

    public Block(int blockSize, int blockingFactor)
    {
        this.blockSize = blockSize;
        BlockData = new T[blockingFactor];
        ValidCount = 0;
    }

    public int GetSize()
    {
        return blockSize;
    }

    public byte[] GetBytes()
    {
        var dummyT = new T();
        byte[] bytes  = new byte[blockSize];
        int offset = 0;
        var validCountB = BitConverter.GetBytes(ValidCount);
        foreach (var b in validCountB)
        {
            bytes[offset] = b;
            offset++;
        }
        foreach (var data in BlockData)
        {
            if (data != null)
            {
                foreach (var b in data.GetBytes())
                {
                    bytes[offset] = b;
                    offset++;
                }
            }
            else
            {
                offset += dummyT.GetSize();
            }
                
        }
        
        return bytes;
    }

    public Block<T> FromBytes(byte[] dataBytes)
    {
        var dummyT = new T();
        var bytesT = new byte[dummyT.GetSize()];
        var block = new Block<T>(blockSize, BlockData.Length);
        int offset = 0;
        block.ValidCount = BitConverter.ToInt32(dataBytes, offset);
        offset += sizeof(int);

        for (int i = 0; i < block.BlockData.Length; i++)
        {
            Array.Copy(dataBytes, offset, bytesT, 0, bytesT.Length);
            block.BlockData[i] = dummyT.FromBytes(bytesT);
            offset += dummyT.GetSize();
        }

        return block;
    }

    public static int GetManageInfoSize()
    {
        return sizeof(int);
    }

    public void IncrementValidCount()
    {
        ValidCount++;
    }

    public void DecrementValidCount()
    {
        ValidCount--;
    }
}
