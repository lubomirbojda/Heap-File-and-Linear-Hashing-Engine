using AUS2_S2.DataClasses;
using AUS2_S2.HeapFile.Base;

namespace AUS2_S2.HeapFile;

public class Block<T> : BaseBlock<T> where T : IData<T>, new()
{
    public Block(int blockSize, int blockingFactor) : base(blockSize, blockingFactor)
    {
    }

    public override int GetManageInfoSize()
    {
        return sizeof(int);
    }

    public override void ResetBlock()
    {
        ValidCount = 0;
    }

    protected override BaseBlock<T> CreateInstance(int blockSize, int blockingFactor)
    {
        return new Block<T>(blockSize, blockingFactor);
    }
}
