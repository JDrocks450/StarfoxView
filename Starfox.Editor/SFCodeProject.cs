using StarFox.Interop;
using StarFox.Interop.ASM;
using StarFox.Interop.MAP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StarFox.Interop.GFX.CAD;

namespace Starfox.Editor
{
    /// <summary>
    /// Represents a StarFox editor Code Project
    /// </summary>
    public class SFCodeProject
    {
        /// <summary>
        /// Palettes that have been included in this project
        /// <para>FilePath, COL</para>
        /// </summary>
        public Dictionary<string, COL> Palettes { get; } = new();
        /// <summary>
        /// Files that are marked as *include files, as in containing symbol information
        /// </summary>
        public HashSet<ASMFile> Includes { get; } = new();
        /// <summary>
        /// All files that have been imported by a <see cref="CodeImporter"/>
        /// </summary>
        public Dictionary<string,IImporterObject> OpenFiles { get; } = new();
        public IEnumerable<MAPFile> OpenMAPFiles => OpenFiles.Values.OfType<MAPFile>();
        /// <summary>
        /// A map of all file nodes by their file name
        /// </summary>
        public Dictionary<string, SFCodeProjectNode> FileNodes = new();
        /// <summary>
        /// Looks for the specified file (or palette) to see if it's included
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        public bool IsFileIncluded(FileInfo File)
        {
            var value = Includes.Any(x => x.OriginalFilePath == File.FullName);
            if (value) return value;
            value = IsPaletteIncluded(File.FullName);
            return value;
        }
        public ASMFile? GetInclude(FileInfo File) => Includes.FirstOrDefault(x => x.OriginalFilePath == File.FullName);
        /// <summary>
        /// Looks to see if that palette has been referenced already
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        public bool IsPaletteIncluded(string FilePath) => Palettes.Any(x => x.Key.ToLower() == FilePath.ToLower());
        /// <summary>
        /// The path to the base directory of this <see cref="SFCodeProject"/> instance
        /// </summary>
        public DirectoryInfo WorkspaceDirectory { get; }
        /// <summary>
        /// The root node of this project file
        /// </summary>
        public SFCodeProjectNode ParentNode { get; private set; }
        /// <summary>
        /// Creates a new <see cref="SFCodeProject"/> with the given path to the base folder of the starfox project file
        /// </summary>
        /// <param name="workspaceDirectory"></param>
        public SFCodeProject(string workspaceDirectory)
        {
            var dirInfo = new DirectoryInfo(workspaceDirectory);
            if (!dirInfo.Exists) throw new ArgumentException("The given directory doesn't actually exist.");
            WorkspaceDirectory = dirInfo;
            ParentNode = new(SFCodeProjectNodeTypes.Project, workspaceDirectory);            
        }
        /// <summary>
        /// Searches for all files with matching filename (not fullpath)
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public IEnumerable<SFCodeProjectNode> SearchFile(string FileName) => 
            FileNodes.Where(x => Path.GetFileName(x.Key).ToLower() == FileName.ToLower()).Select(y => y.Value);
        public bool CloseFile(string FilePath) => OpenFiles.Remove(FilePath);
        public bool CloseFile(IImporterObject File) => CloseFile(File.OriginalFilePath);
        /// <summary>
        /// When called will populate the project file with all directories and files from the given project directory.
        /// <para>Dictated by the <see cref="WorkspaceDirectory"/></para>
        /// </summary>
        /// <returns></returns>
        public Task EnumerateAsync()
        { 
            SFCodeProjectNode processFile(FileInfo File, SFCodeProjectNode ParentNode)
            {// make a new node that represents this file in the parent node   
                var node = ParentNode.AddFile(File);
                FileNodes.Add(File.FullName, node);
                return node;
            }                                    
            SFCodeProjectNode processDirectory(DirectoryInfo Directory, SFCodeProjectNode? ParentNode)
            {
                SFCodeProjectNode? newNode = new SFCodeProjectNode(SFCodeProjectNodeTypes.Directory, Directory.FullName);
                if (ParentNode != null)
                    newNode = ParentNode.AddDirectory(Directory); // make a new node that represents this folder in the parent node
                foreach (var folder in Directory.EnumerateDirectories()) // find every folder inside this one
                    processDirectory(folder, newNode); // add it to this node
                foreach (var file in Directory.EnumerateFiles()) // find every file inside this one
                    processFile(file, newNode); // add the file to this node
                return newNode;
            }
            FileNodes.Clear();
            return Task.Run(delegate // do this work on a background thread
            {
                ParentNode = processDirectory(WorkspaceDirectory, null);
            });
        }
    }
}
