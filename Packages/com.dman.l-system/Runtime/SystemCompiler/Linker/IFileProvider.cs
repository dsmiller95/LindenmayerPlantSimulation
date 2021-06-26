namespace Dman.LSystem.SystemCompiler.Linker
{
    public interface IFileProvider
    {
        public LinkedFile ReadLinkedFile(string fullIdentifier);
    }
}
