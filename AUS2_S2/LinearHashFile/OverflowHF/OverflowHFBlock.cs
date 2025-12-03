using AUS2_S2.DataClasses;
using AUS2_S2.HeapFile;
using AUS2_S2.HeapFile.Base;

namespace AUS2_S2.LinearHashFile.OverflowHF;

public class OverflowHFBlock<T> : Block<T> where T : IData<T>, new()
{
    public long OverflowAddress { get; set; }

    public OverflowHFBlock(int blockSize, int blockingFactor) : base(blockSize, blockingFactor)
    {
        OverflowAddress = -1;
    }

    protected override BaseBlock<T> CreateInstance(int blockSize, int blockingFactor) => new OverflowHFBlock<T>(blockSize, blockingFactor);

    protected override void LoadExtraBytes(byte[] bytes, ref int offset)
    {
        OverflowAddress = BitConverter.ToInt64(bytes, offset);
        offset += sizeof(long);
    }

    protected override void WriteExtraBytes(byte[] bytes, ref int offset)
    {
        BitConverter.GetBytes(OverflowAddress).CopyTo(bytes, offset);
        offset += sizeof(long);
    }

    public override int GetManageInfoSize()
    {
        return base.GetManageInfoSize() + sizeof(long);
    }

    public override void ResetBlock()
    {
        base.ResetBlock();
        OverflowAddress = -1;
    }
}
