using Dman.LSystem.Extern;

namespace Dman.LSystem.SystemRuntime.NativeCollections
{
    public class Interop
    {
        public static SymbolStringInterop From(SymbolString<float> source)
        {
            return new SymbolStringInterop
            {
                symbols = new NativeArrayInteropi32(source.symbols),
                parameters = new NativeArrayInteropf32(source.parameters.data),
                parameter_indexing = new NativeArrayInteropJaggedIndexing(source.parameters.indexing)
            };
        }
        public static SymbolStringInteropMut FromMut(SymbolString<float> source)
        {
            return new SymbolStringInteropMut
            {
                symbols = new NativeArrayInteropi32Mut(source.symbols),
                parameters = new NativeArrayInteropf32Mut(source.parameters.data),
                parameter_indexing = new NativeArrayInteropJaggedIndexingMut(source.parameters.indexing)
            };
        }
    }
}