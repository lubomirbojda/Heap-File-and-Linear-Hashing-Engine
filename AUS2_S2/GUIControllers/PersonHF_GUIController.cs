using AUS2_S2.DataClasses;
using AUS2_S2.HeapFile;
using System.Collections.Generic;

namespace AUS2_S2.GUIControllers
{
    public class PersonHF_GUIController
    {
        private readonly BHeapFile<Person> _heapFile;
        public PersonHF_GUIController(BHeapFile<Person> heapFile)
        {
            _heapFile = heapFile;
        }

        public Person? Get(long address, Person dummy) => _heapFile.Get(address, dummy);

        public void Insert(Person insertingPerson) => _heapFile.Insert(insertingPerson);

        public bool Delete(long address, Person dummy) => _heapFile.Delete(address, dummy);

        public void CloseFile() => _heapFile.Close();

        public (string allBlocks, string partlyClearBlocks, string fullyClearBlocks) GetAllBlocks() => _heapFile.GetAllBlocksAsString();

        public void GenerateDataForHeapFile()
        {
            if (_heapFile == null)
                return;

            var list = new List<(long address, Person person)>();
            Random rand = new();
            var numberOfInserts = 530;
            var numberOfDeletes = 500;

            for (int i = 0; i < 5; i++)
            {
                //inserts
                for (int j = 0; j < numberOfInserts; j++)
                {
                    var birthdate = DateOnly.FromDateTime(DateTime.Now).AddDays(-rand.Next(1, 365)).AddMonths(-rand.Next(1, 12)).AddYears(-rand.Next(18, 50));
                    var randPerson = new Person(Tester.GetRandomString(7, rand), Tester.GetRandomString(5, rand), birthdate, Tester.GetRandomString(8, rand));
                    list.Add((_heapFile.Insert(randPerson), randPerson));
                }

                //deletes
                for (int j = 0; j < numberOfDeletes; j++)
                {
                    if (list.Count > 0)
                    {
                        var randIndex = rand.Next(0, list.Count);
                        var (address, person) = list[randIndex];
                        _heapFile.Delete(address, person);
                        list.RemoveAt(randIndex);
                    }
                }
            }
        }
    }
}
