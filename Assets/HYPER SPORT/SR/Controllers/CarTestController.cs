using UnityEngine;

namespace VehicleTest
{
    [RequireComponent(typeof(Rigidbody))]
    public class CarTestController : MonoBehaviour
    {
        [Header("Wheel Colliders")]
        public WheelCollider frontLeftCollider;
        public WheelCollider frontRightCollider;
        public WheelCollider rearLeftCollider;
        public WheelCollider rearRightCollider;

        [Header("Wheel Meshes")]
        public Transform frontLeftMesh;
        public Transform frontRightMesh;
        public Transform rearLeftMesh;
        public Transform rearRightMesh;

        [Header("Drive Settings")]
        public float motorTorque = 1500f;
        public float brakeTorque = 3000f;
        public float maxSteerAngle = 30f;

        [Header("Speed (Read Only)")]
        public float currentSpeedKmh;
        public float maxAeroSpeed = 180f;

        [Range(0f, 1f)]
        public float normalizedSpeed;

        [Header("State (Read Only)")]
        public bool isBraking;

        private Rigidbody rb;

        private float throttleInput;
        private float steerInput;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
        }

        void Update()
        {
            ReadInput();
            UpdateWheelMeshes();
            UpdateSpeed();
        }

        void FixedUpdate()
        {
            HandleMotor();
            HandleSteering();
            HandleBraking();
        }

        #region Input
        void ReadInput()
        {
            throttleInput = Input.GetAxis("Vertical");   // W / S
            steerInput = Input.GetAxis("Horizontal");   // A / D
            isBraking = Input.GetKey(KeyCode.Space);
        }
        #endregion

        #region Driving
        void HandleMotor()
        {
            float torque = throttleInput * motorTorque;

            rearLeftCollider.motorTorque = torque;
            rearRightCollider.motorTorque = torque;
        }

        void HandleSteering()
        {
            float steer = steerInput * maxSteerAngle;

            frontLeftCollider.steerAngle = steer;
            frontRightCollider.steerAngle = steer;
        }

        void HandleBraking()
        {
            float brake = isBraking ? brakeTorque : 0f;

            frontLeftCollider.brakeTorque = brake;
            frontRightCollider.brakeTorque = brake;
            rearLeftCollider.brakeTorque = brake;
            rearRightCollider.brakeTorque = brake;
        }
        #endregion

        #region Wheels
        void UpdateWheelMeshes()
        {
            UpdateSingleWheel(frontLeftCollider, frontLeftMesh);
            UpdateSingleWheel(frontRightCollider, frontRightMesh);
            UpdateSingleWheel(rearLeftCollider, rearLeftMesh);
            UpdateSingleWheel(rearRightCollider, rearRightMesh);
        }

        void UpdateSingleWheel(WheelCollider collider, Transform mesh)
        {
            collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            mesh.position = pos;
            mesh.rotation = rot;
        }
        #endregion

        #region Speed
        void UpdateSpeed()
        {
            currentSpeedKmh = rb.linearVelocity.magnitude * 3.6f;
            normalizedSpeed = Mathf.Clamp01(currentSpeedKmh / maxAeroSpeed);
        }
        #endregion
    }
}
