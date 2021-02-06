﻿using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    /// <summary>
    /// Used to assist with developing an L-system. will watch the l-system file and live-reload when changes are detected
    /// </summary>
    public class LSystemDeveloper : MonoBehaviour
    {
        public float secondsPerUpdate = 0.25f;
        public float timeBeforeRestart = 5;

        /// <summary>
        /// the system object to compile
        /// </summary>
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

            foreach (var system in GetComponentsInChildren<LSystemBehavior>())
            {
                system.SetSystem(systemObject);
            }
        }

        private void Start()
        {
            systemObject.Compile();
        }

        private bool recompileTriggered = false;
        private void AssetUpdated(object sender, FileSystemEventArgs e)
        {
            Debug.Log("updated");
            recompileTriggered = true;
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
                DoRecompile();
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