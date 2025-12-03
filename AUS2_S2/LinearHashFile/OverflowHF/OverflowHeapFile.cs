using AUS2_S2.DataClasses;
using AUS2_S2.HeapFile.Base;

namespace AUS2_S2.LinearHashFile.OverflowHF;

public class OverflowHeapFile<T> : BaseHeapFile<T, OverflowHFBlock<T>> where T : IData<T>, new()
{
    public OverflowHeapFile(int dataBlockSize, string filePath, string metadataFilePath) : base(dataBlockSize, filePath, metadataFilePath)
    {
    }

    public OverflowHeapFile(string filePath, string metadataFilePath) : base(filePath, metadataFilePath)
    {
    }

    protected override OverflowHFBlock<T> CreateBlock(int blockSize, int blockingFactor)
    {
        return new OverflowHFBlock<T>(blockSize, blockingFactor);
    }
}
