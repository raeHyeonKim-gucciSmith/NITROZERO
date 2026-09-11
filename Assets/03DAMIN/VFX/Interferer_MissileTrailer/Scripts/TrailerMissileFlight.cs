using UnityEngine;
using UnityEngine.Events;
namespace Damin.Trailer.MissileCar {
 [DisallowMultipleComponent] public sealed class TrailerMissileFlight : MonoBehaviour {
  [Header("미사일 뒤쪽 VFX 연결점")]
  public Transform exhaustPoint;
  public UnityEvent onLaunched=new UnityEvent(),onFlightEnded=new UnityEvent();
  public bool Flying{get;private set;}
  public bool ManualSimulation{get;set;}
  Vector3 start,clearanceEnd,direction,up;Quaternion startRotation;float elapsed,clearanceTime,speed,acceleration,lifetime,currentSpeed;
  public void Launch(Vector3 forward,Vector3 liftDirection,float liftHeight,float forwardClearance,float liftTime,float initialSpeed,float speedAcceleration,float life){
   if(Flying)return;transform.SetParent(null,true);start=transform.position;startRotation=transform.rotation;direction=forward.sqrMagnitude>.001f?forward.normalized:transform.forward;up=liftDirection.sqrMagnitude>.001f?liftDirection.normalized:Vector3.up;
   clearanceEnd=start+up*Mathf.Max(0,liftHeight)+direction*Mathf.Max(0,forwardClearance);clearanceTime=Mathf.Max(.01f,liftTime);speed=Mathf.Max(0,initialSpeed);acceleration=Mathf.Max(0,speedAcceleration);lifetime=Mathf.Max(clearanceTime+.01f,life);elapsed=0;currentSpeed=speed;Flying=true;onLaunched.Invoke();
  }
  void Update(){if(!ManualSimulation)Advance(Time.deltaTime);}
  public void Advance(float dt){if(!Flying||dt<=0)return;float before=elapsed;elapsed+=dt;
   if(before<clearanceTime){float t=Mathf.Clamp01(elapsed/clearanceTime);transform.position=Vector3.Lerp(start,clearanceEnd,Mathf.SmoothStep(0,1,t));transform.rotation=Quaternion.Slerp(startRotation,Quaternion.LookRotation(direction,up),t);}
   float travel=Mathf.Max(0,elapsed-Mathf.Max(before,clearanceTime));if(travel>0){transform.position+=direction*(currentSpeed*travel+.5f*acceleration*travel*travel);currentSpeed+=acceleration*travel;transform.rotation=Quaternion.LookRotation(direction,up);}
   if(elapsed>=lifetime){Flying=false;onFlightEnded.Invoke();if(Application.isPlaying)Destroy(gameObject);else gameObject.SetActive(false);}
  }
 }
}
