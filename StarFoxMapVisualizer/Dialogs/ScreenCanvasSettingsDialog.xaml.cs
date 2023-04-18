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
using System.Windows.Shapes;

namespace StarFoxMapVisualizer.Dialogs
{
    /// <summary>
    /// Interaction logic for ScreenCanvasSettings.xaml
    /// </summary>
    public partial class ScreenCanvasSettingsDialog : Window
    {
        public int SelectedQuadrant { get; private set; } = -1;
        public ScreenCanvasSettingsDialog()
        {
            InitializeComponent();
        }

        private void Q1_Click(object sender, RoutedEventArgs e)
        {
            if (sender  == Q1)
                SelectedQuadrant= 0;
            else if (sender == Q2)
                SelectedQuadrant= 1;
            else if (sender == Q3)
                SelectedQuadrant= 2;
            else SelectedQuadrant= 3;
            DialogResult= true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedQuadrant = -1;
            DialogResult= true;
            Close();
        }
    }
}
