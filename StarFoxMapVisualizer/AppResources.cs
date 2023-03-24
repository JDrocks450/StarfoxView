using Starfox.Editor;
using StarFox.Interop.ASM;
using StarFox.Interop.MAP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static StarFoxMapVisualizer.Controls.ASMControl;

namespace StarFoxMapVisualizer
{
    /// <summary>
    /// Resources that can be accessed throughout all of the User Interface code
    /// </summary>
    internal static class AppResources
    {
        /// <summary>
        /// Files that are marked as *include files, as in containing symbol information
        /// </summary>
        public static HashSet<ASMFile>? Includes => ImportedProject?.Includes;
        /// <summary>
        /// All files that have been imported by the <see cref="ASMImporter"/>
        /// </summary>
        public static HashSet<ASMFile>? OpenFiles => ImportedProject?.OpenFiles;
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
    }
}
