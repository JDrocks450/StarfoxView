namespace StarFox.Interop
{
    public interface IImporter<T> where T : IImporterObject
    {
        T? ImportedObject { get; }
        Task<T> ImportAsync(string FilePath);
    }
}