using UnityEngine;

namespace Assets.Scripts.UI.PlantData
{
    public class Turntable: MonoBehaviour
    {
        public float rotationSpeed;
        public bool rotate = true;

        private void Start()
        {
            transform.Rotate(Vector3.up, Random.Range(0f, 360f));
        }

        private void Update()
        {
            if (rotate)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
