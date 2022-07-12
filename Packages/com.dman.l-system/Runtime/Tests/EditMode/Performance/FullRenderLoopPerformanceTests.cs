using Cysharp.Threading.Tasks;
using Dman.LSystem.SystemCompiler.Linker;
using Dman.LSystem.SystemRuntime.Sunlight;
using Dman.LSystem.SystemRuntime.Turtle;
using Dman.LSystem.UnityObjects;
using Dman.Utilities;
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

    private static CustomProfileSampler customSamples;

    private class CustomProfileSampler
    {
        private static Dictionary<string, SampleGroup> sampleGroup = new Dictionary<string, SampleGroup>();

        private static Dictionary<string, int> aggregateSamples = new Dictionary<string, int>();

        public void AddSampleGroup(SampleGroup group)
        {
            sampleGroup[group.Name] = group;
            aggregateSamples[group.Name] = 0;
        }

        public void AddSample(string sampleName, int additionalValue)
        {
            aggregateSamples[sampleName] += additionalValue;
        }

        public void ApplySamples()
        {
            foreach (var key in sampleGroup.Keys)
            {
                Measure.Custom(sampleGroup[key], aggregateSamples[key]);
                aggregateSamples[key] = 0;
            }
        }
    }

    private static IEnumerator MeasureAcrossFrames(Func<IEnumerator> acrossFrame, int warmup, int sampleCount)
    {
        customSamples = new CustomProfileSampler();
        customSamples.AddSampleGroup(new SampleGroup("vertex count", SampleUnit.Undefined));
        customSamples.AddSampleGroup(new SampleGroup("Runtime", SampleUnit.Millisecond));

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

            customSamples.AddSample("Runtime", (int)stopwatch.ElapsedMilliseconds);
            customSamples.ApplySamples();

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
            customSamples.AddSample("vertex count", turtle.GetComponent<MeshFilter>().mesh.vertexCount);
        }

        var startTime = DateTime.Now;

        while (true) {

            if((DateTime.Now - startTime) >= TimeSpan.FromSeconds(1))
            {
                throw new Exception("timeout");
            }

            if(turtleTriggered && Time.frameCount >= triggeredFrame + 1)
            {
                return;
            }

            //JobHandleExtensions.PendingAsyncJobs.Complete();
            //await UniTask.Yield(PlayerLoopTiming.EarlyUpdate);
            JobHandleExtensions.PendingAsyncJobs.Complete();
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }


    public static (LSystemBehavior system, TurtleInterpreterBehavior turtle) ConfigureNewLSystem(LSystemObject systemObject, List<TurtleOperationSet> turtleOperations)
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

        return (systemBehavior, turtleInterpretor);
    }

    public static (Camera sunlightCamera, Camera mainCamera) GetCameras()
    {
        var sunlightCamera = GameObject.FindObjectOfType<SunlightCamera>().GetComponent<Camera>();
        sunlightCamera.enabled = false;
        var renderingCameraObject = GameObject.FindGameObjectWithTag("MainCamera");
        var renderingCamera = renderingCameraObject.GetComponent<Camera>();
        renderingCamera.enabled = true;

        return (sunlightCamera, renderingCamera);
    }

    private static LSystemObject GetConfiguredLSystem(string rootFileText)
    {
        var fileSystem = new InMemoryFileProvider();
        fileSystem.RegisterFileWithIdentifier("root.lsystem", rootFileText);
        var linker = new FileLinker(fileSystem);
        var linkedFiles = linker.LinkFiles("root.lsystem");
        return LSystemObject.GetNewLSystemFromFiles(linkedFiles);
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
        JobHandleExtensions.TrackPendingJobs = true;
        

        var systemObject = GetConfiguredLSystem(@"
#axiom aF
#symbols +-\/^&
#iterations 10
#symbols Fna

a -> aF
");

        var turtleOperations = new List<TurtleOperationSet>();
        turtleOperations.Add(OrganPositioningTurtleInterpretorTests.GetDefaultMeshOperations(new[] { 'F' }));
        turtleOperations.Add(OrganPositioningTurtleInterpretorTests.GetDefaultRotateOperations(30));


        {
            var (systemBehavior, turtleInterpretor) = ConfigureNewLSystem(systemObject, turtleOperations);

            var (sunlightCamera, renderingCamera) = GetCameras();

            yield return MeasureAcrossFrames(() =>
            {
                return AsyncCoroutine(FullLoopStep(systemBehavior, turtleInterpretor, renderingCamera));
            }, 5, 30);

        }

        yield return new ExitPlayMode();
    }

    [UnityTest, Performance]
    public IEnumerator LargeTreeLSystemStepsAndRendersWithoutSunlight()
    {
        yield return new EnterPlayMode();
        yield return null;

        // this scene will be preconfigured with all the required context to get lsystems working
        var targetScene = EditorSceneManager.GetSceneByName("PERFORMANCE_TEST_SCENE");
        yield return EditorSceneManager.LoadSceneAsync(targetScene.buildIndex, UnityEngine.SceneManagement.LoadSceneMode.Single);
        yield return null;
        JobHandleExtensions.TrackPendingJobs = true;

        var systemObject = GetConfiguredLSystem(@"
#axiom a(10)F
#symbols +-\/^&
#iterations 10
#symbols Fna

a(x) : x > 0 -> FFF\[+a(x - 1)][-a(x - 1)]
a(x) : x <= 0 -> /F
");

        var turtleOperations = new List<TurtleOperationSet>();
        turtleOperations.Add(OrganPositioningTurtleInterpretorTests.GetDefaultMeshOperations(new[] { 'F' }));
        turtleOperations.Add(OrganPositioningTurtleInterpretorTests.GetDefaultRotateOperations(30));


        {
            var (systemBehavior, turtleInterpretor) = ConfigureNewLSystem(systemObject, turtleOperations);
            systemBehavior.logStates = true;

            var (sunlightCamera, renderingCamera) = GetCameras();
            yield return MeasureAcrossFrames(() =>
            {
                return AsyncCoroutine(FullLoopStep(systemBehavior, turtleInterpretor, renderingCamera));
            }, 5, 30);

        }

        yield return new ExitPlayMode();
    }


    [UnityTest, Performance]
    public IEnumerator ManySmallTreeSystemsStepAndRenderWithoutSunlight()
    {
        yield return new EnterPlayMode();
        yield return null;

        // this scene will be preconfigured with all the required context to get lsystems working
        var targetScene = EditorSceneManager.GetSceneByName("PERFORMANCE_TEST_SCENE");
        yield return EditorSceneManager.LoadSceneAsync(targetScene.buildIndex, UnityEngine.SceneManagement.LoadSceneMode.Single);
        yield return null;
        JobHandleExtensions.TrackPendingJobs = true;

        var systemObject = GetConfiguredLSystem(@"
#axiom a(10)F
#symbols +-\/^&
#iterations 10
#symbols Fna

a(x) : x > 0 -> FFF\[+a(x - 1)][-a(x - 1)]
a(x) : x <= 0 -> /F
");

        var turtleOperations = new List<TurtleOperationSet>();
        turtleOperations.Add(OrganPositioningTurtleInterpretorTests.GetDefaultMeshOperations(new[] { 'F' }));
        turtleOperations.Add(OrganPositioningTurtleInterpretorTests.GetDefaultRotateOperations(30));


        {
            var (systemBehavior, turtleInterpretor) = ConfigureNewLSystem(systemObject, turtleOperations);
            systemBehavior.logStates = true;

            var (sunlightCamera, renderingCamera) = GetCameras();
            yield return MeasureAcrossFrames(() =>
            {
                return AsyncCoroutine(FullLoopStep(systemBehavior, turtleInterpretor, renderingCamera));
            }, 5, 30);

        }

        yield return new ExitPlayMode();
    }
}
