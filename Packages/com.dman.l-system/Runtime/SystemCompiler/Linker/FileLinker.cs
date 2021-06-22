using System;
using System.Collections.Generic;
using System.Linq;

namespace Dman.LSystem.SystemCompiler.Linker
{
    [Serializable]
    public class SymbolDefinition
    {
        public string sourceFileDefinition;
        public char characterInSourceFile;
        public int actualSymbol;

        public override string ToString()
        {
            return $"{characterInSourceFile} : {sourceFileDefinition.Split('.')[0]}";
        }
    }

    public class FileLinker
    {
        private IFileProvider fileProvider;

        public FileLinker(IFileProvider fileProvider)
        {
            this.fileProvider = fileProvider;
        }

        public LinkedFileSet LinkFiles(string basePath)
        {
            var allFilesByFullIdentifier = ParseFullFileTree(basePath);
            var leafFirstFileSort = GetTopologicalSort(basePath, allFilesByFullIdentifier);
            var allSymbols = AssignSymbolRemappingsToFiles(allFilesByFullIdentifier, leafFirstFileSort);

            return new LinkedFileSet(basePath, allFilesByFullIdentifier, allSymbols);
        }

        private List<SymbolDefinition> AssignSymbolRemappingsToFiles(Dictionary<string, LinkedFile> allFiles, List<string> leafFirstSortedFiles)
        {
            var globalSymbols = new Dictionary<char, int>();
            var completeSymbolDefinitions = new List<SymbolDefinition>();
            int nextSymbolAssignment = 0;
            foreach (var fileIdentifier in leafFirstSortedFiles)
            {
                var parsedFile = allFiles[fileIdentifier];
                var remappedInFile = new Dictionary<char, int>();

                foreach (var globalSymbol in parsedFile.globalCharacters)
                {
                    if (!globalSymbols.TryGetValue(globalSymbol, out var remapped))
                    {
                        remapped = globalSymbols[globalSymbol] = nextSymbolAssignment;
                        nextSymbolAssignment++;
                    }
                    remappedInFile[globalSymbol] = remapped;
                }

                foreach (var include in parsedFile.links)
                {
                    var referencedFile = allFiles[include.fullImportIdentifier];
                    foreach (var remappedImport in include.importedSymbols)
                    {
                        var exportedSymbolIdentity = referencedFile.GetExportedSymbol(remappedImport.importName);
                        if (remappedInFile.ContainsKey(remappedImport.remappedSymbol))
                        {
                            if (exportedSymbolIdentity != remappedInFile[remappedImport.remappedSymbol])
                            {
                                throw new LinkException(
                                    LinkExceptionType.IMPORT_COLLISION,
                                    $"Import collision on '{remappedImport.remappedSymbol}'. Import of {remappedImport.importName} from {referencedFile.fileSource} would conflict with a previous definition on that same symbol", fileIdentifier);
                            }
                        }
                        else
                        {
                            var existingValues = remappedInFile.Where(x => x.Value == exportedSymbolIdentity && x.Key != remappedImport.remappedSymbol).ToList();
                            if (existingValues.Any())
                            {
                                var existing = existingValues.First();
                                throw new LinkException(
                                    LinkExceptionType.IMPORT_DISSONANCE,
                                    $"Import dissonance on '{remappedImport.remappedSymbol}'. Import of {remappedImport.importName} from {referencedFile.fileSource} would re-import the same symbol already defined as {existing.Key}", fileIdentifier);
                            }
                            remappedInFile[remappedImport.remappedSymbol] = exportedSymbolIdentity;
                        }
                    }
                }

                foreach (var nonImportedSymbols in parsedFile.allSymbols)
                {
                    if (remappedInFile.ContainsKey(nonImportedSymbols))
                    {
                        continue;
                    }
                    remappedInFile[nonImportedSymbols] = nextSymbolAssignment;
                    nextSymbolAssignment++;
                }

                parsedFile.allSymbolAssignments = remappedInFile.Select(x => new SymbolRemap
                {
                    remappedSymbol = x.Value,
                    sourceCharacter = x.Key
                }).ToList();

                completeSymbolDefinitions.AddRange(parsedFile.allSymbolAssignments.Select(x => new SymbolDefinition
                {
                    actualSymbol = x.remappedSymbol,
                    sourceFileDefinition = fileIdentifier,
                    characterInSourceFile = x.sourceCharacter
                }));
            }
            return completeSymbolDefinitions;
        }

        private Dictionary<string, LinkedFile> ParseFullFileTree(string basePath)
        {
            var allFiles = new Dictionary<string, LinkedFile>();
            var leafIdentifiers = new Stack<string>();
            leafIdentifiers.Push(basePath);

            while (leafIdentifiers.Count > 0)
            {
                var next = leafIdentifiers.Pop();
                if (allFiles.ContainsKey(next))
                {
                    continue;
                }

                var file = fileProvider.ReadLinkedFile(next);
                if (file == null)
                {
                    throw new LinkException(LinkExceptionType.MISSING_FILE, $"Tried to import {next}, but file does not exist");
                }
                allFiles[next] = file;
                foreach (var link in file.links)
                {
                    leafIdentifiers.Push(link.fullImportIdentifier);
                }
            }
            return allFiles;
        }

        private List<string> GetTopologicalSort(string basePath, Dictionary<string, LinkedFile> allFiles)
        {
            // set of all files which have been visited
            var visited = new HashSet<string>();
            // stack of the full parental line, including the current node
            var lineage = new Stack<string>();
            // list of all nodes, sorted leaf-first
            var topologicalNodes = new List<string>();

            GetTopologicalSortInternal(basePath, visited, lineage, topologicalNodes, allFiles);

            return topologicalNodes;
        }

        private void GetTopologicalSortInternal(
            string node,
            ISet<string> visited,
            Stack<string> lineage,
            List<string> sortedNodes,
            Dictionary<string, LinkedFile> allFiles)
        {
            visited.Add(node);

            lineage.Push(node);
            var currentFile = allFiles[node];
            foreach (var link in currentFile.links)
            {
                var nextNode = link.fullImportIdentifier;
                if (lineage.Contains(nextNode))
                {
                    var cyclePath = lineage.SkipWhile(x => x != nextNode).Append(nextNode);
                    throw new LinkException(LinkExceptionType.CYCLIC_DEPENDENCY, "Cyclic include detected, file includes itself", cyclePath.ToArray());
                }
                if (visited.Contains(nextNode))
                {
                    continue;
                }
                GetTopologicalSortInternal(nextNode, visited, lineage, sortedNodes, allFiles);
            }
            lineage.Pop();
            sortedNodes.Add(node);
        }
    }
}
