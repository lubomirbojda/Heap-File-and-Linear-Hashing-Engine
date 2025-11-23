using System.Text;

namespace AUS2_S2.HeapFile;

public class BHeapFile<T> where T : IData<T>, new()
{
    private int blockingFactor;
    private int blockSize;
    private long lastBlockAdress;
    private FileStream fileStream;
    private Block<T> actualBlock;
    private T dummyT;
    private List<long> partlyClearBlocks = [];
    private List<long> fullyClearBlocks = [];

    public BHeapFile(int dataBlockSize, string filePath)
    {
        dummyT = new T();
        blockSize = dataBlockSize;
        int availableBytes = dataBlockSize - Block<T>.GetManageInfoSize();
        blockingFactor = (availableBytes >= dummyT.GetSize()) ? (availableBytes / dummyT.GetSize()) : throw new InvalidOperationException("Block is not big enough)");
        actualBlock = new Block<T>(blockSize, blockingFactor);
        lastBlockAdress = 0;

        if (File.Exists(filePath))
            fileStream = new(filePath, FileMode.Open, FileAccess.ReadWrite);
        else
            fileStream = new(filePath, FileMode.Create, FileAccess.ReadWrite);
    }

    public long Insert(T insertingData)
    {
        long returningAddress;
        if (partlyClearBlocks.Count > 0)
        {
            return InsertDataToFirstBlockFromAdressList(partlyClearBlocks, insertingData);
        }

        if (fullyClearBlocks.Count > 0)
        {
            returningAddress = InsertDataToFirstBlockFromAdressList(fullyClearBlocks, insertingData);
            fullyClearBlocks.Remove(returningAddress);
            partlyClearBlocks.Add(returningAddress);
            return returningAddress;
        }

        fileStream.Seek(0, SeekOrigin.End);
        lastBlockAdress = fileStream.Position;
        returningAddress = fileStream.Position;
        var newBlock = new Block<T>(blockSize, blockingFactor);
        newBlock.BlockData[0] = insertingData;
        newBlock.IncrementValidCount();
        fileStream.Write(newBlock.GetBytes(), 0, newBlock.GetSize());
        fileStream.Flush();
        if (blockingFactor > 1)
            partlyClearBlocks.Add(lastBlockAdress);
        return returningAddress;
    }

    private long InsertDataToFirstBlockFromAdressList(List<long> listOfBlocksAdresses, T data)
    {
        long returningPos = listOfBlocksAdresses[0];
        Seek(returningPos);
        ChangeActualBlockByActualPosition();
        actualBlock.BlockData[actualBlock.ValidCount] = data;
        actualBlock.IncrementValidCount();
        fileStream.Write(actualBlock.GetBytes(), 0, blockSize);
        fileStream.Flush();
        if (actualBlock.ValidCount == blockingFactor)
            listOfBlocksAdresses.RemoveAt(0);
        return returningPos;
    }

    public T? Get(long address, T dummy)
    {
        Seek(address);
        ChangeActualBlockByActualPosition();
        var p = actualBlock.BlockData.Take(actualBlock.ValidCount).FirstOrDefault(x => x.Equals(dummy));
        return p;
    }

    public bool Delete(long address, T dummy)
    {
        Seek(address);
        ChangeActualBlockByActualPosition();
        var dataIndex = Array.FindIndex(actualBlock.BlockData.Take(actualBlock.ValidCount).ToArray(), x => x.Equals(dummy));
        if (dataIndex == -1 || dataIndex > actualBlock.ValidCount)
            return false;

        for (int i = dataIndex + 1; i < actualBlock.ValidCount; i++)
        {
            actualBlock.BlockData[i - 1] = actualBlock.BlockData[i];
        }
        actualBlock.DecrementValidCount();

        if (partlyClearBlocks.Contains(address))
        {
            if (actualBlock.ValidCount == 0)
            {
                partlyClearBlocks.Remove(address);
                if (address == lastBlockAdress)
                {
                    var adressForNewFileSize = address;
                    var newAddress = address - blockSize;
                    while (newAddress >= 0)
                    {
                        Seek(newAddress);
                        ChangeActualBlockByActualPosition();

                        if (actualBlock.ValidCount != 0)
                        {
                            break;
                        }

                        adressForNewFileSize = newAddress;
                        fullyClearBlocks.Remove(newAddress);
                        newAddress -= blockSize;
                    }
                    SetNewFileSize(adressForNewFileSize);
                    return true;
                }
                else
                {
                    fullyClearBlocks.Add(address);
                }
            }
        }
        else
        {
            partlyClearBlocks.Add(address);
        }

        fileStream.Write(actualBlock.GetBytes(), 0, blockSize);
        fileStream.Flush();

        return true;
        //throw new NotImplementedException();
    }

    private void Seek(long adress)
    {
        fileStream.Seek(adress, SeekOrigin.Begin);
    }

    private void SetNewFileSize(long newEndAdress)
    {
        if (fileStream.Seek(0, SeekOrigin.End) > newEndAdress)
        {
            fileStream.SetLength(newEndAdress);
        }
    }

    public void Close()
    {
        fileStream.Close();
        //throw new NotImplementedException();
    }

    private void ChangeActualBlockByActualPosition()
    {
        var bytes = new byte[blockSize];
        var positionSave = fileStream.Position;
        fileStream?.Read(bytes, 0, actualBlock.GetSize());
        actualBlock = actualBlock.FromBytes(bytes);
        fileStream?.Seek(positionSave, SeekOrigin.Begin);
    }

    public (string allBlocks, string partlyClearBlocks, string fullyClearBlocks) GetAllBlocksAsString()
    {
        var sbAllBlocks = new StringBuilder();
        long actualAdress = 0;
        long blockNumber = 0;
        while (actualAdress < fileStream.Length)
        {
            Seek(actualAdress);
            ChangeActualBlockByActualPosition();
            blockNumber = actualAdress / blockSize;
            sbAllBlocks.AppendLine("-----BLOCK " + blockNumber + " -----");
            sbAllBlocks.AppendLine("Adress: " + actualAdress);
            sbAllBlocks.AppendLine("Valid count: " + actualBlock.ValidCount);

            if (actualBlock.ValidCount == 0)
            {
                sbAllBlocks.AppendLine("EMPTY BLOCK");
            }
            else
            {
                for (int i = 0; i < actualBlock.ValidCount; i++)
                    sbAllBlocks.AppendLine((i + 1) + ") " + actualBlock.BlockData[i].GetPrintString());
            }

            sbAllBlocks.AppendLine("---------------------------------------");
            actualAdress += blockSize;
        }

        var sbPCB = new StringBuilder().AppendLine("Partly clear blocks adresses:");
        var sbFCB = new StringBuilder().AppendLine("Fully clear blocks adresses:");
        foreach (var adress in partlyClearBlocks)
            sbPCB.AppendLine(adress.ToString());
        foreach (var adress in fullyClearBlocks)
            sbFCB.AppendLine(adress.ToString());

        return (sbAllBlocks.ToString(), sbPCB.ToString(), sbFCB.ToString());
    }
}
