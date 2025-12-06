namespace AUS2_S2.DataClasses;

public class Person : IData<Person>
{
    private const int FIRST_NAME_MAX_CHARS = 15;
    private const int LAST_NAME_MAX_CHARS = 14;
    private const int ID_MAX_CHARS = 10;
    private const int MAX_TESTS = 6;

    private string firstName;
    private string lastName;
    private DateOnly birthDate;
    private readonly string id;

    private int firstNameValidChars;
    private int lastNameValidChars;
    private readonly int idValidChars;

    public int[] TestsCodes { get; private set; } = new int[MAX_TESTS];
    public int TestsCount { get; set; } = 0;

    public string FirstName => firstName.Substring(0, firstNameValidChars);
    public string LastName => lastName.Substring(0, lastNameValidChars);
    public DateOnly BirthDate => birthDate;
    public string Id => id.Substring(0, idValidChars);

    public Person(string firstName, string lastName, DateOnly birthDate, string id)
    {
        this.firstName = firstName;
        this.lastName = lastName;
        this.birthDate = birthDate;
        this.id = id;

        firstNameValidChars = Math.Min(firstName.Length, FIRST_NAME_MAX_CHARS);
        lastNameValidChars = Math.Min(lastName.Length, LAST_NAME_MAX_CHARS);
        idValidChars = Math.Min(id.Length, ID_MAX_CHARS);
    }

    public Person() : this(string.Empty, string.Empty, default, string.Empty) { }
    public Person(string id) : this(string.Empty, string.Empty, default, id) { }

    public void SetNewFirstName(string newFirstName)
    {
        firstName = newFirstName;
        firstNameValidChars = Math.Min(firstName.Length, FIRST_NAME_MAX_CHARS);
    }

    public void SetNewLastName(string newLastName)
    {
        lastName = newLastName;
        lastNameValidChars = Math.Min(lastName.Length, LAST_NAME_MAX_CHARS);
    }

    public void SetNewBirthdate(DateOnly newBirthdate)
    {
        birthDate = newBirthdate;
    }

    public bool Equals(Person other)
    {
        return id.Equals(other.id);
    }

    public Person FromBytes(byte[] dataBytes)
    {
        int offset = 0;

        //strings valid chars
        var firstNameCount = BitConverter.ToInt32(dataBytes, offset);
        offset += sizeof(int);
        var lastNameCount = BitConverter.ToInt32(dataBytes, offset);
        offset += sizeof(int);
        var idCount = BitConverter.ToInt32(dataBytes, offset);
        offset += sizeof(int);

        //strings
        var firstName = ConvertBytesToString(dataBytes, offset, firstNameCount);
        offset += FIRST_NAME_MAX_CHARS * sizeof(char);     
        var lastName = ConvertBytesToString(dataBytes, offset, lastNameCount);
        offset += LAST_NAME_MAX_CHARS * sizeof(char);
        var id = ConvertBytesToString(dataBytes, offset, idCount);
        offset += ID_MAX_CHARS * sizeof(char);

        //birtdate
        var birthDateDays = BitConverter.ToInt32(dataBytes, offset);
        offset += sizeof(int);
        var birthDate = DateOnly.FromDayNumber(birthDateDays);

        //tests count
        var testCount = BitConverter.ToInt32(dataBytes, offset);
        offset += sizeof(int);

        //tests codes
        int[] testCodes = new int[MAX_TESTS];
        for (int i = 0; i < MAX_TESTS; i++)
        {
            testCodes[i] = BitConverter.ToInt32(dataBytes, offset);
            offset += sizeof(int);
        }

        Person createdPerson = new(firstName, lastName, birthDate, id)
        {
            TestsCount = testCount,
            TestsCodes = testCodes
        };

        return createdPerson;
    }

    public byte[] GetBytes()
    {
        var firstNameB = ConvertStringToBytes(firstName, FIRST_NAME_MAX_CHARS);
        var lastNameB = ConvertStringToBytes(lastName, LAST_NAME_MAX_CHARS);
        var idB = ConvertStringToBytes(id, ID_MAX_CHARS);
        var birthDateB = BitConverter.GetBytes(birthDate.DayNumber);
        var testCountB = BitConverter.GetBytes(TestsCount);

        byte[] result = new byte[GetSize()];
        int offset = 0;

        //strings valid chars
        Array.Copy(BitConverter.GetBytes(firstNameValidChars), 0, result, offset, sizeof(int));
        offset += sizeof(int);
        Array.Copy(BitConverter.GetBytes(lastNameValidChars), 0, result, offset, sizeof(int));
        offset += sizeof(int);
        Array.Copy(BitConverter.GetBytes(idValidChars), 0, result, offset, sizeof(int));
        offset += sizeof(int);

        //strings
        Array.Copy(firstNameB, 0, result, offset, firstNameB.Length);
        offset += firstNameB.Length;
        Array.Copy(lastNameB, 0, result, offset, lastNameB.Length);
        offset += lastNameB.Length;
        Array.Copy(idB, 0, result, offset, idB.Length);
        offset += idB.Length;

        //birthdate
        Array.Copy(birthDateB, 0, result, offset, sizeof(int));
        offset += sizeof(int);

        //tests count
        Array.Copy(testCountB, 0, result, offset, sizeof(int));
        offset += sizeof(int);

        //tests codes
        for (int i = 0; i < MAX_TESTS; i++)
        {
            Array.Copy(BitConverter.GetBytes(TestsCodes[i]), 0, result, offset, 4);
            offset += sizeof(int);
        }

        return result;
    }

    public int GetSize()
    {
        return
            FIRST_NAME_MAX_CHARS * sizeof(char) + LAST_NAME_MAX_CHARS * sizeof(char) + 
            ID_MAX_CHARS * sizeof(char) + (5 + MAX_TESTS) * sizeof(int);
    }

    public static string SetStringLength(string stringToSetLength, int length)
    {
        if (stringToSetLength.Length > length)
            return stringToSetLength.Substring(0, length);
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
        return "First name: " + FirstName + ", Last Name: " + LastName + ", Birthdate: " + BirthDate.ToString("dd/MM/yyyy") + ", ID: " + Id +
            ", Number of tests: " + TestsCount;
    }

    public override int GetHashCode()
    {
        int hash = 23;
        for (int i = 0; i < idValidChars; i++)
            hash = hash * 31 + id[i];
        return hash ^ (hash >> 16);
    }
}
