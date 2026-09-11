using UnityEngine;

namespace VehicleTest
{
    public class TailWingController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the car test controller")]
        public CarTestController carController;

        [Tooltip("Wing transform that will rotate")]
        public Transform wingTransform;

        [Header("Deployment Settings")]
        [Tooltip("Speed (KM/H) at which the wing fully deploys")]
        public float deploySpeed = 120f;

        [Tooltip("Wing rotation when retracted (X axis)")]
        public float retractedAngle = 0f;

        [Tooltip("Wing rotation when deployed (X axis)")]
        public float deployedAngle = 25f;

        [Tooltip("How fast the wing rotates")]
        public float rotationSpeed = 5f;

        private float currentAngle;

        void Start()
        {
            if (wingTransform != null)
                currentAngle = wingTransform.localEulerAngles.x;
        }

        void Update()
        {
            if (carController == null || wingTransform == null)
                return;

            UpdateWingRotation();
        }

        void UpdateWingRotation()
        {
            float targetAngle = carController.currentSpeedKmh >= deploySpeed
                ? deployedAngle
                : retractedAngle;

            currentAngle = Mathf.Lerp(
                currentAngle,
                targetAngle,
                Time.deltaTime * rotationSpeed
            );

            Vector3 localEuler = wingTransform.localEulerAngles;
            localEuler.x = currentAngle;
            wingTransform.localEulerAngles = localEuler;
        }
    }
}
