using Dman.LSystem.Extern;

namespace Dman.LSystem
{
    public static class RustExternals
    {
        public static int double_input(int x) => SystemRuntimeRust.double_input(x);
        public static int triple_input(int x)  => SystemRuntimeRust.triple_input(x);
    }
}
