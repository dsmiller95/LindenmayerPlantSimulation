using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Dman.LSystem
{
    public class LSystemDeveloper : MonoBehaviour
    {
        public float secondsPerUpdate = 0.25f;
        public float timeBeforeRestart = 5;

        public LSystemObject systemObject;

        private FileSystemWatcher lSystemAssetWatcher;

        private void Awake()
        {
            var assetPath = AssetDatabase.GetAssetPath(systemObject);
            var directoryName = Path.GetDirectoryName(assetPath);
            var fileName = Path.GetFileName(assetPath);

            Debug.Log($"\"{directoryName}\" , \"{fileName}\"");
            lSystemAssetWatcher = new FileSystemWatcher(Path.GetDirectoryName(assetPath), Path.GetFileName(assetPath));
            lSystemAssetWatcher.Changed += AssetUpdated;

            lSystemAssetWatcher.NotifyFilter = NotifyFilters.LastWrite;
            lSystemAssetWatcher.EnableRaisingEvents = true;
        }

        private void Start()
        {
            systemObject.Compile();
        }

        private bool recompileTriggered = false;
        private void AssetUpdated(object sender, FileSystemEventArgs e)
        {
            Debug.Log("updated");
            this.recompileTriggered = true;
        }
        private void DoRecompile()
        {
            systemObject.TriggerReloadFromFile();
            systemObject.Compile();
        }

        private void OnDestroy()
        {
            lSystemAssetWatcher.Dispose();
        }
 
        private void Update()
        {
            if (recompileTriggered)
            {
                recompileTriggered = false;
                this.DoRecompile();
            }
            var maxUpdates = systemObject.iterations;

            foreach (var system in GetComponentsInChildren<LSystemBehavior>())
            {
                if (system.lastUpdateChanged
                    && system.totalSteps < maxUpdates 
                    && Time.time > system.lastUpdateTime + secondsPerUpdate)
                {
                    system.StepSystem();
                }
                else if (
                    (!system.lastUpdateChanged
                    || system.totalSteps >= maxUpdates)
                    && Time.time > system.lastUpdateTime + timeBeforeRestart)
                {
                    system.ResetState();
                }
            }
        }
    }
}
