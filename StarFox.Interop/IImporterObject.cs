namespace StarFox.Interop
{
    public interface IImporterObject
    {        
        string OriginalFilePath { get; }
        /// <summary>
        /// The file name of this file using <see cref="OriginalFilePath"/>
        /// </summary>
        public string FileName => Path.GetFileNameWithoutExtension(OriginalFilePath);
    }
}