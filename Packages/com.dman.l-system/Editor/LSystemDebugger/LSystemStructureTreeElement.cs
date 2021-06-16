using Dman.LSystem.SystemCompiler.Linker;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Dman.LSystem.Editor.LSystemDebugger
{
    public class LSystemStructureTreeElement : TreeViewItem
    {
        private int symbol;
        private float[] parameters;

        public LSystemStructureTreeElement(
            LinkedFileSet sourceFileSet,
            SymbolString<float> symbols,
            int indexInSymbols,
            int branchSymbolIndex = -1)
        {
            symbol = symbols[indexInSymbols];
            parameters = symbols.newParameters.AsArray(indexInSymbols);


            var symbolDefinition = sourceFileSet.GetLeafMostSymbolDefinition(symbol);

            var builder = new StringBuilder();
            builder.Append(symbolDefinition.characterInSourceFile);
            if (!"[]".Contains(symbolDefinition.characterInSourceFile))
            {
                symbols.WriteParamString(indexInSymbols, builder);
                builder.Append(" : ");
                builder.Append(Path.GetFileName(symbolDefinition.sourceFileDefinition));
            }
            displayName = builder.ToString();

            if (branchSymbolIndex != -1)
            {
                id = -branchSymbolIndex - 1;
            }
            else
            {
                id = indexInSymbols;
            }

        }


        public Color GetBackgroundColoring(float fadeoutTimeSpan)
        {
            var timeSinceExecuted = 0;// Time.time - node.LastExecuted;
            if (timeSinceExecuted > fadeoutTimeSpan)
            {
                return Color.clear;
            }
            var fadeFactor = 0;// 1 - (timeSinceExecuted / fadeoutTimeSpan);

            var baseColor = ColorBasedOnStatus();
            return new Color(baseColor.r, baseColor.g, baseColor.b, fadeFactor * fadeFactor);
        }

        private static readonly Color failColor = Color.red;
        private static readonly Color runningColor = Color.cyan;
        private static readonly Color successColor = Color.green;
        private Color ColorBasedOnStatus()
        {
            return runningColor;
            //switch (node.LastStatus)
            //{
            //    case NodeStatus.FAILURE:
            //        return failColor;
            //    case NodeStatus.SUCCESS:
            //        return successColor;
            //    case NodeStatus.RUNNING:
            //        return runningColor;
            //    default:
            //        return failColor;
            //}
        }

        private struct TreeConstructingState
        {
            public TreeViewItem currentParent;
            public LSystemStructureTreeElement treeElement;
            public int indexInString;
            public int branchingIndexer;
        }

        public static TreeViewItem ConstructTreeFromString(
            LinkedFileSet sourceFileSet,
            SymbolString<float> systemState,
            ISet<int> includeSymbols,
            int branchStartChar,
            int branchEndChar,
            int branchSymbolMaxBranchingFactorAsPowerOf2 = 4)
        {
            var root = new TreeViewItem { id = -1, depth = -1, displayName = "Root" };

            int indexInString = 0;
            for (;
                indexInString < systemState.Length && !includeSymbols.Contains(systemState[indexInString]);
                indexInString++)
            { }
            if (indexInString >= systemState.Length)
            {
                return null;
            }


            var currentState = new TreeConstructingState
            {
                indexInString = indexInString,
                currentParent = root,
                branchingIndexer = 0,
            };
            var stateStack = new Stack<TreeConstructingState>();


            for (;
                indexInString < systemState.Length;
                indexInString++)
            {
                var symbol = systemState[indexInString];
                if (!includeSymbols.Contains(symbol))
                {
                    continue;
                }
                if (symbol == branchStartChar)
                {
                    var branchBitDepth = stateStack.Count * branchSymbolMaxBranchingFactorAsPowerOf2;
                    var nextBranchAdd = 1 << branchBitDepth;
                    currentState.branchingIndexer += nextBranchAdd;

                    var newBranchState = new TreeConstructingState
                    {
                        indexInString = indexInString,
                        treeElement = new LSystemStructureTreeElement(
                            sourceFileSet,
                            systemState,
                            indexInString,
                            currentState.branchingIndexer),
                        currentParent = currentState.currentParent,
                        branchingIndexer = currentState.branchingIndexer
                    };
                    currentState.currentParent.AddChild(newBranchState.treeElement);
                    currentState = newBranchState;

                    stateStack.Push(currentState);
                    currentState.currentParent = currentState.treeElement;
                    continue;
                }
                if (symbol == branchEndChar)
                {
                    if (stateStack.Count <= 0)
                    {
                        Debug.LogWarning($"Too many branch end characters. aborting debug at index {indexInString}");
                        Debug.Log(systemState);
                        break;
                    }
                    currentState = stateStack.Pop();
                    continue;
                }
                var newState = new TreeConstructingState
                {
                    indexInString = indexInString,
                    treeElement = new LSystemStructureTreeElement(
                        sourceFileSet,
                        systemState,
                        indexInString),
                    currentParent = currentState.currentParent,
                    branchingIndexer = currentState.branchingIndexer
                };
                currentState.currentParent.AddChild(newState.treeElement);
                currentState = newState;
            }

            RemoveEmptyBranches(root, branchStartChar);

            return root;
        }

        private static bool RemoveEmptyBranches(TreeViewItem root, int openBranchSymbol)
        {
            if (root.hasChildren)
            {
                for (int i = root.children.Count - 1; i >= 0; i--)
                {
                    var child = root.children[i];
                    if (RemoveEmptyBranches(child, openBranchSymbol))
                    {
                        root.children.RemoveAt(i);
                    }
                }
            }
            else if (
              root is LSystemStructureTreeElement element &&
              element.symbol == openBranchSymbol
              )
            {
                return true;
            }
            return false;
        }
    }
}
