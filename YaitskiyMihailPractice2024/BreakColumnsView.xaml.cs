using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace YaitskiyMihailPractice2024
{
    /// <summary>
    /// Interaction logic for BreakColumnsView.xaml
    /// </summary>
    public partial class BreakColumnsView : Window
    {
        public IList<ColumnToBreak> columnsToBreak = new List<ColumnToBreak>();
        public BreakColumnsView()
        {
            InitializeComponent();
            dataGrid.ItemsSource = columnsToBreak;
        }
        private void Button_click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Skibidi");
        }
        
    }
}
  