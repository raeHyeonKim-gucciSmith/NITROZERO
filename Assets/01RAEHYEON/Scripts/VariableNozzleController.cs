using UnityEngine;

public sealed class VariableNozzleController : MonoBehaviour
{
    [Header("Nozzle petals")]
    [SerializeField] private Transform[] petalPivots = new Transform[17];

    [Header("Motion")]
    [SerializeField, Range(-1f, 1f)] private float targetClosure;
    [SerializeField, Min(0f)] private float closeAngle = 15f;
    [SerializeField, Min(0f)] private float movementSpeed = 2f;

    private Quaternion[] openRotations;
    private float currentClosure;
    private bool initialized;

    private void Awake()
    {
        InitializeOnce();
    }

    private void InitializeOnce()
    {
        if (initialized)
            return;

        openRotations = new Quaternion[petalPivots.Length];

        for (int i = 0; i < petalPivots.Length; i++)
        {
            if (petalPivots[i] != null)
            {
                openRotations[i] = petalPivots[i].localRotation;
            }
        }

        initialized = true;
    }

    private void Update()
    {
        InitializeOnce();

        currentClosure = Mathf.MoveTowards(
            currentClosure,
            targetClosure,
            movementSpeed * Time.deltaTime);

        ApplyClosure(currentClosure);
    }

    public void SetClosure(float value)
    {
        targetClosure = Mathf.Clamp(value, -1f, 1f);
    }

    public void ApplyClosureImmediate(float value)
    {
        InitializeOnce();
        targetClosure = Mathf.Clamp(value, -1f, 1f);
        currentClosure = targetClosure;
        ApplyClosure(currentClosure);
    }

    public void SetOpenRotations(Quaternion[] rotations)
    {
        if (rotations == null || rotations.Length != petalPivots.Length)
            return;

        openRotations = (Quaternion[])rotations.Clone();
        initialized = true;
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
