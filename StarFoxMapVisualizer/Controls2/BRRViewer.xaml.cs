using Microsoft.WindowsAPICodePack.Dialogs;
using StarFox.Interop.BRR;
using StarFoxMapVisualizer.Misc.Audio;
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
using WpfPanAndZoom.CustomControls;

namespace StarFoxMapVisualizer.Controls2
{
    /// <summary>
    /// Interaction logic for BRRViewer.xaml
    /// </summary>
    public partial class BRRViewer : UserControl
    {
        private int DEFAULT_FREQ = (int)WAVSampleRates.MED2;

        private List<string> FilePathsCache = new();
        private Stream SelectedSampleStream;
        private int extractedSampleRate = 0;
        private AudioPlaybackEngine playbackEngine;
        private int SelectedFileIndex = -1;

        private BRRFile? SelectedFile
        {
            get
            {
                if (SelectedFileIndex < 0) return null;
                var searchRange = FilePathsCache.ElementAtOrDefault(SelectedFileIndex);
                if (searchRange == null) return null;
                AppResources.ImportedProject.Samples.TryGetValue(searchRange, out var val);
                return val;
            }
        }
        private BRRSample? SelectedSample { get; set; }

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
                if (rate > 20000) continue;
                SampleRates.Children.Add(new RadioButton()
                {
                    IsChecked = rate == DEFAULT_FREQ,
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

        public void SelectFile(string FilePath)
        {
            if (!baseOpenFile(FilePathsCache.IndexOf(FilePath)))
                ; // TODO: HANDLE ERROR HERE
        }

        private void FileSelected(object sender, SelectionChangedEventArgs e)
        {
            if (!baseOpenFile(FileBrowser.SelectedIndex))
                ; // TODO: HANDLE ERROR HERE
        }

        private bool baseOpenFile(int index)
        {
            SelectedFileIndex= index;
            if (SelectedFile == null) return false;
            SamplesList.Items.Clear();
            FileField.Text = System.IO.Path.GetFileNameWithoutExtension(SelectedFile.OriginalFilePath).ToUpperInvariant();
            int i = -1;
            foreach (var sample in SelectedFile.Effects.Values)
            {
                if (sample == default) continue;
                i++;
                UniformGrid grid = new()
                {
                    Columns = 3
                };
                var nameBox = new TextBox()
                {
                    Background = null,
                    BorderBrush = null,
                    Foreground = Brushes.White,
                    Text = sample.Name ?? $"Sample {i + 1}"
                };
                nameBox.KeyUp += FinalizeName;
                grid.Children.Add(nameBox);
                grid.Children.Add(new TextBlock(new Run("0:00")));
                grid.Children.Add(new TextBlock(new Run($"{sample.SampleData.Count} Samples")
                {
                    Foreground = sample.SampleData.Count >= 250 ? Brushes.White : Brushes.Coral
                }));
                ListViewItem item = new()
                {
                    Content = grid,
                    Tag = sample
                };
                item.Selected += ItemClicked;
                SamplesList.Items.Add(item);
            }
            //SET FILE BROWSER SELECTION TO BE ON THE CURRENT FILE
            FileBrowser.SelectionChanged -= FileSelected;
            FileBrowser.SelectedIndex = index;
            FileBrowser.SelectionChanged += FileSelected;
            if (SelectedFile.Effects.Count == 1)
                SelectSample(SelectedFile.Effects[0]);
            return true;
        }

        private void FinalizeName(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            if (SelectedSample == null) return;
            var name = SelectedSample.Name = (sender as TextBox).Text;
            SampleField.Text = name;
        }

        private void ItemClicked(object sender, RoutedEventArgs e)
        {
            SampleSelected(null, null);
        }

        private void SampleSelected(object sender, SelectionChangedEventArgs e)
        {            
            if (SamplesList.SelectedItem == null) return;
            var sample = ((ListViewItem)SamplesList.SelectedItem)?.Tag as BRRSample;
            SelectSample(sample);
        }
        public void SelectSample(BRRSample Sample)
        {
            var sample = Sample;
            SelectedSample = sample;
            SampleField.Text = sample.Name;
            Extract();
            Play();
        }
        private void Extract()
        {
            //Is a sample opened and cached?
            if (SelectedSampleStream != default)
            { // close it.
                SelectedSampleStream.Dispose();
                SelectedSampleStream = default;
            }
            //Get the desired sample rate
            int sampleRate = GetSampleRate();
            //Is there an open playback engine? Did the frequency change?
            if (playbackEngine != default && sampleRate != extractedSampleRate)
            {
                playbackEngine.Dispose();
                playbackEngine = null;
            }            
            //Create a playback engine for this samplerate
            playbackEngine = new AudioPlaybackEngine(sampleRate, 1);
            //load the sample
            var sample = SelectedSample;
            var ms = new MemoryStream();
            {
                //Disable the UI
                IsEnabled = false;
                //Dump the contents of the WAV to a Stream
                BRRInterface.WriteSample(ms, sample, BRRInterface.BRRExportFileTypes.WaveFormat, sampleRate);                
                
                //Set the cached stream and sample rate
                SelectedSampleStream = ms;
                extractedSampleRate = sampleRate;
            }
            //Disable the UI
            IsEnabled = true;
            ShowWaveForm(sample);
        }

        private void ShowWaveForm(BRRSample Sample) => WaveFormDisplay.Display(Sample);

        private void Play()
        {
            if (SelectedSampleStream == null) return;
            if (!SelectedSampleStream.CanRead || !SelectedSampleStream.CanSeek) return;
            if (GetSampleRate() != extractedSampleRate) // user changed sample rate            
                Extract(); // reextract
            SelectedSampleStream.Seek(0, System.IO.SeekOrigin.Begin);
            playbackEngine.PlaySound(SelectedSampleStream);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Play();
        }

        private void BackwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (SamplesList.SelectedIndex>0)
                SamplesList.SelectedIndex--;            
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (SamplesList.SelectedIndex < SamplesList.Items.Count-1)
                SamplesList.SelectedIndex++;
        }

        private void SetStartButton_Click(object sender, RoutedEventArgs e)
        {
            SamplesList.SelectedIndex = 0;
        }

        private void SetEndButton_Click(object sender, RoutedEventArgs e)
        {
            SamplesList.SelectedIndex = SamplesList.Items.Count-1;
        }
        /// <summary>
        /// Show in BIG mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WaveFormDisplay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var sample = SelectedSample;
            if (sample == default) return;
            Grid panel = new()
            {
                Margin = new Thickness(10),
            };
            WaveFormControl control = new(sample)
            {
                Foreground = Brushes.DeepSkyBlue,
                BorderBrush = Brushes.White,
                Width = sample.SampleData.Count,
                Height = 700
            };
            PanAndZoomCanvas box = new PanAndZoomCanvas()
            {
                ClipToBounds = true,
                Background = Brushes.Transparent,
                LineColor = Colors.Transparent,
            };
            box.Children.Add(control);
            Button exit = new()
            {
                Margin = new Thickness(0,10,0,0),
                Width = 100,
                Content = "OK",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
            };                        
            panel.Children.Add(box);
            panel.Children.Add(exit);
            Window dialog = new()
            {
                Height = 500,
                Content = panel,
                Width = 720,
                Background = Brushes.Black,
                Title = "Wave File Viewer",
                Foreground = Brushes.White,
                Owner = Application.Current.MainWindow
            };
            //dialog.SetResourceReference(StyleProperty, "SFDialogModalStyle");
            exit.Click += delegate
            {
                dialog.Close();
            };
            dialog.Show();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var file = SelectedFile;
            if (file == default) return;
            var directory = System.IO.Path.GetDirectoryName(file.OriginalFilePath);
            directory = System.IO.Path.Combine(directory, "Export",
                System.IO.Path.GetFileNameWithoutExtension(file.OriginalFilePath));
            ExportAll(file, directory);
        }        
        private void ExportAll(BRRFile file, string Folder, BRRInterface.BRRExportFileTypes Format = BRRInterface.BRRExportFileTypes.WaveFormat)
        {
            if (file == default) return;
            try
            {
                IsEnabled = false;
                var freq = GetSampleRate();
                MessageBox.Show($"Are you sure you want to export all {file.Effects.Count} effects " +
                    $"at the frequency (Sample Rate): {freq}Hz?\n" +
                    $"The Directory selected is: {Folder}" +
                    $"\nPress No to go back now. This may take some time.", "Yo!", MessageBoxButton.YesNo);
                Directory.CreateDirectory(Folder);
                foreach (var sample in file.Effects.Values)
                {
                    string extension = Format switch
                    {
                        BRRInterface.BRRExportFileTypes.WaveFormat => ".wav",
                        BRRInterface.BRRExportFileTypes.BRRFormat => ".brr",
                        _ => throw new Exception($"{nameof(Format)} was supplied as NoneSelected which is not allowed.")
                    };
                    ExportOne(System.IO.Path.Combine(Folder, sample.Name + extension), sample, freq, Format);
                }
                IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured! \n" + ex.ToString());
            }
            finally
            {
                IsEnabled = true;
            }
        }
        private void ExportOne(string FileName, BRRSample Sample, int Freq, BRRInterface.BRRExportFileTypes Format)
        {
            try
            {
                BRRInterface.WriteSample(FileName, "save", Sample, Format, Freq);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured! \n" + ex.ToString());
            }
            finally
            {
                IsEnabled = true;
            }
        }

