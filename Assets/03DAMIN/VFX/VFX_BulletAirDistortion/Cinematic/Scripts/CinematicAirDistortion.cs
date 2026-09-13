using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Damin.VFX.AirDistortion.Cinematic
{
    [DefaultExecutionOrder(100),DisallowMultipleComponent]
    public sealed class CinematicAirDistortion : MonoBehaviour
    {
        [InspectorName("따라갈 총알")] public Transform Target;
        public Camera ViewCamera;
        public Material DistortionMaterial;
        [Header("형태 / 슬로모션 이동 속도와 독립")]
        [Min(.01f)] public float CoreLength=.24f;
        [Min(.01f)] public float DistortionLength=.55f;
        [Min(.01f)] public float DistortionWidth=.28f;
        [Range(0,2)] public float DistortionStrength=1;
        [Range(0,64),Tooltip("배경이 밀리는 화면 픽셀 크기")]
        public float RefractionOffset=22;
        [Range(0,1)] public float OpacityMask=1;
        [Range(1,10)] public float NoiseScale=4;
        [Range(0,10)] public float NoiseSpeed=4;
        [Range(0,2)] public float FineDetail=.6f;
        [Range(.03f,.5f)] public float TrailFade=.2f;
        [Header("속도 반응 / 영화 슬로모션은 측정 끄기")]
        public bool MeasureTargetSpeed;
        [Min(0)] public float BulletSpeed=60;
        [Min(1)] public float FullStrengthSpeed=60;
        [Min(1)] public float TeleportDistance=10;
        public bool ManualSimulation {get;set;}
        public float SpeedFactor {get;private set;}
        public float CurrentSpeed {get;private set;}
        public float CurrentWakeLength=>DistortionLength;
        Mesh mesh;MeshRenderer output;Camera backgroundCamera;RenderTexture background;
        MaterialPropertyBlock props;Transform bound;Renderer[] subjectRenderers;bool[] savedRendering;
        Vector3 lastPosition,heading=Vector3.right;float clock,fade,appearance;bool tracked;
        void OnEnable(){if(Application.isPlaying)Create();}
        void LateUpdate(){if(!ManualSimulation)Simulate(Time.deltaTime);}
        void Create(){
            if(mesh)return;
            var part=new GameObject("AirEnvelope_RefractionOnly");part.transform.SetParent(transform,false);part.layer=gameObject.layer;
            mesh=new Mesh{name="CinematicAirEnvelope"};mesh.MarkDynamic();part.AddComponent<MeshFilter>().sharedMesh=mesh;
            output=part.AddComponent<MeshRenderer>();output.sharedMaterial=DistortionMaterial;output.shadowCastingMode=ShadowCastingMode.Off;output.receiveShadows=false;
            props=new MaterialPropertyBlock();
            var go=new GameObject("AirBackgroundCapture_RuntimeOnly"){hideFlags=HideFlags.DontSave};backgroundCamera=go.AddComponent<Camera>();backgroundCamera.enabled=false;
            var data=backgroundCamera.GetUniversalAdditionalCameraData();data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;data.renderPostProcessing=false;
        }
        public void SetTarget(Transform target){Target=target;ClearTrail();}
        public void Detach(){Target=null;}
        public void ClearTrail(){tracked=false;bound=null;fade=appearance=0;clock=0;if(mesh)mesh.Clear();}
        public void Simulate(float dt){
            if(!isActiveAndEnabled||dt<=0)return;Create();clock+=dt;
            var camera=ViewCamera?ViewCamera:Camera.main;if(!camera||!DistortionMaterial){output.enabled=false;return;}
            bool active=Target&&Target.gameObject.activeInHierarchy;
            if(active){
                if(bound!=Target||!tracked){bound=Target;subjectRenderers=Target.GetComponentsInChildren<Renderer>(true);savedRendering=new bool[subjectRenderers.Length];lastPosition=Target.position;heading=Target.forward;tracked=true;appearance=0;}
                Vector3 delta=Target.position-lastPosition;float distance=delta.magnitude;
                if(distance>TeleportDistance){appearance=0;distance=0;delta=Vector3.zero;heading=Target.forward;}
                if(distance>.00001f)heading=delta/distance;
                CurrentSpeed=MeasureTargetSpeed?distance/dt:BulletSpeed;
                SpeedFactor=Mathf.SmoothStep(0,1,Mathf.Clamp01(CurrentSpeed/Mathf.Max(1,FullStrengthSpeed)));
                lastPosition=Target.position;appearance=Mathf.Min(1,appearance+dt/.12f);fade=SpeedFactor*Mathf.SmoothStep(0,1,appearance);
            }else{tracked=false;fade=Mathf.Max(0,fade-dt/Mathf.Max(.03f,TrailFade));}
            if(fade<=0||DistortionStrength<=0||OpacityMask<=0||RefractionOffset<=0){output.enabled=false;return;}
            CaptureBackground(camera);
            Vector3 side=Vector3.Cross(camera.transform.forward,heading).normalized;if(side.sqrMagnitude<.001f)side=camera.transform.up;
            float front=CoreLength*.5f,back=-(DistortionLength+front),half=DistortionWidth*.5f;
            mesh.Clear();mesh.vertices=new[]{Local(lastPosition+heading*back-side*half,camera),Local(lastPosition+heading*front-side*half,camera),Local(lastPosition+heading*front+side*half,camera),Local(lastPosition+heading*back+side*half,camera)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();
            props.Clear();props.SetTexture("_AirBackground",background);props.SetFloat("_Strength",DistortionStrength*fade);props.SetFloat("_Pixels",RefractionOffset);
            props.SetFloat("_Mask",OpacityMask);props.SetFloat("_NoiseScale",NoiseScale);props.SetFloat("_NoiseSpeed",NoiseSpeed);props.SetFloat("_FineDetail",FineDetail);props.SetFloat("_Clock",clock);
            output.sharedMaterial=DistortionMaterial;output.SetPropertyBlock(props);output.enabled=true;
        }
        // Keep the screen footprint but put refraction behind the complete projectile, including glass.
        Vector3 Local(Vector3 p,Camera camera){Vector3 ray=p-camera.transform.position;float depth=Mathf.Max(.01f,Vector3.Dot(ray,camera.transform.forward));return transform.InverseTransformPoint(p+ray*(.08f/depth));}
        void CaptureBackground(Camera view){
            int width=Mathf.Max(16,view.pixelWidth),height=Mathf.Max(16,view.pixelHeight);
            if(!background||background.width!=width||background.height!=height){if(background){background.Release();Destroy(background);}background=new RenderTexture(width,height,24,RenderTextureFormat.ARGBHalf){name="AirBackgroundWithoutProjectile"};background.Create();}
            backgroundCamera.CopyFrom(view);backgroundCamera.enabled=false;backgroundCamera.targetTexture=background;backgroundCamera.transform.SetPositionAndRotation(view.transform.position,view.transform.rotation);
            var data=backgroundCamera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;
            bool outputHidden=output.forceRenderingOff;
            try{
                output.forceRenderingOff=true;
                if(subjectRenderers!=null)for(int i=0;i<subjectRenderers.Length;i++)if(subjectRenderers[i]){savedRendering[i]=subjectRenderers[i].forceRenderingOff;subjectRenderers[i].forceRenderingOff=true;}
                RenderPipeline.SubmitRenderRequest(backgroundCamera,new RenderPipeline.StandardRequest{destination=background});
            }finally{
                output.forceRenderingOff=outputHidden;
                if(subjectRenderers!=null)for(int i=0;i<subjectRenderers.Length;i++)if(subjectRenderers[i])subjectRenderers[i].forceRenderingOff=savedRendering[i];
            }
        }
        void OnDisable(){if(output)Destroy(output.gameObject);if(mesh)Destroy(mesh);if(backgroundCamera)Destroy(backgroundCamera.gameObject);if(background){background.Release();Destroy(background);}mesh=null;output=null;backgroundCamera=null;background=null;tracked=false;}
    }
}
