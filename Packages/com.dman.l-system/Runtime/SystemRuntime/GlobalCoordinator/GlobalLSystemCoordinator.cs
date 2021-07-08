using Dman.LSystem.SystemRuntime.Sunlight;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.GlobalCoordinator
{
    /// <summary>
    /// responsible for all tasks which require coordination across all L-systems
    ///  for example:
    ///     providing sunlight information
    ///     ensuring all organs are unique between all l-systems, as well as inside the parent l-system
    /// </summary>
    public class GlobalLSystemCoordinator : MonoBehaviour
    {
        public SunlightCamera sunlightCamera;

        public uint uniqueIdMinSpaceRequired;
        [Tooltip("The multiplier used to increase the size of the compute buffer on each resize event")]
        public float idSpaceResizeMultiplier = 2;
        [Tooltip("The usage percentage of the current id space which will trigger a resize")]
        [Range(0, 1)]
        public float idSpaceResizeThreshold = 0.9f;


        public static GlobalLSystemCoordinator instance;

        private List<LSystemGlobalResourceHandle> allResourceReservations;

        private void Awake()
        {
            instance = this;
            allResourceReservations = new List<LSystemGlobalResourceHandle>();
        }

        public LSystemGlobalResourceHandle AllocateResourceHandle()
        {
            var lastReservation = allResourceReservations.LastOrDefault();
            uint originPoint = 0;
            if (lastReservation != null)
            {
                originPoint = lastReservation.uniqueIdOriginPoint + lastReservation.requestedNextReservationSize;
            }
            var newHandle = new LSystemGlobalResourceHandle(
                originPoint,
                uniqueIdMinSpaceRequired,
                allResourceReservations.Count,
                this);
            allResourceReservations.Add(newHandle);

            return newHandle;
        }

        private void Update()
        {

        }
        private void LateUpdate()
        {

        }

        /// <summary>
        /// make new room for all of the l-system resource reservations
        /// returns the total required size of all current reservations
        /// </summary>
        /// <returns></returns>
        public uint ResizeLSystemReservations()
        {
            uint currentOrigin = 1;
            //var layoutDescriptor = new StringBuilder();
            for (int i = 0; i < allResourceReservations.Count; i++)
            {
                var currentReservation = allResourceReservations[i];
                if (currentReservation.requestedNextReservationSize == 0)
                {
                    // skip this resource, and remove it from the list
                    allResourceReservations.RemoveAt(i);
                    i--;
                    continue;
                }
                currentReservation.uniqueIdOriginPoint = currentOrigin;

                currentReservation.uniqueIdReservationSize = currentReservation.requestedNextReservationSize;
                //layoutDescriptor.Append($"{currentOrigin}-{currentReservation.uniqueIdReservationSize}->");

                currentOrigin += currentReservation.uniqueIdReservationSize;
            }
            //Debug.Log(layoutDescriptor.ToString());

            return currentOrigin;
        }

    }
}
