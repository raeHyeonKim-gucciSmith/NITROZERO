using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
[RequireComponent(typeof(ArcadeCarController))]
public sealed class CarBoost : MonoBehaviour {
 [Range(0,100)] public float charge;
 [Min(1)] public float rechargeSeconds=30;
 [Min(.1f)] public float duration=2;
 [Min(1)] public float extraSpeedKmh=80;
 public bool IsBoosting=>remaining>0;
 float remaining;ArcadeCarController car;Rigidbody body;
 void Awake(){car=GetComponent<ArcadeCarController>();body=GetComponent<Rigidbody>();}
 void Update(){
 if(car.ControlsLocked||car.FinishBraking){remaining=0;return;}
 if(remaining>0)remaining=Mathf.Max(0,remaining-Time.deltaTime);
 else if(car.SpeedKmh>5&&car.IsGrounded)charge=Mathf.Min(100,charge+100*Time.deltaTime/rechargeSeconds);
 bool pressed=false;
#if ENABLE_INPUT_SYSTEM
 pressed=Keyboard.current!=null&&Keyboard.current.leftShiftKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
 pressed=Input.GetKeyDown(KeyCode.LeftShift);
#endif
 if(pressed)TryActivate();
 }
 public bool TryActivate(){if(charge<100||IsBoosting||car.ControlsLocked||car.FinishBraking||!car.IsGrounded)return false;charge=0;remaining=duration;return true;}
 void FixedUpdate(){if(!IsBoosting||!car.IsGrounded||car.ControlsLocked||car.FinishBraking)return;
 var forward=transform.forward;float speed=Vector3.Dot(body.linearVelocity,forward);
 float target=(car.CurrentGearSpeedLimit+extraSpeedKmh)/3.6f;
 if(speed<target)body.AddForce(forward*Mathf.Min(45,(target-speed)/Time.fixedDeltaTime),ForceMode.Acceleration);
 }
}
