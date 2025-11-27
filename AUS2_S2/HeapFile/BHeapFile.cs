using System.Text;

namespace AUS2_S2.HeapFile;

public class BHeapFile<T> where T : IData<T>, new()
{
    private int blockingFactor;
    private int blockSize;
    private long lastBlockAdress;
    private readonly FileStream fileStream;
    private Block<T> actualBlock;
    private readonly T dummyT = new();
    private readonly List<long> partlyClearBlocks = [];
    private readonly List<long> fullyClearBlocks = [];
    private readonly FileStream metadataStream;

    public BHeapFile(int dataBlockSize, string filePath, string metadataFilePath)
    {
        blockSize = dataBlockSize;
        int availableBytes = dataBlockSize - Block<T>.GetManageInfoSize();
        blockingFactor = (availableBytes >= dummyT.GetSize()) ? (availableBytes / dummyT.GetSize()) : throw new InvalidOperationException("Block is not big enough)");
        actualBlock = new Block<T>(blockSize, blockingFactor);
        lastBlockAdress = 0;

        if (File.Exists(filePath))
            File.Delete(filePath);

        fileStream = new(filePath, FileMode.Create, FileAccess.ReadWrite);

        if (File.Exists(metadataFilePath))
            File.Delete(metadataFilePath);

        metadataStream = new(metadataFilePath, FileMode.Create, FileAccess.ReadWrite);
    }

    public BHeapFile(string filePath, string metadataFilePath)
    {
        fileStream = new(filePath, FileMode.Open, FileAccess.ReadWrite);
        metadataStream = new(metadataFilePath, FileMode.Open, FileAccess.ReadWrite);
        LoadMetadata();
        actualBlock = new Block<T>(blockSize, blockingFactor);
    }

    private void LoadMetadata()
    {
        using var br = new BinaryReader(metadataStream);
        lastBlockAdress = br.ReadInt64();
        blockingFactor = br.ReadInt32();
        blockSize = br.ReadInt32();
        int partlyClearCount = br.ReadInt32();
        partlyClearBlocks.Clear();
        for (int i = 0; i < partlyClearCount; i++)
            partlyClearBlocks.Add(br.ReadInt64());
        fullyClearBlocks.Clear();
        int fullyClearCount = br.ReadInt32();
        for (int i = 0; i < fullyClearCount; i++)
            fullyClearBlocks.Add(br.ReadInt64());
    }

    private void SaveMetadata()
    {
        using var bw = new BinaryWriter(metadataStream);
        //long
        bw.Write(lastBlockAdress);
        //int
        bw.Write(blockingFactor);
        //int
        bw.Write(blockSize);
        //int
        bw.Write(partlyClearBlocks.Count);
        //long
        foreach (var block in partlyClearBlocks)
            bw.Write(block);
        //int
        bw.Write(fullyClearBlocks.Count);
        //long
        foreach (var block in fullyClearBlocks)
            bw.Write(block);
    }

    public long Insert(T insertingData)
    {
        if (partlyClearBlocks.Count > 0)
        {
            return InsertDataToFirstBlockFromAdressList(partlyClearBlocks, insertingData);
        }

        if (fullyClearBlocks.Count > 0)
        {
            long returningAddress = InsertDataToFirstBlockFromAdressList(fullyClearBlocks, insertingData);
            if (actualBlock.ValidCount < blockingFactor)
                partlyClearBlocks.Add(returningAddress);
            fullyClearBlocks.Remove(returningAddress);
            return returningAddress;
        }

        fileStream.Seek(0, SeekOrigin.End);
        lastBlockAdress = fileStream.Position;
        var newBlock = new Block<T>(blockSize, blockingFactor);
        newBlock.BlockData[0] = insertingData;
        newBlock.IncrementValidCount();
        fileStream.Write(newBlock.GetBytes(), 0, newBlock.GetSize());
        fileStream.Flush();
        if (blockingFactor > 1)
            partlyClearBlocks.Add(lastBlockAdress);
        return lastBlockAdress;
    }

    private long InsertDataToFirstBlockFromAdressList(List<long> listOfBlocksAdresses, T data)
    {
        listOfBlocksAdresses.Sort();
        long returningPos = listOfBlocksAdresses[0];
        Seek(returningPos);
        ChangeActualBlockByActualPosition();
        Seek(returningPos);
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
                    RemoveAllEmptyBlocksFromEnd();
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
            if (actualBlock.ValidCount == 0)
            {
                if (address == lastBlockAdress)
                {
                    RemoveAllEmptyBlocksFromEnd();
                    return true;
                }
                else
                {
                    fullyClearBlocks.Add(address);
                }   
            }              
            else
            {
                partlyClearBlocks.Add(address);
            }     
        }

        Seek(address);
        fileStream.Write(actualBlock.GetBytes(), 0, blockSize);
        fileStream.Flush();

        return true;
    }

    private void RemoveAllEmptyBlocksFromEnd()
    {
        var adressForNewFileSize = lastBlockAdress;
        var newAddress = adressForNewFileSize - blockSize;
        while (newAddress >= 0)
        {
            if (!fullyClearBlocks.Contains(newAddress))
            {
                lastBlockAdress = newAddress;
                break;
            }

            adressForNewFileSize = newAddress;
            fullyClearBlocks.Remove(newAddress);
            newAddress -= blockSize;
        }
        SetNewFileSize(adressForNewFileSize);
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
        SaveMetadata();
        fileStream.Close();
        metadataStream.Close();
    }

    private void ChangeActualBlockByActualPosition()
    {
        var bytes = new byte[blockSize];
        fileStream?.Read(bytes, 0, actualBlock.GetSize());
        actualBlock = actualBlock.FromBytes(bytes);
    }

    public (string allBlocks, string partlyClearBlocks, string fullyClearBlocks) GetAllBlocksAsString()
    {
        var sbAllBlocks = new StringBuilder();
        long actualAdress = 0;
        partlyClearBlocks.Sort();
        fullyClearBlocks.Sort();
        while (actualAdress < fileStream.Length)
        {
            Seek(actualAdress);
            ChangeActualBlockByActualPosition();
            long blockNumber = actualAdress / blockSize;
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
