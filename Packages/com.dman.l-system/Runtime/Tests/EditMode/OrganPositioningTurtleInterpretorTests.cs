using Cysharp.Threading.Tasks;
using Dman.LSystem.SystemCompiler.Linker;
using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.Turtle;
using Dman.LSystem.UnityObjects;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class OrganPositioningTurtleInterpretorTests
{
    [UnityTest]
    public IEnumerator TurtleOrganIdsCompilesSingleOrgan() => UniTask.ToCoroutine(async () =>
    {
        var meshOperations = ScriptableObject.CreateInstance<TurtleMeshOperations>();
        var mesh = new MeshKey
        {
            Character = 'C',
            MeshRef = Resources.GetBuiltinResource<Mesh>("Cube.fbx"),
            MeshVariants = new MeshVariant[0],
            material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat"),
            IndividualScale = Vector3.one,
            ParameterScale = false,
            ScaleIsAdditional = false,
            VolumetricScale = false,
            ScalePerParameter = Vector3.one,
            AlsoMove = true,
            UseThickness = false,
            volumetricDurabilityValue = 0,
        };
        meshOperations.meshKeys = new[] { mesh };

        var opSets = new List<TurtleOperationSet>() { meshOperations };
        var defaultState = new TurtleState
        {
            transformation = Matrix4x4.identity,
            thickness = 1,
            organIdentity = new UIntFloatColor32(0)
        };
        var simpleRemapper = new SimpleSymbolRemapper()
        {
            {'[', 1 },
            {']', 2 },
            {'C', 'C' },
        };
        var customSymbols = new CustomRuleSymbols
        {
            branchOpenSymbol = 1,
            branchCloseSymbol = 2,
        };

        List<TurtleOrganInstance> organInstances;
        using (var turtle = new OrganPositioningTurtleInterpretor(opSets, defaultState, simpleRemapper, customSymbols))
        using (var systemState = new DefaultLSystemState("C"))
        using (var cancellation = new CancellationTokenSource())
        {
            var meshInstances = await turtle.CompileStringToTransformsWithMeshIds(systemState.currentSymbols, Matrix4x4.identity, cancellation.Token);
            organInstances = turtle.FilterOrgansByCharacter(meshInstances, 'C').ToList();
        }

        Assert.AreEqual(1, organInstances.Count);
        var instance = organInstances[0];
        // assert position and orientation
        Assert.AreEqual(Vector3.zero, ((Matrix4x4)instance.organTransform).MultiplyPoint(Vector3.zero));
        Assert.AreEqual(Vector3.one, ((Matrix4x4)instance.organTransform).MultiplyPoint(Vector3.one));
    });

    [UnityTest]
    public IEnumerator TurtleOrganCompilesMultiOrgans() => UniTask.ToCoroutine(async () =>
    {
        var meshOperations = ScriptableObject.CreateInstance<TurtleMeshOperations>();
        var mesh = new MeshKey
        {
            Character = 'C',
            MeshRef = Resources.GetBuiltinResource<Mesh>("Cube.fbx"),
            MeshVariants = new MeshVariant[0],
            material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat"),
            IndividualScale = Vector3.one,
            ParameterScale = false,
            ScaleIsAdditional = false,
            VolumetricScale = false,
            ScalePerParameter = Vector3.one,
            AlsoMove = true,
            UseThickness = false,
            volumetricDurabilityValue = 0,
        };
        meshOperations.meshKeys = new[] { mesh };

        var opSets = new List<TurtleOperationSet>() { meshOperations };
        var defaultState = new TurtleState
        {
            transformation = Matrix4x4.identity,
            thickness = 1,
            organIdentity = new UIntFloatColor32(0)
        };
        var simpleRemapper = new SimpleSymbolRemapper()
        {
            {'[', 1 },
            {']', 2 },
            {'C', 'C' },
        };
        var customSymbols = new CustomRuleSymbols
        {
            branchOpenSymbol = 1,
            branchCloseSymbol = 2,
        };

        List<TurtleOrganInstance> organInstances;
        using (var turtle = new OrganPositioningTurtleInterpretor(opSets, defaultState, simpleRemapper, customSymbols))
        using (var systemState = new DefaultLSystemState("CCCCC"))
        using (var cancellation = new CancellationTokenSource())
        {
            var meshInstances = await turtle.CompileStringToTransformsWithMeshIds(systemState.currentSymbols, Matrix4x4.identity, cancellation.Token);
            organInstances = turtle.FilterOrgansByCharacter(meshInstances, 'C').ToList();
        }

        Assert.AreEqual(5, organInstances.Count);
        for (int instanceNum = 0; instanceNum < organInstances.Count; instanceNum++)
        {
            var instance = organInstances[instanceNum];
            var position = ((Matrix4x4)instance.organTransform).MultiplyPoint(Vector3.zero);
            // assert position and orientation
            var expectedPos = new Vector3(1, 0, 0) * instanceNum;
            Assert.AreEqual(expectedPos, position, $"Expected organ at {expectedPos}, but was at {position}. index {instanceNum}");
        }
    });
}
