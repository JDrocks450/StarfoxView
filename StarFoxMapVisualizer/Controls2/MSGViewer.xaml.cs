using StarFox.Interop.MSG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using static StarFoxMapVisualizer.Controls2.CommunicationMessageControl;

namespace StarFoxMapVisualizer.Controls2
{
    /// <summary>
    /// Interaction logic for MSGViewer.xaml
    /// </summary>
    public partial class MSGViewer : UserControl
    {
        private Timer animationTimer;
        private Dictionary<string, IEnumerable<MSGEntry>> fileMap = new();
        public string SelectedFileName { get; private set; }
        private Characters CurrentSpeaker = Characters.FOX;
        MSGEntry currentMessage;

        public MSGViewer()
        {
            InitializeComponent();
            Loaded += MSGViewer_Loaded;
        }
        /// <summary>
        /// Fox prompts the user with instructions
        /// </summary>
        /// <param name="Prompt"></param>
        /// <returns></returns>
        private DispatcherOperation ClearUIMessages(string Prompt = "select a file!!")
        {
            return Dispatcher.InvokeAsync(delegate
            {
                MugshotControl.Content = Prompt;
            });
        }
        private async void MSGViewer_Loaded(object sender, RoutedEventArgs e)
        {
            InvokeAnimation();
        }
        /// <summary>
        /// Refreshes the files included in this view
        /// </summary>
        public async Task RefreshFiles()
        {
            MessagesItemsHost.Children.Clear();
            await ClearUIMessages(); // fox prompts to select a file!!
            fileMap.Clear();
            foreach(MSGFile messages in AppResources.OpenFiles.Values.OfType<MSGFile>())            
                fileMap.Add(System.IO.Path.GetFileNameWithoutExtension(messages.OriginalFilePath),
                    messages.Entries.Values);
            RefreshUI();
        }

        private void RefreshUI()
        {
            FilesCombo.SelectionChanged -= SourceFileChanged;
            FilesCombo.Items.Clear();
            foreach(var file in fileMap)            
                FilesCombo.Items.Add(file.Key);
            FilesCombo.SelectionChanged += SourceFileChanged;
            if (FilesCombo.Items.Count > 0)
                FilesCombo.SelectedIndex = 0; // Invokes SourceFileChanged here to be the first the file
            else FilesCombo.SelectedIndex = -1;
        }

        private async void SourceFileChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedFileName = (string)FilesCombo.SelectedItem;
            if (!fileMap.ContainsKey(SelectedFileName)) return; // YIKES -- the file isn't in our collection because RefreshFiles hasn't been called
            //**REFRESH UI MESSAGES
            await ClearUIMessages("pick a message!!"); // have fox prompt the user to pick a message
            MessagesItemsHost.Children.Clear();
            Dictionary<string, ListBox> personToListBoxMap = new();
            void AddMessage(MSGEntry Entry)
            {
                if (!personToListBoxMap.TryGetValue(Entry.Speaker, out ListBox listBox))
                { // we haven't created UI containers for this person yet
                    listBox = new(); // make a message list
                    listBox.SelectionChanged += MessageChanged;
                    HeaderedContentControl itemHost = new()
                    { // make a container for the message list
                        Header = Entry.Speaker,
                        Content = listBox
                    };
                    MessagesItemsHost.Children.Add(itemHost); // add the host
                    personToListBoxMap.Add(Entry.Speaker, listBox);
                }
                int items = listBox.Items.Count + 1;
                string cStr = $"{items}: {Entry.English}";
                listBox.Items.Add(new ListBoxItem()
                {
                    Content = cStr.Substring(0,Math.Min(cStr.Length, 25)),
                    Tag = Entry
                });
            }
            foreach (var message in fileMap[SelectedFileName])
            {
                if (message == default) continue;
                AddMessage(message);
            }
        }

        private void MessageChanged(object sender, SelectionChangedEventArgs e)
        {
            var messageEntry = (((ListBox)sender).SelectedItem as ListBoxItem).Tag as MSGEntry;
            MessageChanged(messageEntry);
        }
        private async void MessageChanged(MSGEntry Entry)
        {
            if (Entry == null)
            {
                //**REFRESH UI MESSAGES
                await ClearUIMessages("pick a message!!"); // have fox prompt the user to pick a message
                return;
            }
            var messageEntry = currentMessage = Entry;
            MugshotControl.Content = EnglishButton.IsChecked ?? false ? messageEntry.English : messageEntry.SecondaryLanguage;
            CurrentSpeaker = MapSpeakerToCharacter(messageEntry.Speaker);
            SoundLabel.Text = messageEntry.Sound;
            InvokeAnimation();
        }

        private DispatcherOperation RedrawMugshot(int frame)
        {
            return Dispatcher.InvokeAsync(delegate
            {
                MugshotControl.DrawMugshot(CurrentSpeaker, frame);
            });
        }

        private void InvokeAnimation()
        {
            int dueTime = 50;
            int loops = 0;
            int maxLoops = 31;            
            async void Callback(object? state)
            {
                if (animationTimer == null) return;
                loops++;
                if (loops >= maxLoops) // we've met the max animations, lets close up
                {
                    animationTimer.Dispose(); // get rid of it
                    animationTimer = null;
                    return;
                }
                int frame = loops % 2; // pick frame of animation based on if animation timer is even
                await RedrawMugshot(frame);
            }
            if (animationTimer == null)
                animationTimer = new Timer(Callback, null, dueTime, dueTime); // create timer since none exists rn
            else loops = 0; // reset animation again
        }

        private void EnglishButton_Checked(object sender, RoutedEventArgs e)
        {
            MessageChanged(currentMessage);
        }
    }
}
