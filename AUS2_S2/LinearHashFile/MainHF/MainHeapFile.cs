using AUS2_S2.DataClasses;
using AUS2_S2.HeapFile.Base;

namespace AUS2_S2.LinearHashFile.MainHF;

public class MainHeapFile<T> : BaseHeapFile<T, MainHFBlock<T>> where T : IData<T>, new()
{
    public MainHeapFile(int dataBlockSize, string filePath, string metadataFilePath) : base(dataBlockSize, filePath, metadataFilePath)
    {
    }

    public MainHeapFile(string filePath, string metadataFilePath) : base(filePath, metadataFilePath)
    {
    }

    protected override MainHFBlock<T> CreateBlock(int blockSize, int blockingFactor)
    {
        return new(blockSize, blockingFactor);
    }

}
