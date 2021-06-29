using Dman.LSystem;
using Dman.LSystem.SystemRuntime.Turtle;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[RequireComponent(typeof(Camera))]
public class RenderTrigger : MonoBehaviour
{

    public RenderTexture texture;
    public float secondsPerUpdate = 5;
    private Texture2D targetTexture;
    // Start is called before the first frame update
    void Start()
    {
        var cam = this.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);

        targetTexture = new Texture2D(texture.width, texture.height, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
        targetTexture.alphaIsTransparency = false;
    }

    private float lastUpdate;

    // Update is called once per frame
    void Update()
    {
        if(lastUpdate + secondsPerUpdate > Time.time)
        {
            return;
        }
        lastUpdate = Time.time;
        RenderTexture.active = texture;

        targetTexture.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);

        RenderTexture.active = null;


        var textureClassificiations = new Dictionary<uint, int>();
        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                var pixelValue = new UIntFloatColor32((Color32)targetTexture.GetPixel(x, y)).UIntValue;

                pixelValue = BitMixer.UnMix(pixelValue);


                if (!textureClassificiations.TryGetValue(pixelValue, out var count))
                {
                    count = 0;
                }
                textureClassificiations[pixelValue] = count + 1;
            }
        }


        var result = new StringBuilder();
        foreach (var kvp in textureClassificiations)
        {
            result.Append($"{kvp.Key}: {kvp.Value}\n");
        }
        Debug.Log(result);

    }
}
