using UnityEngine;
using UnityEngine.VFX;
namespace Damin.VFX.HoodMissile {
 // Motion belongs to the demo missile, not to any production vehicle or weapon controller.
 public sealed class MissileVFXFlight : MonoBehaviour {
  [SerializeField] private Transform exhaustPoint;
  [SerializeField] private VisualEffect trail;
  [SerializeField] private GameObject missileVisual;
  [Header("飛行 / Flight")]
  [Min(.1f)] [SerializeField] private float launchSpeed=3f;
  [Min(.1f)] [SerializeField] private float cruiseSpeed=7f;
  [Min(.01f)] [SerializeField] private float accelerationTime=.7f;
  [Min(.01f)] [SerializeField] private float turnDuration=.6f;
  [Min(.1f)] [SerializeField] private float flightDuration=3f;
  [Header("World-space exhaust")]
  [Min(0)] [SerializeField] private float trailSpawnRate=170f;
  [Min(.05f)] [SerializeField] private float trailSpeed=1.1f;
  [Min(.1f)] [SerializeField] private float trailLength=3.2f;
  [Min(.005f)] [SerializeField] private float trailWidth=.065f;
  [Min(0)] [SerializeField] private float trailTurbulence=.35f;
  [ColorUsage(false,true)] [SerializeField] private Color trailColor=new Color(.68f,.71f,.75f,1);
  [Min(.05f)] [SerializeField] private float maximumSmokeLifetime=4f;
  private Vector3 initialDirection,cruiseDirection,clearancePoint;
  private float age,afterStop,peakSmokeLifetime; private bool active,cleared,stopped;
  private Vector3 velocity;
  public bool ManualSimulation {get;set;}
  public bool Finished {get;private set;}
  public VisualEffect Trail=>trail;
  public Transform ExhaustPoint=>exhaustPoint;
  public Vector3 Velocity=>velocity;
  public float CurrentLifetime=>Mathf.Clamp(trailLength/Mathf.Max(.1f,velocity.magnitude+trailSpeed),.08f,maximumSmokeLifetime);
  public void Configure(float rate,float speed,float length,float width,float turbulence,Color color){trailSpawnRate=rate;trailSpeed=speed;trailLength=length;trailWidth=width;trailTurbulence=turbulence;trailColor=color;}
  public void Launch(Vector3 exitPoint,Vector3 direction){
   if(!trail||!exhaustPoint){Debug.LogError("[Hood Missile] Missile trail/exhaust reference missing.",this);return;}
   clearancePoint=exitPoint;initialDirection=(exitPoint-transform.position).normalized;if(initialDirection.sqrMagnitude<.1f)initialDirection=transform.forward;
   cruiseDirection=direction.normalized;if(cruiseDirection.sqrMagnitude<.1f)cruiseDirection=initialDirection;
   transform.rotation=Quaternion.LookRotation(initialDirection);velocity=initialDirection*launchSpeed;
   age=afterStop=0;peakSmokeLifetime=.1f;active=true;cleared=stopped=Finished=false;missileVisual.SetActive(true);
   trail.Reinit();SyncTrail();trail.Play();
  }
  private void Update(){if(!ManualSimulation)Advance(Time.deltaTime);}
  public void Advance(float dt){
   if(!active||Finished)return;dt=Mathf.Max(0,dt);
   if(stopped){afterStop+=dt;SyncTrail();if(afterStop>=peakSmokeLifetime+.2f){Finished=true;active=false;if(Application.isPlaying)Destroy(gameObject);}return;}
   age+=dt;
   if(!cleared){float distance=launchSpeed*dt;Vector3 delta=clearancePoint-transform.position;if(delta.magnitude<=distance){transform.position=clearancePoint;cleared=true;afterStop=0;}else transform.position+=initialDirection*distance;velocity=initialDirection*launchSpeed;}
   else {afterStop+=dt;float t=Mathf.Clamp01(afterStop/Mathf.Max(.01f,turnDuration));Vector3 dir=Vector3.Slerp(initialDirection,cruiseDirection,t*t*(3-2*t)).normalized;float speed=Mathf.Lerp(launchSpeed,cruiseSpeed,Mathf.Clamp01(afterStop/Mathf.Max(.01f,accelerationTime)));velocity=dir*speed;transform.position+=velocity*dt;transform.rotation=Quaternion.LookRotation(dir);}
   if(age>=flightDuration){stopped=true;afterStop=0;missileVisual.SetActive(false);}
   SyncTrail();
  }
  private void SyncTrail(){
   if(!stopped)peakSmokeLifetime=Mathf.Max(peakSmokeLifetime,CurrentLifetime);
   // Positions enter the graph only at Initialize; released smoke does NOT follow this Transform.
   trail.SetVector3("EmitterPosition",exhaustPoint.position);trail.SetVector3("EmitterDirection",exhaustPoint.forward);
   trail.SetFloat("TrailSpawnRate",Mathf.Max(0,trailSpawnRate));trail.SetFloat("TrailSpeed",Mathf.Max(.05f,trailSpeed));
   trail.SetFloat("TrailLength",Mathf.Max(.1f,trailLength));trail.SetFloat("MissileSpeed",velocity.magnitude);
   trail.SetFloat("TrailWidth",Mathf.Max(.005f,trailWidth));trail.SetFloat("TrailTurbulence",Mathf.Max(0,trailTurbulence));
   trail.SetVector3("TrailColor",new Vector3(trailColor.r,trailColor.g,trailColor.b));trail.SetFloat("TrailEmission",cleared&&!stopped?1:0);
   trail.SetFloat("MaxSmokeLifetime",maximumSmokeLifetime);
  }
 }
}
