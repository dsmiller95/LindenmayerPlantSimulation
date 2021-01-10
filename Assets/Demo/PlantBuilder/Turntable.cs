using UnityEngine;

namespace Assets.Scripts.UI.PlantData
{
    public class Turntable: MonoBehaviour
    {
        public float rotationSpeed;

        private void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}
