using System.Collections.Generic;
using System.Linq;

namespace Dman.LSystem
{
    public class LSystem
    {
        public SymbolString currentSymbols { get; private set; }
        private IDictionary<int, IList<IRule>> rules;


        public LSystem(string axiomString, IEnumerable<IRule> rules) : this(new SymbolString(axiomString), rules)
        {
        }

        public LSystem(SymbolString axiomString, IEnumerable<IRule> rules)
        {
            currentSymbols = axiomString;
            this.rules = new Dictionary<int, IList<IRule>>();
            foreach (var rule in rules)
            {
                if (!this.rules.TryGetValue(rule.TargetSymbol, out var ruleList))
                {
                    this.rules[rule.TargetSymbol] = ruleList = new List<IRule>();
                }
                ruleList.Add(rule);
            }
        }

        public void StepSystem()
        {
            var resultString = new SymbolString[currentSymbols.symbols.Length];

            for (int symbolIndex = 0; symbolIndex < currentSymbols.symbols.Length; symbolIndex++)
            {
                var symbol = currentSymbols.symbols[symbolIndex];
                var parameters = currentSymbols.parameters[symbolIndex];

                if (!rules.TryGetValue(symbol, out var ruleList) || ruleList == null || ruleList.Count <= 0)
                {
                    resultString[symbolIndex] = SymbolString.FromSingle(symbol, parameters);
                    continue;
                }
                foreach (var rule in ruleList)
                {
                    var result = rule.ApplyRule(parameters);
                    if (result != null)
                    {
                        resultString[symbolIndex] = result;
                        break;
                    }
                }
            }

            currentSymbols = SymbolString.ConcatAll(resultString);
        }
    }
}
