using AUS2_S2.DataClasses;
using AUS2_S2.LinearHashFile;
using AUS2_S2.PCRTestsSystem;

namespace AUS2_S2.GUIControllers;

public class PCRSytemGUIController
{
    private PCRSystem system;
    public PCRSytemGUIController(string systemFilePath, int personsFileMainBlockSize, int personsFileOverflowBlockSize, 
        int pcrTestsFileMainBlockSize, int pcrTestsFileOverflowBlockSize, int personsInitialBlocksCount, int pcrTestsInitialBlocksCount)
    {
        File.WriteAllText(systemFilePath, "PCR_SYSTEM");
        string dir = systemFilePath + ".data";
        Directory.CreateDirectory(dir);
        LinHashFile<Person> personsLHF = new(personsFileMainBlockSize, Path.Combine(dir, "personsMain"), personsFileOverflowBlockSize,
            Path.Combine(dir, "personsOverflow"), personsInitialBlocksCount, Path.Combine(dir, "personsLHF.metadata"));

        LinHashFile<PCRTest> pcrTestsLHF = new(pcrTestsFileMainBlockSize, Path.Combine(dir, "pcrTestsMain"), pcrTestsFileOverflowBlockSize,
            Path.Combine(dir, "pcrTestsOverflow"), pcrTestsInitialBlocksCount, Path.Combine(dir, "pcrTestsLHF.metada"));

        system = new(personsLHF, pcrTestsLHF);
    }

    public PCRSytemGUIController(string systemFilePath)
    {
        string dir = systemFilePath + ".data";
        LinHashFile<Person> personsLHF = new(Path.Combine(dir, "personsMain"), 
            Path.Combine(dir, "personsOverflow"), 
            Path.Combine(dir, "personsLHF.metadata"));

        LinHashFile<PCRTest> pcrTestsLHF = new(Path.Combine(dir, "pcrTestsMain"),
            Path.Combine(dir, "pcrTestsOverflow"),
            Path.Combine(dir, "pcrTestsLHF.metada"));

        system = new(personsLHF, pcrTestsLHF);
    }

    public (bool result, string message) InsertPCRTest(DateTime datetime, string personID, bool result, double value, string note)
        => system.InsertPCRTest(datetime, personID, result, value, note);

    public (Person? person, List<PCRTest> tests) FindPerson(string personID)
        => system.FindPerson(personID);

    public (PCRTest? test, Person? person) FindPCRTest(int testCode)
        => system.FindPCRTest(testCode);

    public string InsertPerson(string firstName, string lastName, DateOnly birthDate)
        => system.InsertPerson(firstName, lastName, birthDate);

    public bool DeletePCRTest(int testCode)
        => system.DeletePCRTest(testCode);

    public bool DeletePerson(string personId)
        => system.DeletePerson(personId);

    public Person? FindPersonToEdit(string personId)
        => system.FindPersonToEdit(personId);

    public PCRTest? FindPCRTestToEdit(int testCode)
        => system.FindPCRTestToEdit(testCode);

    public bool UpdatePerson(Person editedPerson)
        => system.UpdatePerson(editedPerson);

    public bool UpdatePCRTest(PCRTest editedTest)
        => system.UpdatePCRTest(editedTest);
}
