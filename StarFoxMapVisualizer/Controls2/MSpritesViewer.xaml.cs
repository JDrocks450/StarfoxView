using StarFox.Interop.GFX;
using StarFox.Interop.GFX.DAT;
using StarFox.Interop.GFX.DAT.MSPRITES;
using StarFoxMapVisualizer.Controls;
using StarFoxMapVisualizer.Misc;
using System;
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
        private string PaletteName = SHAPEStandard.DefaultMSpritePalette;
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

        private void SelectionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => ShowMSprite(SelectionCombo.SelectedItem as MSprite);

        public async void ShowMSprite(MSprite Sprite)
        {
            SelectionCombo.SelectionChanged -= SelectionCombo_SelectionChanged;
            SelectionCombo.SelectedItem = Sprite;
            SelectionCombo.SelectionChanged += SelectionCombo_SelectionChanged;

            try
            {
                RenderImage.Source = await SHAPEStandard.RenderMSprite(Sprite, PaletteName);
            }
            catch (Exception ex)
            {
                AppResources.ShowCrash(ex, false, "Viewing an MSprite");
            }
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PaletteWindowButton_Click(object sender, RoutedEventArgs e)
        {
            PaletteSelectionWindow window = new PaletteSelectionWindow()
            {
                Owner = Application.Current.MainWindow
            };
            window.Closed += delegate
            {
                if (window.SelectedPalette == default) return;
                PaletteName = System.IO.Path.GetFileNameWithoutExtension(window.SelectedPalette?.Name) ?? "NIGHT";
                ShowMSprite(SelectionCombo.SelectedItem as MSprite);
            };
            window.Show();
        }
    }
}
