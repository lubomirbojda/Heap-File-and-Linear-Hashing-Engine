namespace AUS2_S2.HeapFile.DataClasses;

public class Person : IData<Person>
{
    public const int FIRST_NAME_MAX_CHARS = 15;
    public const int LAST_NAME_MAX_CHARS = 14;
    public const int ID_MAX_CHARS = 10;

    private string firstName;
    private string lastName;
    private DateOnly birthDate;
    private string id;

    private int firstNameValidChars;
    private int lastNameValidChars;
    private int idValidChars;

    public string FirstName { get { return firstName.Substring(0, firstNameValidChars); } }
    public string LastName { get { return lastName.Substring(0, lastNameValidChars); } }
    public DateOnly BirthDate { get { return birthDate; } }
    public string Id { get { return id.Substring(0, idValidChars); } }

    public Person(string firstName, string lastName, DateOnly birthDate, string id)
    {
        this.firstName = firstName;
        this.lastName = lastName;
        this.birthDate = birthDate;
        this.id = id;

        firstNameValidChars = (firstName.Length < FIRST_NAME_MAX_CHARS) ? firstName.Length : FIRST_NAME_MAX_CHARS;
        lastNameValidChars = (lastName.Length < LAST_NAME_MAX_CHARS) ? lastName.Length : LAST_NAME_MAX_CHARS;
        idValidChars = (id.Length < ID_MAX_CHARS) ? id.Length : ID_MAX_CHARS;
    }

    public Person() : this(string.Empty, string.Empty, default, string.Empty) { }

    public Person(string id) : this(string.Empty, string.Empty, default, id) { }

    public bool Equals(Person other)
    {
        return id.Equals(other.id);
    }

    public Person FromBytes(byte[] dataBytes)
    {
        int offset = 0;
        var firstNameCount = BitConverter.ToInt32(dataBytes, offset);
        offset += sizeof(int);
        var firstName = ConvertBytesToString(dataBytes, offset, firstNameCount);
        offset += FIRST_NAME_MAX_CHARS * sizeof(char);

        var lastNameCount = BitConverter.ToInt32(dataBytes, offset);
        offset += sizeof(int);
        var lastName = ConvertBytesToString(dataBytes, offset, lastNameCount);
        offset += LAST_NAME_MAX_CHARS * sizeof(char);

        var idCount = BitConverter.ToInt32(dataBytes, offset);
        offset += sizeof(int);
        var id = ConvertBytesToString(dataBytes, offset, idCount);
        offset += ID_MAX_CHARS * sizeof(char);

        var birthDateTotalDays = BitConverter.ToInt32(dataBytes, offset);
        var birthDate = DateOnly.FromDayNumber(birthDateTotalDays);

        return new Person(firstName, lastName, birthDate, id);
    }

    public byte[] GetBytes()
    {
        var firstNameB = ConvertStringToBytes(firstName, FIRST_NAME_MAX_CHARS);
        var lastNameB = ConvertStringToBytes(lastName, LAST_NAME_MAX_CHARS);
        var idB = ConvertStringToBytes(id, ID_MAX_CHARS);
        var birthDateTotalDaysB = BitConverter.GetBytes(birthDate.DayNumber);

        byte[][] arrayOfByteArrayStrings = [firstNameB, lastNameB, idB];
        byte[][] arrayOfByteArrayStringsValidCounts = [
            BitConverter.GetBytes(firstNameValidChars), 
            BitConverter.GetBytes(lastNameValidChars), 
            BitConverter.GetBytes(idValidChars)
            ];
        byte[] result = new byte[3 * sizeof(int) + firstNameB.Length + lastNameB.Length + idB.Length + birthDateTotalDaysB.Length];

        int offset = 0;
        for (int i = 0; i < arrayOfByteArrayStrings.Length; i++)
        {
            foreach (var b in arrayOfByteArrayStringsValidCounts[i])
            {
                result[offset] = b;
                offset++;
            }
            for (int j = 0; j < arrayOfByteArrayStrings[i].Length; j++)
            {
                result[offset + j] = arrayOfByteArrayStrings[i][j];
            }
            offset += arrayOfByteArrayStrings[i].Length;
        }

        foreach (var b in birthDateTotalDaysB)
        {
            result[offset] = b;
            offset++;
        }

        return result;
    }

    public int GetSize()
    {
        return (sizeof(char) * FIRST_NAME_MAX_CHARS) + (sizeof(char) * LAST_NAME_MAX_CHARS) + (sizeof(char) * ID_MAX_CHARS) + (4 * sizeof(int));
    }

    public static string SetStringLength(string stringToSetLength, int length)
    {
        if (stringToSetLength.Length > length)
            return stringToSetLength.Substring(0, length);
        else
            return stringToSetLength.PadRight(length, '\0');
    }

    public static byte[] ConvertStringToBytes(string stringToBytes, int length)
    {
        stringToBytes = SetStringLength(stringToBytes, length);
        byte[] bytes = new byte[length * sizeof(char)];

        for (int i = 0; i < length; i++)
        {
            var charsBytes = BitConverter.GetBytes(stringToBytes[i]);
            bytes[i * 2] = charsBytes[0];
            bytes[i * 2 + 1] = charsBytes[1];
        }

        return bytes;
    }

    public static string ConvertBytesToString(byte[] bytes, int offset, int length)
    {
        char[] chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = BitConverter.ToChar(bytes, offset + i * sizeof(char));
        }
        return new string(chars);
    }

    public string GetPrintString()
    {
        return "First name: " + FirstName + ", Last Name: " + LastName + ", Birthdate: " + BirthDate.ToString("dd/MM/yyyy") + ", ID: " + Id;
    }
}
