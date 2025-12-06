using AUS2_S2.DataClasses;

namespace AUS2_S2.HeapFile.Base;

public abstract class BaseBlock<T> where T : IData<T>, new()
{
    public T[] BlockData { get; protected set; }
    public int ValidCount { get; protected set; }

    public int BlockSize { get; protected set; }
    //public long OverflowAddress { get; set; } = -1;

    protected BaseBlock(int blockSize, int blockingFactor)
    {
        BlockSize = blockSize;
        BlockData = new T[blockingFactor];
        ValidCount = 0;
    }

    protected abstract BaseBlock<T> CreateInstance(int blockSize, int blockingFactor);

    protected virtual void LoadExtraBytes(byte[] data, ref int offset)
    {
    }

    protected virtual void WriteExtraBytes(byte[] data, ref int offset)
    {
    }

    public int GetSize()
    {
        return BlockSize;
    }

    public byte[] GetBytes()
    {
        var dummy = new T();
        int recordSize = dummy.GetSize();

        byte[] bytes = new byte[BlockSize];
        int offset = 0;

        BitConverter.GetBytes(ValidCount).CopyTo(bytes, offset);
        offset += sizeof(int);

        WriteExtraBytes(bytes, ref offset);

        foreach (var record in BlockData)
        {
            byte[] recBytes = record != null
                ? record.GetBytes()
                : new byte[recordSize];

            recBytes.CopyTo(bytes, offset);
            offset += recBytes.Length;
        }

        return bytes;

    }

    public BaseBlock<T> FromBytes(byte[] dataBytes)
    {
        int offset = 0;
        var dummy = new T();
        int recordSize = dummy.GetSize();

        var block = CreateInstance(BlockSize, BlockData.Length);

        block.ValidCount = BitConverter.ToInt32(dataBytes, offset);
        offset += sizeof(int);

        block.LoadExtraBytes(dataBytes, ref offset);

        for (int i = 0; i < block.BlockData.Length; i++)
        {
            byte[] rec = new byte[recordSize];
            Array.Copy(dataBytes, offset, rec, 0, recordSize);
            offset += recordSize;

            block.BlockData[i] = dummy.FromBytes(rec);
        }

        return block;
    }

    public abstract void ResetBlock();

    public abstract int GetManageInfoSize();

    public void IncrementValidCount()
    {
        ValidCount++;
    }

    public void DecrementValidCount()
    {
        ValidCount--;
    }
}
