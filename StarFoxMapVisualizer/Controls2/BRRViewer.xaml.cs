﻿using Microsoft.WindowsAPICodePack.Dialogs;
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

        private BRRFile? SelectedFile
        {
            get
            {
                var searchRange = FilePathsCache.ElementAtOrDefault(FileBrowser.SelectedIndex);
                if (searchRange == null) return null;
                AppResources.ImportedProject.Samples.TryGetValue(searchRange, out var val);
                return val;
            }
        }
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
                BRRImporter.WriteSampleToWAVStream("null", "Preview", sample, ms, sampleRate);                
                
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
        private void ExportAll(BRRFile file, string Folder)
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
                    ExportOne(System.IO.Path.Combine(Folder, sample.Name + ".wav"), sample, freq);
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
        private void ExportOne(string FileName, BRRSample Sample, int Freq)
        {
            try
            {
                BRRImporter.WriteSampleToWAV(FileName, "save", Sample, Freq);
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
            ExportAll(file, selectedFolder);
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
                AlwaysAppendDefaultExtension= true,
                CreatePrompt = false,
                EnsureFileExists= false,
                EnsurePathExists = true,
                InitialDirectory = Environment.CurrentDirectory,
                Title = $"Save {sample.Name} at {freq}Hz to?",
                DefaultExtension = ".wav",                
            };
            if (fileDialog.ShowDialog() != CommonFileDialogResult.Ok) return; // OOPSIES
            string selectedFolder = fileDialog.FileName;
            ExportOne(selectedFolder, sample, freq);
        }

        private void CopyItem_Click(object sender, RoutedEventArgs e)
        {
            var sample = SelectedSample;
            if (sample == default) return;
            var file = System.IO.Path.GetTempPath() + $"\\{sample.Name}.wav";
            ExportOne(file, sample, GetSampleRate());
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
