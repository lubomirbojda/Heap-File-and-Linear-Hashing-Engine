using AUS2_S2.DataClasses;
using AUS2_S2.LinearHashFile;
using System.Text;

namespace AUS2_S2.PCRTestsSystem;

public class PCRSystem
{
    private LinHashFile<Person> personsLHF;
    private LinHashFile<PCRTest> pcrTestsLHF;
    private int testsCodesSequence;
    private int idSequence;

    private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public PCRSystem(LinHashFile<Person> personsLHF, LinHashFile<PCRTest> pcrTestsLHF)
    {
        this.personsLHF = personsLHF;
        this.pcrTestsLHF = pcrTestsLHF;
        idSequence = 0;
        testsCodesSequence = 0;
    }

    private string GenerateUniquePersonID()
    {
        Random rand = new();
        var firstPart = idSequence.ToString();
        idSequence++;
        int length = Math.Min(firstPart.Length, 10);
        firstPart = firstPart.Substring(0, length);
        int missingCharsCount = 10 - length;
        StringBuilder sb = new(firstPart);
        for (int i = 0; i < missingCharsCount; i++)
        {
            int index = rand.Next(0, chars.Length);
            sb.Append(chars[index]);
        }
        return sb.ToString();
    }

    //Operations
    //01 - Insert PCRTest
    public (bool result, string message) InsertPCRTest(DateTime datetime, string personID, bool result, double value, string note)
    {
        var pcrTest = new PCRTest(datetime, personID, testsCodesSequence, result, value, note);
        testsCodesSequence++;

        var personDummy = new Person(pcrTest.PersonId);
        var foundPerson = personsLHF.Get(personDummy);
        if (foundPerson == null)
            return (false, "Person with this ID not exists.");

        if (foundPerson.TestsCount == foundPerson.TestsCodes.Length)
            return (false, "Person has already done maximum tests.");

        pcrTestsLHF.Insert(pcrTest);
        foundPerson.TestsCodes[foundPerson.TestsCount] = pcrTest.TestCode;
        foundPerson.TestsCount++;

        UpdatePerson(foundPerson);

        return (true, "PCR test inserted successfully." + "\n" + "Test code: " + pcrTest.TestCode);
    }

    //02 - find person
    public (Person? person, List<PCRTest> tests) FindPerson(string personID)
    {
        List<PCRTest> tests = [];
        var personDummy = new Person(personID);
        var foundPerson = personsLHF.Get(personDummy);
        if (foundPerson == null)
            return (null, tests);

        for (int i = 0; i < foundPerson.TestsCount; i++)
        {
            var pcrtest = new PCRTest(foundPerson.TestsCodes[i]);
            var foundTest = pcrTestsLHF.Get(pcrtest);
            if (foundTest != null)
                tests.Add(pcrtest);
        }

        return (foundPerson, tests);
    }

    //03 - find pcr test
    public (PCRTest? test, Person? person) FindPCRTest(int testCode)
    {
        var testDummy = new PCRTest(testCode);
        var foundTest = pcrTestsLHF.Get(testDummy);
        if (foundTest == null) 
            return (null, null);

        var personDummy = new Person(foundTest.PersonId);
        var foundPerson = personsLHF.Get(personDummy);
        if (foundPerson == null) 
            return (null, null);

        return (foundTest, foundPerson);
    }

    //04 - Insert person
    public string InsertPerson(string firstName, string lastName, DateOnly birthDate)
    {
        var newPersonID = GenerateUniquePersonID();
        Person person = new(firstName, lastName, birthDate, newPersonID);
        personsLHF.Insert(person);
        return "New person ID: " + newPersonID;
    }

    //05 - Delete PCR test
    public bool DeletePCRTest(int testCode)
    {
        var testDummy = new PCRTest(testCode);
        return pcrTestsLHF.Delete(testDummy);
    }

    //06 - Delete person from system + tests
    public bool DeletePerson(string personId)
    {
        var dummyPerson = new Person(personId);
        var foundPerson = personsLHF.Get(dummyPerson);
        if (foundPerson == null) 
            return false;

        for (int i = 0; i < foundPerson.TestsCount; i++)
        {
            var dummyTest = new PCRTest(foundPerson.TestsCodes[i]);
            pcrTestsLHF.Delete(dummyTest);
        }
            
        return true;
    }

    //07 - Find person to edit
    public Person? FindPersonToEdit(string personId)
    {
        var dummyPerson = new Person(personId);
        return personsLHF.Get(dummyPerson);
    }

    //08 - Find PCR test to edit
    public PCRTest? FindPCRTestToEdit(int testCode)
    {
        var testDummy = new PCRTest(testCode);
        return pcrTestsLHF.Get(testDummy);
    }

    //Update methods
    public bool UpdatePerson(Person editedPerson) => personsLHF.Update(editedPerson);

    public bool UpdatePCRTest(PCRTest editedTest) => pcrTestsLHF.Update(editedTest);
}
