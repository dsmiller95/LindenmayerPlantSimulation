using Dman.LSystem.UnityObjects;
using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Dman.LSystem.Editor
{
    [ScriptedImporter(1, "lsystem")]
    public class LSystemAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var systemObject = ScriptableObject.CreateInstance<LSystemObject>();
            systemObject.LoadFromFilePath(ctx.assetPath);

            ctx.AddObjectToAsset("lSystem", systemObject);
            ctx.SetMainObject(systemObject);
        }
    }
}