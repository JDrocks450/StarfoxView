namespace StarFox.Interop.GFX.COLTAB.DEF
{
    /// <summary>
    /// A COLANIM function call, which is a color that changes between different colors per frame
    /// <code>colanim animTableName</code>
    /// </summary>
    public class COLAnimationReference : COLDefinition
    {
        public COLAnimationReference(string tableName)
        {
            TableName = tableName;
        }
        public override CallTypes CallType => CallTypes.Animation;
        public string TableName { get; }
    }
}
