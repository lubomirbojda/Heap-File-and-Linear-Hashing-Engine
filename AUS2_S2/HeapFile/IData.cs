namespace AUS2_S2.HeapFile;

public interface IData<T>
{
    bool Equals(T other);
    int GetSize();
    byte[] GetBytes();
    T FromBytes(byte[] dataBytes);
    string GetPrintString();
}
