namespace AUS2_S2.HeapFile;

public class Block<T> where T : IData<T>, new()
{
    public T[] BlockData { get; private set; }
    public int ValidCount { get; private set; }

    public int BlockSize { get; private set; }
    public long OverflowAddress { get; set; } = -1;

    public Block(int blockSize, int blockingFactor)
    {
        BlockSize = blockSize;
        BlockData = new T[blockingFactor];
        ValidCount = 0;
    }

    public int GetSize()
    {
        return BlockSize;
    }

    public byte[] GetBytes()
    {
        var dummyT = new T();
        byte[] bytes  = new byte[BlockSize];
        int offset = 0;

        BitConverter.GetBytes(ValidCount).CopyTo(bytes, offset);
        offset += sizeof(int);

        BitConverter.GetBytes(OverflowAddress).CopyTo(bytes, offset);
        offset += sizeof(long);

        foreach (var data in BlockData)
        {
            byte[] recordBytes = data != null ? data.GetBytes() : new byte[dummyT.GetSize()];
            recordBytes.CopyTo(bytes, offset);
            offset += recordBytes.Length;
        }

        return bytes;
    }

    public Block<T> FromBytes(byte[] dataBytes)
    {
        var dummyT = new T();
        var bytesT = new byte[dummyT.GetSize()];
        var block = new Block<T>(BlockSize, BlockData.Length);
        int offset = 0;

        block.ValidCount = BitConverter.ToInt32(dataBytes, offset);
        offset += sizeof(int);

        block.OverflowAddress = BitConverter.ToInt64(dataBytes, offset);
        offset += sizeof(long);

        for (int i = 0; i < block.BlockData.Length; i++)
        {
            Array.Copy(dataBytes, offset, bytesT, 0, bytesT.Length);
            block.BlockData[i] = dummyT.FromBytes(bytesT);
            offset += dummyT.GetSize();
        }

        return block;
    }

    public void ResetBlock()
    {
        ValidCount = 0;
        OverflowAddress = -1;
    }

    public static int GetManageInfoSize()
    {
        return sizeof(int) + sizeof(long);
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
