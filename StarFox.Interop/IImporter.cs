namespace StarFox.Interop
{
    public abstract class CodeImporter<T> where T : IImporterObject
    {
        public T? ImportedObject { get; protected set; }
        public abstract Task<T> ImportAsync(string FilePath);
        /// <summary>
        /// Will throw an exception containing a warning message if the given file has warnings before import.
        /// </summary>
        public virtual void CheckWarningMessage(string FilePath)
        {
            ; // override in child
        }
    }
}