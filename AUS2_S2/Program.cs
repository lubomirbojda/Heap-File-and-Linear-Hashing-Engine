using AUS2_S2.HeapFile;
using AUS2_S2.HeapFile.DataClasses;

namespace AUS2_S2;

public class Program
{
    static void Main(string[] args)
    {
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
        var person11 = new Person("Laco", "Nagy", DateOnly.FromDateTime(new DateTime(2002, 10, 2)), "nagy11");
        var person12 = new Person("Laco", "Nagy", DateOnly.FromDateTime(new DateTime(2002, 10, 2)), "nagy12");
        var person13 = new Person("Laco", "Nagy", DateOnly.FromDateTime(new DateTime(2002, 10, 2)), "nagy13");
        var file = new BHeapFile<Person>(person1.GetSize() * 6 + 12, "C:\\Users\\gamet\\Desktop\\pokus.txt");

        file.Insert(person1);
        file.Insert(person2);
        file.Insert(person3);
        file.Insert(person4);
        file.Insert(person5);
        file.Insert(person6);

        //---------
        var add1 = file.Insert(person7);
        file.Insert(person8);
        file.Insert(person9);
        file.Insert(person10);
        file.Insert(person11);
        file.Insert(person12);

        //------
        var add2 = file.Insert(person13);

        file.Delete(add1, new Person("nagy7"));
        file.Delete(add1, new Person("nagy8"));
        file.Delete(add1, new Person("nagy9"));
        file.Delete(add1, new Person("nagy10"));
        file.Delete(add1, new Person("nagy11"));
        file.Delete(add1, new Person("nagy12"));

        //file.Delete(add2, new Person("nagy13"));

        //var peto = file.Get(0, new Person("marcin"));
        var laco = file.Get(add2, new Person("nagy13"));
    }
}
