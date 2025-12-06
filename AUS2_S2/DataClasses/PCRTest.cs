namespace AUS2_S2.DataClasses;

public class PCRTest : IData<PCRTest>
{
    private const int PERSON_ID_MAX_CHARS = 10;
    private const int NOTE_MAX_CHARS = 11;

    public DateTime TestDateTime { get; }
    public string PersonId { get; }
    public int TestCode { get; }
    public bool Result { get; }
    public double Value { get; }
    public string Note { get; }

    private readonly int patientIdValidChars;
    private readonly int noteValidChars;

    public PCRTest(DateTime datetime, string personID, int code, bool result, double value, string note)
    {
        TestDateTime = datetime;
        PersonId = personID;
        TestCode = code;
        Result = result;
        Value = value;
        Note = note;

        patientIdValidChars = Math.Min(personID.Length, PERSON_ID_MAX_CHARS);
        noteValidChars = Math.Min(note.Length, NOTE_MAX_CHARS);
    }

    public PCRTest() : this(default, "", 0, false, 0.0, "") { }

    public PCRTest(int code) : this(default, "", code, false, 0.0, "") { }

    public bool Equals(PCRTest other) => TestCode == other.TestCode;

    public PCRTest FromBytes(byte[] bytes)
    {
        int offset = 0;

        //datetime
        long ticks = BitConverter.ToInt64(bytes, offset);
        offset += sizeof(long);
        DateTime dt = new(ticks);

        //person id
        int personIdValid = BitConverter.ToInt32(bytes, offset);
        offset += sizeof(int);
        string personID = Person.ConvertBytesToString(bytes, offset, personIdValid);
        offset += PERSON_ID_MAX_CHARS * sizeof(char);

        //test code
        int code = BitConverter.ToInt32(bytes, offset);
        offset += sizeof(int);

        //result
        bool result = BitConverter.ToBoolean(bytes, offset);
        offset += sizeof(bool);

        //value
        double value = BitConverter.ToDouble(bytes, offset);
        offset += sizeof(double);

        //note
        int noteLen = BitConverter.ToInt32(bytes, offset);
        offset += sizeof(int);
        string note = Person.ConvertBytesToString(bytes, offset, noteLen);

        return new PCRTest(dt, personID, code, result, value, note);
    }

    public byte[] GetBytes()
    {
        byte[] dateTimeB = BitConverter.GetBytes(TestDateTime.Ticks);
        byte[] personIdValidB = BitConverter.GetBytes(patientIdValidChars);
        byte[] personIdB = Person.ConvertStringToBytes(PersonId, PERSON_ID_MAX_CHARS);
        byte[] noteValidB = BitConverter.GetBytes(noteValidChars);
        byte[] noteB = Person.ConvertStringToBytes(Note, NOTE_MAX_CHARS);
        byte[] codeB = BitConverter.GetBytes(TestCode);
        byte[] resultB = BitConverter.GetBytes(Result);
        byte[] valueB = BitConverter.GetBytes(Value);

        byte[] result = new byte[GetSize()];
        int offset = 0;

        //datetime
        Array.Copy(dateTimeB, 0, result, offset, dateTimeB.Length);
        offset += dateTimeB.Length;

        //person id - valid chars count
        Array.Copy(personIdValidB, 0, result, offset, sizeof(int));
        offset += sizeof(int);

        //person id
        Array.Copy(personIdB, 0, result, offset, personIdB.Length);
        offset += personIdB.Length;

        //code
        Array.Copy(codeB, 0, result, offset, sizeof(int));
        offset += sizeof(int);

        //result
        Array.Copy(resultB, 0, result, offset, sizeof(bool));
        offset += sizeof(bool);

        //value
        Array.Copy(valueB, 0, result, offset, sizeof(double));
        offset += sizeof(double);

        //note - valid chars count
        Array.Copy(noteValidB, 0, result, offset, sizeof(int));
        offset += sizeof(int);

        //note
        Array.Copy(noteB, 0, result, offset, noteB.Length);

        return result;
    }

    public int GetSize()
    {
        return sizeof(long) + sizeof(int) + PERSON_ID_MAX_CHARS * sizeof(char) + 
            sizeof(int) + sizeof(bool) + sizeof(double) + sizeof(int) + NOTE_MAX_CHARS * sizeof(char);
    }

    public string GetPrintString()
    {
        return "Code: " + TestCode + ", Date and time: " + TestDateTime + ", Person ID: " + PersonId + ", Result: " + Result +
            ", Value: " + Value + ", Note: " + Note;
    }

    public override int GetHashCode() => TestCode;
}
