using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Damin.VFX.AirDistortion.Reusable
{
    // This component owns only its rendering resources. Never moves, disables or reparents the target/camera.
    [DefaultExecutionOrder(10000), DisallowMultipleComponent]
    [AddComponentMenu("DAMIN/VFX/Reusable Bullet Air Distortion")]
    public sealed class ReusableBulletAirDistortion : MonoBehaviour
    {
        [Tooltip("Hierarchy의 실제 총알 루트. 모형 Renderer가 이 오브젝트 아래에 있어야 합니다.")]
        public Transform Target;
        [Tooltip("실제 촬영 Camera. Cinemachine 가상 카메라가 아닌 Unity Camera를 연결하세요.")]
        public Camera ViewCamera;
        [Range(0,2)] public float DistortionStrength=1;
        [Range(.1f,4)] public float SizeMultiplier=1;
        [Range(.1f,4)] public float WakeLengthMultiplier=1;
        [Range(.03f,1)] public float TrailFade=.2f;
        public bool AutoSize=true;
        [Min(.001f)] public float ManualBulletLength=.2f;
        [Min(.001f)] public float ManualBulletDiameter=.05f;
        public Vector3 CenterOffset;
        public bool AutoForwardAxis=true;
        [Tooltip("거의 정지했거나 처음 연결할 때의 총알 앞 방향. 총알 로컬 좌표입니다.")]
        public Vector3 ForwardAxis=Vector3.forward;
        [Range(.1f,4)] public float WidthMultiplier=1;
        [Range(0,64)] public float RefractionPixels=22;
        [Range(0,1)] public float OpacityMask=1;
        [Range(1,10)] public float NoiseScale=4;
        [Range(0,10)] public float NoiseSpeed=4;
        [Range(0,2)] public float FineDetail=.6f;
        [Tooltip("기본은 OFF: 슬로모션에서도 일정한 왜곡. ON이면 실제 이동 속도에 반응합니다.")]
        public bool MeasureTargetSpeed;
        [Min(.01f)] public float FullStrengthSpeed=60;
        [Min(.01f)] public float TeleportDistance=10;
        public Material DistortionMaterial;

        public float CurrentBulletLength {get;private set;}
        public float CurrentBulletDiameter {get;private set;}
        public float CurrentWakeLength {get;private set;}
        public string RuntimeIssue {get;private set;}="";
        public bool IsRendering=>output&&output.enabled;
        public bool ManualSimulation {get;set;}
        Transform bound;
        Renderer[] subjectRenderers=Array.Empty<Renderer>();
        bool[] savedRendering=Array.Empty<bool>();
        Mesh mesh;
        MeshRenderer output;
        Camera captureCamera,preparedCamera;
        UniversalAdditionalCameraData captureData;
        Skybox captureSky;
        RenderTexture background;
        MaterialPropertyBlock props;
        readonly Vector3[] vertices=new Vector3[4];
        Vector3 previousPosition,center,heading=Vector3.right;
        Bounds worldBounds;
        bool tracking,hasBounds,hasHistory;
        float age,fade,clock;
        int rendererIndex=-1;
        Camera rendererCamera;
        ScriptableRenderer selectedRenderer;
        string lastLoggedIssue;

        public void SetTarget(Transform target){Target=target;ClearTrail();}
        public void SetCamera(Camera camera){ViewCamera=camera;preparedCamera=null;rendererCamera=null;}
        public void Detach(){Target=null;}
        public void Bind(Transform target,Camera camera){SetCamera(camera);SetTarget(target);}
        public void ClearTrail(){bound=null;tracking=hasHistory=false;age=fade=clock=0;Hide();}
        public void RefreshTarget(){bound=null;tracking=false;}

        void OnEnable(){RenderPipelineManager.beginCameraRendering+=BeforeCamera;ClearTrail();}
        void LateUpdate(){if(!ManualSimulation)Simulate(Time.deltaTime);}
        void BeforeCamera(ScriptableRenderContext context,Camera camera)
        {
            if(output)output.forceRenderingOff=camera!=preparedCamera;
        }

        public string ConfigurationIssue()
        {
            if(!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))return "이 프리팹은 URP 전용입니다.";
            if(!ViewCamera)return "촬영 카메라를 연결하세요.";
            if(!DistortionMaterial||!DistortionMaterial.shader||!DistortionMaterial.shader.isSupported)return "왜곡 재질/셰이더가 없거나 지원되지 않습니다.";
            if(ViewCamera.stereoEnabled)return "XR/스테레오 카메라는 지원하지 않습니다.";
            if(ViewCamera.rect!=new Rect(0,0,1,1))return "카메라 Viewport Rect는 전체 화면 (0,0,1,1)을 사용하세요.";
            if(ViewCamera.clearFlags==CameraClearFlags.Nothing||ViewCamera.clearFlags==CameraClearFlags.Depth)return "카메라 배경은 Skybox 또는 Solid Color로 설정하세요.";
            if(ViewCamera.TryGetComponent<UniversalAdditionalCameraData>(out var data))
            {
                if(data.renderType!=CameraRenderType.Base)return "Overlay가 아닌 Base 카메라를 연결하세요.";
                if(!(data.scriptableRenderer is UniversalRenderer))return "URP 3D Universal Renderer를 사용하세요 (2D Renderer 제외).";
                if(data.cameraStack!=null&&data.cameraStack.Count>0)return "이 버전은 카메라 Stack 없이 사용하는 단일 Base 카메라용입니다.";
                if(data.antialiasing==AntialiasingMode.TemporalAntiAliasing)return "이 버전은 TAA를 지원하지 않습니다. None/FXAA/SMAA를 사용하세요.";
            }
            else if(!(UniversalRenderPipeline.asset.scriptableRenderer is UniversalRenderer))return "URP 3D Universal Renderer가 필요합니다.";
            if(Mathf.Abs(transform.lossyScale.x*transform.lossyScale.y*transform.lossyScale.z)<.000001f)return "VFX 프리팹 Scale에 0을 사용할 수 없습니다.";
            if(Target&&(Target==transform||transform.IsChildOf(Target)))return "VFX 루트를 총알 아래에 넣지 말고 씬에 별도로 두세요. Target에는 총알만 연결하세요.";
            return "";
        }

        public void Simulate(float dt)
        {
            if(!Application.isPlaying||!isActiveAndEnabled)return;
            string issue=ConfigurationIssue();
            if(!string.IsNullOrEmpty(issue)){RuntimeIssue=issue;Hide();return;}
            RuntimeIssue="";
            if(!ViewCamera.gameObject.activeInHierarchy){Hide();return;}
            EnsureResources();
            dt=Mathf.Max(0,dt);clock+=dt;
            bool active=Target&&Target.gameObject.activeInHierarchy;
            if(active)
            {
                if(bound!=Target||!tracking)
                {
                    bound=Target;subjectRenderers=Target.GetComponentsInChildren<Renderer>(true);
                    savedRendering=new bool[subjectRenderers.Length];previousPosition=Target.position;
                    heading=Target.TransformDirection(ForwardAxis.sqrMagnitude>.0001f?ForwardAxis.normalized:Vector3.forward);
                    age=fade=0;tracking=hasHistory=true;
                }
                Vector3 delta=Target.position-previousPosition;
                float distance=delta.magnitude;
                if(distance>TeleportDistance){delta=Vector3.zero;distance=0;age=0;}
                if(distance>.00001f)heading=delta/distance;
                previousPosition=Target.position;
                ReadDimensions();
                age+=dt;
                float speed=MeasureTargetSpeed?(dt>0?distance/dt:0):FullStrengthSpeed;
                fade=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.12f))*Mathf.SmoothStep(0,1,Mathf.Clamp01(speed/Mathf.Max(.01f,FullStrengthSpeed)));
            }
            else
            {
                tracking=false;
                fade=Mathf.Max(0,fade-dt/Mathf.Max(.03f,TrailFade));
            }
            if(!hasHistory||fade<=0||DistortionStrength<=0||OpacityMask<=0||RefractionPixels<=0){Hide();return;}
            var camera=ViewCamera;
            Vector3 along=Vector3.ProjectOnPlane(heading,camera.transform.forward);
            if(along.sqrMagnitude<.01f){RuntimeIssue="총알을 정면보다 옆/비스듬한 각도에서 촬영하세요. 진행 방향이 시선과 겹치면 꼬리를 표시하지 않습니다.";Hide();return;}
            along.Normalize();
            float depth=Vector3.Dot(center-camera.transform.position,camera.transform.forward);
            if(depth<camera.nearClipPlane||depth>camera.farClipPlane){Hide();return;}
            float core=CurrentBulletLength*1.2f*SizeMultiplier;
            CurrentWakeLength=CurrentBulletLength*2.75f*SizeMultiplier*WakeLengthMultiplier;
            float half=Mathf.Max(CurrentBulletLength*1.4f,CurrentBulletDiameter*2.2f)*SizeMultiplier*WidthMultiplier*.5f;
            Vector3 side=Vector3.Cross(camera.transform.forward,along).normalized;
            // Offset behind the full subject depth, not a fixed distance tuned to one missile.
            float radius=hasBounds?Vector3.Dot(Abs(camera.transform.forward),worldBounds.extents):CurrentBulletDiameter*.5f;
            float extrusion=radius+Mathf.Max(.002f,CurrentBulletLength*.02f);
            vertices[0]=Local(center-along*(CurrentWakeLength+core*.5f)-side*half,camera,extrusion);
            vertices[1]=Local(center+along*core*.5f-side*half,camera,extrusion);
            vertices[2]=Local(center+along*core*.5f+side*half,camera,extrusion);
            vertices[3]=Local(center-along*(CurrentWakeLength+core*.5f)+side*half,camera,extrusion);
            mesh.vertices=vertices;mesh.RecalculateBounds();
            output.gameObject.layer=gameObject.layer;
            if((camera.cullingMask&(1<<gameObject.layer))==0){RuntimeIssue="카메라 Culling Mask에 이 VFX의 Layer가 포함되어 있지 않습니다.";Hide();return;}
            try{Capture(camera);}
            catch(Exception e)
            {
                RuntimeIssue="배경 촬영 실패: "+e.Message;Hide();
                if(lastLoggedIssue!=RuntimeIssue){Debug.LogWarning(RuntimeIssue,this);lastLoggedIssue=RuntimeIssue;}
                return;
            }
            props.Clear();props.SetTexture("_AirBackground",background);
            props.SetFloat("_Strength",DistortionStrength*fade);props.SetFloat("_Pixels",RefractionPixels);
            props.SetFloat("_Mask",OpacityMask*fade);props.SetFloat("_NoiseScale",NoiseScale);
            props.SetFloat("_NoiseSpeed",NoiseSpeed);props.SetFloat("_FineDetail",FineDetail);props.SetFloat("_Clock",clock);
            output.sharedMaterial=DistortionMaterial;output.SetPropertyBlock(props);output.enabled=true;preparedCamera=camera;
        }

        static Vector3 Abs(Vector3 v)=>new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
        void ReadDimensions()
        {
            Bounds localBounds=default;bool found=false;hasBounds=false;
            foreach(var r in subjectRenderers)
            {
                if(!r||!r.enabled||!r.gameObject.activeInHierarchy||(!(r is MeshRenderer)&&!(r is SkinnedMeshRenderer)))continue;
                if(!hasBounds){worldBounds=r.bounds;hasBounds=true;}else worldBounds.Encapsulate(r.bounds);
                Bounds b=r.localBounds;
                for(int i=0;i<8;i++)
                {
                    Vector3 p=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    p=Target.InverseTransformPoint(r.transform.TransformPoint(p));
                    if(!found){localBounds=new Bounds(p,Vector3.zero);found=true;}else localBounds.Encapsulate(p);
                }
            }
            center=Target.TransformPoint((found?localBounds.center:Vector3.zero)+CenterOffset);
            CurrentBulletLength=Mathf.Max(.001f,ManualBulletLength);CurrentBulletDiameter=Mathf.Max(.001f,ManualBulletDiameter);
            if(AutoSize&&found)
            {
                Vector3 s=localBounds.size;
                float x=Target.TransformVector(new Vector3(s.x,0,0)).magnitude;
                float y=Target.TransformVector(new Vector3(0,s.y,0)).magnitude;
                float z=Target.TransformVector(new Vector3(0,0,s.z)).magnitude;
                CurrentBulletLength=Mathf.Max(.001f,Mathf.Max(x,Mathf.Max(y,z)));
                CurrentBulletDiameter=Mathf.Max(.001f,x+y+z-Mathf.Max(x,Mathf.Max(y,z))-Mathf.Min(x,Mathf.Min(y,z)));
                if(AutoForwardAxis&&age<=0)heading=Target.TransformDirection(x>=y&&x>=z?Vector3.right:y>=z?Vector3.up:Vector3.forward);
            }
            if(AutoSize&&!found)RuntimeIssue="총알의 MeshRenderer를 찾지 못해 수동 크기를 사용 중입니다.";
        }
        Vector3 Local(Vector3 p,Camera camera,float extrusion)
        {
            if(camera.orthographic)return transform.InverseTransformPoint(p+camera.transform.forward*extrusion);
            Vector3 ray=p-camera.transform.position;
            return transform.InverseTransformPoint(p+ray*(extrusion/Mathf.Max(.001f,Vector3.Dot(ray,camera.transform.forward))));
        }
        void EnsureResources()
        {
            if(mesh)return;
            var go=new GameObject("AirRefraction_RuntimeOnly"){hideFlags=HideFlags.DontSave};go.transform.SetParent(transform,false);
            mesh=new Mesh{name="ReusableAirEnvelope",hideFlags=HideFlags.DontSave};mesh.MarkDynamic();
            mesh.vertices=vertices;mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
            mesh.triangles=new[]{0,1,2,0,2,3};go.AddComponent<MeshFilter>().sharedMesh=mesh;
            output=go.AddComponent<MeshRenderer>();output.enabled=false;output.shadowCastingMode=ShadowCastingMode.Off;output.receiveShadows=false;
            props=new MaterialPropertyBlock();
            go=new GameObject("AirCapture_RuntimeOnly"){hideFlags=HideFlags.HideAndDontSave};captureCamera=go.AddComponent<Camera>();captureCamera.enabled=false;
            captureData=go.AddComponent<UniversalAdditionalCameraData>();captureSky=go.AddComponent<Skybox>();
        }
        void Capture(Camera view)
        {
            int width=Mathf.Max(16,view.pixelWidth),height=Mathf.Max(16,view.pixelHeight);
            var format=SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)?RenderTextureFormat.ARGBHalf:RenderTextureFormat.ARGB32;
            if(!background||background.width!=width||background.height!=height)
            {
                ReleaseBackground();background=new RenderTexture(width,height,24,format){name="ReusableAirCleanBackground",hideFlags=HideFlags.HideAndDontSave};background.Create();
            }
            captureCamera.CopyFrom(view);captureCamera.enabled=false;captureCamera.targetTexture=background;
            captureCamera.transform.SetPositionAndRotation(view.transform.position,view.transform.rotation);
            captureCamera.projectionMatrix=view.projectionMatrix;
            view.TryGetComponent<UniversalAdditionalCameraData>(out var viewData);
            var desired=viewData?viewData.scriptableRenderer:UniversalRenderPipeline.asset.scriptableRenderer;
            if(rendererCamera!=view||selectedRenderer!=desired)
            {
                rendererIndex=-1;var asset=UniversalRenderPipeline.asset;
                for(int i=0;i<asset.rendererDataList.Length;i++)if(asset.rendererDataList[i]&&asset.GetRenderer(i)==desired){rendererIndex=i;break;}
                captureData.SetRenderer(rendererIndex);rendererCamera=view;selectedRenderer=desired;
            }
            captureData.renderType=CameraRenderType.Base;captureData.renderPostProcessing=false;
            captureData.antialiasing=AntialiasingMode.None;captureData.requiresColorOption=CameraOverrideOption.Off;captureData.requiresDepthOption=CameraOverrideOption.Off;
            captureData.renderShadows=!viewData||viewData.renderShadows;
            if(viewData){captureData.volumeLayerMask=viewData.volumeLayerMask;captureData.volumeTrigger=viewData.volumeTrigger;}
            var sky=view.GetComponent<Skybox>();captureSky.enabled=sky&&sky.enabled;captureSky.material=sky?sky.material:null;
            bool old=output.forceRenderingOff;
            try
            {
                output.forceRenderingOff=true;
                for(int i=0;i<subjectRenderers.Length;i++)if(subjectRenderers[i]){savedRendering[i]=subjectRenderers[i].forceRenderingOff;subjectRenderers[i].forceRenderingOff=true;}
                RenderPipeline.SubmitRenderRequest(captureCamera,new RenderPipeline.StandardRequest{destination=background});
            }
            finally
            {
                output.forceRenderingOff=old;
                for(int i=0;i<subjectRenderers.Length;i++)if(subjectRenderers[i])subjectRenderers[i].forceRenderingOff=savedRendering[i];
            }
        }
        void Hide(){if(output)output.enabled=false;preparedCamera=null;}
        static void Dispose(UnityEngine.Object obj){if(!obj)return;if(Application.isPlaying)Destroy(obj);else DestroyImmediate(obj);}
        void ReleaseBackground(){if(background){background.Release();Dispose(background);}background=null;}
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering-=BeforeCamera;Hide();
            if(output)Dispose(output.gameObject);Dispose(mesh);if(captureCamera)Dispose(captureCamera.gameObject);ReleaseBackground();
            mesh=null;output=null;captureCamera=null;captureData=null;rendererCamera=null;selectedRenderer=null;
            subjectRenderers=Array.Empty<Renderer>();savedRendering=Array.Empty<bool>();ClearTrail();
        }
    }
}
