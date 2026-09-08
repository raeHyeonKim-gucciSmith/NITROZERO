using UnityEngine;

public sealed class BoosterCoverController : MonoBehaviour
{
    [System.Serializable]
    public sealed class Cover
    {
        public Transform pivot;
        public Vector3 localAxis = Vector3.right;
        public float openAngle = 55f;
        [Tooltip("Displacement in the hinge parent's local space: X outward, Y up, Z toward the roof.")]
        public Vector3 openOffset;
        [System.NonSerialized] public Quaternion closedRotation;
        [System.NonSerialized] public Vector3 closedPosition;
    }

    [SerializeField, Range(0f, 1f)] private float openAmount;
    [SerializeField] private Cover[] covers = new Cover[0];

    [Header("Back Track")]
    [SerializeField] private Transform backTrack;
    [SerializeField] private Vector3 backTrackOpenOffset = new Vector3(0f, 0.05f, 0f);
    [SerializeField] private Vector3 backTrackOpenEulerAngles = new Vector3(9.46f, 0f, 0f);

    private Vector3 backTrackClosedPosition;
    private Quaternion backTrackClosedRotation;

    private void Awake()
    {
        foreach (Cover cover in covers)
            if (cover != null && cover.pivot != null)
            {
                cover.closedRotation = cover.pivot.localRotation;
                cover.closedPosition = cover.pivot.localPosition;
            }

        if (backTrack != null)
        {
            backTrackClosedPosition = backTrack.localPosition;
            backTrackClosedRotation = backTrack.localRotation;
        }
    }

    private void LateUpdate()
    {
        float amount = Mathf.Clamp01(openAmount);
        float movement = Mathf.SmoothStep(0f, 1f, amount);
        foreach (Cover cover in covers)
        {
            if (cover == null || cover.pivot == null || cover.localAxis.sqrMagnitude < 0.000001f)
                continue;
            cover.pivot.localRotation = cover.closedRotation * Quaternion.AngleAxis(
                cover.openAngle * amount, cover.localAxis.normalized);
            cover.pivot.localPosition = cover.closedPosition + cover.openOffset * movement;
        }


        if (backTrack != null)
        {
            backTrack.localPosition = backTrackClosedPosition + backTrackOpenOffset * movement;
            backTrack.localRotation = backTrackClosedRotation
                * Quaternion.Euler(backTrackOpenEulerAngles * movement);
        }
    }

    public void SetOpenAmount(float value)
    {
        openAmount = Mathf.Clamp01(value);
    }
}
