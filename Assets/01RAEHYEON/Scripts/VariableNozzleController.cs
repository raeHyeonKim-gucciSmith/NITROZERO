using UnityEngine;

public sealed class VariableNozzleController : MonoBehaviour
{
    [Header("Nozzle petals")]
    [SerializeField] private Transform[] petalPivots = new Transform[17];

    [Header("Motion")]
    [SerializeField, Range(0f, 1f)] private float targetClosure;
    [SerializeField, Min(0f)] private float closeAngle = 15f;
    [SerializeField, Min(0f)] private float movementSpeed = 2f;

    private Quaternion[] openRotations;
    private float currentClosure;

    private void Awake()
    {
        openRotations = new Quaternion[petalPivots.Length];

        for (int i = 0; i < petalPivots.Length; i++)
        {
            if (petalPivots[i] != null)
            {
                openRotations[i] = petalPivots[i].localRotation;
            }
        }
    }

    private void Update()
    {
        currentClosure = Mathf.MoveTowards(
            currentClosure,
            targetClosure,
            movementSpeed * Time.deltaTime);

        ApplyClosure(currentClosure);
    }

    public void SetClosure(float value)
    {
        targetClosure = Mathf.Clamp01(value);
    }

    private void ApplyClosure(float closure)
    {
        Quaternion closingRotation = Quaternion.AngleAxis(
            closeAngle * closure,
            Vector3.up);

        for (int i = 0; i < petalPivots.Length; i++)
        {
            if (petalPivots[i] != null)
            {
                petalPivots[i].localRotation = openRotations[i] * closingRotation;
            }
        }
    }
}
