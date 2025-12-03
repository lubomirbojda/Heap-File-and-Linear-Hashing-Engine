namespace AUS2_S2.DataClasses;

public interface IData<T>
{
    bool Equals(T other);
    int GetSize();
    byte[] GetBytes();
    T FromBytes(byte[] dataBytes);
    string GetPrintString();
}
