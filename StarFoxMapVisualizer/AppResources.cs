using Starfox.Editor;
using StarFox.Interop;
using StarFox.Interop.ASM;
using StarFox.Interop.MAP;
using StarFoxMapVisualizer.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace StarFoxMapVisualizer
{
    /// <summary>
    /// Resources that can be accessed throughout all of the User Interface code
    /// </summary>
    internal static class AppResources
    {
        public const string ApplicationName = "SF-View";
        public static string GetTitleLabel
        {
            get
            {
                try
                {
                    string? version = FileVersionInfo.GetVersionInfo(Environment.ProcessPath ?? "")?.FileVersion;
                    return $"{ApplicationName} | v{version ?? "Error"} [BETA]";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Polling for a version number did not complete successfully.\n" +
                        ex.Message);
                }
                return $"{ApplicationName}";
            }
        }
        /// <summary>
        /// Dictates whether the <see cref="MAPImporter"/> can automatically find refernenced level sections and populate them
        /// </summary>
        public static Boolean MapImporterAutoDereferenceMode = true;
        /// <summary>
        /// Files that are marked as *include files, as in containing symbol information
        /// </summary>
        public static HashSet<ASMFile>? Includes => ImportedProject?.Includes;
        /// <summary>
        /// All files that have been imported by the <see cref="ASMImporter"/>
        /// <para/>Key is File FullName (FullPath)
        /// </summary>
        public static Dictionary<string, IImporterObject>? OpenFiles => ImportedProject?.OpenFiles;
        public static IEnumerable<MAPFile>? OpenMAPFiles => ImportedProject?.OpenMAPFiles;
        public static bool IsFileIncluded(FileInfo File) => ImportedProject?.IsFileIncluded(File) ?? false;
        /// <summary>
        /// The project imported by the user, if one has been imported already
        /// <para>See: </para>
        /// </summary>
        public static SFCodeProject? ImportedProject { get; internal set; } = null;
        /// <summary>
        /// Attempts to load a project from the given source code folder
        /// </summary>
        /// <param name="ProjectDirectory"></param>
        /// <returns></returns>
        public static async Task<bool> TryImportProject(DirectoryInfo ProjectDirectory)
        {
            try
            {
                SFCodeProject codeProject = new(ProjectDirectory.FullName);
                await codeProject.EnumerateAsync(); // populate the project with files and folders
                ImportedProject = codeProject;
            }
            catch(Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message);
#endif
                return false;
            }
            return true;
        }

        /// <summary>
        /// Shows the CrashWindow with the given parameters
        /// </summary>
        /// <param name="Exception"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal static bool? ShowCrash(Exception Exception, bool Fatal, string Tip)
        {
            CrashWindow window = new CrashWindow(Exception, Fatal, Tip)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            return window.ShowDialog();
        }
    }
}
