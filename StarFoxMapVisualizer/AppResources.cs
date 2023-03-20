using Starfox.Editor;
using StarFox.Interop.ASM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StarFoxMapVisualizer
{
    /// <summary>
    /// Resources that can be accessed throughout all of the User Interface code
    /// </summary>
    internal static class AppResources
    {
        public static HashSet<ASMFile> Includes { get; } = new();
        public static bool IsFileIncluded(FileInfo File) => AppResources.Includes.Any(x => x.OriginalFilePath == File.FullName);
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
