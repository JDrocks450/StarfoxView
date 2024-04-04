using StarFox.Interop;
using System;
using System.Windows;
using System.Windows.Controls;

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
    /// <summary>
    /// Interaction logic for FileImportMenu.xaml
    /// </summary>
    public partial class GenericMenuDialog : Window
    {
        private readonly string[] selections;
        public int Selection { get; private set; } = -1;
        public string SelectedItem => selections[Selection];

        public GenericMenuDialog()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        public GenericMenuDialog(string Caption, string Message, params string[] Selections) : this()
        {
            Title = Caption;
            BlurbText.Text = Message;

            selections = Selections;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            showOptions();
        }

        private void showOptions()
        {
            SelectionMenu.Items.Clear();
            int index = -1;
            foreach (var selection in selections)
            {
                index++;
                var item = new MenuItem()
                {
                    Header = selection,
                    Tag = index
                };
                item.PreviewMouseLeftButtonUp += delegate
                {
                    int selectIndex = (int)item.Tag;
                    Dismiss(selectIndex);
                };
                SelectionMenu.Items.Add(item);
            }
            var citem = new MenuItem()
            {
                Header = "Nevermind"
            };
            citem.PreviewMouseLeftButtonUp += delegate
            {                
                DialogResult = false;
                Close();
            };
            SelectionMenu.Items.Add(citem);

            Activate();
        }

        /// <summary>
        /// Dismiss the window with the specified result
        /// </summary>
        /// <param name="FileType"></param>
        private void Dismiss(int Index)
        {
            DialogResult = true;
            Selection = Index;
            Close();
        }
    }
}
