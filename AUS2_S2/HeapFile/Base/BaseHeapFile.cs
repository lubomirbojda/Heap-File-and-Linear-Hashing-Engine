using AUS2_S2.DataClasses;
using System.Text;

namespace AUS2_S2.HeapFile.Base;

public abstract class BaseHeapFile<T, BlockT> where T : IData<T>, new() where BlockT : BaseBlock<T>
{
    public int BlockingFactor { get; private set; }
    public int BlockSize { get; private set; }
    private long LastBlockAdress;
    private readonly FileStream fileStream;
    //private Block<T> ActualBlock;
    private BlockT ActualBlock;
    private readonly T dummyT = new();
    private readonly List<long> partlyClearBlocks = [];
    public List<long> fullyClearBlocks { get; private set; } = [];
    private readonly FileStream metadataStream;

    protected abstract BlockT CreateBlock(int blockSize, int blockingFactor);

    public BaseHeapFile(int dataBlockSize, string filePath, string metadataFilePath)
    {
        BlockSize = dataBlockSize;
        var helpBlock = CreateBlock(0, 0);
        int availableBytes = dataBlockSize - helpBlock.GetManageInfoSize();
        BlockingFactor = (availableBytes >= dummyT.GetSize()) ? (availableBytes / dummyT.GetSize()) : throw new InvalidOperationException("Block is not big enough)");
        //ActualBlock = new Block<T>(BlockSize, BlockingFactor);
        ActualBlock = CreateBlock(BlockSize, BlockingFactor);

        LastBlockAdress = -1;

        if (File.Exists(filePath))
            File.Delete(filePath);

        fileStream = new(filePath, FileMode.Create, FileAccess.ReadWrite);

        if (File.Exists(metadataFilePath))
            File.Delete(metadataFilePath);

        metadataStream = new(metadataFilePath, FileMode.Create, FileAccess.ReadWrite);
    }

    public BaseHeapFile(string filePath, string metadataFilePath)
    {
        fileStream = new(filePath, FileMode.Open, FileAccess.ReadWrite);
        metadataStream = new(metadataFilePath, FileMode.Open, FileAccess.ReadWrite);
        LoadMetadata();
        ActualBlock = CreateBlock(BlockSize, BlockingFactor);
    }

    private void LoadMetadata()
    {
        using var br = new BinaryReader(metadataStream);
        LastBlockAdress = br.ReadInt64();
        BlockingFactor = br.ReadInt32();
        BlockSize = br.ReadInt32();
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
        bw.Write(LastBlockAdress);
        //int
        bw.Write(BlockingFactor);
        //int
        bw.Write(BlockSize);
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
            if (ActualBlock.ValidCount < BlockingFactor)
                partlyClearBlocks.Add(returningAddress);
            fullyClearBlocks.Remove(returningAddress);
            return returningAddress;
        }

