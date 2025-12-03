using AUS2_S2.DataClasses;
using AUS2_S2.HeapFile.Base;

namespace AUS2_S2.HeapFile;

public class BHeapFile<T> : BaseHeapFile<T, Block<T>> where T : IData<T>, new()
{
    public BHeapFile(int dataBlockSize, string filePath, string metadataFilePath) : base(dataBlockSize, filePath, metadataFilePath)
    {
    }

    public BHeapFile(string filePath, string metadataFilePath) : base(filePath, metadataFilePath)
    {
    }

    protected override Block<T> CreateBlock(int blockSize, int blockingFactor)
    {
        return new Block<T>(blockSize, blockingFactor);
    }
}
