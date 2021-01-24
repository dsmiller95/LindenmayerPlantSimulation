using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem
{
    public interface IRule
    {
        /// <summary>
        /// the symbol which this rule will replace. Apply rule will only ever be called with this symbol.
        /// This property will always return the same value for any given instance. It can be used to index the rule
        /// </summary>
        public int TargetSymbol { get; }

        /// <summary>
        /// retrun the symbol string to replace the given symbol with. return null if no match
        /// </summary>
        /// <param name="parameters">the parameters applied to the symbol. Could be null if no parameters.</param>
        /// <returns></returns>
        public SymbolString ApplyRule(float[] parameters);
    }
}