        private void ExportAs_Click(object sender, RoutedEventArgs e)
        {
            var file = SelectedFile;
            if (file == default) return;
            CommonOpenFileDialog fileDialog = new()
            {
                IsFolderPicker = true,
                InitialDirectory = Environment.CurrentDirectory,
                Multiselect = false,
                Title = "Select a Folder to Drop These Sounds Into"
            };
            if (fileDialog.ShowDialog() != CommonFileDialogResult.Ok) return; // OOPSIES
            string selectedFolder = fileDialog.FileName;
            var result = 
                MessageBox.Show("Export as Microsoft Wave format?\n\nSelecting No will export to *.BRR format.",
                "Format Selection", MessageBoxButton.YesNoCancel);
            var format = BRRInterface.BRRExportFileTypes.NoneSelected;
            switch (result)
            {
                case MessageBoxResult.Yes: format = BRRInterface.BRRExportFileTypes.WaveFormat; break;
                case MessageBoxResult.No: format = BRRInterface.BRRExportFileTypes.BRRFormat; break;
                case MessageBoxResult.Cancel: return;
            }
            ExportAll(file, selectedFolder, format);
        }

        private void ExportSoundButton_Click(object sender, RoutedEventArgs e)
        {
            var file = SelectedFile;
            if (file == default) return;
            var sample = SelectedSample; 
            if (sample == default) return;
            var freq = GetSampleRate();
            CommonSaveFileDialog fileDialog = new()
            {                
                AlwaysAppendDefaultExtension = true,
                CreatePrompt = false,
                EnsureFileExists= false,
                EnsurePathExists = true,
                InitialDirectory = AppResources.ImportedProject.WorkspaceDirectory.FullName,
                Title = $"Save {sample.Name} at {freq}Hz to?",       
                DefaultExtension = "wav",
                DefaultFileName = (SelectedSample.Name ?? "Untitled") + ".wav"
            };
            fileDialog.Filters.Add(new CommonFileDialogFilter("Wave Format", "wav"));
            fileDialog.Filters.Add(new CommonFileDialogFilter("Bit Rate Reduction Format", "brr"));
            if (fileDialog.ShowDialog() != CommonFileDialogResult.Ok) return; // OOPSIES
            string selectedFolder = System.IO.Path.GetDirectoryName(fileDialog.FileName);
            string fileName = fileDialog.FileAsShellObject.Name;
            var format = BRRInterface.BRRExportFileTypes.WaveFormat;
            if (fileName.ToUpper().EndsWith("BRR")) // BRR Selected
                format = BRRInterface.BRRExportFileTypes.BRRFormat;
            var path = System.IO.Path.Combine(selectedFolder, fileName);
            ExportOne(path, sample, freq, format);
        }

        private void CopyItem_Click(object sender, RoutedEventArgs e)
        {
            var sample = SelectedSample;
            if (sample == default) return;
            var file = System.IO.Path.GetTempPath() + $"\\{sample.Name}.wav";
            ExportOne(file, sample, GetSampleRate(), BRRInterface.BRRExportFileTypes.WaveFormat);
            var collect = new System.Collections.Specialized.StringCollection();
            collect.Add(file);
            Clipboard.SetFileDropList(collect);
        }

        private void OtherOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            OtherOptionsContextMenu.IsOpen = true;
        }
    }
}
