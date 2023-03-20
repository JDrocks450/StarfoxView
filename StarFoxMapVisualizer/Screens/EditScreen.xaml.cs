using Starfox.Editor;
using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFoxMapVisualizer.Controls;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Xml.Serialization;

namespace StarFoxMapVisualizer.Screens
{
    /// <summary>
    /// Interaction logic for EditScreen.xaml
    /// </summary>
    public partial class EditScreen : Page
    {
        private ASMImporter ASMImport = new();

        public EditScreen()
        {
            InitializeComponent();

            SolutionExplorerView.Items.Clear();
            MacroExplorerView.Items.Clear();

            Loaded += OnLoad;
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            UpdateInterface();
            LoadingSpan.Visibility = Visibility.Collapsed;
        }

        private void Import()
        {
            SolutionExplorerView.Items.Clear();

            var currentProject = AppResources.ImportedProject;
            if (currentProject == null) return;

            TreeViewItem AddProjectNode(SFCodeProjectNode Node)
            {
                TreeViewItem node = new()
                {
                    IsExpanded = true
                };
                //node.SetResourceReference(TreeViewItem.StyleProperty, "ProjectTreeStyle");
                foreach (var child in Node.ChildNodes)
                {
                    switch (child.Type)
                    {
                        case SFCodeProjectNodeTypes.Directory:
                            AddDirectory(in node, child);
                            break;
                        case SFCodeProjectNodeTypes.File:
                            AddFile(in node, child);
                            break;
                    }
                }
                return node;
            }
            void AddDirectory(in TreeViewItem Parent, SFCodeProjectNode DirNode)
            {
                TreeViewItem thisTreeNode = new()
                {
                    Header = System.IO.Path.GetFileName(DirNode.FilePath),
                };
                thisTreeNode.SetResourceReference(TreeViewItem.StyleProperty, "FolderTreeStyle");
                foreach (var child in DirNode.ChildNodes)
                {
                    switch (child.Type)
                    {
                        case SFCodeProjectNodeTypes.Directory:
                            AddDirectory(in thisTreeNode, child);
                            break;
                        case SFCodeProjectNodeTypes.File:
                            AddFile(in thisTreeNode, child);
                            break;
                    }
                }
                Parent.Items.Add(thisTreeNode);
            }
            void AddFile(in TreeViewItem Parent, SFCodeProjectNode FileNode)
            {
                var fileInfo = new FileInfo(FileNode.FilePath);
                var item = new TreeViewItem()
                {
                    Header = fileInfo.Name,
                };
                if (!AppResources.IsFileIncluded(fileInfo))
                    item.SetResourceReference(TreeViewItem.StyleProperty, "FileTreeStyle");
                else item.SetResourceReference(TreeViewItem.StyleProperty, "FileImportTreeStyle");
                item.Selected += async delegate
                {
                    await ASMFileSelected(fileInfo);
                };
                Parent.Items.Add(item);
            }
            SolutionExplorerView.Items.Add(AddProjectNode(currentProject.ParentNode));

        }

        private async Task ASMFileSelected(FileInfo File)
        {            
            LoadingSpan.Visibility = Visibility.Visible;
            //DO FILE PARSE NOW
            ASMImport.SetImports(AppResources.Includes.ToArray());
            var asmfile = await ASMImport.ImportAsync(File.FullName); 
            
            // FILE INCLUDE ROUTINE
            if (File.Extension.ToUpper().EndsWith("INC"))
            { // INC files should be included automatically
                if (!AppResources.IsFileIncluded(File))
                {
                    //INCLUDE FILE FOR SYMBOL LINKING
                    AppResources.Includes.Add(asmfile);
                }
            }
           
            await ASMViewer.OpenFileContents(File, asmfile);
            UpdateInterface();
            LoadingSpan.Visibility = Visibility.Collapsed;
            MacroFileCombo.SelectedValue = System.IO.Path.GetFileNameWithoutExtension(File.Name);
        }

        private void UpdateInterface()
        {
            //update explorer
            Import();
            //UPDATE INCLUDES
            MacroFileCombo.ItemsSource = AppResources.Includes.Select(x => System.IO.Path.GetFileNameWithoutExtension(x.OriginalFilePath));            
        }

        private void ShowMacrosForFile(ASMFile file)
        {
            void AddMacro(ASMMacro macro)
            {
                MacroTooltip tooltip = new();
                tooltip.Attach(macro);

                ListBoxItem item = new()
                {
                    Content = macro.Name,
                    ToolTip = new ToolTip()
                    {
                        Background = null,
                        BorderBrush = null,
                        HasDropShadow = true,
                        Content = tooltip
                    },
                    Tag = macro
                };
                MacroExplorerView.Items.Add(item);
            }
            MacroExplorerView.Items.Clear();
            var macros = file.Chunks.OfType<ASMMacro>();
            foreach(var macro in macros)
                AddMacro(macro);
        }

        private void MacroFileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int newSelection = MacroFileCombo.SelectedIndex;
            if (newSelection < 0) return;
            var file = AppResources.Includes.ElementAtOrDefault(newSelection);
            if (file == null) return;
            ShowMacrosForFile(file);
        }

        private async void MacroExplorerView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MacroExplorerView.SelectedItem == null) return;
            if (((ListBoxItem)MacroExplorerView.SelectedItem).Tag is ASMChunk chunk)
                await ASMViewer.OpenSymbol(chunk);
        }
    }
}
