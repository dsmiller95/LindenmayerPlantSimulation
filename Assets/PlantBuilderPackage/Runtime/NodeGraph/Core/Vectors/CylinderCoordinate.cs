using UnityEngine;

namespace PlantBuilder.NodeGraph.Core.Vectors
{
    [System.Serializable]
    public struct CylinderCoordinate: System.IEquatable<CylinderCoordinate>
    {
        [SerializeField]
        public float y;
        /// <summary>
        /// angle away from the x axis, in radians
        /// </summary>
        [SerializeField]
        public float azimuth;
        /// <summary>
        /// Distance from the y axis
        /// </summary>
        [SerializeField]
        public float axialDistance;

        //public CylinderCoordinate(Vector3 cartesian)
        //{

        //}

        public Vector3 ToCartesian()
        {
            return new Vector3
            {
                y = y,
                x = axialDistance * Mathf.Cos(azimuth),
                z = axialDistance * Mathf.Sin(azimuth)
            };
        }

        public override bool Equals(object obj)
        {

            if(!(obj is CylinderCoordinate cyl))
            {
                return false;
            }
            return Equals(cyl);
        }

        public override string ToString()
        {
            return $"y: {y:F1} angle: {azimuth:F2} magnitude: {axialDistance:F1}";
        }

        public bool Equals(CylinderCoordinate other)
        {
            return other.axialDistance == axialDistance
                && other.azimuth == azimuth
                && other.y == y;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}