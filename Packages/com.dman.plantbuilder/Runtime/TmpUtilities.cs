using UnityEngine;

namespace PlantBuilder
{
    public static class Extensions
    {
        public static Vector3 AbsoluteValue(this Vector3 self)
        {
            return new Vector3(
                Mathf.Abs(self.x),
                Mathf.Abs(self.y),
                Mathf.Abs(self.z));
        }
        public static Vector3 ReciprocalByComponent(this Vector3 self)
        {
            return new Vector3(
                1 / self.x,
                1 / self.y,
                1 / self.z);
        }
    }
}
