using System;
using System.Windows.Controls;
using static StarFox.Interop.GFX.CAD;

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
    /// <summary>
    /// Interaction logic for PaletteListView.xaml
    /// </summary>
    public partial class PaletteListView : ListView
    {
        public (string Name, COL Palette)? SelectedPalette => SelectedItem != null ? (ValueTuple<string, COL>)(SelectedItem as ListViewItem)?.Tag : default;

        public PaletteListView()
        {
            InitializeComponent();
        }

        public void InvalidatePalettes()
        {
            Items.Clear();
            var COLFiles = AppResources.ImportedProject?.Palettes;
            if (COLFiles == null) return;
            foreach (var col in COLFiles)
            {
                var item = new ListViewItem()
                {
                    Content = System.IO.Path.GetFileNameWithoutExtension(col.Key),
                    Tag = (col.Key, col.Value)
                };
                Items.Add(item);
                if (col.Value == SelectedPalette?.Palette)
                    SelectedItem = item;
            }
        }
    }
}
