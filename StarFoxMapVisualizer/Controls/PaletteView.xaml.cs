using StarFoxMapVisualizer.Misc;
using System;
using System.Collections.Generic;
using System.Drawing;
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
    /// Interaction logic for PaletteView.xaml
    /// </summary>
    public partial class PaletteView : Window
    {
        public PaletteView()
        {
            InitializeComponent();
        }

        public void SetupControl(COL Palette)
        {
            using (var _bitmap = Palette.RenderPalette())
                PaletteViewImage.Source = _bitmap.Convert();
            ColorsBlock.Text = Palette.GetPalette().Length.ToString();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(PaletteViewImage.Source as BitmapSource);
        }
    }
}
