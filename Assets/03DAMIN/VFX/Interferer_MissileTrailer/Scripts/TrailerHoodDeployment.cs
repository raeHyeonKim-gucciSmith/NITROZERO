using UnityEngine;
namespace Damin.Trailer.MissileCar {
 [DisallowMultipleComponent] public sealed class TrailerHoodDeployment : MonoBehaviour {
  [Header("연결 — 새 연출용 차량 내부만")]
  public Transform vehicleFrame,hatch,hinge,lift;
  [Header("기존 보넷 연출에서 가져온 설정")]
  public Vector3 hingeAxisInVehicle=Vector3.left;
  public float openAngle=30;
  [Min(.01f)] public float openDuration=.8f;
  [Min(0)] public float delayAfterHatch=.1f;
  public Vector3 liftOffset=new Vector3(0,.025f,0);
  [Min(.01f)] public float liftDuration=.7f;
  [SerializeField,HideInInspector] Vector3 closedHatchPosition,closedLiftPosition;
  [SerializeField,HideInInspector] Quaternion closedHatchRotation=Quaternion.identity;
  [SerializeField,HideInInspector] bool captured;
  public float Duration=>Mathf.Max(.01f,openDuration)+Mathf.Max(0,delayAfterHatch)+Mathf.Max(.01f,liftDuration);
  public bool Ready=>vehicleFrame&&hatch&&hinge&&lift;
  public void CapturePose(){if(!Ready)return;closedHatchPosition=vehicleFrame.InverseTransformPoint(hatch.position);closedHatchRotation=Quaternion.Inverse(vehicleFrame.rotation)*hatch.rotation;closedLiftPosition=lift.localPosition;captured=true;}
  public void Apply(float elapsed){if(!Ready)return;if(!captured)CapturePose();float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/Mathf.Max(.01f,openDuration)));var axis=vehicleFrame.TransformDirection(hingeAxisInVehicle.sqrMagnitude>.0001f?hingeAxisInVehicle.normalized:Vector3.left);var q=Quaternion.AngleAxis(openAngle*t,axis);hatch.position=hinge.position+q*(vehicleFrame.TransformPoint(closedHatchPosition)-hinge.position);hatch.rotation=q*vehicleFrame.rotation*closedHatchRotation;float b=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-openDuration-delayAfterHatch)/Mathf.Max(.01f,liftDuration)));lift.localPosition=closedLiftPosition+liftOffset*b;}
  public void ResetPose()=>Apply(0);
  public void ApplyTimeline(float time,float hatchStart,float bayStart)
  {
   float h=Mathf.SmoothStep(0,1,Mathf.Clamp01((time-hatchStart)/Mathf.Max(.01f,openDuration)));
   ApplyTimelineAngle(time,bayStart,openAngle*h);
  }
  public void ApplyTimelineAngle(float time,float bayStart,float angle)
  {
   if(!Ready)return;if(!captured)CapturePose();
   float b=Mathf.SmoothStep(0,1,Mathf.Clamp01((time-bayStart)/Mathf.Max(.01f,liftDuration)));
   var axis=vehicleFrame.TransformDirection(hingeAxisInVehicle.sqrMagnitude>.0001f?hingeAxisInVehicle.normalized:Vector3.left);
   var q=Quaternion.AngleAxis(angle,axis);
   hatch.position=hinge.position+q*(vehicleFrame.TransformPoint(closedHatchPosition)-hinge.position);
   hatch.rotation=q*vehicleFrame.rotation*closedHatchRotation;lift.localPosition=closedLiftPosition+liftOffset*b;
  }
 }
}
