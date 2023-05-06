using StarFox.Interop.BRR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
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
using System.Xml;

namespace StarFoxMapVisualizer.Controls2
{
    /// <summary>
    /// Interaction logic for BRRViewer.xaml
    /// </summary>
    public partial class BRRViewer : UserControl
    {
        private List<string> FilePathsCache = new();
        private Stream SelectedSampleStream;
        private int extractedSampleRate = 0;

        private BRRFile SelectedFile => 
            AppResources.ImportedProject.Samples[FilePathsCache.ElementAt(FileBrowser.SelectedIndex)];
        private BRRSample? SelectedSample => ((ListViewItem)SamplesList.SelectedItem)?.Tag as BRRSample;

        public BRRViewer()
        {
            InitializeComponent();
            FileBrowser.Items.Clear();
            Loaded += OnLoad;
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {            
            FileBrowser.SelectionChanged += FileSelected;
            SamplesList.SelectionChanged += SampleSelected;
            SampleRates.Children.Clear();
            foreach(int rate in Enum.GetValues<WAVSampleRates>())
            {
                if (rate > 35000) continue;
                SampleRates.Children.Add(new RadioButton()
                {
                    Content = $"{rate}Hz",
                    Tag = rate
                });
            }
        }    
        
        private int GetSampleRate()
        {
            foreach(RadioButton button in SampleRates.Children)
            {
                if (button.IsChecked ?? false)
                    return (int)button.Tag;
            }
            return (int)WAVSampleRates.MED2;
        }

        internal void RefreshFiles()
        {
            FileBrowser.SelectionChanged -= FileSelected;
            FilePathsCache.Clear();
            FilePathsCache.AddRange(AppResources.ImportedProject.Samples.Keys);
            
            FileBrowser.ItemsSource = FilePathsCache.Select(x => System.IO.Path.GetFileName(x).ToUpper());
            FileBrowser.SelectionChanged += FileSelected;
        }

        private void FileSelected(object sender, SelectionChangedEventArgs e)
        {
            FileField.Text = FileBrowser.SelectedItem.ToString();
            SamplesList.Items.Clear();
            int i = -1;
            foreach(var sample in SelectedFile.Effects.Values)
            {
                if (sample == default) continue;
                i++;
                UniformGrid grid = new()
                {
                    Columns = 3
                };
                grid.Children.Add(new TextBox() 
                {
                    Background = null,
                    BorderBrush = null,
                    Foreground = Brushes.White,
                    Text = sample.Name ?? $"Sample {i + 1}" 
                });
                grid.Children.Add(new TextBlock(new Run("0:00")));
                grid.Children.Add(new TextBlock(new Run($"{sample.SampleData.Count} Samples")));
                ListViewItem item = new()
                {
                    Content= grid,
                    Tag = sample
                };
                item.PreviewMouseLeftButtonDown += ItemClicked; ;
                SamplesList.Items.Add(item);
            }
        }

        private void ItemClicked(object sender, MouseButtonEventArgs e)
        {
            SampleSelected(null, null);
        }

        private void SampleSelected(object sender, SelectionChangedEventArgs e)
        {            
            if (SamplesList.SelectedItem == null) return;
            var sample = SelectedSample;
            SampleField.Text = sample.Name;
            Extract();
            Play();
        }
        private void Extract()
        {
            if (SelectedSampleStream != default)
            {
                SelectedSampleStream.Dispose();
                SelectedSampleStream = default;
            }
            var sample = SelectedSample;
            var ms = new MemoryStream();
            {
                IsEnabled = false;
                int sampleRate = GetSampleRate();
                BRRImporter.WriteSampleToWAVStream("null", "Preview", sample, ms, sampleRate/2);
                BRRImporter.WriteSampleToWAV("export/sample.wav", "sample", sample, sampleRate);                                
                SelectedSampleStream = ms;
                extractedSampleRate = sampleRate;
            }
        }
        private void Play()
        {
            if (SelectedSampleStream == null) return;
            if (!SelectedSampleStream.CanRead || !SelectedSampleStream.CanSeek) return;
            if (GetSampleRate() != extractedSampleRate) // user changed sample rate            
                Extract();            
            SelectedSampleStream.Seek(0, System.IO.SeekOrigin.Begin);
            using (SoundPlayer player = new SoundPlayer(SelectedSampleStream))
            {
                player.Play();
                IsEnabled = true;
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Play();
        }
    }
}
