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
using static StarFox.Interop.GFX.CAD;

namespace StarFoxMapVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for PaletteViewWindow.xaml
    /// </summary>
    public partial class PaletteSelectionWindow : Window
    {
        public (string Name, COL Palette)? SelectedPalette => PaletteSelection.SelectedPalette;

        public PaletteSelectionWindow()
        {
            InitializeComponent();
            Loaded += PaletteSelectionWindow_Loaded;
        }

        private void PaletteSelectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PaletteSelection.InvalidatePalettes();
        }

        private void PaletteSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PaletteSelection.SelectedItem == default) return;
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
