namespace Dman.LSystem.SystemCompiler
{
    public class SyntaxException : System.Exception
    {
        public string ruleText;
        public string errorDescription;
        public int errorStartIndex;
        public int errorLength;

        public int ErrorEndIndex
        {
            get => errorStartIndex + errorLength;
            set => errorLength = value - errorStartIndex;
        }

        public SyntaxException(
            string description,
            int startIndex = 0,
            int errorLength = 1,
            string ruleText = "") : base()
        {
            this.ruleText = ruleText;
            errorStartIndex = startIndex;
            errorDescription = description;
            this.errorLength = errorLength;
        }

        public void RecontextualizeIndex(int indexOfSourceInTarget, string newRuleText = "")
        {
            errorStartIndex += indexOfSourceInTarget;
            ruleText = newRuleText;
        }

        public override string Message
        {
            get
            {
                if (errorStartIndex == ruleText.Length)
                {
                    // end of stream error
                    return ruleText + " : " + errorDescription;
                }
                if(string.IsNullOrEmpty(ruleText))
                {
                    return errorDescription;
                }
                var prefix = ruleText.Substring(0, errorStartIndex);
                var errorRule = $"<color=red>{ruleText.Substring(errorStartIndex, errorLength)}</color>";
                var suffix = ruleText.Substring(ErrorEndIndex, ruleText.Length - ErrorEndIndex);
                return $"{prefix + errorRule + suffix} : {errorDescription}";
            }
        }
    }
}
