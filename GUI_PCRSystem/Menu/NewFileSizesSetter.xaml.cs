using System.Windows;

namespace GUI_PCRSystem.Menu
{
    /// <summary>
    /// Interaction logic for NewFileSizesSetter.xaml
    /// </summary>
    public partial class NewFileSizesSetter : Window
    {
        public int PersonsMainBlockSize_ { get; private set; }
        public int PersonsOverflowBlockSize_ { get; private set; }
        public int PCRTestsMainBlockSize_ { get; private set; }
        public int PCRTestsOverflowBlockSize_ { get; private set; }
        public NewFileSizesSetter()
        {
            InitializeComponent();
        }

        private void CreateFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (PersonsMainBlockSize.Value.HasValue && PersonsOverflowBlockSize.Value.HasValue
                && PCRTestsMainBlockSize.Value.HasValue && PCRTestsOverflowBlockSize.Value.HasValue)
            {
                PersonsMainBlockSize_ = PersonsMainBlockSize.Value.Value;
                PersonsOverflowBlockSize_ = PersonsOverflowBlockSize.Value.Value;
                PCRTestsMainBlockSize_ = PCRTestsMainBlockSize.Value.Value;
                PCRTestsOverflowBlockSize_ = PCRTestsOverflowBlockSize.Value.Value;

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
