namespace Dman.LSystem.SystemRuntime
{
    public class LSystemRuntimeException : System.Exception
    {
        public override string StackTrace => _extraStack + base.StackTrace;
        private string _extraStack = "";

        public LSystemRuntimeException(
            string description) : base(description)
        {
        }

        public LSystemRuntimeException AddContext(string stackLine)
        {
            return new LSystemRuntimeException(this.Message + $"\n{stackLine}");
        }
    }
}
