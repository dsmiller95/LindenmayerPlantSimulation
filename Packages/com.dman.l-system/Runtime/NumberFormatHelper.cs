using System.Globalization;
using System.Threading;
using UnityEngine;

namespace Dman.LSystem
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public static class NumberFormatHelper
    {
        static NumberFormatHelper()
        {
            InitializeInvariantNumberFormat();
        }

        /// <summary>
        /// Sets the NumberFormat of the CurrentThreads CurrentCulture to Invariant, for when deserializing l-systems.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void InitializeInvariantNumberFormat()
        {
            var currentThreadsCultureInfoClone = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            currentThreadsCultureInfoClone.NumberFormat = NumberFormatInfo.InvariantInfo;
            System.Threading.Thread.CurrentThread.CurrentCulture = currentThreadsCultureInfoClone;

            #if DEBUG
            Debug.Log($"NumberFormatHelper: CurrentCultures NumberFormat set to Invariant.");
            #endif
        }
    }
}
