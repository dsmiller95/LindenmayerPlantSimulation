using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Dman.LSystem.SystemRuntime.Turtle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Dman.LSystem.SystemRuntime.Sunlight
{
    public class SunlightCalculator
    {
        private SunlightCamera sunlightCamera;
        public SunlightCalculator(SunlightCamera sunlightCamera)
        {
            this.sunlightCamera = sunlightCamera;
        }

        public void ApplySunlightToSymbols(
            DependencyTracker<SymbolString<float>> symbolsTracker,
            CustomRuleSymbols customSymbols, int openBranchSymbol, int closeBranchSymbol)
        {
            if(!(customSymbols.hasSunlight && customSymbols.hasIdentifiers))
            {
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("Sunlight Texture Getting");

            var sunlightTexture = sunlightCamera.sunlightTexture;
            var targetTexture = new Texture2D(sunlightTexture.width, sunlightTexture.height, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
            targetTexture.alphaIsTransparency = false;

            RenderTexture.active = sunlightTexture;
            targetTexture.ReadPixels(new Rect(0f, 0f, sunlightTexture.width, sunlightTexture.height), 0, 0);
            RenderTexture.active = null;
            UnityEngine.Profiling.Profiler.EndSample();


            UnityEngine.Profiling.Profiler.BeginSample("Sunlight Texture Summation");
            var textureData = targetTexture.GetRawTextureData<uint>();
            using var organCounts = new NativeHashMap<uint, uint>(100, Allocator.TempJob);

            if(textureData.Length != targetTexture.width * targetTexture.height)
            {
                Debug.LogError("texture data size does not match pixel size");
            }

            var sunlightExposureJob = new SunlightExposureRule()
            {
                allOrganIds = textureData,
                organIdCounts = organCounts
            };

            var dependency = sunlightExposureJob.Schedule();
            dependency.Complete();
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("Sunlight result apply");

            using var tmpIdentityStack = new TmpNativeStack<SunlightExposureApplyJob.BranchIdentity>(10, Allocator.TempJob);
            var applyJob = new SunlightExposureApplyJob
            {
                symbols = symbolsTracker.Data,
                organIdCounts = organCounts,
                lastIdentityStack = tmpIdentityStack,

                sunlightPerPixel = sunlightCamera.sunlightPerPixel,
                branchOpen = openBranchSymbol,
                branchClose = closeBranchSymbol,
                customSymbols = customSymbols
            };

            dependency = applyJob.Schedule();
            symbolsTracker.RegisterDependencyOnData(dependency);

            dependency.Complete();
            UnityEngine.Profiling.Profiler.EndSample();


        }

    }
}
