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
    [RequireComponent(typeof(TurtleInterpreterBehavior))]
    [RequireComponent(typeof(LSystemBehavior))]
    public class LSystemDeveloper : MonoBehaviour
    {
        public float secondsPerUpdate;
        public float timeBeforeRestart = 5;
        private LSystemBehavior System => GetComponent<LSystemBehavior>();
        private TurtleInterpreterBehavior Turtle => GetComponent<TurtleInterpreterBehavior>();

        private FileSystemWatcher lSystemAssetWatcher;

        private void Awake()
        {
            var assetPath = AssetDatabase.GetAssetPath(System.systemObject);
            var directoryName = Path.GetDirectoryName(assetPath);
            var fileName = Path.GetFileName(assetPath);

            Debug.Log($"\"{directoryName}\" , \"{fileName}\"");
            lSystemAssetWatcher = new FileSystemWatcher(Path.GetDirectoryName(assetPath), Path.GetFileName(assetPath));
            lSystemAssetWatcher.Changed += AssetUpdated;

            lSystemAssetWatcher.NotifyFilter = NotifyFilters.LastWrite;
            lSystemAssetWatcher.EnableRaisingEvents = true;
        }

        private bool recompileTriggered = false;
        private void AssetUpdated(object sender, FileSystemEventArgs e)
        {
            Debug.Log("updated");
            this.recompileTriggered = true;
        }
        private void DoRecompile()
        {
            System.systemObject.TriggerReloadFromFile();
            System.Recompile();
            FinishSimulationStepping();
            lastUpdate = 0;
        }

        private void OnDestroy()
        {
            lSystemAssetWatcher.Dispose();
        }
 
        private float lastUpdate;
        private int currentUpdates = 0;
        private void Update()
        {
            if (recompileTriggered)
            {
                recompileTriggered = false;
                this.DoRecompile();
            }
            var maxUpdates = System.systemObject.iterations;
            if (currentUpdates < maxUpdates && Time.time > lastUpdate + secondsPerUpdate)
            {
                lastUpdate = Time.time;
                UpdateMeshAndSystem();
                currentUpdates++;
            }
            else if (currentUpdates >= maxUpdates && Time.time > lastUpdate + timeBeforeRestart)
            {
                lastUpdate = Time.time;
                currentUpdates = 0;
                System.ResetState();
            }
        }

        private void FinishSimulationStepping()
        {
            currentUpdates = System.systemObject.iterations;
        }

        private void UpdateMeshAndSystem()
        {
            if (!System.systemValid)
            {
                FinishSimulationStepping();
                return;
            }
            Turtle.InterpretSymbols(System.CurrentState);
            if (!System.StepSystem())
            {
                FinishSimulationStepping();
            }
        }
    }
}
