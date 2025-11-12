namespace AUS2_S2.HeapFile.DataClasses;

public class Person : IData<Person>
{
    public string FirstName { get; private set; } = new string()
    public string LastName { get; private set; }
    public DateOnly BirthDate { get; private set; }
    public string Id { get; private set; }

    public Person(string firstName, string lastName, DateOnly birthDate, string id)
    {
        FirstName = firstName;
        LastName = lastName;
        BirthDate = birthDate;
        Id = id;
    }

    public Person() : this(string.Empty, string.Empty, default, string.Empty) { }

    public Person(string id) : this(string.Empty, string.Empty, default, id) { }

    public bool Equals(Person other)
    {
        return Id == other.Id;
    }

    public Person FromBytes(byte[] dataBytes)
    {
        throw new NotImplementedException();
    }

    public byte[] GetBytes()
    {
        throw new NotImplementedException();
    }

    public int GetSize()
    {
        return 
    }
}
