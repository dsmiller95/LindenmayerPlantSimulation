using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GraphProcessor;
using UnityEngine.UIElements;
using PlantBuilder.NodeGraph.Mesh;

namespace PlantBuilder.NodeGraph
{
	[CustomEditor(typeof(PlantMeshGeneratorGraph), true)]
	public class PlantMeshGeneratorInspector : GraphInspector
	{
		protected override void CreateInspector()
		{
			base.CreateInspector();

			root.Add(new Button(() => EditorWindow.GetWindow<PlantMeshGeneratorWindow>().InitializeGraph(target as BaseGraph))
			{
				text = "Open base graph window"
			});
		}
	}
}