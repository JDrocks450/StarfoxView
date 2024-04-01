using Microsoft.WindowsAPICodePack.Dialogs;
using Starfox.Editor;
using StarFox.Interop;
using StarFox.Interop.ASM;
using StarFox.Interop.BRR;
using StarFox.Interop.BSP;
using StarFox.Interop.BSP.SHAPE;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.COLTAB;
using StarFox.Interop.GFX.DAT.MSPRITES;
using StarFox.Interop.MAP;
using StarFox.Interop.MSG;
using StarFox.Interop.SPC;
using StarFoxMapVisualizer.Controls;
using StarFoxMapVisualizer.Controls.Subcontrols;
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
    /// Common helper functions for interacting with files in the editor environment
    /// </summary>
    internal static class FILEStandard
    {
        internal static readonly ASMImporter ASMImport = new();
        internal static readonly MAPImporter MAPImport = new();
        internal static readonly BSPImporter BSPImport = new();
        internal static readonly MSGImporter MSGImport = new();
        internal static readonly COLTABImporter COLTImport = new();
        internal static readonly BRRImporter BRRImport = new();
        internal static readonly SPCImporter SPCImport = new();
        internal static readonly MSpritesImporter DEFSPRImport = new();

        /// <summary>
        /// Includes a <see cref="SFCodeProjectFileTypes.Assembly"/>, <see cref="SFCodeProjectFileTypes.Include"/> or 
        /// <see cref="SFCodeProjectFileTypes.Palette"/>.
        /// <para>Note that passing generic type T is optional, since it will return default if it is not a matching type.</para>
        /// <para>Note that unless ContextualFileType is passed and not default, a dialog is displayed asking the user what kind of file this is.</para>
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        public static async Task<T?> IncludeFile<T>(FileInfo File, SFFileType.ASMFileTypes? ContextualFileType = default) where T : class
        {
            if (!AppResources.IsFileIncluded(File))
            {
                switch (File.GetSFFileType())
                {
                    case SFCodeProjectFileTypes.Include:
                    case SFCodeProjectFileTypes.Assembly:
                        var asmFile = await ParseFile(File, ContextualFileType);
                        if (asmFile == default) return default; // USER CANCEL                                                              
                        AppResources.Includes.Add(asmFile); // INCLUDE FILE FOR SYMBOL LINKING
                        return asmFile as T;
                    case SFCodeProjectFileTypes.Palette:
                        using (var file = File.OpenRead())
                        {
                            var palette = StarFox.Interop.GFX.CAD.COL.Load(file);
                            if (palette == default) return default;
                            AppResources.ImportedProject.Palettes.Add(File.FullName, palette);
                            return palette as T;
                        }
                }
            }
            else return AppResources.Includes.First(x => x.OriginalFilePath== File.FullName) as T;
            return default;
        }
        public static void IncludeFile(ASMFile asmFile)
        {
            if (!AppResources.IsFileIncluded(new FileInfo(asmFile.OriginalFilePath)))
            {
                //INCLUDE FILE FOR SYMBOL LINKING
                AppResources.Includes.Add(asmFile);
            }
        }
        public static bool SearchProjectForFile(string FileName, out FileInfo? File, bool IgnoreHyphens = false)
        {
            File = null;
            var results = AppResources.ImportedProject.SearchFile(FileName, IgnoreHyphens);
            if (results.Count() == 0) return false;
            if (results.Count() > 1) // ambiguous
                return false;
            File = new FileInfo(results.First().FilePath);
            return true;
        }
        /// <summary>
        /// Common helper function that will set imports on all <see cref="CodeImporter{T}"/>s
        /// to be <see cref="AppResources.Includes"/>
        /// <code>COLTImport, ASMImport, MAPImport, BSPImport and MSGImport</code>        
        /// </summary>
        public static void ReadyImporters()
        {
            COLTImport.SetImports(AppResources.Includes.ToArray());
            ASMImport.SetImports(AppResources.Includes.ToArray());
            MAPImport.SetImports(AppResources.Includes.ToArray());
            BSPImport.SetImports(AppResources.Includes.ToArray());
            MSGImport.SetImports(AppResources.Includes.ToArray());
            DEFSPRImport.SetImports(AppResources.Includes.ToArray());
        }
        private static async Task<bool> HandleImportMessages<T>(FileInfo File, CodeImporter<T> importer) where T : IImporterObject
        {
            async Task AutoIncludeNow(string message, IEnumerable<string> ExpectedIncludes)
            {
                //**AUTO INCLUDE
                List<string> autoIncluded = new List<string>();
                if (!string.IsNullOrWhiteSpace(message))
                { // attempt to silence the warning
                    var includes = ExpectedIncludes;
                    foreach (var include in includes)
                    {
                        if (!SearchProjectForFile(include, out var file)) continue;
                        await IncludeFile<object>(file, SFFileType.ASMFileTypes.ASM);
                        autoIncluded.Add(file.Name);
                    }
                }
                if (autoIncluded.Any())
                    MessageBox.Show($"Auto-Include included these files to the project:\n" +
                        $" {string.Join(", ", autoIncluded)}");
                //** END AUTO INCLUDE
            }
            var message = importer.CheckWarningMessage(File.FullName);
            await AutoIncludeNow(message, importer.ExpectedIncludes);
            ReadyImporters();
            message = importer.CheckWarningMessage(File.FullName);
            if (!string.IsNullOrWhiteSpace(message))
            {
                if (MessageBox.Show(message, "Continue?", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    return false;
            }
            return true;
        }
        public static async Task<bool> TryIncludeColorTable(FileInfo File)
        {
            if (!AppResources.IsFileIncluded(File))
            {
                ReadyImporters();
                if (!await HandleImportMessages(File, COLTImport)) return false;
                var result = await COLTImport.ImportAsync(File.FullName);
                if (result == default) return false;
                AppResources.Includes.Add(result); // INCLUDE FILE FOR SYMBOL LINKING
                var msg = string.Join(Environment.NewLine, result.Groups);
                MessageBox.Show(msg, "Success!", MessageBoxButton.OKCancel);
                if (!AppResources.ImportedProject.Palettes.Any())
                {
                    if (!SearchProjectForFile("night.col", out var file)) return false;
                    await IncludeFile<ASMFile>(file);
                }
            }
            return true;
        }

        public static async Task<CAD.COL?> GetPalette(FileInfo File)
        {
            if (!AppResources.IsFileIncluded(File))
            {
                var success = await IncludeFile<object>(File) != default;
                if (!success) return null;
            }
            return AppResources.ImportedProject.Palettes[File.FullName];
        }

        public static async Task OpenPalette(FileInfo File)
        {
            if (!AppResources.IsFileIncluded(File))
            {
                var success = await IncludeFile<object>(File) != default;
                if (!success) return;
            }
            var col = AppResources.ImportedProject.Palettes[File.FullName];
            PaletteView view = new()
            {
                Owner = Application.Current.MainWindow
            };
            view.SetupControl(col);
            view.ShowDialog();            
        }       
        public static bool CloseFileIfOpen(FileInfo File)
        {
            if (AppResources.OpenFiles.ContainsKey(File.FullName))
            {
                AppResources.ImportedProject.CloseFile(File.FullName);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Opens the specified BSP File, if it's already open, will return the file.
        /// </summary>
        /// <param name="File"></param>
        /// <param name="ForceReload">Forces the library to reload the file from disk.</param>
        /// <returns></returns>
        public static async Task<BSPFile?> OpenBSPFile(FileInfo File, bool ForceReload = false)
        {
            if (AppResources.ImportedProject.OpenFiles.TryGetValue(File.FullName, out var fileInstance))
            { // FILE IS OPEN
                if (fileInstance is BSPFile && !ForceReload) return fileInstance as BSPFile; // RETURN IT
                // WE HAVE TO RELOAD IT, SO CLOSE IT
                CloseFileIfOpen(File);
            }
            //3D IMPORT LOGIC
            if (!await HandleImportMessages(File, BSPImport)) return default;
            //**AUTO-INCLUDE COLTABS.ASM
            if (SearchProjectForFile("coltabs.asm", out var projFile))
                await TryIncludeColorTable(projFile);
            //**
            var file = await BSPImport.ImportAsync(File.FullName);
            return file;
        }
        public static async Task<MAPFile?> OpenMAPFile(FileInfo File)
        {
            //MAP IMPORT LOGIC   
            if (!await HandleImportMessages(File, MAPImport)) return default;
            if (!MAPImport.MapContextsSet)
                await MAPImport.ProcessLevelContexts();
            var rObj = await MAPImport.ImportAsync(File.FullName);
            var errors = MAPImport.ErrorOut.ToString();
            if (!string.IsNullOrWhiteSpace(errors))
                MessageBox.Show(errors + "\nThe file was still imported -- use caution when viewing for inaccuracies.", 
                    "Errors Occured when Importing this File");
            return rObj;
        }
        public static async Task<ASMFile?> OpenMSGFile(FileInfo File)
        {
            //MSG IMPORT LOGIC   
            if (!await HandleImportMessages(File, MSGImport)) return default;
            var rObj = await MSGImport.ImportAsync(File.FullName);
            var errors = MSGImport.ErrorOut.ToString();
            if (!string.IsNullOrWhiteSpace(errors))
                MessageBox.Show(errors + "\nThe file was still imported -- use caution when viewing for inaccuracies.",
                    "Errors Occured when Importing this File");
            return rObj;
        }
        /// <summary>
        /// Will import an *.ASM file into the project's OpenFiles collection and return the parsed result.
        /// <para>NOTE: This function WILL call a dialog to have the user select which kind of file this is. 
        /// This can cause a softlock if this logic is nested with other Parse logic.</para>
        /// <para>To avoid this, please make diligent use of the ContextualFileType parameter.</para>
        /// </summary>
        /// <param name="File">The file to parse.</param>
        /// <param name="ContextualFileType">Will skip the Dialog asking what kind of file this is parsing by using this value
        /// <para>If <see langword="default"/>, the dialog is displayed.</para></param>
        /// <returns></returns>
        private static async Task<ASMFile?> ParseFile(FileInfo File, SFFileType.ASMFileTypes? ContextualFileType = default)
        {                     
            //GET IMPORTS SET
            ReadyImporters();
            //DO FILE PARSE NOW            
            ASMFile? asmfile = default;
            if (File.GetSFFileType() is SFCodeProjectFileTypes.Assembly or
                                        SFCodeProjectFileTypes.Include) // assembly file
            { // DOUBT AS TO FILE TYPE
                //CREATE THE MENU WINDOW
                SFFileType.ASMFileTypes selectFileType = SFFileType.ASMFileTypes.ASM;
                if (!ContextualFileType.HasValue)
                {
                    FileImportMenu importMenu = new()
                    {
                        Owner = Application.Current.MainWindow
                    };
                    if (!importMenu.ShowDialog() ?? true) return default; // USER CANCEL
                    selectFileType = importMenu.FileType;
                }
                else selectFileType = ContextualFileType.Value;
                switch (selectFileType)
                {
                    default: return default;
                    case StarFox.Interop.SFFileType.ASMFileTypes.ASM:
                        goto general;
                    case StarFox.Interop.SFFileType.ASMFileTypes.MAP:
                        asmfile = await OpenMAPFile(File);
                        break;
                    case StarFox.Interop.SFFileType.ASMFileTypes.BSP:
                        asmfile = await OpenBSPFile(File);
                        break;
                    case SFFileType.ASMFileTypes.MSG:
                        asmfile = await OpenMSGFile(File);
                        break;
                    case SFFileType.ASMFileTypes.DEFSPR:
                        asmfile = await OpenDEFSPRFile(File);
                        break;
                }
                goto import;
            }
        general:
            asmfile = await ASMImport.ImportAsync(File.FullName);
        import:
            if (asmfile == default) return default;
            if (!AppResources.OpenFiles.ContainsKey(File.FullName))
                AppResources.OpenFiles.Add(File.FullName, asmfile);
            return asmfile;
        }

        private static async Task<ASMFile?> OpenDEFSPRFile(FileInfo file)
        {
            //DEFSPR IMPORT LOGIC   
            if (!await HandleImportMessages(file, DEFSPRImport)) return default;
            var rObj = await DEFSPRImport.ImportAsync(file.FullName);
            var errors = DEFSPRImport.ErrorOut.ToString();
            if (!string.IsNullOrWhiteSpace(errors))
                MessageBox.Show(errors + "\nThe file was still imported -- use caution when viewing for inaccuracies.",
                    "Errors Occured when Importing this File");
            return rObj;
        }

        internal static async Task<ASMFile?> OpenASMFile(FileInfo File, bool IgnoreDialogs = false)
        {
            //DO FILE PARSE NOW            
            return await ParseFile(File, IgnoreDialogs ? SFFileType.ASMFileTypes.ASM : null);            
        }

        /// <summary>
        /// Spawns a new <see cref="CommonFileDialog"/> with the desired options
        /// </summary>
        /// <param name="Title"></param>
        /// <param name="FolderBrowser"></param>
        /// <param name="InitialDirectory"></param>
        /// <param name="Multiselect"></param>
        /// <returns></returns>
        internal static string? ShowGenericFileBrowser(string Title, bool FolderBrowser = false, string? InitialDirectory = null, bool Multiselect = false)
        {
            if (InitialDirectory == default) InitialDirectory = AppResources.ImportedProject.WorkspaceDirectory.FullName;
            CommonOpenFileDialog d = new CommonOpenFileDialog()
            {
                Title = Title,
                IsFolderPicker = FolderBrowser,
                Multiselect = Multiselect,
                InitialDirectory = InitialDirectory
            }; // CREATE THE FOLDER PICKER
            if (d.ShowDialog() is not CommonFileDialogResult.Ok) return default; // OOPSIES x2
            var directory = d.FileName; // Selected DIR
            if (!Directory.Exists(directory)) return default; // Random error?
            return directory;
        }
        /// <summary>
        /// Attempts to load the given <see cref="SFOptimizerNode"/> for the given type
        /// <para/>Upon failure, will throw an exception
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal static SFOptimizerNode GetOptimizerByType(SFOptimizerTypeSpecifiers Type)
        {
            var project = AppResources.ImportedProject;
            //Load the SFOptimizer
            if (!project.Optimizers.Any())
                throw new Exception("There aren't any optimizers added to this project yet.\n" +
                    "Use the Refresh <Type>Map button to create this.");
            //Find the one that is a STAGE MAP
            var sfOptim = project.GetOptimizerByTypeOrDefault(Type);
            if (sfOptim == default)
                throw new Exception($"This project has Optimizers, but none of them are for {Type}.\n" +
                    "Use the Refresh <Type>Map button to create this.");
            return sfOptim;
        }

        /// <summary>
        /// This uses an SFOptimizer in the node that stores Levels to map Level Macro Name to the File it appears in.
        /// <para/>This will look up the level by it's name as it appears in it's header.
        /// </summary>
        /// <returns></returns>
        internal static async Task<MAPScript?> GetMapScriptByMacroName(string LevelMacroName)
        {
            var HeaderName = LevelMacroName;
            var stageOptim = GetOptimizerByType(SFOptimizerTypeSpecifiers.Levels);
            var stageMap = stageOptim.OptimizerData.ObjectMap;
            //Try to find the file that contains the stage we want
            if (!stageMap.TryGetValue(HeaderName, out var FileName)) return default;
            var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(stageOptim.FilePath), FileName);
            //Open the file
            var file = await OpenMAPFile(new FileInfo(path));
            if (file.Scripts.TryGetValue(LevelMacroName, out var script))
                return script;
            return default;
        }
    }
}
