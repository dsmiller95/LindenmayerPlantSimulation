using UnityEngine;

namespace Dman.LSystem.SystemRuntime.NativeJobsUtilities
{
    public class LSystemJobExecutionConfig : MonoBehaviour
    {

        private static LSystemJobExecutionConfig _instance;
        public static LSystemJobExecutionConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GameObject.FindObjectOfType<LSystemJobExecutionConfig>();
                    if(_instance == null)
                    {
                        Debug.LogError("No completable executor found. create a completable executor in the scene");
                    }
                }
                return _instance;
            }
        }

        public bool forceUpdates = true;
    }
}
