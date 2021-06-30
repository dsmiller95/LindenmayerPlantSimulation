using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Sunlight
{
    [RequireComponent(typeof(Camera))]
    public class SunlightCamera: MonoBehaviour
    {
        public float sunlightPerSquareUnit = 1;

        public float totalSunlight => Mathf.Pow(GetComponent<Camera>().orthographicSize, 2) * sunlightPerSquareUnit;
        public float sunlightPerPixel => totalSunlight / (sunlightTexture.width * sunlightTexture.height);
        public RenderTexture sunlightTexture => GetComponent<Camera>().targetTexture;
    }
}
