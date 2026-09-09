using UnityEngine;

/// <summary>
/// Stores the dimensions of the flat center corridor prepared for the asphalt road.
/// The object has no renderer or collider and is safe to keep in the final scene.
/// </summary>
public sealed class MoonRoadPlacementGuide : MonoBehaviour
{
    public float mapLength = 4000f;
    public float flatWidth = 100f;
    public float blendedWidth = 250f;
    public float roadSurfaceY;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.72f, 0.12f, 0.85f);
        Gizmos.DrawWireCube(transform.position, new Vector3(mapLength, 0.5f, flatWidth));
        Gizmos.color = new Color(1f, 0.72f, 0.12f, 0.25f);
        Gizmos.DrawWireCube(transform.position, new Vector3(mapLength, 0.25f, blendedWidth));
    }
#endif
}
