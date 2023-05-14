using StarFox.Interop.Audio.ABIN;
using StarFox.Interop.BRR;
using StarFox.Interop.BSP;
using StarFox.Interop.SPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StarFoxMapVisualizer.Misc
{
    internal static class AUDIOStandard
    {
        /// <summary>
        /// Opens a *.BRR file and adds it to the <see cref="Starfox.Editor.SFCodeProject.Samples"/> collection
        /// <para>If the file has already been loaded once before at runtime, it will return the cached version.</para>
        /// <para>If <paramref name="ReloadFromDisk"/> is <see langword="true"/> then the cached version will be deleted and reloaded.</para>
        /// </summary>
        /// <param name="File">The filename of the BRR file to import</param>
        /// <param name="ShowWarnings">If <see langword="true"/> then a warning will appear if the loaded Sample file has no samples inside.</param>
        /// <returns>The Imported Samples File</returns>
        public static async Task<BRRFile> OpenBRR(FileInfo File, bool ShowWarnings = true, bool ReloadFromDisk = false)
        {
            //set samples dictionary
            var samples = AppResources.ImportedProject.Samples;
            //TRY LOADING FROM CACHE FIRST            
            if (samples.TryGetValue(File.FullName, out BRRFile BRRFile))
            {
                if (ReloadFromDisk) // WE'RE FORCED TO RELOAD IT!
                    samples.Remove(File.FullName); // SO REMOVE THE REFERENCE!
                else return BRRFile; // Loaded from cache
            }
            var file = await FILEStandard.BRRImport.ImportAsync(File.FullName);     
            if (ShowWarnings && !file.Effects.Any()) // Huh, no samples! Let's ask the user what they wanna do about this.
            { // Warning Dialog
                if (MessageBox.Show("This file doesn't have any samples in it.\n\n" +
                    "Want to try experimental loading without error checking to find samples?", "No Samples Found", 
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    file = await FILEStandard.BRRImport.ImportAsync(File.FullName, false); // load without error checking
                // If the user cancels, the file with no samples is loaded any way.
            }
            samples.Add(File.FullName, file); // add it to the cache
            return file;
        }
        /// <summary>
        /// Opens the SPC Properties Dialog for the specified *.SPC file.
        /// <para>See: <see cref="SPCImporter.ImportAsync(string)"/> for usage details.</para>
        /// </summary>
        /// <param name="File"></param>
        /// <returns>Nothing.</returns>
        public static async Task OpenSPCProperties(FileInfo File)
        {
            var file = await OpenSPC(File);
            Dialogs.SPCInformationDialog dialog = new(file)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
        }
        /// <summary>
        /// This is a macro function to call: <c>FILEStandard.SPCImport.ImportAsync(File.FullName)</c>
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        public static Task<SPCFile> OpenSPC(FileInfo File) => FILEStandard.SPCImport.ImportAsync(File.FullName);
        /// <summary>
        /// Converts a *.BIN file to a *.SPC file.
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        public static async Task<bool> ConvertBINToSPC(FileInfo File, string SPCFilePath = default)
        {
            //IMPORT THE ABIN FILE FIRST
            ABINImporter import = new();
            //INTERPRET IT INTO AN INSTANCE
            var file = await import.ImportAsync(File.FullName);
            var dir = File.DirectoryName;
            dir = System.IO.Path.Combine(dir, System.IO.Path.GetFileNameWithoutExtension(File.Name));
            //CREATE DIRECTORIES
            Directory.CreateDirectory(dir);

            //EXPORT THE ABIN FILE TO ASM and BIN
            (string asmFilePath, string binFilePath) paths = await ABINExport.ExportToDirectory(dir, file);

            //CONFIRMATION DIALOG
            MessageBox.Show("Review the following information for accuracy.", "Review");
            //EXPORTED BIN FILE DATA            
            var newFileName = File.Name.Replace(System.IO.Path.GetExtension(File.FullName), ".SPC");
            var newPath = System.IO.Path.Combine(dir, newFileName);
            if (SPCFilePath != null) newPath = SPCFilePath;
            //MAKE A SAMPLE SPC FILE
            var spcFile = new SPCFile(newPath)
            {
                DumperName = "Bisquick",
                SongTitle = System.IO.Path.GetFileNameWithoutExtension(newPath),
                GameTitle = "Star Fox",
                Comments = "Dumped using SFView <3",
                DefaultChannelDisables = 0,
                Emulator = 48,
                ID666Included = 26,
                MinorVersion = 30,
                PC = 1132,
                A = 5,
                X = 69,
                Y = 6,
                SP = 207,
                PSW = 9,
            };
            //WRITE THE BIN FILE TO THE SPC FILE
            await SPCImporter.WriteBINtoSPCAsync(spcFile, file, paths.binFilePath, true);
            //AS MANY TIMES AS IT TAKES TO GET IT RIGHT ...
            while (true)
            {
                Dialogs.SPCInformationDialog dialog = new(spcFile)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow
                };
                if (!dialog.ShowDialog() ?? true)
                    return false; // USER CANCELLED!
                //TRY TO WRITE THE DATA
                using (var fs = new FileStream(newPath, FileMode.Create))
                    if (await baseWriteToStream(spcFile, fs)) break; // UPON FAILURE ... TRY AGAIN!
            }
            return true;
        }
        /// <summary>
        /// Writes the SPCFile to a stream
        /// </summary>
        /// <param name="spcFile"></param>
        /// <param name="fs"></param>
        /// <returns></returns>
        private static async Task<bool> baseWriteToStream(SPCFile spcFile, Stream fs)
        {
            try
            {
                if (spcFile == null) throw new ArgumentNullException("SPCFile was supplied as NULL.");
                await SPCImporter.WriteAsync(spcFile, fs);
                return true;
            }
            catch (Exception Ex)
            {
                MessageBox.Show($"An error occured when exporting the SPC.\n" +
                    $"{Ex.Message}\n" +
                    $"\nLet's review the SPC info again to make sure it's correct." +
                    $"\nHaving trouble? Pressing 'Cancel' on the Properties window will exit this process.");
            }
            return false;
        }
    }
}
