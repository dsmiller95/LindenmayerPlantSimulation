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
using System.Linq;
using Unity.PerformanceTesting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class FullRenderLoopPerformanceTests
{

    private static CustomProfileSampler customSamples;

    private class CustomProfileSampler
    {
        private Dictionary<string, SampleGroup> sampleGroup = new Dictionary<string, SampleGroup>();

        private Dictionary<string, int> aggregateSamples = new Dictionary<string, int>();

        public bool samplingEnabled;

        public void AddSampleGroup(SampleGroup group, bool aggregator = true)
        {
            sampleGroup[group.Name] = group;
            if (aggregator)
            {
                aggregateSamples[group.Name] = 0;
            }
        }

        public void AddSample(string sampleName, int additionalValue)
        {
            if (samplingEnabled)
                aggregateSamples[sampleName] += additionalValue;
        }

        public void SingleSample(string sampleName, int value)
        {
            if (samplingEnabled)
                Measure.Custom(sampleGroup[sampleName], value);
        }

        public void ApplySamples()
        {
            if (!samplingEnabled)
            {
                return;
            }
            foreach (var key in aggregateSamples.Keys.ToList())
            {
                Measure.Custom(sampleGroup[key], aggregateSamples[key]);
                aggregateSamples[key] = 0;
            }
        }
    }

    private static IEnumerator MeasureAcrossFrames(Func<IEnumerator> acrossFrame, int warmup, int sampleCount)
    {
        customSamples = new CustomProfileSampler();
        customSamples.AddSampleGroup(new SampleGroup("vertex count", SampleUnit.Undefined), false);
        customSamples.AddSampleGroup(new SampleGroup("SingleStepLifetime", SampleUnit.Millisecond), false);
        customSamples.samplingEnabled = false;

        var runNumber = 0;

        while (runNumber < warmup)
        {
            runNumber++;
            yield return acrossFrame();
        }

        customSamples.samplingEnabled = true;
        using (Measure.Frames().Scope("Frame Time"))
        {
            while (runNumber < (warmup + sampleCount))
            {
                yield return acrossFrame();
                customSamples.ApplySamples();

                runNumber++;
            }
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

    private static async UniTask FullGardenGrowth(LSystemBehavior[] behaviors, int stepsPer)
    {
        var primaryTask = UniTask.WhenAll(behaviors.Select(async x =>
        {
            for (int i = 0; i < stepsPer; i++)
            {
                await FullLoopStep(x, null, false);
            }
        }).ToList());

        var running = true;
        await UniTask.WhenAny(primaryTask, PipeJobUpdates());
        running = false;

        async UniTask PipeJobUpdates()
        {
            while (running)
            {
                JobHandleExtensions.PendingAsyncJobs.Complete();
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }
    }

    private static async UniTask FullLoopStep(LSystemBehavior behavior, Camera camera = null, bool pipeJobs = true)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Restart();

        behavior.StepSystem();

        var turtleTriggered = false;
        var triggeredFrame = 0;

        var turtle = behavior.GetComponent<TurtleInterpreterBehavior>();
        turtle.OnTurtleMeshUpdated += OnTurtleUpdate;

        void OnTurtleUpdate()
        {
            turtle.OnTurtleMeshUpdated -= OnTurtleUpdate;
            camera?.Render();
            turtleTriggered = true;
            triggeredFrame = Time.frameCount;
            customSamples.SingleSample("vertex count", turtle.GetComponent<MeshFilter>().mesh.vertexCount);
        }

        var startTime = DateTime.Now;

        while (true)
        {

            if ((DateTime.Now - startTime) >= TimeSpan.FromSeconds(10))
            {
                throw new Exception("timeout");
            }

            if (turtleTriggered && Time.frameCount >= triggeredFrame + 1)
            {
                break;
            }

            if (pipeJobs)
            {
                JobHandleExtensions.PendingAsyncJobs.Complete();
            }
            await UniTask.Yield(PlayerLoopTiming.Update);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        stopwatch.Stop();
        customSamples.SingleSample("SingleStepLifetime", (int)stopwatch.ElapsedMilliseconds);
    }


    public static LSystemBehavior ConfigureNewLSystem(LSystemObject systemObject, List<TurtleOperationSet> turtleOperations)
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

        return systemBehavior;
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
    public IEnumerator SimpleLine()
    {
        yield return new EnterPlayMode();
        yield return null;

        // this scene will be preconfigured with all the required context to get lsystems working
        var targetSceneIndex = SceneUtility.GetBuildIndexByScenePath("Test resources/PERFORMANCE_TEST_SCENE");
        yield return EditorSceneManager.LoadSceneAsync(targetSceneIndex, UnityEngine.SceneManagement.LoadSceneMode.Single);
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
            var systemBehavior = ConfigureNewLSystem(systemObject, turtleOperations);

            var (sunlightCamera, renderingCamera) = GetCameras();

            yield return MeasureAcrossFrames(() =>
            {
                return AsyncCoroutine(FullLoopStep(systemBehavior, renderingCamera));
            }, 5, 30);

        }

        yield return new ExitPlayMode();
    }

    [UnityTest, Performance]
    public IEnumerator LargeTreeLowVertexCount()
    {
        yield return new EnterPlayMode();
        yield return null;

        // this scene will be preconfigured with all the required context to get lsystems working
        var targetSceneIndex = SceneUtility.GetBuildIndexByScenePath("Test resources/PERFORMANCE_TEST_SCENE");
        yield return EditorSceneManager.LoadSceneAsync(targetSceneIndex, UnityEngine.SceneManagement.LoadSceneMode.Single);
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
            var systemBehavior = ConfigureNewLSystem(systemObject, turtleOperations);

            var (sunlightCamera, renderingCamera) = GetCameras();
            yield return MeasureAcrossFrames(() =>
            {
                return AsyncCoroutine(FullLoopStep(systemBehavior, renderingCamera));
            }, 5, 20);

        }

        yield return new ExitPlayMode();
    }

    [UnityTest, Performance]
    public IEnumerator LargeTreeHighVertexCount()
    {
        yield return new EnterPlayMode();
        yield return null;

        // this scene will be preconfigured with all the required context to get lsystems working
        var targetSceneIndex = SceneUtility.GetBuildIndexByScenePath("Test resources/PERFORMANCE_TEST_SCENE");
        yield return EditorSceneManager.LoadSceneAsync(targetSceneIndex, UnityEngine.SceneManagement.LoadSceneMode.Single);
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
        turtleOperations.Add(OrganPositioningTurtleInterpretorTests.GetDefaultMeshOperations(new[] { 'F' }, defaultMesh: Resources.GetBuiltinResource<Mesh>("Sphere.fbx")));
        turtleOperations.Add(OrganPositioningTurtleInterpretorTests.GetDefaultRotateOperations(30));


        {
            var systemBehavior = ConfigureNewLSystem(systemObject, turtleOperations);

            var (sunlightCamera, renderingCamera) = GetCameras();
            yield return MeasureAcrossFrames(() =>
            {
                return AsyncCoroutine(FullLoopStep(systemBehavior, renderingCamera));
            }, 5, 20);

        }

        yield return new ExitPlayMode();
    }

    [UnityTest, Performance]
    public IEnumerator ManyTreesLowVertexCount()
    {
        yield return new EnterPlayMode();
        yield return null;

        // this scene will be preconfigured with all the required context to get lsystems working
        var targetSceneIndex = SceneUtility.GetBuildIndexByScenePath("Test resources/PERFORMANCE_TEST_SCENE");
        yield return EditorSceneManager.LoadSceneAsync(targetSceneIndex, UnityEngine.SceneManagement.LoadSceneMode.Single);
        yield return null;
        JobHandleExtensions.TrackPendingJobs = true;

        var systemObject = GetConfiguredLSystem(@"
#axiom a(6)F
#symbols +-\/^&
#iterations 10
#symbols Fna

P(1/3) | a(x) : x > 0 -> FFF\[+a(x - 1)][-a(x - 1)]
P(2/3) | a(x) : x > 0 -> a(x)
a(x) : x <= 0 -> /F
");
        var turtleOperations = new List<TurtleOperationSet>();
        turtleOperations.Add(OrganPositioningTurtleInterpretorTests.GetDefaultMeshOperations(new[] { 'F' }));
        turtleOperations.Add(OrganPositioningTurtleInterpretorTests.GetDefaultRotateOperations(30));

        {
            var behaviors = new LSystemBehavior[20];
            for (int i = 0; i < behaviors.Length; i++)
            {
                behaviors[i] = ConfigureNewLSystem(systemObject, turtleOperations);
                var posOnGround = UnityEngine.Random.insideUnitCircle * 8;
                behaviors[i].transform.position += new Vector3(posOnGround.x, 0, posOnGround.y);
            }

            var (sunlightCamera, renderingCamera) = GetCameras();
            yield return null;
            yield return MeasureAcrossFrames(() =>
            {
                return AsyncCoroutine(FullGardenGrowth(behaviors, 25));
            }, 1, 2);
        }

        yield return new ExitPlayMode();
    }
    
    [UnityTest, Performance]
    public IEnumerator ManyTreesHighVertexCount()
    {
        yield return new EnterPlayMode();
        yield return null;

        // this scene will be preconfigured with all the required context to get lsystems working
        var targetSceneIndex = SceneUtility.GetBuildIndexByScenePath("Test resources/PERFORMANCE_TEST_SCENE");
        yield return EditorSceneManager.LoadSceneAsync(targetSceneIndex, UnityEngine.SceneManagement.LoadSceneMode.Single);
        yield return null;
        JobHandleExtensions.TrackPendingJobs = true;

        var systemObject = GetConfiguredLSystem(@"
#axiom a(6)F
#symbols +-\/^&
#iterations 10
#symbols Fna

P(1/3) | a(x) : x > 0 -> FFF\[+a(x - 1)][-a(x - 1)]
P(2/3) | a(x) : x > 0 -> a(x)
a(x) : x <= 0 -> /F
");
        var turtleOperations = new List<TurtleOperationSet>();
        turtleOperations.Add(OrganPositioningTurtleInterpretorTests.GetDefaultMeshOperations(new[] { 'F' }, defaultMesh: Resources.GetBuiltinResource<Mesh>("Sphere.fbx")));
        turtleOperations.Add(OrganPositioningTurtleInterpretorTests.GetDefaultRotateOperations(30));

        {
            var behaviors = new LSystemBehavior[20];
            for (int i = 0; i < behaviors.Length; i++)
            {
                behaviors[i] = ConfigureNewLSystem(systemObject, turtleOperations);
                var posOnGround = UnityEngine.Random.insideUnitCircle * 8;
                behaviors[i].transform.position += new Vector3(posOnGround.x, 0, posOnGround.y);
            }

            var (sunlightCamera, renderingCamera) = GetCameras();
            yield return null;
            yield return MeasureAcrossFrames(() =>
            {
                return AsyncCoroutine(FullGardenGrowth(behaviors, 25));
            }, 1, 2);
        }

        yield return new ExitPlayMode();
    }
}
