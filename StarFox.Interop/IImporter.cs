using StarFox.Interop.ASM;
using System.Text;

namespace StarFox.Interop
{
    public abstract class CodeImporter<T> where T : IImporterObject
    {
        /// <summary>
        /// A list of the names of expected include files.
        /// <para>This is intended to be more of a guideline, as some users may rename these files on their own.</para>
        /// <para>See: <see cref="CheckWarningMessage(string)"/> for the function that uses this property.</para>
        /// </summary>
        public virtual string[] ExpectedIncludes { get; } = new string[] { };
        internal abstract ImporterContext<IncludeType>? GetCurrentContext<IncludeType>() where IncludeType : IImporterObject;
        public T? ImportedObject { get; protected set; }
        public StringBuilder ErrorOut { get; protected set; } = new();
        public abstract void SetImports(params ASMFile[] Includes);
        /// <summary>
        /// Imports the selected file now with the current context.
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        public abstract Task<T> ImportAsync(string FilePath);
        /// <summary>
        /// Will throw an exception containing a warning message if the given file has warnings before import.
        /// </summary>
        public virtual string? CheckWarningMessage(string FilePath)
        {
            var context = GetCurrentContext<ASMFile>();
            if (context == null) return default; // can't check this importer's includes
            StringBuilder builder = new();
            foreach(var expectedFile in ExpectedIncludes) {
                if (!context.Includes.Any(x => 
                    Path.GetFileName(x.OriginalFilePath) == Path.GetFileName(expectedFile)))
                        builder.AppendLine($"{expectedFile} was expected to be included, but not found.");
            }            
            return builder.ToString();
        }
    }
}