﻿using Cysharp.Threading.Tasks;
using Dman.LSystem.SystemCompiler.Linker;
using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Dman.LSystem.SystemRuntime.VolumetricData;
using Dman.LSystem.SystemRuntime.VolumetricData.Layers;
using Dman.LSystem.UnityObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class OrganPositioningTurtleInterpretor : IDisposable
    {
        private DependencyTracker<NativeTurtleData> nativeDataTracker;

        private TurtleState defaultState;
        private CustomRuleSymbols customSymbols;
        private ISymbolRemapper symbolRemapper;

        public OrganPositioningTurtleInterpretor(
            List<TurtleOperationSet> operationSets,
            TurtleState defaultState,
            ISymbolRemapper symbolRemapper,
            CustomRuleSymbols customSymbols)
        {
            foreach (var operationSet in operationSets)
            {
                operationSet.InternalCacheOperations();
            }

            var totalRequirements = operationSets.Select(x => x.DataReqs).Aggregate(new TurtleDataRequirements(), (agg, req) => agg + req);
            var nativeData = new NativeTurtleData(totalRequirements);
            var nativeWriter = new TurtleNativeDataWriter();

            foreach (var operationSet in operationSets)
            {
                operationSet.WriteIntoNativeData(nativeData, nativeWriter);
            }

            nativeData.operationsByKey = new NativeHashMap<int, TurtleOperation>(nativeWriter.operators.Count(), Allocator.Persistent);
            foreach (var ops in nativeWriter.operators)
            {
                var realSymbol = symbolRemapper.GetSymbolFromRoot(ops.characterInRootFile);
                nativeData.operationsByKey[realSymbol] = ops.operation;
            }

            this.customSymbols = customSymbols;

            // TODO: don't need the vertex, triangle, or material data in here
            nativeDataTracker = new DependencyTracker<NativeTurtleData>(nativeData);
            this.defaultState = defaultState;
            this.symbolRemapper = symbolRemapper;
        }

        public async UniTask<NativeList<TurtleOrganInstance>> CompileStringToMeshOrganInstances(
            DependencyTracker<SymbolString<float>> symbols,
            Matrix4x4 localToWorldTransform,
            CancellationToken token)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("Turtle has been disposed and cannot be used");
            }

            var meshResult = await TurtleStringReadingCompletable.ReadString(
                symbols,
                nativeDataTracker,
                defaultState,
                customSymbols,
                null,
                localToWorldTransform,
                token);

            return meshResult.organInstances;
        }

        public class TurtleOrganIdentifier
        {
            public int actualSymbol;
            public char characterInRootFile;
            /// <summary>
            /// ranges from 0 to n-1, where n is how many different mesh variants the symbol can be used to represent
            /// </summary>
            public int indexInVariants;
        }

        public List<TurtleOrganInstance> FilterOrgansByCharacter(NativeList<TurtleOrganInstance> organs, char filterRootCharcter)
        {
            UnityEngine.Profiling.Profiler.BeginSample("filter organs");
            var range = GetOrganIndexesForCharacter(filterRootCharcter);
            var organInstances = organs.ToArray().Where(x => range.ContainsIndex(x.organIndexInAllOrgans));
            UnityEngine.Profiling.Profiler.EndSample();

            return organInstances.ToList(); ;
        }

        public JaggedIndexing GetOrganIndexesForCharacter(char character)
        {
            var symbolId = symbolRemapper.GetSymbolFromRoot(character);
            var operation = nativeDataTracker.Data.operationsByKey[symbolId];
            if(operation.operationType != TurtleOperationType.ADD_ORGAN)
            {
                return JaggedIndexing.INVALID;
            }

            return operation.meshOperation.organIndexRange;
        }

        public TurtleOrganIdentifier GetOrganIdentifier(int organIndex)
        {
            foreach (var kvp in nativeDataTracker.Data.operationsByKey)
            {
                if(kvp.Value.operationType != TurtleOperationType.ADD_ORGAN)
                {
                    continue;
                }
                var meshOp = kvp.Value.meshOperation;
                if (!meshOp.organIndexRange.ContainsIndex(organIndex))
                {
                    continue;
                }
                var organIdentifier = new TurtleOrganIdentifier();
                organIdentifier.indexInVariants = organIndex - meshOp.organIndexRange.index;
                organIdentifier.actualSymbol = kvp.Key;
                organIdentifier.characterInRootFile = symbolRemapper.GetCharacterInRoot(organIdentifier.actualSymbol);
                return organIdentifier;
            }
            return null;
        }

        private bool IsDisposed = false;

        public void Dispose()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException("Cannot dispose turtle, has already been disposed");
            }
            IsDisposed = true;
            nativeDataTracker.Dispose();
        }

    }
}