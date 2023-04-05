namespace Starfox.Editor
{
    /// <summary>
    /// An abstract class that represents a node in a <see cref="SFCodeProject"/>
    /// </summary>
    public class SFCodeProjectNode
    {
        /// <summary>
        /// The type of file object this node represents
        /// </summary>
        public SFCodeProjectNodeTypes Type { get; internal set; }
        public SFCodeProjectFileTypes RecognizedFileType { get; private set; }
        /// <summary>
        /// The path to this Code Object in the host file system
        /// <para>This is a FullPath to the file on the host file system.</para>
        /// </summary>
        public string FilePath { get; set; }
        /// <summary>
        /// The children nodes attached to this parent node, if any.
        /// </summary>
        public List<SFCodeProjectNode> ChildNodes { get; } = new();

        /// <summary>
        /// Makes a new <see cref="SFCodeProjectNode"/> with the given type and file path
        /// </summary>
        /// <param name="type"></param>
        /// <param name="FilePath"></param>
        public SFCodeProjectNode(SFCodeProjectNodeTypes type, string FilePath)
        {
            Type = type;
            this.FilePath = FilePath;
            if (Type is SFCodeProjectNodeTypes.File)
                RecognizedFileType = SFCodeProjectFileExtensions.GetSFFileType(FilePath);
        }
        /// <summary>
        /// Adds a folder to this project node
        /// </summary>
        /// <param name="Directory">The folder to add</param>
        public SFCodeProjectNode AddDirectory(DirectoryInfo Directory)
        {
            var node = new SFCodeProjectNode(SFCodeProjectNodeTypes.Directory, Directory.FullName);
            ChildNodes.Add(node);
            return node;
        }
        /// <summary>
        /// Adds a file to this project node
        /// </summary>
        /// <param name="File">The file to add</param>
        public SFCodeProjectNode AddFile(FileInfo File)
        {
            var node = new SFCodeProjectNode(SFCodeProjectNodeTypes.File, File.FullName);
            if (node.RecognizedFileType is SFCodeProjectFileTypes.SF_EDIT_OPTIM)
                node = new SFOptimizerNode(File.FullName);
            ChildNodes.Add(node);
            return node;
        }
        public SFOptimizerNode AddOptimizer(string Name, SFOptimizerDataStruct Data)
        {
            if (Type != SFCodeProjectNodeTypes.Directory) throw new Exception("You can't create an Optimizer on a File Node, only a directory node.");
            return SFOptimizerNode.Create(FilePath, Name, Data);
        }
        /// <summary>
        /// Sets the type of node that this object currently is
        /// </summary>
        /// <param name="NewType"></param>
        public void SetType(SFCodeProjectNodeTypes NewType) => Type = NewType;
    }
}
