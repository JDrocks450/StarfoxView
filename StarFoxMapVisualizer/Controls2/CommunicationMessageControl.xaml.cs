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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StarFoxMapVisualizer.Controls2
{
    /// <summary>
    /// Interaction logic for CommunicationMessageControl.xaml
    /// </summary>
    public partial class CommunicationMessageControl : ContentControl, INotifyPropertyChanged
    {
        public Rect ImageRect { get; set; } = new Rect(0,0,31,39);

        public enum Characters
        {
            FOX,
            FALCON,
            RABBIT,
            FROG,
            PEPPER,
            ANDROSS,
            BETA_SLIPPY
        }
        public static Characters MapSpeakerToCharacter(string Speaker) => Speaker switch
        {
            "fox" or "fox3" => Characters.FOX,
            "falcon" or "falcon3" => Characters.FALCON,
            "rabbit" or "rabbit3" => Characters.RABBIT,
            "frog" or "frog3" => Characters.FROG,
            _ => Characters.FOX,
        };

        public CommunicationMessageControl()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void DrawMugshot(Characters Character, int Frame = 0)
        {
            int baseX = 0;
            int baseY = 0;
            int charWidth = 31;
            int charHeight = 39;// FOX FRAME 1            
            switch (Character)
            {
                case Characters.FOX: break;
                case Characters.FALCON: baseX = (charWidth * 2) + 2; break;
                case Characters.RABBIT: baseY = charHeight + 1; break;
                case Characters.FROG: baseX = (charWidth * 2) + 2; baseY = charHeight + 1; break;
            }
            ImageRect = new Rect(baseX + Frame * (charWidth + 1),baseY,charWidth,charHeight); 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageRect)));
        }
    }
}
