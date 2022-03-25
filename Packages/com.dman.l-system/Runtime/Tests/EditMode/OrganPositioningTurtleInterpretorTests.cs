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
    private OrganPositioningTurtleInterpretor GetInterpretor(char[] meshKeys)
    {
        var meshOperations = ScriptableObject.CreateInstance<TurtleMeshOperations>();
        meshOperations.meshKeys = meshKeys.Select(x =>
        {
            return new MeshKey
            {
                Character = x,
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
        }).ToArray();

        var turnOperations = ScriptableObject.CreateInstance<TurtleRotateOperations>();
        turnOperations.defaultRollTheta = 90;
        turnOperations.defaultTurnTheta = 90;
        turnOperations.defaultTiltTheta = 90;

        var opSets = new List<TurtleOperationSet>() { meshOperations, turnOperations };
        var defaultState = new TurtleState
        {
            transformation = Matrix4x4.identity,
            thickness = 1,
            organIdentity = new UIntFloatColor32(0)
        };
        var simpleRemapper = new SimpleSymbolRemapper();

        var customSymbols = new CustomRuleSymbols
        {
            branchOpenSymbol = '[',
            branchCloseSymbol = ']',
        };

        return new OrganPositioningTurtleInterpretor(opSets, defaultState, simpleRemapper, customSymbols);
    }


    private void ExpectPositions(List<TurtleOrganInstance> organs, List<Vector3> positions)
    {
        Assert.AreEqual(positions.Count, organs.Count);
        for (int instanceNum = 0; instanceNum < organs.Count; instanceNum++)
        {
            var instance = organs[instanceNum];
            var position = ((Matrix4x4)instance.organTransform).MultiplyPoint(Vector3.zero);
            // assert position and orientation
            var expectedPos = positions[instanceNum];

            Assert.AreEqual(expectedPos.x, position.x, 1e-5, $"Expected organ at {expectedPos}, but was at {position}. index {instanceNum}");
            Assert.AreEqual(expectedPos.y, position.y, 1e-5, $"Expected organ at {expectedPos}, but was at {position}. index {instanceNum}");
            Assert.AreEqual(expectedPos.z, position.z, 1e-5, $"Expected organ at {expectedPos}, but was at {position}. index {instanceNum}");
        }
    }

    [UnityTest]
    public IEnumerator TurtleOrganIdsCompilesSingleOrgan() => UniTask.ToCoroutine(async () =>
    {
        List<TurtleOrganInstance> organInstances;
        using (var turtle = GetInterpretor(new[] { 'C' }))
        using (var systemState = new DefaultLSystemState("C"))
        using (var cancellation = new CancellationTokenSource())
        {
            var meshInstances = await turtle.CompileStringToMeshOrganInstances(systemState.currentSymbols, Matrix4x4.identity, cancellation.Token);
            organInstances = turtle.FilterOrgansByCharacter(meshInstances, 'C').ToList();
        }

        var expected = new List<Vector3>
        {
            new Vector3(0, 0, 0),
        };
        ExpectPositions(organInstances, expected);
    });

    [UnityTest]
    public IEnumerator TurtleOrganCompilesMultiOrgans() => UniTask.ToCoroutine(async () =>
    {
        List<TurtleOrganInstance> organInstances;
        using (var turtle = GetInterpretor(new[] { 'C' }))
        using (var systemState = new DefaultLSystemState("CCCCC"))
        using (var cancellation = new CancellationTokenSource())
        {
            var meshInstances = await turtle.CompileStringToMeshOrganInstances(systemState.currentSymbols, Matrix4x4.identity, cancellation.Token);
            organInstances = turtle.FilterOrgansByCharacter(meshInstances, 'C').ToList();
        }

        var expected = new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(2, 0, 0),
            new Vector3(3, 0, 0),
            new Vector3(4, 0, 0),
        };
        ExpectPositions(organInstances, expected);
    });

    [UnityTest]
    public IEnumerator TurtleOrganCompilesMultiOrganTypes() => UniTask.ToCoroutine(async () =>
    {
        List<TurtleOrganInstance> organInstances;
        using (var turtle = GetInterpretor(new[] { 'C', 'D' }))
        using (var systemState = new DefaultLSystemState("CDCDDCDCDCDDDC"))
        using (var cancellation = new CancellationTokenSource())
        {
            var meshInstances = await turtle.CompileStringToMeshOrganInstances(systemState.currentSymbols, Matrix4x4.identity, cancellation.Token);
            organInstances = turtle.FilterOrgansByCharacter(meshInstances, 'C').ToList();
        }

        var expected = new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(2, 0, 0),
            new Vector3(5, 0, 0),
            new Vector3(7, 0, 0),
            new Vector3(9, 0, 0),
            new Vector3(13, 0, 0),
        };
        ExpectPositions(organInstances, expected);
    });

    [UnityTest]
    public IEnumerator TurtleOrganCompilesWithBending() => UniTask.ToCoroutine(async () =>
    {
        List<TurtleOrganInstance> organInstances;
        using (var turtle = GetInterpretor(new[] { 'C' }))
        using (var systemState = new DefaultLSystemState("C+C-C+C-C"))
        using (var cancellation = new CancellationTokenSource())
        {
            var meshInstances = await turtle.CompileStringToMeshOrganInstances(systemState.currentSymbols, Matrix4x4.identity, cancellation.Token);
            organInstances = turtle.FilterOrgansByCharacter(meshInstances, 'C').ToList();
        }

        var expected = new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),
            new Vector3(2, 0, 1),
            new Vector3(2, 0, 2),
        };
        ExpectPositions(organInstances, expected);
    });

    [UnityTest]
    public IEnumerator TurtleOrganCompilesWithBendingAndBranching() => UniTask.ToCoroutine(async () =>
    {
        List<TurtleOrganInstance> organInstances;
        using (var turtle = GetInterpretor(new[] { 'C' }))
        using (var systemState = new DefaultLSystemState("C[+CCC][-CCC]"))
        using (var cancellation = new CancellationTokenSource())
        {
            var meshInstances = await turtle.CompileStringToMeshOrganInstances(systemState.currentSymbols, Matrix4x4.identity, cancellation.Token);
            organInstances = turtle.FilterOrgansByCharacter(meshInstances, 'C').ToList();
        }

        var expected = new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),
            new Vector3(1, 0, 2),
            new Vector3(1, 0, 0),
            new Vector3(1, 0,-1),
            new Vector3(1, 0,-2),
        };
        ExpectPositions(organInstances, expected);
    });
}