        fileStream.Seek(0, SeekOrigin.End);
        LastBlockAdress = fileStream.Position;
        var newBlock = CreateBlock(BlockSize, BlockingFactor);
        newBlock.BlockData[0] = insertingData;
        newBlock.IncrementValidCount();
        fileStream.Write(newBlock.GetBytes(), 0, newBlock.GetSize());
        fileStream.Flush();
        if (BlockingFactor > 1)
            partlyClearBlocks.Add(LastBlockAdress);
        return LastBlockAdress;
    }

    private long InsertDataToFirstBlockFromAdressList(List<long> listOfBlocksAdresses, T data)
    {
        listOfBlocksAdresses.Sort();
        long returningPos = listOfBlocksAdresses[0];
        Seek(returningPos);
        ChangeActualBlockByActualPosition();
        ActualBlock.BlockData[ActualBlock.ValidCount] = data;
        ActualBlock.IncrementValidCount();
        WriteActualBlockAtPosition(returningPos);
        if (ActualBlock.ValidCount == BlockingFactor)
            listOfBlocksAdresses.RemoveAt(0);
        return returningPos;
    }

    public T? Get(long address, T dummy)
    {
        Seek(address);
        ChangeActualBlockByActualPosition();
        var p = ActualBlock.BlockData.Take(ActualBlock.ValidCount).FirstOrDefault(x => x.Equals(dummy));
        return p;
    }

    public bool Delete(long address, T dummy)
    {
        Seek(address);
        ChangeActualBlockByActualPosition();
        var dataIndex = Array.FindIndex(ActualBlock.BlockData.Take(ActualBlock.ValidCount).ToArray(), x => x.Equals(dummy));
        if (dataIndex == -1 || dataIndex > ActualBlock.ValidCount)
            return false;

        for (int i = dataIndex + 1; i < ActualBlock.ValidCount; i++)
        {
            ActualBlock.BlockData[i - 1] = ActualBlock.BlockData[i];
        }
        ActualBlock.DecrementValidCount();

        if (partlyClearBlocks.Contains(address))
        {
            if (ActualBlock.ValidCount == 0)
            {
                partlyClearBlocks.Remove(address);
                if (address == LastBlockAdress)
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
            if (ActualBlock.ValidCount == 0)
            {
                if (address == LastBlockAdress)
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

        WriteActualBlockAtPosition(address);

        return true;
    }

    public void RemoveAllEmptyBlocksFromEnd()
    {
        var adressForNewFileSize = LastBlockAdress;
        var newAddress = adressForNewFileSize;
        while (newAddress >= 0)
        {
            if (!fullyClearBlocks.Contains(newAddress))
            {
                LastBlockAdress = newAddress;
                break;
            }

            adressForNewFileSize = newAddress;
            fullyClearBlocks.Remove(newAddress);
            newAddress -= BlockSize;
        }

        if (adressForNewFileSize != LastBlockAdress)
            SetNewFileSize(adressForNewFileSize);
    }

    public void Seek(long adress)
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

    public void ChangeActualBlockByActualPosition()
    {
        var bytes = new byte[BlockSize];
        fileStream?.Read(bytes, 0, ActualBlock.GetSize());
        ActualBlock = (BlockT)ActualBlock.FromBytes(bytes);
    }

    public void WriteActualBlockAtPosition(long address)
    {
        Seek(address);
        fileStream.Write(ActualBlock.GetBytes(), 0, BlockSize);
        fileStream.Flush();
    }

    public BlockT ReadBlock(long address)
    {
        var bytes = new byte[BlockSize];
        fileStream.Seek(address, SeekOrigin.Begin);
        fileStream?.Read(bytes, 0, BlockSize);
        return (BlockT)ActualBlock.FromBytes(bytes);
    }

    public void WriteBlock(long address, BlockT block)
    {
        fileStream.Seek(address, SeekOrigin.Begin);
        fileStream.Write(block.GetBytes(), 0, BlockSize);
        fileStream.Flush();
    }

    public long AllocateNewEmptyBlock()
    {
        fileStream.Seek(0, SeekOrigin.End);
        long addr = fileStream.Position;

        var b = CreateBlock(BlockSize, BlockingFactor);
        fileStream.Write(b.GetBytes(), 0, BlockSize);
        fileStream.Flush();

        fullyClearBlocks.Remove(addr);

        LastBlockAdress = addr;
        return addr;
    }

    public long GetBlockAddressByIndex(int index)
    {
        return (long)index * BlockSize;
    }

    public void MarkBlockAsFree(long address)
    {
        if (!fullyClearBlocks.Contains(address))
            fullyClearBlocks.Add(address);
    }

    public void RemoveLastBlock()
    {
        if (LastBlockAdress >= 0)
        {
            fullyClearBlocks.Remove(LastBlockAdress);
            SetNewFileSize(LastBlockAdress);
            LastBlockAdress -= BlockSize;
            if (LastBlockAdress < 0)
                LastBlockAdress = -1;
        }
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
            long blockNumber = actualAdress / BlockSize;
            sbAllBlocks.AppendLine("-----BLOCK " + blockNumber + " -----");
            sbAllBlocks.AppendLine("Adress: " + actualAdress);

            //if (ActualBlock.OverflowAddress >= 0)
            //    sbAllBlocks.AppendLine("Overflow address: " + ActualBlock.OverflowAddress);
            //else
            //    sbAllBlocks.AppendLine("Overflow address: none");

            sbAllBlocks.AppendLine("Valid count: " + ActualBlock.ValidCount);

            if (ActualBlock.ValidCount == 0)
            {
                sbAllBlocks.AppendLine("EMPTY BLOCK");
            }
            else
            {
                for (int i = 0; i < ActualBlock.ValidCount; i++)
                    sbAllBlocks.AppendLine((i + 1) + ") " + ActualBlock.BlockData[i].GetPrintString());
            }

            sbAllBlocks.AppendLine("---------------------------------------");
            actualAdress += BlockSize;
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
