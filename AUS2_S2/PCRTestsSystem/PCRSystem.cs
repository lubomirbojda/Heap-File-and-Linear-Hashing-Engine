using AUS2_S2.DataClasses;
using AUS2_S2.LinearHashFile;
using System.Reflection.Emit;
using System.Text;

namespace AUS2_S2.PCRTestsSystem;

public class PCRSystem
{
    private LinHashFile<Person> personsLHF;
    private LinHashFile<PCRTest> pcrTestsLHF;
    private int testsCodesSequence;
    private int idSequence;

    private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private readonly FileStream metadataStream;

    public PCRSystem(LinHashFile<Person> personsLHF, LinHashFile<PCRTest> pcrTestsLHF, string metadataPath, bool open = false)
    {
        this.personsLHF = personsLHF;
        this.pcrTestsLHF = pcrTestsLHF;
        if (open)
        {
            this.personsLHF = personsLHF;
            this.pcrTestsLHF = pcrTestsLHF;
            metadataStream = new(metadataPath, FileMode.Open, FileAccess.ReadWrite);
            LoadMetadata();
        }
        else
        {
            idSequence = 0;
            testsCodesSequence = 0;
            metadataStream = new(metadataPath, FileMode.Create, FileAccess.ReadWrite);
        }
        
    }

    private void LoadMetadata()
    {
        metadataStream.Position = 0;
        using var br = new BinaryReader(metadataStream, Encoding.Default, leaveOpen: true);
        idSequence = br.ReadInt32();
        testsCodesSequence = br.ReadInt32();
    }

    private void SaveMetadata()
    {
        metadataStream.Position = 0;
        metadataStream.SetLength(0);
        using var bw = new BinaryWriter(metadataStream, Encoding.Default, leaveOpen: true);
        bw.Write(idSequence);
        bw.Write(testsCodesSequence);
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

    public void Close()
    {
        personsLHF.Close();
        pcrTestsLHF.Close();
        SaveMetadata();
        metadataStream.Close();
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
                tests.Add(foundTest);
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
        return newPersonID;
    }

    //05 - Delete PCR test
    public bool DeletePCRTest(int testCode)
    {
        var testDummy = new PCRTest(testCode);
        var testToDelete = pcrTestsLHF.Get(testDummy);
        if (testToDelete == null)
            return false;

        var personWithTest = personsLHF.Get(new Person(testToDelete.PersonId));

        if (personWithTest != null)
        {
            int index = Array.IndexOf(personWithTest.TestsCodes, testCode);
            for (int i = index + 1; i < personWithTest.TestsCount; i++)
                personWithTest.TestsCodes[i - 1] = personWithTest.TestsCodes[i];
            personWithTest.TestsCount--;
            personsLHF.Update(personWithTest);
        }
        pcrTestsLHF.Delete(testToDelete);

        return true;
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

        personsLHF.Delete(dummyPerson);
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


    //Generate data for system
    public void GenerateData()
    {
        var numberOfPersons = 2000;
        var numberOfTests = 5000;
        Random rand = new();
        List<Person> listPersons = [];

        for (int i = 0; i < numberOfPersons; i++)
        {

            var birthdate = DateOnly.FromDateTime(DateTime.Now).AddDays(-rand.Next(1, 365)).AddMonths(-rand.Next(1, 12)).AddYears(-rand.Next(18, 50));
            int charsName = rand.Next(2, 14);
            int charsLastName = rand.Next(2, 13);
            //var randPerson = new Person(Tester.GetRandomString(charsName, rand), Tester.GetRandomString(charsLastName, rand), birthdate,
            //    Tester.GetRandomString(8, rand));
            var id = InsertPerson(Tester.GetRandomString(charsName, rand), Tester.GetRandomString(charsLastName, rand), birthdate);
            listPersons.Add(new Person(id));
        }

        for (int i = 0; i < numberOfTests; i++)
        {
            var randIndex = rand.Next(0, listPersons.Count);
            var person = listPersons[randIndex];
            double totalSeconds = (DateTime.Now - DateTime.Now.AddYears(-2)).TotalSeconds;
            double randomSeconds = rand.NextDouble() * totalSeconds;
            var testTime = DateTime.Now.AddSeconds(randomSeconds);
            var result = rand.NextDouble() < 0.5;
            double value = rand.NextDouble() * 10;
            int noteLength = rand.Next(0, 5);
            var note = Tester.GetRandomString(noteLength, rand);
            InsertPCRTest(testTime, person.Id, result, value, note);
        }
    }

    public string GetPrintPersonsMainFile() => personsLHF.GetPrintMainFile();
    public string GetPrintPersonsOverflowFile() => personsLHF.GetPrintOverflowFile();
    public string GetPrintPCRTestsMainFile() => pcrTestsLHF.GetPrintMainFile();
    public string GetPrintPCRTestsOverflowFile() => pcrTestsLHF.GetPrintOverflowFile();
}
