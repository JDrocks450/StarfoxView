using StarFox.Interop.GFX;
using StarFox.Interop.GFX.DAT;
using StarFox.Interop.GFX.DAT.MSPRITES;
using StarFoxMapVisualizer.Misc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace StarFoxMapVisualizer.Controls2
{
    /// <summary>
    /// Interaction logic for MSpritesViewer.xaml
    /// </summary>
    public partial class MSpritesViewer : Window
    {
        private MSpritesDefinitionFile mSpritesDefinitionFile;

        public MSpritesViewer()
        {
            InitializeComponent();
        }

        public MSpritesViewer(MSpritesDefinitionFile mSpritesDefinitionFile) : this()
        {
            this.mSpritesDefinitionFile = mSpritesDefinitionFile;

            Loaded += Load;
        }

        private void Load(object sender, RoutedEventArgs e)
        {
            List<MSprite> sprites = new();
            foreach(var bank in mSpritesDefinitionFile.Banks)            
                sprites.AddRange(bank.Value.Sprites.Values);            
            SelectionCombo.ItemsSource = sprites;
        }

        private void SelectionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowMSprite(SelectionCombo.SelectedItem as MSprite);
        }

        public async void ShowMSprite(MSprite Sprite)
        {
            SelectionCombo.SelectionChanged -= SelectionCombo_SelectionChanged;
            SelectionCombo.SelectedItem = Sprite;
            SelectionCombo.SelectionChanged += SelectionCombo_SelectionChanged;

            string[] banks =
            {
                "TEX_01_low.cgx",
                "TEX_01_high.cgx",
                "TEX_23_low.cgx",
                "TEX_23_high.cgx",
                "TEX_23_A_low.cgx",
                "TEX_23_A_high.cgx",
            };
            List<FXCGXFile> cgxs = new List<FXCGXFile>();
            foreach(var bankName in banks)
            {
                var hit = AppResources.ImportedProject.SearchFile(bankName).FirstOrDefault();
                if (hit == default)
                    throw new FileNotFoundException($"Could not find {bankName}");
                cgxs.Add(SFGFXInterface.OpenCGX(hit.FilePath));
            }
            var colHit = AppResources.ImportedProject.SearchFile("P_COL.COL").FirstOrDefault();
            if (colHit == default)
                throw new FileNotFoundException($"Could not find P_COL.COL");

            var pCol = await FILEStandard.GetPalette(new FileInfo(colHit.FilePath));
            if (pCol == default)
                throw new InvalidDataException("PCOL was not found.");

            var bmp = SFGFXInterface.RenderMSprite(Sprite, pCol, cgxs.ToArray());
            RenderImage.Source = bmp.Convert();
        }
    }
}
