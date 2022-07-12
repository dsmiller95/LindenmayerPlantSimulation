using Cysharp.Threading.Tasks;
using Dman.LSystem.SystemCompiler.Linker;
using Dman.LSystem.SystemRuntime.Sunlight;
using Dman.LSystem.SystemRuntime.Turtle;
using Dman.LSystem.UnityObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.PerformanceTesting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class FullRenderLoopPerformanceTests
{

    private static IEnumerator MeasureAcrossFrames(Func<IEnumerator> acrossFrame, int warmup, int sampleCount)
    {
        var definition = new SampleGroup("Runtime", SampleUnit.Millisecond);

        var runNumber = 0;

        while (runNumber < warmup)
        {
            runNumber++;
            yield return acrossFrame();
        }

        var stopwatch = new Stopwatch();
        while (runNumber < (warmup + sampleCount))
        {
            stopwatch.Restart();
            yield return acrossFrame();
            stopwatch.Stop();

            Measure.Custom(definition, stopwatch.ElapsedMilliseconds);

            runNumber++;
        }
    }

    private static IEnumerator AsyncCoroutine(UniTask async)
    {
        Exception error = null;
        yield return async.ToCoroutine(e =>
        {
            UnityEngine.Debug.LogException(e);
            error = e;
        });
        if (error != null)
        {
            throw error;
        }
    }

    private static async UniTask FullLoopStep(LSystemBehavior behavior, TurtleInterpreterBehavior turtle, Camera camera)
    {
        behavior.StepSystem();

        var turtleTriggered = false;
        var triggeredFrame = 0;
        turtle.OnTurtleMeshUpdated += OnTurtleUpdate;

        void OnTurtleUpdate()
        {
            turtle.OnTurtleMeshUpdated -= OnTurtleUpdate;
            camera.Render();
            turtleTriggered = true;
            triggeredFrame = Time.frameCount;
        }

        var startTime = DateTime.Now;
        await UniTask.WaitUntil(() =>
        {
            if ((DateTime.Now - startTime) >= TimeSpan.FromSeconds(1))
            {
                return true;
            }

            return
                turtleTriggered &&
                Time.frameCount >= triggeredFrame + 1;
        }, PlayerLoopTiming.Update);
    }

    [UnityTest, Performance]
    public IEnumerator SimpleLineLSystemStepsAndRendersWithoutSunlight()
    {
        yield return new EnterPlayMode();
        yield return null;

        // this scene will be preconfigured with all the required context to get lsystems working
        var targetScene = EditorSceneManager.GetSceneByName("PERFORMANCE_TEST_SCENE");
        yield return EditorSceneManager.LoadSceneAsync(targetScene.buildIndex, UnityEngine.SceneManagement.LoadSceneMode.Single);
        yield return null;

        var testFile = @"
#axiom aF
#symbols +-\/^&
#iterations 10
#symbols Fna

a -> aF
";
        var fileSystem = new InMemoryFileProvider();
        fileSystem.RegisterFileWithIdentifier("root.lsystem", testFile);
        var linker = new FileLinker(fileSystem);
        var linkedFiles = linker.LinkFiles("root.lsystem");
        var systemObject = LSystemObject.GetNewLSystemFromFiles(linkedFiles);


        var turtleOperations = new List<TurtleOperationSet>();
        turtleOperations.Add(OrganPositioningTurtleInterpretorTests.GetDefaultMeshOperations(new[] { 'a' }));
        turtleOperations.Add(OrganPositioningTurtleInterpretorTests.GetDefaultRotateOperations(30));


        {
            var systemRenderer = new GameObject("lsystem renderer");
            systemRenderer.SetActive(false);
            systemRenderer.transform.position = Vector3.zero;
            systemRenderer.transform.localEulerAngles = new Vector3(0, 0, 90);
            systemRenderer.AddComponent<MeshFilter>();
            systemRenderer.AddComponent<MeshRenderer>();
            var systemBehavior = systemRenderer.AddComponent<LSystemBehavior>();
            var turtleInterpretor = systemRenderer.AddComponent<TurtleInterpreterBehavior>();

            systemBehavior.systemObject = systemObject;
            turtleInterpretor.operationSets = turtleOperations;
            turtleInterpretor.initialScale = new Vector3(.3f, .3f, .3f);

            systemRenderer.SetActive(true);

            var sunlightCamera = GameObject.FindObjectOfType<SunlightCamera>().GetComponent<Camera>();
            sunlightCamera.enabled = false;

            var renderingCameraObject = GameObject.FindGameObjectWithTag("MainCamera");
            var renderingCamera = renderingCameraObject.GetComponent<Camera>();
            renderingCamera.enabled = false;
            yield return MeasureAcrossFrames(() =>
            {
                return AsyncCoroutine(FullLoopStep(systemBehavior, turtleInterpretor, renderingCamera));
            }, 5, 30);

        }

        yield return new ExitPlayMode();
        yield return null;


        yield return null;
    }


}
