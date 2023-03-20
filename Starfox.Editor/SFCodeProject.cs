using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starfox.Editor
{
    /// <summary>
    /// Represents a StarFox editor Code Project
    /// </summary>
    public class SFCodeProject
    {
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
        /// When called will populate the project file with all directories and files from the given project directory.
        /// <para>Dictated by the <see cref="WorkspaceDirectory"/></para>
        /// </summary>
        /// <returns></returns>
        public Task EnumerateAsync()
        { 
            SFCodeProjectNode processFile(FileInfo File, SFCodeProjectNode ParentNode) => ParentNode.AddFile(File); // make a new node that represents this file in the parent node                                       
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
            return Task.Run(delegate // do this work on a background thread
            {
                ParentNode = processDirectory(WorkspaceDirectory, null);
            });
        }
    }
}
