namespace Dman.LSystem
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
            this.errorStartIndex += indexOfSourceInTarget;
            this.ruleText = newRuleText;
        }

        public override string Message
        {
            get
            {
                if(errorStartIndex == ruleText.Length)
                {
                    // end of stream error
                    return ruleText + " : " + errorDescription;
                }

                var prefix = ruleText.Substring(0, errorStartIndex);
                var errorRule = $"<color=red>{ruleText.Substring(errorStartIndex, errorLength)}</color>";
                var suffix = ruleText.Substring(ErrorEndIndex, ruleText.Length - ErrorEndIndex);
                //var resultRule = ruleText.Substring(0, errorStartIndex) +
                //    $"<color=red>{ruleText.Substring(errorStartIndex, errorEndIndex - errorStartIndex)}</color>" +
                //    ruleText.Substring(errorEndIndex, ruleText.Length - errorEndIndex);
                return $"{prefix + errorRule + suffix} : {errorDescription}";
            }
        }

        //public override string ToString()
        //{
        //    return base.ToString();
        //}
    }
}
