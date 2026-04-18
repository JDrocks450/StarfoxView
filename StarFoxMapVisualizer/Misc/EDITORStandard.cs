using Starfox.Editor;
using StarFox.Interop.ASM;
using StarFox.Interop.GFX.DAT.MSPRITES;
using StarFox.Interop.MAP.EVT;
using StarFoxMapVisualizer.Controls;
using StarFoxMapVisualizer.Controls.Subcontrols;
using StarFoxMapVisualizer.Dialogs;
using StarFoxMapVisualizer.Screens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StarFoxMapVisualizer.Misc
{
    /// <summary>
    /// Common helper functions for creating an interactive editor environment
    /// </summary>
    internal static class EDITORStandard
    {
        /// <summary>
        /// There should only ever be one instance of the Edit screen at any given time
        /// </summary>
        internal static EditScreen? CurrentEditorScreen { get; set; }
        private static LoadingWindow? _loadingWindow;
        private static bool WelcomeWagonShownOnce = false;

        /// <summary>
        /// Shows a Loading... Window in the middle of the Window.
        /// <para/> If a EditScreen is created, it will be hidden using the <see cref="DimEditorScreen"/> method
        /// </summary>
        public static void ShowLoadingWindow()
        {
            DimEditorScreen();
            if (_loadingWindow == null)
                _loadingWindow = new LoadingWindow()
                {
                    Owner = Application.Current.MainWindow
                };
            _loadingWindow.Show();
        }
        /// <summary>
        /// Hides the <see cref="ShowLoadingWindow"/> window
        /// </summary>
        public static void HideLoadingWindow()
        {
            UndimEditorScreen();
            if (_loadingWindow == null) return;
            _loadingWindow?.Hide();
        }
        /// <summary>
        /// If an <see cref="EditScreen"/> is added to this program, this will dim it.
        /// </summary>
        public static void DimEditorScreen()
        {
            if (CurrentEditorScreen != default)
                CurrentEditorScreen.LoadingSpan.Visibility = System.Windows.Visibility.Visible;
        }
        /// <summary>
        /// See: <see cref="DimEditorScreen"/>
        /// </summary>
        public static void UndimEditorScreen()
        {
            if (CurrentEditorScreen != default)
                CurrentEditorScreen.LoadingSpan.Visibility = System.Windows.Visibility.Collapsed;
        }

        /// <summary>
        /// Changes the interface to be on the Editor passed as an argument
        /// </summary>
        public static async Task SwitchEditorView(EditScreen.ViewMode View) => 
            await CurrentEditorScreen.SwitchView(View);
        /// <summary>
        /// See: <see cref="SHAPEControl.ShowShape(string, int)"/>
        /// </summary>
        /// <param name="ShapeName"></param>
        /// <param name="Frame"></param>
        /// <returns></returns>
        public static async Task<bool> ShapeEditor_ShowShapeByName(string ShapeName, int Frame = -1)
        {
            if (!await CurrentEditorScreen.OBJViewer.ShowShape(ShapeName, Frame))
                return false;
            await SwitchEditorView(EditScreen.ViewMode.OBJ);
            return true;
        }
        /// <summary>
        /// This function is used when the user selects a MapNode in the MAPControl.
        /// <para/>Map Nodes can represent many types of information, using the <paramref name="ComponentSelected"/>
        /// can narrow down what the user actually meant to select to get more info on.
        /// <para/><paramref name="ComponentSelected"/> being null indicates it's unclear what they meant to select
        /// and the most generic action should be taken
        /// </summary>
        /// <param name="MapEvent"></param>
        /// <param name="ComponentSelected"></param>
        /// <returns></returns>
        public static Task<bool> MapEditor_MapNodeSelected(MAPEvent MapEvent, Type? Component) => 
            CurrentEditorScreen.MAPViewer.MapNodeSelected(MapEvent, Component);
        /// <summary>
        /// Opens the ASMViewer in the editor to the passed <see cref="ASMChunk"/>
        /// </summary>
        /// <param name="Symbol"></param>
        /// <returns></returns>
        public static async Task AsmEditor_OpenSymbol(ASMChunk Symbol)
        {
            await SwitchEditorView(EditScreen.ViewMode.ASM);
            await CurrentEditorScreen.ASMViewer.OpenSymbol(Symbol);            
        }

        private static async Task<SFOptimizerDataStruct?> Editor_BaseDoRefreshMap(SFOptimizerTypeSpecifiers Type, 
            Func<FileInfo,Dictionary<string,string>, Task<bool>> ProcessFunction, string? InitialDirectory = default, string? KeyFile = default)
        {
            retry:
            var FilesSelected = FILEStandard.ShowGenericFileBrowser($"Select ALL of your {Type.ToString().ToUpper()} Files", false, InitialDirectory, true);
            if (FilesSelected == default) return default; // User Cancelled
            StringBuilder errorBuilder = new(); // ERRORS
            if (!FilesSelected.Any()) return default;
            var dirInfo = System.IO.Path.GetDirectoryName(FilesSelected.First());
            if (dirInfo == null || !Directory.Exists(dirInfo)) return default;
            //TEST SOMETHING OUT
            if (!FilesSelected.Select(x => System.IO.Path.GetFileName(x).ToLower()).Contains(KeyFile.ToLower()))
            {
                if (MessageBox.Show("It looks like the directory you selected doesn't have at least " +
                    $"a {KeyFile.ToUpper()} file in it. Have you selected the {Type.ToString().ToUpper()} directory in your workspace?\n" +
                    "\n" +
                    "Would you like to continue anyway? No will go back to file selection.", "Directory Selection Message",
                    MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    goto retry;
                }
            }
            //GET IMPORTS SET
            FILEStandard.ReadyImporters();
            Dictionary<string, string> shapesMap = new();
            foreach (var file in FilesSelected.Select(x => new FileInfo(x))) // ITERATE OVER DIR FILES
            {
                try
                {                   
                    bool result = await ProcessFunction(file, shapesMap);
                    if (!result) break;
                }
                catch (Exception ex)
                {
                    errorBuilder.AppendLine($"The file: {file.FullName} could not be exported.\n" +
                        $"***\n{ex.ToString()}\n***"); // ERROR INFO
                }
            }
            return new SFOptimizerDataStruct(Type, dirInfo, shapesMap)
            {
                ErrorOut = errorBuilder
            };
        }

        /// <summary>
        /// Prompts the user to export all 3D models and will export them
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static async Task Editor_ExportAll3DShapes()
        {
            // EXPORT 3D FUNCTION -- I MADE HISTORY HERE TODAY. 11:53PM 03/31/2023 JEREMY GLAZEBROOK.
            // I MADE A GUI PROGRAM THAT EXTRACTED STARFOX SHAPES SUCCESSFULLY AND DUMPED THEM ALL IN READABLE FORMAT.
            var r = MessageBox.Show($"Welcome to the Export 3D Assets Wizard!\n" +
                $"This tool will do the following: Export all 3D assets from the selected directory to *.sfshape files and palettes.\n" +
                $"It will dump them to the exports/models directory.\n" +
                $"You will get a manifest of all files dumped with their model names as well.\n" +
                $"Happy hacking! - Love Bisquick <3", "Export 3D Assets Wizard", MessageBoxButton.OKCancel); // WELCOME MSG
            if (r is MessageBoxResult.Cancel) return; // OOPSIES!

            var FilesSelected = FILEStandard.ShowGenericFileBrowser($"Select ALL of your SHAPES Files", false, null, true);
            if (FilesSelected == default) return; // User Cancelled
            if (!FilesSelected.Any()) return;

            EDITORStandard.ShowLoadingWindow();
            StringBuilder errorBuilder = new(); // ERRORS
            StringBuilder exportedBSPs = new(); // BSPS
            StringBuilder exportedFiles = new(); // ALL FILES
            //GET IMPORTS SET
            FILEStandard.ReadyImporters();
            foreach (var file in FilesSelected.Select(x => new FileInfo(x))) // ITERATE OVER DIR FILES
            {
                try
                {
                    var bspFile = await FILEStandard.OpenBSPFile(file); // IMPORT THE BSP
                    foreach (var shape in bspFile.Shapes) // FIND ALL SHAPES
                    {
                        var files = await SHAPEStandard.ExportShapeToSfShape(shape); // USE STANDARD EXPORT FUNC
                        if (files.Count() == 0) continue; // HUH, WEIRD?
                        foreach (var eFile in files) // EXPORTED FILES
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
            HideLoadingWindow();
        }

        /// <summary>
        /// Refreshes the map that is provided using the <paramref name="Type"/> parameter
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        internal static async Task<SFOptimizerNode?> Editor_RefreshMap(SFOptimizerTypeSpecifiers Type, string? InitialDirectory = null)
        {
            async Task<bool> GetShapeMap(FileInfo File, Dictionary<string, string> Map)
            {
                Dictionary<string, string> shapeMap = Map;
                var bspFile = await FILEStandard.OpenBSPFile(File); // IMPORT THE BSP
                foreach (var shape in bspFile.Shapes)
                {
                    var sName = shape.Header.Name.ToUpper();
                    var fooSName = sName;
                    int tries = 1;
                    while (shapeMap.ContainsKey(fooSName))
                    {
                        fooSName = sName + "_" + tries;
                        tries++;
                    }
                    sName = fooSName;
                    shapeMap.Add(sName, File.Name);
                }
                int delta = bspFile.ShapeHeaderEntries.Count - bspFile.Shapes.Count;
                return true;
            }
            async Task<bool> GetLevelMap(FileInfo File, Dictionary<string, string> Map)
            {
                Dictionary<string, string> stageMap = Map;
                var mapFile = await FILEStandard.OpenMAPFile(File); // IMPORT THE MAP
                foreach (var level in mapFile.Scripts)
                {
                    var sName = level.Key;
                    stageMap.TryAdd(sName, File.Name);
                }
                return true;
            }
            async Task<bool> GetMSpriteMap(FileInfo File, Dictionary<string, string> Map)
            {
                string ext = File.Extension.ToUpper();
                if (ext == ".BIN" || ext == ".DAT")
                    Map.Add(File.FullName, "");
                return true;
                var defSpr = await FILEStandard.OpenDEFSPRFile(File, true);
                if (defSpr == default) return false;
                // IMPORT THE DEFSPR
                foreach (var bank in defSpr.Banks)
                {
                    foreach(var sprite in bank.Value.Sprites)                    
                        Map.Add(sprite.Key, File.Name);                    
                }
                return true;
            }

            if (AppResources.ImportedProject == null) return default;

            SFOptimizerDataStruct? dataStruct = default;
            try
            {
                switch (Type)
                {
                    case SFOptimizerTypeSpecifiers.Shapes:
                        dataStruct = await Editor_BaseDoRefreshMap(Type, GetShapeMap, InitialDirectory, "shapes.asm"); break;
                    case SFOptimizerTypeSpecifiers.Maps:
                        dataStruct = await Editor_BaseDoRefreshMap(Type, GetLevelMap, InitialDirectory, "level1_1.asm"); break;
                    case SFOptimizerTypeSpecifiers.MSprites:
                        dataStruct = await Editor_BaseDoRefreshMap(Type, GetMSpriteMap, InitialDirectory, "tex_01.bin"); break;
                }
            }
            catch (Exception ex)
            {
                AppResources.ShowCrash(ex, false, $"Refreshing the {Type} optimizer");
            }
            if (dataStruct == null) return null;

            if (dataStruct.HasErrors)
                MessageBox.Show($"The following error(s) occured with optimizing that directory.\n{dataStruct.ErrorOut}");

            //Attempt to add to project
            var dirNode = AppResources.ImportedProject.SearchDirectory(Path.GetFileName(dataStruct.DirectoryPath)).FirstOrDefault();
            if (dirNode == null)
            {
                AppResources.ShowCrash(new FileNotFoundException("Couldn't find the node that matches this directory in the Code Project."),
                    false, $"Could not refresh {Type} because the directory it corresponds with isn't in this project.");
                return default;
            }
            var node = dirNode.AddOptimizer(Type.ToString(), dataStruct);
            MessageBox.Show($"The {Type} Code Project Optimizer has been updated with {dataStruct.ObjectMap.Count} items.");
            return node;
        }

        /// <summary>
        /// Opens up the best editor to open an item mapped in the given optimizer type.
        /// </summary>
        /// <param name="OptimizerType">The type of <see cref="SFOptimizerNode"/> this item appears in</param>
        /// <param name="ObjectName">The name of the object in the optimizer: Shapes, Levels, etc.</param>
        /// <exception cref="NotImplementedException"></exception>
        internal static Task<bool> InvokeOptimizerMapItem(SFOptimizerTypeSpecifiers OptimizerType, string ObjectName)
        {
            switch (OptimizerType)
            {
                case SFOptimizerTypeSpecifiers.Shapes:
                    return ShapeEditor_ShowShapeByName(ObjectName);
                case SFOptimizerTypeSpecifiers.Maps:
                default:
                    throw new NotImplementedException("There is no way to handle that item yet.");
            }            
        }

        /// <summary>
        /// Ensures all prerequesites are added to the project
        /// </summary>
        /// <returns>True if any changes were made to the project, false if there are no changes</returns>
        internal static async Task<bool> WelcomeWagon()
        {
            if (WelcomeWagonShownOnce) return false;
            if (AppResources.ImportedProject == null) return false;
            if (AppResources.ImportedProject.EnsureOptimizers(out SFOptimizerTypeSpecifiers[] missing)) return false;
            foreach (var missingType in missing)
            {
                if (MessageBox.Show($"Your project is missing the {missingType}Map optimizer.\n" +
                    $"\nWould you like to add this now?", $"Missing {missingType}Map Optimizer", MessageBoxButton.YesNo)
                    == MessageBoxResult.No)
                    continue;
                await Editor_RefreshMap(missingType);
            }
            WelcomeWagonShownOnce = true;
            return true;
        }

        /// <summary>
        /// Shows a new <see cref="Notification"/> on the <see cref="MainWindow"/> and waits for the old one to expire
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Callback"></param>
        /// <param name="Lifespan"></param>
        /// <returns></returns>
        internal static async Task ShowNotification(string Text, Action Callback, TimeSpan? Lifespan = default)
        {
            var notif = await Notification.CreateAsync(Text, Lifespan ?? TimeSpan.FromSeconds(2.5), Callback);
            (Application.Current.MainWindow as MainWindow).PushNotification(notif);
        }
    }
}
