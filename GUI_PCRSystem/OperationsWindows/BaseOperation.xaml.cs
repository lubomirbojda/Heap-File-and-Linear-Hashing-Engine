using AUS2_S2.DataClasses;
using System.Windows;

namespace GUI_PCRSystem.OperationsWindows
{
    /// <summary>
    /// Interaction logic for BaseOperation.xaml
    /// </summary>
    public partial class BaseOperation : Window
    {
        public BaseOperation()
        {
            InitializeComponent();
        }

        public void SetNewResult(IEnumerable<Person>? persons, IEnumerable<PCRTest>? pcrTests)
        {
            PersonsListView.ItemsSource = persons;
            PCRTestsListView.ItemsSource = pcrTests;
            PCRTestsCountBox.Text = PCRTestsListView.Items.Count.ToString();
        }
    }
}
