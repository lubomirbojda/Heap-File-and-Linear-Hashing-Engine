using AUS2_S2.HeapFile;
using AUS2_S2.HeapFile.DataClasses;

namespace AUS2_S2;

public class Program
{
    static void Main(string[] args)
    {
        var person = new Person("Peter", "Marcin", DateOnly.FromDateTime(new DateTime(2003, 8, 7)), "marcin");
        var person1 = new Person("Laco", "Nagy", DateOnly.FromDateTime(new DateTime(2002, 10, 2)), "nagy1");
        var person2 = new Person("Laco", "Nagy", DateOnly.FromDateTime(new DateTime(2002, 10, 2)), "nagy2");
        var person3 = new Person("Laco", "Nagy", DateOnly.FromDateTime(new DateTime(2002, 10, 2)), "nagy3");
        var person4 = new Person("Laco", "Nagy", DateOnly.FromDateTime(new DateTime(2002, 10, 2)), "nagy4");
        var person5 = new Person("Laco", "Nagy", DateOnly.FromDateTime(new DateTime(2002, 10, 2)), "nagy5");
        var person6 = new Person("Laco", "Nagy", DateOnly.FromDateTime(new DateTime(2002, 10, 2)), "nagy6");
        var person7 = new Person("Laco", "Nagy", DateOnly.FromDateTime(new DateTime(2002, 10, 2)), "nagy7");
        var person8 = new Person("Laco", "Nagy", DateOnly.FromDateTime(new DateTime(2002, 10, 2)), "nagy8");
        var person9 = new Person("Laco", "Nagy", DateOnly.FromDateTime(new DateTime(2002, 10, 2)), "nagy9");
        var person10 = new Person("Laco", "Nagy", DateOnly.FromDateTime(new DateTime(2002, 10, 2)), "nagy10");
        var file = new BHeapFile<Person>(person.GetSize() * 6 + 12, "C:\\Users\\gamet\\Desktop\\pokus.txt");

        file.Insert(person);
        file.Insert(person1);
        file.Insert(person1);
        file.Insert(person1);
        file.Insert(person1);
        file.Insert(person1);
        file.Insert(person1);
        file.Insert(person1);
        file.Insert(person1);
        file.Insert(person1);

        var peto = file.Get(0, new Person("marcin"));
        var laco = file.Get(0, new Person("nagy"));
    }
}
