using UnityEngine;

namespace Assets.Scripts.UI.PlantData
{
    public class CameraOrbitMRI : MonoBehaviour
    {
        public float rotationSpeed;
        public bool rotate = true;
        public Vector3 axis = Vector3.up;

        public Camera camera;
        public float maxClippingPlane;
        public float timeToDoMri;
        public float timeBetweenMris;

        private void Start()
        {
            transform.Rotate(axis, Random.Range(0f, 360f));
        }

        private float lastMriBeginTime = 0;
        private void Update()
        {

            var time = Time.time;
            if (lastMriBeginTime + timeToDoMri > time)
            {
                var t = (time - lastMriBeginTime) / timeToDoMri;
                var mriFactor = (0.5f - Mathf.Abs(t - .5f)) * 2f;
                var clipPlane = mriFactor * maxClippingPlane;
                camera.nearClipPlane = clipPlane;
            }
            else if (lastMriBeginTime + timeToDoMri + timeBetweenMris > time)
            {
                if (rotate)
                {
                    transform.Rotate(axis, rotationSpeed * Time.deltaTime);
                }
            }
            else if (lastMriBeginTime + timeToDoMri + timeBetweenMris <= time)
            {
                lastMriBeginTime = time;
            }
        }
    }
}
