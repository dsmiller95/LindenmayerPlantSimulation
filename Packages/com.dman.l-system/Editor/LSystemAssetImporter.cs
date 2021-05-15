using Dman.LSystem.UnityObjects;
using System.IO;
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
            var lSystemCode = File.ReadAllText(ctx.assetPath);
            systemObject.ParseRulesFromCode(lSystemCode);

            ctx.AddObjectToAsset("lSystem", systemObject);
            ctx.SetMainObject(systemObject);
        }
    }
}