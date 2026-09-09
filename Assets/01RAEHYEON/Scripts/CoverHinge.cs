using UnityEngine;

public sealed class CoverHinge : MonoBehaviour
{
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.035f);
        Gizmos.DrawLine(transform.position - transform.right * 0.2f,
            transform.position + transform.right * 0.2f);
    }
}
