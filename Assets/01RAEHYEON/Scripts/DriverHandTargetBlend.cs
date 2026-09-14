using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Moves one internal hand IK target between a steering-wheel target and a
/// shifter, booster, or handbrake target. The original hand pose is retained at blend zero.
/// </summary>
[DefaultExecutionOrder(-400)]
public sealed class DriverHandTargetBlend : MonoBehaviour
{
    [SerializeField] private Transform steeringSource;
    [SerializeField] private Transform shifterSource;
    [SerializeField, Range(0f, 1f)] private float shifterBlend;
    [SerializeField] private Transform boosterSource;
    [SerializeField, Range(0f, 1f)] private float boosterBlend;
    [SerializeField] private Transform handbrakeSource;
    [SerializeField, Range(0f, 1f)] private float handbrakeBlend;
    [SerializeField] private TwoBoneIKConstraint rightArmIK;

    private Transform capturedSteeringSource;
    private Vector3 steeringPositionOffset;
    private Quaternion steeringRotationOffset;

    private void OnEnable()
    {
        CaptureSteeringOffset();
    }

    private void Update()
    {
        // Never overwrite a hand pose that is being calibrated in Edit mode.
        if (!Application.isPlaying || steeringSource == null || shifterSource == null)
        {
            return;
        }

        if (capturedSteeringSource != steeringSource)
        {
            CaptureSteeringOffset();
        }

        // Grip actions rotate the wrist; steering and shifting retain their
        // existing wrist behavior. A missing source cannot enable rotation.
        if (rightArmIK != null)
        {
            ref var ikData = ref rightArmIK.data;
            float boosterWrist = boosterSource != null ? boosterBlend : 0f;
            float handbrakeWrist = handbrakeSource != null ? handbrakeBlend : 0f;
            ikData.targetRotationWeight = Mathf.Max(boosterWrist, handbrakeWrist);
        }

        Vector3 steeringPosition = steeringSource.TransformPoint(steeringPositionOffset);
        Quaternion steeringRotation = steeringSource.rotation * steeringRotationOffset;

        // The shifter marker is the desired IK pose at blend one. Adjust that
        // marker on the lever to fine-tune the final hand contact.
        Vector3 handPosition = Vector3.Lerp(steeringPosition, shifterSource.position, shifterBlend);
        Quaternion handRotation = Quaternion.Slerp(steeringRotation, shifterSource.rotation, shifterBlend);

        // At zero, the existing steering-to-shifter animation is unchanged.
        if (boosterSource != null && boosterBlend > 0f)
        {
            handPosition = Vector3.Lerp(handPosition, boosterSource.position, boosterBlend);
            handRotation = Quaternion.Slerp(handRotation, boosterSource.rotation, boosterBlend);
        }

        if (handbrakeSource != null && handbrakeBlend > 0f)
        {
            handPosition = Vector3.Lerp(handPosition, handbrakeSource.position, handbrakeBlend);
            handRotation = Quaternion.Slerp(handRotation, handbrakeSource.rotation, handbrakeBlend);
        }

        transform.SetPositionAndRotation(handPosition, handRotation);
    }

    [ContextMenu("Capture Current Steering Offset")]
    public void CaptureSteeringOffset()
    {
        capturedSteeringSource = steeringSource;
        if (steeringSource == null)
        {
            return;
        }

        steeringPositionOffset = steeringSource.InverseTransformPoint(transform.position);
        steeringRotationOffset = Quaternion.Inverse(steeringSource.rotation) * transform.rotation;
    }
}
