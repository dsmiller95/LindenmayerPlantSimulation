namespace Dman.LSystem
{
    public interface ISymbolString
    {
        int Length { get; }
        int this[int index]
        {
            get;
        }

        int ParameterSize(int index);
    }
}
