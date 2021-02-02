using Dman.LSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;
using System.IO;
using System.Text;
using Dman.LSystem.SystemCompiler;
using System.Text.RegularExpressions;
using System.Linq;

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



