using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace Damin.VFX.HoodMissile {
 public sealed class HoodMissileVFXController : MonoBehaviour {
  [Header("독립 모형 / VFX 연결")]
  [SerializeField] private Transform hoodHinge,equipmentBay,interiorSmokeVolume,leftVentPoint,rightVentPoint,missileSpawnPoint,clearancePoint,flightDirection,launchBurstPoint;
  [SerializeField] private VisualEffect vehicleVFX;
  [SerializeField] private MissileVFXFlight missilePrefab;
  [Header("보넷과 장착구")]
  [Range(0,1)] [SerializeField] private float hoodOpenAmount=.25f;
  [SerializeField] private float referenceOpenAngle=30;
  [SerializeField] private Vector3 hingeLocalAxis=Vector3.left;
  [Min(.01f)] [SerializeField] private float hoodOpenDuration=.8f;
  [Min(0)] [SerializeField] private float delayAfterHatch=.1f;
  [SerializeField] private Vector3 bayDeployLocalOffset=new Vector3(0,.025f,0);
  [Min(.01f)] [SerializeField] private float bayDeployDuration=.7f;
  [Header("연출 타이밍 — 발사는 장착구 전개 후")]
  [Min(0)] [SerializeField] private float hoodStartDelay=.2f;
  [Min(.01f)] [SerializeField] private float interiorFillTime=.35f;
  [Min(0)] [SerializeField] private float sideLeakStart=.48f;
  [Min(.01f)] [SerializeField] private float sideLeakRiseTime=.45f;
  [Min(0)] [SerializeField] private float launchDelayAfterBay=.12f;
  [Min(0)] [SerializeField] private float sideHoldAfterLaunch=.12f;
  [Min(0)] [SerializeField] private float interiorHoldAfterLaunch=.4f;
  [Min(.05f)] [SerializeField] private float fadeOutTime=1.1f;
  [Header("내부에 고이는 연기")]
  [Min(0)] [SerializeField] private float interiorSmokeDensity=1f;
  [Min(0)] [SerializeField] private float interiorSmokeTurbulence=.022f;
  [ColorUsage(false)] [SerializeField] private Color smokeColor=new Color(.69f,.73f,.78f);
  [Header("좁은 틈의 좌우 누출")]
  [Min(0)] [SerializeField] private float sideLeakSpawnRate=90;
  [Min(0)] [SerializeField] private float sideLeakSpeed=.58f;
  [Min(0)] [SerializeField] private float sideLeakSpread=.20f;
  [Min(0)] [SerializeField] private float sideLeakTurbulence=.07f;
  [Range(0,.3f)] [SerializeField] private float sideAsymmetry=.10f;
  [Header("발사 / 미사일 Trail")]
  [Min(0)] [SerializeField] private float launchBurstIntensity=1;
  [Min(0)] [SerializeField] private float trailSpawnRate=170;
  [Min(.05f)] [SerializeField] private float trailSpeed=1.1f;
  [Min(.1f)] [SerializeField] private float trailLength=3.2f;
  [Min(.005f)] [SerializeField] private float trailWidth=.065f;
  [Min(0)] [SerializeField] private float trailTurbulence=.35f;
  [ColorUsage(false,true)] [SerializeField] private Color trailColor=new Color(.68f,.71f,.75f);
  [Header("입력 / 이벤트")]
  [SerializeField] private bool useKeyboard=true;
  [SerializeField] private UnityEvent onSequenceStarted=new UnityEvent(),onMissileLaunched=new UnityEvent(),onSequenceFinished=new UnityEvent();
  [SerializeField,HideInInspector] private Quaternion closedHingeRotation=Quaternion.identity;
  [SerializeField,HideInInspector] private Vector3 closedBayPosition;
  [SerializeField,HideInInspector] private bool poseCaptured;
  private float elapsed,leftVariation=1,rightVariation=1;
  private bool running,launched;private int sequenceNumber;
  private MissileVFXFlight currentMissile;
  public bool ManualSimulation {get;set;}
  public bool IsRunning=>running;
  public bool HasLaunched=>launched;
  public float Elapsed=>elapsed;
  public float LaunchTime=>hoodStartDelay+hoodOpenDuration+delayAfterHatch+bayDeployDuration+launchDelayAfterBay;
  public MissileVFXFlight CurrentMissile=>currentMissile;
  public VisualEffect VehicleVFX=>vehicleVFX;
  public void CaptureClosedPose(){if(running||!hoodHinge||!equipmentBay)return;closedHingeRotation=hoodHinge.localRotation;closedBayPosition=equipmentBay.localPosition;poseCaptured=true;}
  private void Awake(){if(!poseCaptured)CaptureClosedPose();if(vehicleVFX)Sync(0,0);}
  private void Update(){if(ManualSimulation)return;if(useKeyboard){if(ResetPressed()){ResetSequence();return;}if(ShiftPressed())PlaySequence();}Advance(Time.deltaTime);}
  public void PlaySequence(){
   if(running)return;if(!Ready()){Debug.LogError("[Hood Missile] Required references are missing. Use the complete standalone prefab.",this);return;}
   ResetSequence();var random=new System.Random(731+sequenceNumber++);leftVariation=1+((float)random.NextDouble()*2-1)*sideAsymmetry;rightVariation=1+((float)random.NextDouble()*2-1)*sideAsymmetry;
   vehicleVFX.Reinit();Sync(0,0);vehicleVFX.Play();running=true;onSequenceStarted.Invoke();
  }
  public void Advance(float dt){
   if(!running)return;elapsed+=Mathf.Max(0,dt);float hoodTime=elapsed-hoodStartDelay;
   float angle=referenceOpenAngle*Mathf.Clamp01(hoodOpenAmount)*Smooth(hoodTime/Mathf.Max(.01f,hoodOpenDuration));
   hoodHinge.localRotation=closedHingeRotation*Quaternion.AngleAxis(angle,hingeLocalAxis.sqrMagnitude>.0001f?hingeLocalAxis.normalized:Vector3.left);
   float bayT=(hoodTime-hoodOpenDuration-delayAfterHatch)/Mathf.Max(.01f,bayDeployDuration);equipmentBay.localPosition=closedBayPosition+bayDeployLocalOffset*Smooth(bayT);
   float interior=Smooth(elapsed/Mathf.Max(.01f,interiorFillTime))*(1-Smooth((elapsed-LaunchTime-interiorHoldAfterLaunch)/Mathf.Max(.05f,fadeOutTime)));
   float side=Smooth((elapsed-sideLeakStart)/Mathf.Max(.01f,sideLeakRiseTime))*(1-Smooth((elapsed-LaunchTime-sideHoldAfterLaunch)/Mathf.Max(.05f,fadeOutTime)));
   Sync(interior,side);
   if(currentMissile)currentMissile.Configure(trailSpawnRate,trailSpeed,trailLength,trailWidth,trailTurbulence,trailColor);
   if(!launched&&elapsed>=LaunchTime){launched=true;vehicleVFX.SendEvent("Launch");currentMissile=Instantiate(missilePrefab,missileSpawnPoint.position,missileSpawnPoint.rotation);currentMissile.name="Missile_Runtime";currentMissile.ManualSimulation=ManualSimulation;currentMissile.Configure(trailSpawnRate,trailSpeed,trailLength,trailWidth,trailTurbulence,trailColor);currentMissile.Launch(clearancePoint.position,flightDirection.forward);onMissileLaunched.Invoke();}
   if(elapsed>LaunchTime+Mathf.Max(sideHoldAfterLaunch,interiorHoldAfterLaunch)+fadeOutTime+2 && (!currentMissile||currentMissile.Finished)){running=false;Sync(0,0);onSequenceFinished.Invoke();}
  }
  public void ResetSequence(){
   running=launched=false;elapsed=0;if(!poseCaptured)CaptureClosedPose();
   if(poseCaptured){if(hoodHinge)hoodHinge.localRotation=closedHingeRotation;if(equipmentBay)equipmentBay.localPosition=closedBayPosition;}
   if(currentMissile){if(Application.isPlaying)Destroy(currentMissile.gameObject);else DestroyImmediate(currentMissile.gameObject);currentMissile=null;}
   if(vehicleVFX){vehicleVFX.Reinit();Sync(0,0);vehicleVFX.Stop();}
  }
  private bool Ready()=>hoodHinge&&equipmentBay&&interiorSmokeVolume&&leftVentPoint&&rightVentPoint&&missileSpawnPoint&&clearancePoint&&flightDirection&&launchBurstPoint&&vehicleVFX&&missilePrefab;
  private void Sync(float interior,float side){
   if(!vehicleVFX||!interiorSmokeVolume||!leftVentPoint||!rightVentPoint||!launchBurstPoint)return;
   Transform t=vehicleVFX.transform;
   vehicleVFX.SetVector3("InteriorCenter",t.InverseTransformPoint(interiorSmokeVolume.position));
   vehicleVFX.SetVector3("InteriorSize",new Vector3(t.InverseTransformVector(interiorSmokeVolume.TransformVector(Vector3.right)).magnitude,t.InverseTransformVector(interiorSmokeVolume.TransformVector(Vector3.up)).magnitude,t.InverseTransformVector(interiorSmokeVolume.TransformVector(Vector3.forward)).magnitude));
   vehicleVFX.SetVector3("LeftVentPosition",t.InverseTransformPoint(leftVentPoint.position));vehicleVFX.SetVector3("RightVentPosition",t.InverseTransformPoint(rightVentPoint.position));
   vehicleVFX.SetVector3("LeftVentDirection",t.InverseTransformDirection(leftVentPoint.forward));vehicleVFX.SetVector3("RightVentDirection",t.InverseTransformDirection(rightVentPoint.forward));vehicleVFX.SetVector3("BurstPosition",t.InverseTransformPoint(launchBurstPoint.position));
   vehicleVFX.SetFloat("InteriorLevel",interior);vehicleVFX.SetFloat("SideLevelLeft",side*leftVariation);vehicleVFX.SetFloat("SideLevelRight",side*rightVariation);
   vehicleVFX.SetFloat("InteriorSmokeDensity",Mathf.Max(0,interiorSmokeDensity));vehicleVFX.SetFloat("InteriorSmokeTurbulence",Mathf.Max(0,interiorSmokeTurbulence));
   vehicleVFX.SetFloat("SideLeakSpawnRate",Mathf.Max(0,sideLeakSpawnRate));vehicleVFX.SetFloat("SideLeakSpeed",Mathf.Max(0,sideLeakSpeed));vehicleVFX.SetFloat("SideLeakSpread",Mathf.Max(0,sideLeakSpread));vehicleVFX.SetFloat("SideLeakTurbulence",Mathf.Max(0,sideLeakTurbulence));
   vehicleVFX.SetFloat("LaunchBurstIntensity",Mathf.Max(0,launchBurstIntensity));vehicleVFX.SetVector3("SmokeColor",new Vector3(smokeColor.r,smokeColor.g,smokeColor.b));
  }
  private void OnDisable(){if(Application.isPlaying)ResetSequence();}
  private static float Smooth(float t){t=Mathf.Clamp01(t);return t*t*(3-2*t);}
  private static bool ShiftPressed(){
#if ENABLE_INPUT_SYSTEM
   if(Keyboard.current!=null)return Keyboard.current.leftShiftKey.wasPressedThisFrame||Keyboard.current.rightShiftKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
   return Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift);
#else
   return false;
#endif
  }
  private static bool ResetPressed(){
#if ENABLE_INPUT_SYSTEM
   if(Keyboard.current!=null)return Keyboard.current.rKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
   return Input.GetKeyDown(KeyCode.R);
#else
   return false;
#endif
  }
 }
}
