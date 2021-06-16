namespace Dman.LSystem.SystemCompiler.Linker
{
    public interface IFileProvider
    {
        public ParsedFile ReadLinkedFile(string fullIdentifier);
    }
}
