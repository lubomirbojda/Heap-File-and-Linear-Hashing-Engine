using System.Windows;

namespace GUI_HeapFIle.OperationsWindows
{
    /// <summary>
    /// Interaction logic for NewFileBlockSizeWindow.xaml
    /// </summary>
    public partial class NewFileBlockSizeWindow : Window
    {
        public int BlockSize { get; private set; }
        public NewFileBlockSizeWindow()
        {
            InitializeComponent();
        }

        private void CreateFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(BlockSizeTextBox.Text, out int result))
            {
                BlockSize = result;
                DialogResult = true;
            }
            else
            {
                DialogResult = false;
            }

            Close();
        }
    }
}
