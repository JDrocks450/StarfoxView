using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
    /// <summary>
    /// Interaction logic for ContractableMenu.xaml
    /// </summary>
    public partial class ContractableMenu : ContentControl, INotifyPropertyChanged
    {
        public bool Opened { get; private set; } = true;
        public string ContractContent { get; private set; } = ">";

        public ContractableMenu()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void ExpandContractButton_Click(object sender, RoutedEventArgs e) => ToggleToolbox();

        /// <summary>
        /// Opens/Closes the toolbox on the right side. 
        /// <para/><paramref name="EnsureState"/> can be used to manually set the state to ON/OFF.
        /// </summary>
        /// <param name="EnsureState"></param>
        /// <returns>The new state of if it's opened or not</returns>
        public bool ToggleToolbox(bool? EnsureState = default)
        {
            bool stateChanging = Opened;
            Opened = EnsureState ?? !Opened;
            stateChanging = stateChanging != Opened;
            //This code effectively just checks to see if we set the state to the same as it already is.
            if (!stateChanging) return Opened;

            double DEST_WIDTH = ActualWidth; // toolbox has a manual width set, which makes this fine here.
            Thickness DEST_LOC = new Thickness(0);
            if (!Opened)
                DEST_LOC.Right = -DEST_WIDTH;
            var anim = new ThicknessAnimation(DEST_LOC, TimeSpan.FromSeconds(.5))
            {
                AccelerationRatio = .7,
                DecelerationRatio = .3
            }; // half second
            BeginAnimation(MarginProperty, anim); // start the animation

            ContractContent = Opened ? ">" : "<";

            return Opened;
        }
    }
}
