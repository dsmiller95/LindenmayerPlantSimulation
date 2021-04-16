using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSystem.Runtime.SystemRuntime
{
    public struct LSystemStepMatchIntermediate
    {
        public int parametersStartIndex;
        /// <summary>
        /// Set to true if there are no rules which apply to this symbol.
        ///     Used to save time when batching over this symbol later on
        /// </summary>
        public bool isTrivial;
        public byte matchedParametersCount;
        /// <summary>
        /// the index of the rule inside the structured L-system rule structure,
        ///     after indexing by the symbol
        /// </summary>
        public byte matchedRuleIndexInPossible;
        /// <summary>
        /// the ID of the stochastically selected replacement pattern
        /// </summary>
        public byte selectedReplacementPattern;


        public ushort replacementSymbolLength;
        public int replacementSymbolStartIndex;

        public LSystemMatchErrorCode errorCode;
    }

    public enum LSystemMatchErrorCode
    {
        NONE= 0,
        TOO_MANY_PARAMETERS = 1,
        TRIVIAL_SYMBOL_NOT_INDICATED_AT_MATCH_TIME = 2,
        TRIVIAL_SYMBOL_NOT_INDICATED_AT_REPLACEMENT_TIME = 3
    }
}
