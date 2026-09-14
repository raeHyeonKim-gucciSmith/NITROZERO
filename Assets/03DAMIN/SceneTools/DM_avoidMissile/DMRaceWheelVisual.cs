using System;
using UnityEngine;
namespace Damin.SceneOnly {
 [DisallowMultipleComponent,DefaultExecutionOrder(1500)]
 public sealed class DMRaceWheelVisual:MonoBehaviour {
  [Serializable] public sealed class Part {public Transform target;public Vector3 restPosition;public Quaternion restRotation;public bool caliper;}
  [Serializable] public sealed class Wheel {public string label;public Vector3 centre;public float radius=.35f;public bool front;public Part[] parts;}
  [Serializable] public sealed class ContactMesh {public Transform mesh;public Vector3[] supportPoints;}
  [Serializable] public sealed class TyreContact {public Transform hub;public ContactMesh[] meshes;}
  public Wheel[] wheels=Array.Empty<Wheel>();
  [Tooltip("최신 차량의 분리된 조향·회전 리그를 사용합니다. 회전은 TrailerCruiseMotion이 담당합니다.")]
  public bool useCruiseRig;
  [Tooltip("실제 타이어 형상으로 평면 도로 접지 높이를 보정합니다. 메시/Scale 변경 없이 바퀴 위치만 조정합니다.")]
  public bool keepTyresGrounded;
  public float roadSurfaceY=-29.65f;
  public TyreContact[] tyreContacts=Array.Empty<TyreContact>();
  [Tooltip("이 씬의 기존 회전/키보드 조작과 중복되지 않게 재생 중에만 비활성화합니다.")]
  public Behaviour[] conflictingDrivers=Array.Empty<Behaviour>();
  bool[] enabledBefore;bool borrowed;double distance;float steer;
  public float CurrentSteer=>steer;
  public double CurrentDistance=>distance;
  public void Begin(){if(borrowed)return;enabledBefore=new bool[conflictingDrivers.Length];for(int i=0;i<conflictingDrivers.Length;i++)if(conflictingDrivers[i]){enabledBefore[i]=conflictingDrivers[i].enabled;conflictingDrivers[i].enabled=false;}borrowed=true;}
  public void Sample(double metres,float degrees){Begin();distance=metres;steer=degrees;Apply();}
  void LateUpdate(){if(Application.isPlaying&&borrowed)Apply();}
  void Apply(){if(useCruiseRig){GroundTyres();return;}foreach(var w in wheels){
   var turn=Quaternion.AngleAxis(w.front?steer:0,Vector3.up);
   var spin=Quaternion.AngleAxis((float)(distance/Math.Max(.01f,w.radius)*Mathf.Rad2Deg%360),Vector3.right);
   foreach(var p in w.parts)if(p.target){var rot=turn*(p.caliper?Quaternion.identity:spin);p.target.SetPositionAndRotation(transform.TransformPoint(w.centre+rot*(p.restPosition-w.centre)),transform.rotation*rot*p.restRotation);}
  }}
  void GroundTyres(){if(!keepTyresGrounded)return;foreach(var tyre in tyreContacts){if(!tyre.hub)continue;float min=float.PositiveInfinity;foreach(var m in tyre.meshes)if(m.mesh)foreach(var p in m.supportPoints)min=Mathf.Min(min,m.mesh.TransformPoint(p).y);if(float.IsFinite(min)){float delta=roadSurfaceY-min;if(Mathf.Abs(delta)<.5f)tyre.hub.position+=Vector3.up*delta;}}}
  public void Release(){if(!borrowed)return;if(!useCruiseRig)foreach(var w in wheels)foreach(var p in w.parts)if(p.target)p.target.SetPositionAndRotation(transform.TransformPoint(p.restPosition),transform.rotation*p.restRotation);for(int i=0;i<conflictingDrivers.Length;i++)if(conflictingDrivers[i])conflictingDrivers[i].enabled=enabledBefore[i];borrowed=false;}
  void OnDisable(){if(Application.isPlaying)Release();}
 }
}
