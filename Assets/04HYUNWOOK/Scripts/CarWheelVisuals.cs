using UnityEngine;

/// <summary>Keeps the tire meshes aligned with suspension, steering and wheel rotation.</summary>
[DisallowMultipleComponent]
public class CarWheelVisuals : MonoBehaviour
{
    [System.Serializable]
    public struct Wheel
    {
        public WheelCollider collider;
        public Transform visual;
        public Vector3 modelRotation;
    }

    public Wheel[] wheels;

    void LateUpdate()
    {
        if (wheels == null) return;
        foreach (var wheel in wheels)
        {
            if (!wheel.collider || !wheel.visual) continue;
            wheel.collider.GetWorldPose(out var position, out var rotation);
            wheel.visual.SetPositionAndRotation(position, rotation * Quaternion.Euler(wheel.modelRotation));
        }
    }
}
