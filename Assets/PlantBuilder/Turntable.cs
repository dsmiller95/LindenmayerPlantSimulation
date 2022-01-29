using UnityEngine;

namespace Assets.Scripts.UI.PlantData
{
    public class Turntable : MonoBehaviour
    {
        public float rotationSpeed;
        public bool rotate = true;
        public Vector3 axis = Vector3.up;

        private void Start()
        {
            transform.Rotate(axis, Random.Range(0f, 360f));
        }

        private void Update()
        {
            if (rotate)
            {
                transform.Rotate(axis, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
