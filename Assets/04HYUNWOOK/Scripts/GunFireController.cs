using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
[RequireComponent(typeof(AudioSource))]
public sealed class GunFireController : MonoBehaviour
{
    public Transform muzzle;
    public GunProjectile projectilePrefab;
    public GunSlideRecoil recoil;
    public AudioClip shotSound;
    public bool fireWithLeftMouse = true;
    [Min(1)] public float projectileSpeed = 300f;
    [Min(.01f)] public float shotInterval = .18f;
    [Range(0,1)] public float shotVolume = .65f;
    float nextShot;
    AudioSource source;
    void Awake()
    {
        source=GetComponent<AudioSource>();
        if(recoil==null)recoil=GetComponent<GunSlideRecoil>();
        if(recoil!=null)recoil.previewWithLeftMouse=false;
    }
    void Update()
    {
        bool pressed=false;
#if ENABLE_INPUT_SYSTEM
        pressed=Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        pressed=Input.GetMouseButtonDown(0);
#endif
        if(fireWithLeftMouse && pressed) Fire();
    }
    public bool Fire()
    {
        if(!Application.isPlaying || muzzle==null || projectilePrefab==null || Time.time<nextShot)return false;
        nextShot=Time.time+shotInterval;
        var shot=Instantiate(projectilePrefab,muzzle.position,muzzle.rotation);
        float scale = Mathf.Max(.0001f, Mathf.Abs(transform.lossyScale.x));
        shot.transform.localScale *= scale; shot.radius *= scale;
        shot.Launch(transform,projectileSpeed);
        if(recoil!=null)recoil.PlayRecoil();
        if(shotSound!=null)source.PlayOneShot(shotSound,shotVolume);
        return true;
    }
}

