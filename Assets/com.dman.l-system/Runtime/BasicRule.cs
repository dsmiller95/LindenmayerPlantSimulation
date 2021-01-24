using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Dman.LSystem
{
    public class BasicRule :IRule
    {
        /// <summary>
        /// the symbol which this rule will replace. Apply rule will only ever be called with this symbol.
        /// </summary>
        public int TargetSymbol => _targetSymbol;
        private readonly int _targetSymbol;

        public int[] replacementSymbols { get; private set; }

        /// <summary>
        /// builds a rule based on the string definition, of format:
        ///   "A -> BACCB"
        ///   first char is always the target character
        ///   "->" delimits between target char and replacement string
        ///   everything after "->" is the replacement string
        /// </summary>
        /// <param name="ruleDef"></param>
        public BasicRule(string ruleDef)
        {
            var ruleMatch = Regex.Match(ruleDef, @"\s*(?<target>\w)\s*->\s*(?<replacement>.+)");
            if (!ruleMatch.Success)
            {
                throw new System.ArgumentException($"Error parsing rule defintion string '{ruleDef}'");
            }
            _targetSymbol = ruleMatch.Groups["target"].Value[0];
            replacementSymbols = ruleMatch.Groups["replacement"].Value.ToIntArray();
        }

        public BasicRule(int matchingSymbol, int[] replacementSymbols)
        {
            _targetSymbol = matchingSymbol;
            this.replacementSymbols = replacementSymbols;
        }
        /// <summary>
        /// retrun the symbol string to replace the given symbol with. return null if no match
        /// </summary>
        /// <param name="symbol">the symbol to be replaced</param>
        /// <param name="parameters">the parameters applied to the symbol. Could be null if no parameters.</param>
        /// <returns></returns>
        public SymbolString ApplyRule(float[] parameters)
        {
            if(parameters != null && parameters.Length > 0)
            {
                return null;
            }

            return new SymbolString(replacementSymbols);
        }
    }
}
