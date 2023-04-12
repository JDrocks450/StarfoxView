using Microsoft.WindowsAPICodePack.Dialogs;
using Starfox.Editor;
using StarFox.Interop;
using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.BSP;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.COLTAB;
using StarFox.Interop.MAP;
using StarFoxMapVisualizer.Controls;
using StarFoxMapVisualizer.Controls.Subcontrols;
using StarFoxMapVisualizer.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace StarFoxMapVisualizer.Screens
{
    /// <summary>
    /// Interaction logic for EditScreen.xaml
    /// </summary>
    public partial class EditScreen : Page
    {
        public enum ViewMode
        {
            NONE,ASM,MAP,OBJ,
            GFX
        }        

        internal static EditScreen Current { get; private set; }
        public ViewMode CurrentMode { get; set; }

        public EditScreen()
        {
            InitializeComponent();

            SolutionExplorerView.Items.Clear();
            MacroExplorerView.Items.Clear();

            Current = this;

            Loaded += OnLoad;
        }

        private async void OnLoad(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.NONE;
            await HandleViewModes();
            UpdateInterface();
            LoadingSpan.Visibility = Visibility.Collapsed;
        }

        private void ContextEnableDisable()
        {

        }

        internal async Task ImportCodeProject(bool Flush = false)
        {            
            var currentProject = AppResources.ImportedProject;
            if (currentProject == null)
                throw new InvalidDataException("No project loaded.");
            if (Flush)
                await currentProject.EnumerateAsync();
            var expandedHeaders = new List<string>();
            void CheckNode(in TreeViewItem Node)
            {
                if (Node.IsExpanded && Node.Tag != default && Node.Tag is SFCodeProjectNode sfNode)
                    expandedHeaders.Add(sfNode.FilePath);
                foreach (var treeNode in Node.Items.OfType<TreeViewItem>())
                    CheckNode(in treeNode);
            }
            foreach(var treeNode in SolutionExplorerView.Items.OfType<TreeViewItem>())
                CheckNode(in treeNode);
            var selectedItemHeader = (SolutionExplorerView.SelectedItem as TreeViewItem)?.Tag as SFCodeProjectNode;
            SolutionExplorerView.Items.Clear();
            if (currentProject == null) return;
            void CreateClosableContextMenu(SFCodeProjectNode FileNode, in ContextMenu contextMenu, string Message = "Close File")
            {
                //INCLUDE FILE ITEM
                var importItem = new MenuItem()
                {
                    Header = Message
                };
                importItem.Click += async delegate
                {
                    //DO INCLUDE
                    var result = AppResources.ImportedProject.CloseFile(FileNode.FilePath);
                    if (!result)
                    {
                        MessageBox.Show("That file could not be closed at this time.", "File Close Error");
                    }
                    UpdateInterface();
                };
                contextMenu.Items.Add(importItem);
            }
            void CreateINCContextMenu(SFCodeProjectNode FileNode, in ContextMenu contextMenu, string Message = "Include File")
            {
                //INCLUDE FILE ITEM
                var importItem = new MenuItem()
                {
                    Header = Message
                };
                importItem.Click += async delegate
                {
                    //DO INCLUDE
                    var result = await FILEStandard.IncludeFile<object>(new FileInfo(FileNode.FilePath)) != default;
                    if (!result)
                    {
                        MessageBox.Show("That file could not be imported at this time.", "File Include Error");
                    }
                    UpdateInterface();
                };
                contextMenu.Items.Add(importItem);
            }
            void CreateCOLTABContextMenu(SFCodeProjectNode FileNode, in ContextMenu contextMenu, string Message = "Include File as Color Table")
            {
                //INCLUDE FILE ITEM
                var importItem = new MenuItem()
                {
                    Header = Message
                };
                importItem.Click += async delegate
                {
                    //DO INCLUDE
                    var result = await FILEStandard.TryIncludeColorTable(new FileInfo(FileNode.FilePath));
                    if (!result)
                    {
                        MessageBox.Show("That file could not be imported at this time.", "File Include Error");
                    }
                    UpdateInterface();
                };
                contextMenu.Items.Add(importItem);
            }
            async Task<TreeViewItem> AddProjectNode(SFCodeProjectNode Node)
            {
                TreeViewItem node = new()
                {
                    IsExpanded = true,
                    Tag = Node
                };
                //node.SetResourceReference(TreeViewItem.StyleProperty, "ProjectTreeStyle");
                foreach (var child in Node.ChildNodes)
                {
                    switch (child.Type)
                    {
                        case SFCodeProjectNodeTypes.Directory:
                            await AddDirectory(node, child);
                            break;
                        case SFCodeProjectNodeTypes.File:
                            await AddFile(node, child);
                            break;
                    }
                }
                return node;
            }
            async Task AddDirectory(TreeViewItem Parent, SFCodeProjectNode DirNode)
            {
                TreeViewItem thisTreeNode = new()
                {
                    Header = System.IO.Path.GetFileName(DirNode.FilePath),
                    Tag = DirNode
                };
                thisTreeNode.SetResourceReference(TreeViewItem.StyleProperty, "FolderTreeStyle");
                foreach (var child in DirNode.ChildNodes)
                {
                    switch (child.Type)
                    {
                        case SFCodeProjectNodeTypes.Directory:
                            await AddDirectory(thisTreeNode, child);
                            break;
                        case SFCodeProjectNodeTypes.File:
                            await AddFile(thisTreeNode, child);
                            break;
                    }
                }
                if (expandedHeaders.Contains(DirNode.FilePath)) // was expanded
                    thisTreeNode.IsExpanded = true;
                Parent.Items.Add(thisTreeNode);
            }
            async Task AddFile(TreeViewItem Parent, SFCodeProjectNode FileNode)
            {
                var fileInfo = new FileInfo(FileNode.FilePath);
                var contextMenu = new ContextMenu();
                var item = new TreeViewItem()
                {
                    Header = fileInfo.Name,
                    Tag = FileNode,
                    ContextMenu= contextMenu
                };
                switch (FileNode.RecognizedFileType)
                {
                    case SFCodeProjectFileTypes.Palette:
                        retry:
                        item.SetResourceReference(StyleProperty, "PaletteTreeStyle");
                        if (!AppResources.IsFileIncluded(fileInfo))
                        {
                            if (await FILEStandard.IncludeFile<object>(fileInfo)!=default)
                                goto retry;
                            CreateINCContextMenu(FileNode, in contextMenu);
                            item.Foreground = Brushes.White; // Indicate with white that it isn't included yet
                        }
                        break;
                    case SFCodeProjectFileTypes.SCR:
                        item.SetResourceReference(StyleProperty, "ScreenTreeStyle");
                        if (AppResources.OpenFiles.ContainsKey(fileInfo.FullName))
                            CreateClosableContextMenu(FileNode, in contextMenu);
                        break;
                    case SFCodeProjectFileTypes.CGX:
                        item.SetResourceReference(StyleProperty, "SpriteTreeStyle");
                        if (AppResources.OpenFiles.ContainsKey(fileInfo.FullName))
                            CreateClosableContextMenu(FileNode, in contextMenu);
                        break;
                    case SFCodeProjectFileTypes.Include:
                        if (!AppResources.IsFileIncluded(fileInfo))
                        {
                            item.SetResourceReference(StyleProperty, "FileTreeStyle");
                            CreateINCContextMenu(FileNode, in contextMenu);
                        }
                        else item.SetResourceReference(StyleProperty, "FileImportTreeStyle");
                        break;
                    default:
                        // allow other files to be included under certain circumstances
                        var include = AppResources.ImportedProject?.GetInclude(fileInfo);
                        if (include != default) // file is included
                        {
                            if (include is COLTABFile) // color tables
                                item.SetResourceReference(StyleProperty, "ColorTableTreeStyle");
                            else // all other files
                                item.SetResourceReference(StyleProperty, "FileImportTreeStyle");
                        }
                        else
                        {
                            CreateCOLTABContextMenu(FileNode, in contextMenu);
                            item.SetResourceReference(StyleProperty, "FileTreeStyle");
                        }
                        break;
                }
                item.Selected += async delegate
                {
                    try
                    {
                        await FileSelected(fileInfo);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"The importer responsible for this type of file says: \n" +
                            $"{ex}\n" +
                            $"Sorry!", "Error Loading File");
                        return;
                    }
                    finally
                    {
                        LoadingSpan.Visibility = Visibility.Collapsed;
                    }
                };
                if (selectedItemHeader != default && selectedItemHeader.FilePath == FileNode.FilePath)// was selected
                    item.BringIntoView();
                Parent.Items.Add(item);
            }
            SolutionExplorerView.Items.Add(await AddProjectNode(currentProject.ParentNode));
        }        

        public DispatcherOperation HandleViewModes() => Dispatcher.InvokeAsync(async delegate
        {
            ViewASMButton.Checked -= ViewASMButton_Checked;
            ViewMapButton.Checked -= ViewMapButton_Checked;
            ViewBSTButton.Checked -= ViewBSTButton_Checked;
            ViewGFXButton.Checked -= ViewGFXButton_Checked;
            ViewGFXButton.IsChecked = false;
            ViewASMButton.IsChecked = false;
            ViewMapButton.IsChecked = false;
            ViewBSTButton.IsChecked = false;
            ViewModeHost.Visibility = Visibility.Visible;
            switch (CurrentMode)
            {
                default:
                case ViewMode.NONE:
                    ViewModeHost.Visibility = Visibility.Collapsed;
                    break;
                case ViewMode.ASM:
                    OBJViewer.Pause();
                    await ASMViewer.Unpause();
                    MAPViewer.Pause();
                    ViewModeHost.SelectedItem = ASMTab;
                    ViewASMButton.IsChecked = true;
                    TitleBlock.Text = "Assembly Viewer";
                    break;
                case ViewMode.MAP:
                    OBJViewer.Pause();
                    MAPViewer.Unpause();
                    ASMViewer.Pause();
                    ViewModeHost.SelectedItem = MAPTab;
                    ViewMapButton.IsChecked = true;
                    TitleBlock.Text = "Map Event Node Viewer";
                    break;
                case ViewMode.OBJ:
                    try
                    {
                        OBJViewer.Unpause();
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show($"The Shape Viewer has reported an error: {ex.Message}");
                    }
                    MAPViewer.Pause();
                    ASMViewer.Pause();
                    ViewModeHost.SelectedItem = OBJTab;
                    ViewBSTButton.IsChecked = true;
                    TitleBlock.Text = "Shape Viewer";
                    break;
                case ViewMode.GFX:
                    MAPViewer.Pause();
                    ASMViewer.Pause();
                    OBJViewer.Pause();
                    GFXViewer.RefreshFiles();
                    ViewModeHost.SelectedItem = GFXTab;
                    ViewGFXButton.IsChecked = true;
                    TitleBlock.Text = "Graphics Viewer";
                    break;
            }
            ViewASMButton.Checked += ViewASMButton_Checked;
            ViewMapButton.Checked += ViewMapButton_Checked;
            ViewBSTButton.Checked += ViewBSTButton_Checked;
            ViewGFXButton.Checked += ViewGFXButton_Checked;
        });               

        private async Task FileSelected(FileInfo File)
        {                        
            LoadingSpan.Visibility = Visibility.Visible;            
            //CHECK IF ITS A KNOWN FILE
            switch (File.GetSFFileType())
            { // YEAH?
                case SFCodeProjectFileTypes.Palette: // HANDLE PALETTE
                    await FILEStandard.OpenPalette(File);                    
                    LoadingSpan.Visibility = Visibility.Collapsed;
                    UpdateInterface();
                    return;
                case SFCodeProjectFileTypes.BINFile: // EXTRACT BIN
                    await SFGFXInterface.TranslateDATFile(File.FullName);            
                    LoadingSpan.Visibility = Visibility.Collapsed;
                    UpdateInterface(true); // Files changed!
                    return;
                case SFCodeProjectFileTypes.CCR: // EXTRACT COMPRESSED GRAPHICS
                    {
                        await GFXStandard.ExtractCCR(File);
                        LoadingSpan.Visibility = Visibility.Collapsed;
                        UpdateInterface(true); // Files changed!
                    }
                    return;
                case SFCodeProjectFileTypes.PCR: // EXTRACT COMPRESSED GRAPHICS
                    {
                        await SFGFXInterface.TranslateCompressedPCR(File.FullName);
                        LoadingSpan.Visibility = Visibility.Collapsed;
                        UpdateInterface(true); // Files changed!
                    }
                    return;
                case SFCodeProjectFileTypes.SCR: // screens
                    //OPEN THE SCR FILE
                    await GFXStandard.OpenSCR(File);
                    LoadingSpan.Visibility = Visibility.Collapsed;
                    UpdateInterface();
                    CurrentMode = ViewMode.GFX;
                    await HandleViewModes();
                    return;
                case SFCodeProjectFileTypes.CGX: // graphics
                    //OPEN THE CGX FILE
                    await GFXStandard.OpenCGX(File);
                    LoadingSpan.Visibility = Visibility.Collapsed;
                    UpdateInterface();
                    CurrentMode = ViewMode.GFX;
                    await HandleViewModes();
                    return;
            }
            //SWITCH TO ASM VIEWER IF WE HAVEN'T ALREADY
            CurrentMode = ViewMode.ASM;
            //HANDLE VIEW MODES -- PAUSE / ENABLE VIEW MODE CONTROLS
            await HandleViewModes();
            //DO FILE PARSE NOW            
            var asmfile = await FILEStandard.OpenASMFile(File);
            bool isMap = asmfile is MAPFile;
            bool isObj = asmfile is BSPFile;
            if (asmfile == default)
            {
                LoadingSpan.Visibility = Visibility.Collapsed;
                return;
            }
            // FILE INCLUDE ROUTINE
            if (File.GetSFFileType() is SFCodeProjectFileTypes.Include)
            { // INC files should be included automatically
                FILEStandard.IncludeFile(asmfile);
            }
            // GET DEFAULT ACTION
            if (isMap)
            { // IF THIS IS A MAP -- SWITCH VIEW, INCUR UPDATE. THE MAP VIEW WILL SEE THE NEWLY ADDED FILE
                CurrentMode = ViewMode.MAP;
                await HandleViewModes();
            }
            if (isObj)
            { // IF THIS IS AN OBJ -- SWITCH VIEW, INCUR UPDATE. THE OBJ VIEW WILL SEE THE NEWLY ADDED FILE
                CurrentMode = ViewMode.OBJ;
                await HandleViewModes();
            }
            else
            {
                //ENQUEUE THIS FILE TO BE OPENED BY THE ASM VIEWER
                await ASMViewer.OpenFileContents(File, asmfile); // tell the ASMControl to look at the new file            
            }
            UpdateInterface();
            LoadingSpan.Visibility = Visibility.Collapsed;
            MacroFileCombo.SelectedValue = System.IO.Path.GetFileNameWithoutExtension(File.Name);
        }

        private async void UpdateInterface(bool FlushFiles = false)
        {
            //update explorer
            await ImportCodeProject(FlushFiles);
            //UPDATE INCLUDES
            MacroFileCombo.ItemsSource = AppResources.Includes.Select(x => System.IO.Path.GetFileNameWithoutExtension(x.OriginalFilePath));
            //VIEW MODE
            if (CurrentMode is ViewMode.MAP) MAPViewer.InvalidateFiles();
            if (CurrentMode is ViewMode.GFX) GFXViewer.RefreshFiles();
        }

        private void ShowMacrosForFile(ASMFile file)
        {
            void AddSymbol<T>(T symbol) where T : ASMChunk, IASMNamedSymbol
            {
                MacroTooltip tooltip = new();
                tooltip.Attach(symbol);

                ListBoxItem item = new()
                {
                    Content = symbol.Name,
                    ToolTip = new ToolTip()
                    {
                        Background = null,
                        BorderBrush = null,
                        HasDropShadow = true,
                        Content = tooltip
                    },
                    Tag = symbol
                };
                MacroExplorerView.Items.Add(item);
            }
            MacroExplorerView.Items.Clear();
            if (MacroFilterRadio.IsChecked ?? false)
            {
                MacroExplorerView.SetBinding(Control.ForegroundProperty, new Binding("Foreground")
                {
                    Source = MacroFilterRadio,
                });
                var macros = file.Chunks.OfType<ASMMacro>(); // filter all chunks by macros only
                foreach (var macro in macros)
                    AddSymbol(macro);
            }
            else if (DefineFilterRadio.IsChecked ?? false)
            {
                MacroExplorerView.SetBinding(Control.ForegroundProperty, new Binding("Foreground")
                {
                    Source = DefineFilterRadio,
                });
                var defines = file.Constants; // constants are kept separate
                foreach (var define in defines)
                    if (define != default)
                        AddSymbol(define);
            }
        }

        private void FilterFileChanged(object sender, SelectionChangedEventArgs e)
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

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            FilterFileChanged(sender, null);
        }

        private void ViewMapButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.MAP;
            HandleViewModes();
            UpdateInterface();
        }
        private void ViewASMButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.ASM;
            HandleViewModes();
            UpdateInterface();
        }
        private void ViewBSTButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.OBJ;
            HandleViewModes();
            UpdateInterface();
        }

        private void ViewGFXButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.GFX;
            HandleViewModes();
            UpdateInterface();
        }

        private DirectoryInfo? Generic_SelectShapeDirectory()
        {
            if (AppResources.ImportedProject.ShapesDirectoryPathSet)
                return new DirectoryInfo(AppResources.ImportedProject.ShapesDirectoryPath);
            CommonOpenFileDialog d = new CommonOpenFileDialog()
            {
                Title = "Select a Directory that contains Shape files",
                IsFolderPicker = true,
                Multiselect = false,
                InitialDirectory = AppResources.ImportedProject.WorkspaceDirectory.FullName
            }; // CREATE THE FOLDER PICKER
            if (d.ShowDialog() is not CommonFileDialogResult.Ok) return default; // OOPSIES x2
            var directory = d.FileName; // Selected DIR
            if (!Directory.Exists(directory)) return default; // Random error?
            AppResources.ImportedProject.ShapesDirectoryPath= directory;
            return new DirectoryInfo(directory);
        }

        private async void ExportAll3DButton_Click(object sender, RoutedEventArgs e)
        {
            // EXPORT 3D FUNCTION -- I MADE HISTORY HERE TODAY. 11:53PM 03/31/2023 JEREMY GLAZEBROOK.
            // I EXTRACTED STARFOX SHAPES SUCCESSFULLY AND DUMPED THEM ALL IN READABLE FORMAT.
            var r = MessageBox.Show($"Welcome to the Export 3D Assets Wizard!\n" +
                $"This tool will do the following: Export all 3D assets from the selected directory to *.sfshape files and palettes.\n" +
                $"It will dump them to the exports/models directory.\n" +
                $"You will get a manifest of all files dumped with their model names as well.\n" +
                $"Happy hacking! - Love Bisquick <3", "Export 3D Assets Wizard", MessageBoxButton.OKCancel); // WELCOME MSG
            if (r is MessageBoxResult.Cancel) return; // OOPSIES!
            var dirInfo = Generic_SelectShapeDirectory();
            if (dirInfo == default) return; // OOPSIES!
            LoadingSpan.Visibility = Visibility.Visible;
            StringBuilder errorBuilder = new(); // ERRORS
            StringBuilder exportedBSPs = new(); // BSPS
            StringBuilder exportedFiles = new(); // ALL FILES
            //GET IMPORTS SET
            FILEStandard.ReadyImporters();
            foreach (var file in dirInfo.GetFiles()) // ITERATE OVER DIR FILES
            {
                try
                {
                    var bspFile = await FILEStandard.OpenBSPFile(file); // IMPORT THE BSP
                    foreach (var shape in bspFile.Shapes) // FIND ALL SHAPES
                    {
                        var files =await SHAPEStandard.ExportShapeToSfShape(shape); // USE STANDARD EXPORT FUNC
                        if (files.Count() == 0) continue; // HUH, WEIRD?
                        foreach(var eFile in files) // EXPORTED FILES
                            exportedFiles.AppendLine(eFile);
                        var bspFileAddr = files.ElementAt(0);
                        exportedBSPs.AppendLine(bspFileAddr); // BSP ONLY
                    }
                }
                catch (Exception ex)
                {
                    errorBuilder.AppendLine($"The file: {file.FullName} could not be exported.\n" +
                        $"***\n{ex.ToString()}\n***"); // ERROR INFO
                }                
            }
            //CREATE THE MANIFEST FILE
            File.WriteAllText(System.IO.Path.Combine(SHAPEStandard.DefaultShapeExtractionDirectory, "manifest.txt"), exportedBSPs.ToString());
            MessageBox.Show($"{exportedFiles}", "Exported Files");
            if (!string.IsNullOrWhiteSpace(errorBuilder.ToString()))
                MessageBox.Show($"{errorBuilder}", "Errors");
            if (MessageBox.Show($"Files exported to:\n" +
                $"{SHAPEStandard.DefaultShapeExtractionDirectory}\n" +
                $"Do you want to copy its location to the clipboard?", "Complete",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                Clipboard.SetText(SHAPEStandard.DefaultShapeExtractionDirectory);
            LoadingSpan.Visibility = Visibility.Collapsed;
        }

        private async void SHAPEMapRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            //This function will create a SHAPE Map -- a file that links SHAPESX.ASM files to Shape Names
            var dirInfo = Generic_SelectShapeDirectory();
            if (dirInfo == default) return; // User Cancelled
            StringBuilder errorBuilder = new(); // ERRORS
            Dictionary<string, string> shapeMap = new();
            //GET IMPORTS SET
            FILEStandard.ReadyImporters();
            foreach (var file in dirInfo.GetFiles()) // ITERATE OVER DIR FILES
            {
                try
                {
                    var bspFile = await FILEStandard.OpenBSPFile(file); // IMPORT THE BSP
                    foreach(var shape in bspFile.Shapes)
                    {
                        var sName = shape.Header.UniqueName;
                        var fooSName = sName;
                        int tries = 1;
                        while (shapeMap.ContainsKey(fooSName))
                        {
                            fooSName = sName + "_" + tries;
                            tries++;
                        }
                        sName = fooSName.ToUpper();
                        shapeMap.Add(sName, file.Name);
                    }
                }
                catch (Exception ex)
                {
                    errorBuilder.AppendLine($"The file: {file.FullName} could not be exported.\n" +
                        $"***\n{ex.ToString()}\n***"); // ERROR INFO
                }
            }
            var dirNode = AppResources.ImportedProject.SearchDirectory(dirInfo.Name).FirstOrDefault();
            if (dirNode == default)
                throw new FileNotFoundException("Couldn't find the node that matches this directory in the Code Project.");
            dirNode.AddOptimizer("ShapesMap", new SFOptimizerDataStruct(
                SFOptimizerTypeSpecifiers.Shapes, shapeMap));
            MessageBox.Show("The ShapesMap Code Project Optimizer has been updated.");
            UpdateInterface(true); // files updated!
        }

        private void BGSASMViewerButton_Click(object sender, RoutedEventArgs e)
        {
            var file = FILEStandard.MAPImport?.LoadedContextDefinitions;
            if (file == null)
            {
                MessageBox.Show("Level contexts have not been loaded yet. Open a level file to have this information populated.");
                return;
            }
            LevelContextViewer viewer = new LevelContextViewer(file)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };
            viewer.Show();
        }
    }
}
