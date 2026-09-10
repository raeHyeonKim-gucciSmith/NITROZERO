using UnityEngine;
public sealed class GunProjectile : MonoBehaviour
{
    public float speed = 300f;
    public float lifetime = 3f;
    public float radius = .02f;
    public LayerMask collisionMask = ~0;
    Transform owner;
    float age;
    public void Launch(Transform source, float velocity) { owner=source; speed=velocity; age=0f; }
    void Update() { Advance(Time.deltaTime); }
    public void Advance(float seconds)
    {
        float distance = Mathf.Max(0f,speed)*seconds;
        var hits=Physics.SphereCastAll(transform.position,radius,transform.forward,distance,collisionMask,QueryTriggerInteraction.Ignore);
        float closest=float.PositiveInfinity;
        foreach(var hit in hits)
        {
            if(hit.transform.IsChildOf(transform) || (owner!=null && hit.transform.IsChildOf(owner))) continue;
            closest=Mathf.Min(closest,hit.distance);
        }
        if(!float.IsPositiveInfinity(closest))
        {
            transform.position+=transform.forward*closest;
            gameObject.SetActive(false); Destroy(gameObject); return;
        }
        transform.position+=transform.forward*distance;
        age+=seconds;
        if(age>=lifetime) { gameObject.SetActive(false); Destroy(gameObject); }
    }
}
