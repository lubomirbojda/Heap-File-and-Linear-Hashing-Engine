using AUS2_S2.HeapFile;
using AUS2_S2.HeapFile.DataClasses;
using GUI_HeapFIle.OperationsWindows;
using System.IO;
using System.Windows;

namespace GUI_HeapFIle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string path = "C:\\Users\\gamet\\Desktop\\pokus.txt";
        private BHeapFile<Person> file;
        public MainWindow()
        {
            InitializeComponent();
            if (File.Exists(path))
                File.Delete(path);

            var dummy = new Person();
            file = new BHeapFile<Person>(dummy.GetSize() * 6 + 12, path);

            FillHeapFile();
        }

        private void FillHeapFile()
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
        }

        private void PrintFileButton_Click(object sender, RoutedEventArgs e)
        {
            TextBoxHeapFile.Text = file.GetAllBlocksAsString().allBlocks;
            TextBoxPartlyClearBlocks.Text = file.GetAllBlocksAsString().partlyClearBlocks;
            TextBoxFullyClearBlocks.Text = file.GetAllBlocksAsString().fullyClearBlocks;
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            new InsertWindow(file).ShowDialog();
        }

        private void GetButton_Click_1(object sender, RoutedEventArgs e)
        {
            new GetWindow(file).ShowDialog();
        }

        private void DeleteButton_Click_2(object sender, RoutedEventArgs e)
        {
            new DeleteWindow(file).ShowDialog();
        }
    }
}