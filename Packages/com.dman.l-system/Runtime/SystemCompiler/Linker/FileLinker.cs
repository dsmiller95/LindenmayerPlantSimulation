using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dman.LSystem.SystemCompiler.Linker
{
    public class SymbolDefinition
    {
        public string sourceFileDefinition;
        public char characterInSourceFile;
        public int actualSymbol;
    }

    public class FileLinker
    {
        private IFileProvider fileProvider;
        private string basePath;

        public Dictionary<string, ParsedFile> allFilesByFullIdentifier = new Dictionary<string, ParsedFile>();
        public List<string> leafFirstFileSort;
        public List<SymbolDefinition> allSymbolDefinitions;

        public FileLinker(IFileProvider fileProvider, string basePath)
        {
            this.fileProvider = fileProvider;
            this.basePath = basePath;
        }

        public void LinkFiles()
        {
            this.ParseFullFileTree();
            leafFirstFileSort = this.GetTopologicalSort();
            AssignSymbolRemappingsToFiles();
        }

        private void AssignSymbolRemappingsToFiles()
        {
            allSymbolDefinitions = new List<SymbolDefinition>();
            int nextSymbolAssignment = 0;
            foreach (var fileIdentifier in leafFirstFileSort)
            {
                var parsedFile = allFilesByFullIdentifier[fileIdentifier];
                var remappedInFile = new Dictionary<char, int>();


                // TODO: check for bad imports which cause impossible remapping arrangements
                foreach (var include in parsedFile.links)
                {
                    var referencedFile = allFilesByFullIdentifier[include.fullImportIdentifier];
                    foreach (var remappedImport in include.importedSymbols)
                    {
                        remappedInFile[remappedImport.remappedSymbol] = referencedFile.GetExportedSymbol(remappedImport.importName);
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

                allSymbolDefinitions.AddRange(parsedFile.allSymbolAssignments.Select(x => new SymbolDefinition
                {
                    actualSymbol = x.remappedSymbol,
                    sourceFileDefinition = fileIdentifier,
                    characterInSourceFile = x.sourceCharacter
                }));
            }
        }

        private void ParseFullFileTree()
        {
            var leafIdentifiers = new Stack<string>();
            leafIdentifiers.Push(this.basePath);

            while(leafIdentifiers.Count > 0)
            {
                var next = leafIdentifiers.Pop();
                if (allFilesByFullIdentifier.ContainsKey(next))
                {
                    continue;
                }

                var file = fileProvider.ReadLinkedFile(next);
                if(file == null)
                {
                    throw new LinkException(LinkExceptionType.MISSING_FILE, $"Tried to import {next}, but file does not exist");
                }
                allFilesByFullIdentifier[next] = file;
                foreach (var link in file.links)
                {
                    leafIdentifiers.Push(link.fullImportIdentifier);
                }
            }
        }

        private List<string> GetTopologicalSort()
        {
            // set of all files which have been visited
            var visited = new HashSet<string>();
            // stack of the full parental line, including the current node
            var lineage = new Stack<string>();
            // list of all nodes, sorted leaf-first
            var topologicalNodes = new List<string>();

            GetTopologicalSortInternal(basePath, visited, lineage, topologicalNodes);

            return topologicalNodes;
        }

        private void GetTopologicalSortInternal(string node, ISet<string> visited, Stack<string> lineage, List<string> sortedNodes)
        {
            visited.Add(node);

            lineage.Push(node);
            var currentFile = this.allFilesByFullIdentifier[node];
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
                GetTopologicalSortInternal(nextNode, visited, lineage, sortedNodes);
            }
            lineage.Pop();
            sortedNodes.Add(node);
        }



        private ParsedFile GetIngestedFile(string fullIdentifier)
        {
            if(allFilesByFullIdentifier.TryGetValue(fullIdentifier, out var existingFile))
            {
                return existingFile;
            }
            var file = fileProvider.ReadLinkedFile(fullIdentifier);
            allFilesByFullIdentifier[fullIdentifier] = file;
            return file;
        }
    }
}
