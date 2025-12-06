using System.Windows;

namespace GUI_PCRSystem.OperationsWindows.PrintFile
{
    /// <summary>
    /// Interaction logic for FilePrint.xaml
    /// </summary>
    public partial class FilePrint : Window
    {
        public FilePrint(string filePrintString)
        {
            InitializeComponent();
            TextBoxHeapFile.Text = filePrintString;
        }
    }
}
