using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
    /// <summary>
    /// Interaction logic for BackgroundRendererViewOptions.xaml
    /// </summary>
    public partial class BackgroundRendererViewOptions : HeaderedContentControl
    {
        public event EventHandler<(bool Horizontal, double ScrollValue)> BG2_ScrollValueChanged, BG3_ScrollValueChanged;

        private void XScrollSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            bool Horizontal = sender == XScrollSlider;
            BG3_ScrollValueChanged?.Invoke(this, (Horizontal, e.NewValue));
        }

        private void XScrollSlider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            bool Horizontal = sender == XScrollSlider2;
            BG2_ScrollValueChanged?.Invoke(this, (Horizontal, e.NewValue));
        }

        public BackgroundRendererViewOptions()
        {
            InitializeComponent();
        }
    }
}
