using ProceduralToolkit;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem
{
    public interface ITurtleOperator<T>
    {
        char TargetSymbol { get; }
        T Operate(T initialState, double[] parameters, TurtleMeshInstanceTracker targetDraft);
    }

    public struct TurtleMeshInstance
    {
        public Matrix4x4 transformation;
        public int meshIndex;
    }

    public struct TurtleMeshTemplate : IEquatable<TurtleMeshTemplate>
    {
        public MeshDraft draft;
        public Material material;

        public bool Equals(TurtleMeshTemplate other)
        {
            return other.draft.Equals(draft) && other.material.Equals(material);
        }
    }

    public class TurtleMeshInstanceTracker
    {
        private IList<TurtleMeshInstance> meshInstances = new List<TurtleMeshInstance>();
        private IList<TurtleMeshTemplate> meshTemplates = new List<TurtleMeshTemplate>();

        public void AddMeshInstance(int index, Matrix4x4 transformation)
        {
            meshInstances.Add(new TurtleMeshInstance
            {
                meshIndex = index,
                transformation = transformation
            });
        }

        public int AddOrGetMeshTemplate(TurtleMeshTemplate mesh)
        {
            var index = meshTemplates.IndexOf(mesh);
            if (index == -1)
            {
                meshTemplates.Add(mesh);
                return meshTemplates.Count - 1;
            }
            return index;
        }
        public TurtleMeshTemplate GetMeshTemplate(int templateId)
        {
            return meshTemplates[templateId];
        }

        public IEnumerable<TurtleMeshInstance> GetTurtleMeshInstances()
        {
            return meshInstances;
        }
    }
}
