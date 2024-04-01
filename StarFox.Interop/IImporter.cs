using StarFox.Interop.ASM;
using StarFox.Interop.BSP;
using System.Text;

namespace StarFox.Interop
{
    /// <summary>
    /// A <see cref="CodeImporter{T}"/> that is tailored towards importing Binary files.
    /// <para><see cref="BasicCodeImporter{T}"/> should be used in the event of importing <see cref="ASMFile"/> (Assembly Files)</para>
    /// <para>Some types of files in StarFox are not based on Assembly code but instead compiled data in the form of Binary files. 
    /// This is the proper <see cref="CodeImporter{T}"/> to use in most circumstances involving binary files.</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BinaryCodeImporter<T> : CodeImporter<T> where T : IImporterObject
    {
        /// <summary>
        /// Not compatible with this importer.
        /// <para>Binary files cannot use Imports as they are not assembly code.</para>
        /// </summary>
        /// <param name="Includes"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public override void SetImports(params ASMFile[] Includes)
        {
            throw new InvalidOperationException("This importer is not compatible with includes. " +
                "There is no reason to include any files as the source file type (binary) is not assembly code.");
        }
        /// <summary>
        /// Not compatible with this importer.
        /// <para>Binary files cannot use Imports as they are not assembly code.</para>
        /// </summary>
        /// <typeparam name="IncludeType"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public override ImporterContext<IncludeType>? GetCurrentContext<IncludeType>()
        {
            throw new InvalidOperationException("This importer is not compatible with Contexts. " +
                "This importer cannot use Contexts because it is a binary file.");
        }
    }
    /// <summary>
    /// A <see cref="BasicCodeImporter{T}"/> is a <see cref="CodeImporter{T}"/> that first parses data from a 
    /// <see cref="ASMFile"/> first through the <see cref="ASMImporter"/>
    /// <para>This importer is perfect for if the data you wish to interpret is in the form of assembly, as the 
    /// <see cref="ASMImporter"/> is already created for you and the <see cref="GetCurrentContext{IncludeType}"/> and
    /// <see cref="SetImports(ASMFile[])"/> functions are already defined for you</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BasicCodeImporter<T> : CodeImporter<T> where T : ASMFile
    {
        /// <summary>
        /// <see cref="BasicCodeImporter{T}"/> utilizes a <see cref="ASMImporter"/> to import assembly
        /// before processing the assembly data into the type <see cref="T"/> -- which is accessed through 
        /// this property.
        /// </summary>
        protected ASMImporter baseImporter { get; set; } = new();        
        public override void SetImports(params ASMFile[] Imports)
        {
            baseImporter.SetImports(Imports);
        }
        public override ImporterContext<IncludeType>? GetCurrentContext<IncludeType>()
        {
            return baseImporter.Context as ImporterContext<IncludeType>;
        }
        /// <summary>
        /// Will import an <see cref="ASMFile"/> at the specified <paramref name="FileName"/> 
        /// using the <see cref="baseImporter"/>
        /// <para>This method is a macro for:</para>
        /// <code>baseImporter.ImportAsync(FileName)</code>
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        protected Task<ASMFile> BaseImportAsync(string FileName) => baseImporter.ImportAsync(FileName);
    }
    /// <summary>
    /// A <see cref="CodeImporter{T}"/> is an <see langword="abstract"/> type used to define functionality for interpreting a file
    /// in the Starfox source code into usable data objects. 
    /// <para>Importers are subject to specific data types and the functionality is implemented in inheritors</para>
    /// <para>They can specify other files expected to be included first before importing, and can offer warning messages 
    /// to users through the <see cref="CheckWarningMessage(string)"/> method</para>
    /// <para>They are all designed to be <see langword="async"/> and reusable</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class CodeImporter<T> where T : IImporterObject
    {
        /// <summary>
        /// A list of the names of expected include files.
        /// <para>This is intended to be more of a guideline, as some users may rename these files on their own.</para>
        /// <para>See: <see cref="CheckWarningMessage(string)"/> for the function that uses this property.</para>
        /// </summary>
        public virtual string[] ExpectedIncludes { get; } = new string[] { };
        /// <summary>
        /// Pipeline to getting the current context associated with this <see cref="CodeImporter{T}"/>
        /// <para>Many importers use custom <see cref="ImporterContext{T}"/> types to keep their state.</para>
        /// <para>This is mainly used for <see cref="CheckWarningMessage(string)"/> to see <see cref="ImporterContext{T}.Includes"/></para>
        /// </summary>
        /// <typeparam name="IncludeType">The type of file this <see cref="ImporterContext{T}"/> expects as a file.</typeparam>
        /// <returns></returns>
        public abstract ImporterContext<IncludeType>? GetCurrentContext<IncludeType>() where IncludeType : IImporterObject;
        public T? ImportedObject { get; protected set; }
        public StringBuilder ErrorOut { get; protected set; } = new();
        /// <summary>
        /// Sets the currently included symbol definitions files.
        /// </summary>
        /// <param name="Includes"></param>
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
                if (!context.Includes?.Any(x => 
                    Path.GetFileName(x.OriginalFilePath) == Path.GetFileName(expectedFile)) ?? true)
                        builder.AppendLine($"{expectedFile} was expected to be included, but not found.");
            }            
            return builder.ToString();
        }
    }
}