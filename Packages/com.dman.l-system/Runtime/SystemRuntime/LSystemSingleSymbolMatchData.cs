using Dman.LSystem.SystemRuntime;
using Dman.LSystem.SystemRuntime.NativeCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dman.LSystem.SystemRuntime
{
    public struct LSystemSingleSymbolMatchData
    {
        ////// #1 memory allocation step //////

        /// <summary>
        /// Indexing inside the captured parameter memory
        /// step #1 will populate the index in such a way that
        ///     reserves enough space for the largest possible allocation.
        /// Step #3 will modify the index and true size based on the specific match which is selected
        /// </summary>
        public JaggedIndexing tmpParameterMemorySpace;
        /// <summary>
        /// indexing inside the possible match memory
        /// step #1 will populate the index in such a way that
        ///     reserves enough space for the largest possible allocation.
        /// Step #2 will populate the true length of the range, as it matches all possible matches based
        ///     on context, ignoring conditionals
        /// </summary>
        public JaggedIndexing possibleMatchSpace;

        /// <summary>
        /// Set to true if there are no rules which apply to this symbol.
        ///     Used to save time when batching over this symbol later on
        /// Can be set to true at several points through the batching process, at any point where
        ///     it is determined that there remain no rules which match this symbol
        /// </summary>
        public bool isTrivial;

        ////// #2 finding all potential match step //////

        /// <summary>
        /// the index of the rule inside the structured L-system rule structure,
        ///     after indexing by the symbol.
        ///     populated by step #3, and used to represent the index of the rule selected as the True Match
        /// </summary>
        public byte matchedRuleIndexInPossible;

        ////// #3 selecting specific match step //////

        /// <summary>
        /// the ID of the stochastically selected replacement pattern. Populated by step #3, after
        ///     the single matched rule is identified
        /// </summary>
        public byte selectedReplacementPattern;

        /// <summary>
        /// the memory space reserved for replacement symbols
        ///     length is Populated by step #3 based on the specific rule selected.
        ///     index is populated by step #4, ensuring enough space for all replacement symbols
        /// </summary>
        public JaggedIndexing replacementSymbolIndexing;

        /// <summary>
        /// the memory space reserved for replacement parameters
        ///     length is Populated by step #3 based on the specific rule selected.
        ///     index is populated by step #4, ensuring enough space for all replacement parameters
        /// </summary>
        public JaggedIndexing replacementParameterIndexing;

        public LSystemMatchErrorCode errorCode;
    }

    public struct LSystemPotentialMatchData
    {
        /// <summary>
        /// the index of the rule inside the structured L-system rule structure,
        ///     after indexing by the symbol.
        ///     Populated by #2, and used to represent the specific rule matched
        /// </summary>
        public byte matchedRuleIndexInPossible;
        /// <summary>
        /// reference to tmp parameter capture memory space, indicating the parameters captured by this specific match
        /// Step #2 populates the index and length, based on the rule match
        /// </summary>
        public JaggedIndexing matchedParameters;
    }

    public enum LSystemMatchErrorCode
    {
        NONE= 0,
        TOO_MANY_PARAMETERS = 1,
        TRIVIAL_SYMBOL_NOT_INDICATED_AT_MATCH_TIME = 2,
        TRIVIAL_SYMBOL_NOT_INDICATED_AT_REPLACEMENT_TIME = 3
    }
}
