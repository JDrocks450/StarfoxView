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

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
    /// <summary>
    /// Interaction logic for CopyableImage.xaml
    /// </summary>
    public partial class CopyableImage : Image
    {
        public CopyableImage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Copies the image to the <see cref="Clipboard"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyImage_Click(object sender, RoutedEventArgs e)
        {
            var image = this.Source as BitmapImage;
            if (image != null) Clipboard.SetImage(image);
            else
            {
                var error = new InvalidOperationException("Copying that image failed, it isn't of the correct type.\n" +
                    "Probably my bad, I apologize. Let me know with a screenshot please. :)");
                AppResources.ShowCrash(error, false, "Copying/Exporting an image.");
            }
        }

        private void ExportItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
