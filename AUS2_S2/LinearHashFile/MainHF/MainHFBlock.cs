using AUS2_S2.DataClasses;
using AUS2_S2.HeapFile.Base;
using AUS2_S2.LinearHashFile.OverflowHF;

namespace AUS2_S2.LinearHashFile.MainHF;

public class MainHFBlock<T> : OverflowHFBlock<T> where T : IData<T>, new()
{
    public int OverflowBlocksCount { get; set; }
    public int OverflowValidRecordsCount { get; set; }
    public MainHFBlock(int blockSize, int blockingFactor) : base(blockSize, blockingFactor)
    {
        OverflowBlocksCount = 0;
        OverflowValidRecordsCount = 0;
    }

    protected override BaseBlock<T> CreateInstance(int blockSize, int blockingFactor) => new MainHFBlock<T>(blockSize, blockingFactor);

    protected override void LoadExtraBytes(byte[] bytes, ref int offset)
    {
        base.LoadExtraBytes(bytes, ref offset);

        OverflowBlocksCount = BitConverter.ToInt32(bytes, offset);
        offset += sizeof(int);

        OverflowValidRecordsCount = BitConverter.ToInt32(bytes, offset);
        offset += sizeof(int);

    }

    protected override void WriteExtraBytes(byte[] bytes, ref int offset)
    {
        base.WriteExtraBytes(bytes, ref offset);

        BitConverter.GetBytes(OverflowBlocksCount).CopyTo(bytes, offset);
        offset += sizeof(int);

        BitConverter.GetBytes(OverflowValidRecordsCount).CopyTo(bytes, offset);
        offset += sizeof(int);
    }

    public override int GetManageInfoSize()
    {
        return base.GetManageInfoSize() + 2 * sizeof(int);
    }

    public override void ResetBlock()
    {
        base.ResetBlock();
        OverflowBlocksCount = 0;
        OverflowValidRecordsCount = 0;
    }
}
