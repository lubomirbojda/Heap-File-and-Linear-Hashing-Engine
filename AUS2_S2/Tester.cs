using AUS2_S2.DataClasses;
using AUS2_S2.HeapFile;
using AUS2_S2.LinearHashFile;

namespace AUS2_S2;

public class Tester
{
    private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string GetRandomString(int stringLenght, Random rand)
    {
        return new string(Enumerable.Repeat(chars, stringLenght).Select(s => s[rand.Next(s.Length)]).ToArray());
    }

    public void TestHeapFile()
    {
        var numberOfOperation = 100000;
        var path = Path.Combine(Environment.CurrentDirectory, "testingHF");
        Random rand = new();
        if (File.Exists(path))
            File.Delete(path);
        var heapFile = new BHeapFile<Person>(576, path, path + ".metadata");
        List<(long address, Person person)> list = [];

        for (int i = 0; i < numberOfOperation; i++)
        {
            Console.WriteLine("Operation number: " + i);
            var operation = rand.NextDouble();
            //insert
            if (operation < 0.4)
            {
                var birthdate = DateOnly.FromDateTime(DateTime.Now).AddDays(-rand.Next(1, 365)).AddMonths(-rand.Next(1, 12)).AddYears(-rand.Next(18, 50));
                var randPerson = new Person(GetRandomString(7, rand), GetRandomString(5, rand), birthdate, GetRandomString(8, rand));
                var address = heapFile.Insert(randPerson);
                list.Add((address, randPerson));
            }
            //delete
            else if (operation < 0.8)
            {
                if (list.Count > 0)
                {
                    var randIndex = rand.Next(0, list.Count);
                    var (address, person) = list[randIndex];
                    if (!heapFile.Delete(address, person))
                    {
                        Console.WriteLine("Error at delete from heapfile!");
                        return;
                    }
                    list.RemoveAt(randIndex);

                    var loaded = heapFile.Get(address, person);
                    if (loaded != null)
                    {
                        Console.WriteLine("Error at delete from heapfile!");
                        return;
                    }
                }
            }
            //get
            else
            {
                if (list.Count > 0)
                {
                    var randIndex = rand.Next(0, list.Count);
                    var (address, person) = list[randIndex];
                    var foundPerson = heapFile.Get(address, person);
                    if (foundPerson == null || !foundPerson.Equals(person))
                    {
                        Console.WriteLine("Error at get from heapfile!");
                        return;
                    }
                } 
            }

            //check if all persons are at addresses
            foreach (var (address, person) in list)
            {
                var foundPerson = heapFile.Get(address, person);
                if (foundPerson == null || !foundPerson.Equals(person))
                {
                    Console.WriteLine("File does not contain all data it should!");
                    return;
                }
            }
        }

        Console.WriteLine("Heapfile test passed.");
    }


    public void TestLinHashFile()
    {
        var numberOfOperation = 10000;
        var path = Environment.CurrentDirectory;
        var mainPath = Path.Combine(path, "mainLHF");
        var overPath = Path.Combine(path, "overLHF");
        Random rand = new();
        List<Person> list = [];
        var lhfile = new LinHashFile<Person>(1055, mainPath, 780, overPath, 2, Path.Combine(path, "LHFmetadata"));

        for (int i = 0; i < numberOfOperation; i++)
        {
            var operation = rand.NextDouble();
            Console.WriteLine("Doing operation number: " + i);

            //insert
            if (operation < 0.45)
            {
                var birthdate = DateOnly.FromDateTime(DateTime.Now).AddDays(-rand.Next(1, 365)).AddMonths(-rand.Next(1, 12)).AddYears(-rand.Next(18, 50));
                int charsName = rand.Next(2, 14);
                int charsLastName = rand.Next(2, 13);
                var randPerson = new Person(GetRandomString(charsName, rand), GetRandomString(charsLastName, rand), birthdate, GetRandomString(8, rand));
                list.Add(randPerson);
                lhfile.Insert(randPerson);
            }
            //get
            else if (operation < 0.6)
            {
                if (list.Count > 0)
                {
                    var randIndex = rand.Next(0, list.Count);
                    var person = list[randIndex];
                    var foundPerson = lhfile.Get(person);
                    if (foundPerson == null || !foundPerson.Equals(person))
                    {
                        Console.WriteLine("Error at get from linear hash file!");
                        return;
                    }
                }
            }
            //delete
            else
            {
                if (list.Count > 0)
                {
                    var randIndex = rand.Next(0, list.Count);
                    var person = list[randIndex];
                    if (!lhfile.Delete(person))
                    {
                        Console.WriteLine("Error at delete from linear hash file!");
                        return;
                    }
                    var foundPerson = lhfile.Get(person);
                    if (foundPerson != null)
                    {
                        Console.WriteLine("Error at delete from linear hash file!");
                        return;
                    }

                    list.Remove(person);
                }
            }


            foreach (var person in list)
            {
                var foundPerson = lhfile.Get(person);
                if (foundPerson == null || !foundPerson.Equals(person))
                {
                    Console.WriteLine("Linear hash file does not contain all data it should!");
                    return;
                }
            }
        }

        Console.WriteLine("Linear has file test successful.");
    }
}
