using StarFox.Interop.MAP;
using StarFox.Interop.MAP.CONTEXT;
using StarFoxMapVisualizer.Misc;
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
using System.Windows.Shapes;

namespace StarFoxMapVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for LevelContextViewer.xaml
    /// </summary>
    public partial class LevelContextViewer : Window
    {
        /// <summary>
        /// This event is raised when the "Use as Level Context" button is pressed.
        /// <para>You should update the preview of the level context with this new selection when this event is raised.</para>
        /// </summary>
        public event EventHandler<MAPContextDefinition> EditorPreviewSelectionChanged;

        public LevelContextViewer()
        {
            InitializeComponent();
            ViewBar.Visibility = Visibility.Collapsed;
        }
        public LevelContextViewer(MAPContextDefinition? levelContext) : this()
        {
            if (levelContext != null)
                Loaded += async delegate { 
                    await Attach(levelContext); 
                };
        }
        public LevelContextViewer(MAPContextFile ContextFile) : this()
        {
            if (ContextFile != default)
                Loaded += async delegate {
                    await AttachMany(ContextFile);
                }; 
        }
        public LevelContextViewer(params MAPContextDefinition[] Contexts) : this()
        {
            Loaded += async delegate {
                await AttachMany(Contexts);
            };
        }
        public MAPContextDefinition? SelectedLevelContext { get; private set; }
        public MAPContextFile? SelectedFile { get; private set; }
        private MAPContextDefinition ViewSwitcherSelectionAsContext => (MAPContextDefinition)ViewSwitcher.SelectedItem;

        public async Task Attach(MAPContextDefinition levelContext, bool ExtractCCR = false, bool ExtractPCR = false)
        {
            IsEnabled = false;
            SelectedLevelContext = levelContext;
            if (SelectedLevelContext == null)
            {
                IsEnabled= true;
                return;
            }
            Title = SelectedLevelContext.MapInitName;
            await LevelViewerControl.Attach(levelContext, ExtractCCR, ExtractPCR);
            IsEnabled = true;
        }
        public async Task AttachMany(MAPContextFile contextFile)
        {
            SelectedFile = contextFile;
            if (SelectedFile == null || !SelectedFile.Definitions.Any()) return;
            await AttachMany(contextFile.Definitions.Values.ToArray());
        }
        public async Task AttachMany(params MAPContextDefinition[] Contexts)
        {
            ViewBar.Visibility = Visibility.Collapsed;
            if (Contexts.Length > 1)
            {
                ViewBar.Visibility = Visibility.Visible;
                ViewSwitcher.SelectionChanged -= ChangeDefinition;
                ViewSwitcher.ItemsSource = Contexts;
                ViewSwitcher.SelectionChanged += ChangeDefinition;
                if (ViewSwitcher.HasItems)
                    ViewSwitcher.SelectedIndex = 1;
            }
            else if (Contexts.Length == 1)
                await Attach(Contexts[0]);
            else return;
        }

        private async void ChangeDefinition(object sender, SelectionChangedEventArgs e)
        {
            await Attach(ViewSwitcherSelectionAsContext);
        }

        private async void ReextractButton_Click(object sender, RoutedEventArgs e)
        {
            await Attach(ViewSwitcherSelectionAsContext, true, true);
        }

        private void HOST_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void HOST_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            var image = LevelViewerControl.ImageContent.BG2Render.ImageSource as BitmapImage;
            if (image != null) Clipboard.SetImage(image);
        }

        private void UseAsButton_Click(object sender, RoutedEventArgs e)
        {
            EditorPreviewSelectionChanged?.Invoke(this, SelectedLevelContext);
        }
    }
}
