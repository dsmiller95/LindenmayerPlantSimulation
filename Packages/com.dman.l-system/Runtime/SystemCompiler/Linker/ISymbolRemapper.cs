namespace Dman.LSystem.SystemCompiler.Linker
{
    public interface ISymbolRemapper
    {
        public int GetSymbolFromRoot(char character);
        public char GetCharacterInRoot(int symbol);
    }

    /// <summary>
    /// maps the character to its character code directly
    /// </summary>
    public class SimpleSymbolRemapper : ISymbolRemapper
    {
        public char GetCharacterInRoot(int symbol)
        {
            return (char)symbol;
        }

        public int GetSymbolFromRoot(char character)
        {
            return character;
        }
    }
}


