using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Damin.VFX.AirDistortion
{
    [DisallowMultipleComponent, DefaultExecutionOrder(100)]
    [AddComponentMenu("DAMIN/VFX/Bullet Air Distortion")]
    public sealed class BulletAirDistortionVFX : MonoBehaviour
    {
        [Header("연결 / 실제 총알로 교체할 때 Target만 지정")]
        [InspectorName("따라갈 총알"),Tooltip("지금은 내부 테스트 큐브. 나중에는 씬에서 실제 총알 Transform을 드래그하세요. 총알 에셋을 자동 생성하거나 이동시키지는 않습니다.")]
        public Transform Target;
        public Camera ViewCamera;
        public Material DistortionMaterial;
        [Header("공기 왜곡 / 연기나 발광 없음")]
        [Range(0,2)] public float DistortionStrength=.8f;
        [Min(.01f),Tooltip("월드 공간 꼬리 최대 길이 (m). 속도에 따라 짧아집니다.")]
        public float DistortionLength=.9f;
        [Min(.005f)] public float DistortionWidth=.14f;
        [Min(.1f)] public float NoiseScale=3.5f;
        [Min(0)] public float NoiseSpeed=2.5f;
        [Range(.01f,.5f),Tooltip("지나간 공기가 사라지는 수명 (초)")]
        public float TrailFade=.18f;
        [Min(0),Tooltip("Measured Speed를 끄면 이 값으로 VFX 강도만 제어. 총알을 움직이지 않습니다.")]
        public float BulletSpeed=60;
        [Range(0,20),Tooltip("최대 화면 굴절 이동량 (pixel)")]
        public float RefractionOffset=8;
        [Range(0,1),Tooltip("연기 불투명도가 아니라 굴절 마스크의 가중치")]
        public float OpacityMask=.8f;
        [Header("속도 반응 / 총알 이동 제어와 독립")]
        [InspectorName("실제 이동 속도로 왜곡 조절"),Tooltip("영화 슬로모션은 끈 상태로 사용하세요. Bullet Speed는 왜곡 반응에만 쓰며 총알을 움직이지 않습니다.")]
        public bool MeasureTargetSpeed=false;
        [Min(0)] public float MinimumSpeed=3;
        [Min(.01f)] public float FullStrengthSpeed=60;
        [Min(.01f)] public float CoreLength=.23f;
        [Min(1),Tooltip("이 거리 이상 순간이동하면 꼬리 연결을 끊습니다. 풀링 재발사 시 ClearTrail 권장.")]
        public float TeleportDistance=30;
        [Header("선택형 압력층 / 기본 끔")]
        public bool EnablePressureLayer;
        [Range(0,1)] public float PressureStrength=.25f;
        [Header("미리보기 배속 / 프로젝트 Time.timeScale은 변경하지 않음")]
        [Range(.01f,2)] public float SimulationTimeScale=1;

        public float CurrentSpeed {get;private set;}
        public float SpeedFactor {get;private set;}
        public int TrailPointCount=>samples.Count;
        public bool ManualSimulation {get;set;}
        struct Sample {public Vector3 p;public float time,power;public Sample(Vector3 p,float t,float f){this.p=p;time=t;power=f;}}
        readonly List<Sample> samples=new List<Sample>(128);
        readonly List<Vector3> vertices=new List<Vector3>(512);
        readonly List<Vector2> uv=new List<Vector2>(512),data=new List<Vector2>(512);
        readonly List<int> indices=new List<int>(768);
        Mesh coreMesh,trailMesh,pressureMesh;
        MeshRenderer coreRenderer,trailRenderer,pressureRenderer;
        MaterialPropertyBlock properties;
        Vector3 previous,heading=Vector3.forward;
        Transform boundTarget;
        float clock;
        bool tracked,warned;
        static readonly int Strength=Shader.PropertyToID("_DistortionStrength"),Scale=Shader.PropertyToID("_NoiseScale"),
            Noise=Shader.PropertyToID("_NoiseSpeed"),Offset=Shader.PropertyToID("_RefractionOffset"),Mask=Shader.PropertyToID("_OpacityMask"),
            Clock=Shader.PropertyToID("_EffectTime");

        void OnEnable(){if(Application.isPlaying){Create();ClearTrail();}}
        void LateUpdate(){if(!ManualSimulation)Simulate(Time.deltaTime*SimulationTimeScale);}
        void Create(){
            if(coreMesh)return;properties=new MaterialPropertyBlock();
            coreMesh=CreatePart("01_DistortionCore",out coreRenderer);
            trailMesh=CreatePart("02_ShortWorldWake",out trailRenderer);
            pressureMesh=CreatePart("03_OptionalPressure",out pressureRenderer);
        }
        Mesh CreatePart(string name,out MeshRenderer renderer){
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.layer=gameObject.layer;
            var mesh=new Mesh{name=name+"_Runtime"};mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh=mesh;renderer=go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=DistortionMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            return mesh;
        }
        public void SetTarget(Transform target){Target=target;ClearTrail();}
        public void Detach(){Target=null;boundTarget=null;tracked=false;}
        public void ClearTrail(){
            samples.Clear();clock=0;tracked=false;boundTarget=Target;CurrentSpeed=SpeedFactor=0;
            if(coreMesh)coreMesh.Clear();if(trailMesh)trailMesh.Clear();if(pressureMesh)pressureMesh.Clear();
        }
        public void Simulate(float dt){
            if(!isActiveAndEnabled||dt<=0)return;Create();clock+=dt;
            var camera=ViewCamera?ViewCamera:Camera.main;
            if(!camera){SetVisible(false);return;}
            if(!warned){
                var urp=camera.GetComponent<UniversalAdditionalCameraData>();
                bool available=urp?urp.requiresColorTexture:GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline&&pipeline.supportsCameraOpaqueTexture;
                if(!available){Debug.LogWarning("[Bullet Air Distortion] View Camera 또는 Main Camera의 Opaque Texture를 On으로 설정하세요. 공용 렌더 설정은 자동 변경하지 않습니다.",this);warned=true;}
            }
            bool active=Target&&Target.gameObject.activeInHierarchy;
            if(boundTarget!=Target){samples.Clear();tracked=false;boundTarget=Target;}
            if(active){
                Vector3 now=Target.position;
                if(!tracked){samples.Clear();previous=now;heading=Target.forward;tracked=true;}
                float distance=Vector3.Distance(now,previous);
                if(distance>Mathf.Max(1,TeleportDistance)){samples.Clear();previous=now;distance=0;}
                CurrentSpeed=MeasureTargetSpeed?distance/dt:Mathf.Max(0,BulletSpeed);
                SpeedFactor=Mathf.SmoothStep(0,1,Mathf.InverseLerp(MinimumSpeed,Mathf.Max(MinimumSpeed+.01f,FullStrengthSpeed),CurrentSpeed));
                if(distance>.00001f){
                    heading=(now-previous)/distance;
                    float step=Mathf.Max(.006f,DistortionWidth*.3f);
                    int count=Mathf.Clamp(Mathf.CeilToInt(distance/step),1,64);
                    for(int i=1;i<=count;i++)samples.Add(new Sample(Vector3.Lerp(previous,now,(float)i/count),clock-dt+dt*i/count,SpeedFactor));
                }
                previous=now;
            }else{tracked=false;CurrentSpeed=SpeedFactor=0;}
            float life=Mathf.Max(.01f,TrailFade);
            int expired=0;while(expired<samples.Count&&clock-samples[expired].time>=life)expired++;
            if(expired>0)samples.RemoveRange(0,expired);
            while(samples.Count>96)samples.RemoveAt(0);
            // Keep a short spatial wake even at very high bullet speeds.
            if(active&&samples.Count>1){
                float length=0,maxLength=Mathf.Max(.01f,DistortionLength)*Mathf.Lerp(.25f,1,SpeedFactor);
                for(int i=samples.Count-2;i>=0;i--){
                    float segment=Vector3.Distance(samples[i+1].p,samples[i].p);
                    if(length+segment>maxLength){
                        var cut=samples[i];float f=Mathf.Clamp01((maxLength-length)/Mathf.Max(.00001f,segment));
                        cut.p=Vector3.Lerp(samples[i+1].p,cut.p,f);samples[i]=cut;
                        if(i>0)samples.RemoveRange(0,i);break;
                    }length+=segment;
                }
            }
            Vector3 across=Vector3.Cross(camera.transform.forward,heading).normalized;
            if(across.sqrMagnitude<.001f)across=camera.transform.right;
            float width=Mathf.Max(.005f,DistortionWidth);
            Quad(coreMesh,active?Target.position:previous,heading,across,CoreLength,width,0,active?SpeedFactor:0);
            Quad(pressureMesh,(active?Target.position:previous)+heading*CoreLength*.25f,heading,across,CoreLength*1.1f,width*1.7f,2,active&&EnablePressureLayer?SpeedFactor*PressureStrength:0);
            BuildWake(camera,life,width);
            Apply(coreRenderer);Apply(trailRenderer);Apply(pressureRenderer);SetVisible(DistortionMaterial&&RefractionOffset>0&&DistortionStrength>0&&OpacityMask>0);
        }
        void BeginMesh(){vertices.Clear();uv.Clear();data.Clear();indices.Clear();}
        void Vertex(Vector3 p,Vector2 tex,float kind,float power){vertices.Add(transform.InverseTransformPoint(p));uv.Add(tex);data.Add(new Vector2(kind,power));}
        void Commit(Mesh mesh){mesh.Clear();if(vertices.Count==0)return;mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,data);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();}
        void Quad(Mesh mesh,Vector3 center,Vector3 along,Vector3 across,float length,float width,float kind,float power){
            BeginMesh();if(power<=0){Commit(mesh);return;}
            Vertex(center-along*length*.5f-across*width*.5f,new Vector2(0,0),kind,power);
            Vertex(center+along*length*.5f-across*width*.5f,new Vector2(1,0),kind,power);
            Vertex(center+along*length*.5f+across*width*.5f,new Vector2(1,1),kind,power);
            Vertex(center-along*length*.5f+across*width*.5f,new Vector2(0,1),kind,power);
            indices.Add(0);indices.Add(1);indices.Add(2);indices.Add(0);indices.Add(2);indices.Add(3);Commit(mesh);
        }
        void BuildWake(Camera camera,float life,float width){
            BeginMesh();if(samples.Count<2){Commit(trailMesh);return;}
            for(int i=0;i<samples.Count;i++){
                var s=samples[i];Vector3 tangent=i+1<samples.Count?samples[i+1].p-s.p:s.p-samples[i-1].p;
                Vector3 side=Vector3.Cross(camera.transform.forward,tangent.normalized).normalized;if(side.sqrMagnitude<.001f)side=camera.transform.up;
                float age=Mathf.Clamp01((clock-s.time)/life),p=s.power*(1-age)*(1-age),u=(float)i/(samples.Count-1);
                float half=width*.5f*Mathf.Lerp(.7f,1.2f,age);
                Vertex(s.p-side*half,new Vector2(u,0),1,p);Vertex(s.p+side*half,new Vector2(u,1),1,p);
                if(i>0){int n=i*2;indices.Add(n-2);indices.Add(n);indices.Add(n+1);indices.Add(n-2);indices.Add(n+1);indices.Add(n-1);}
            }Commit(trailMesh);
        }
        void Apply(MeshRenderer r){
            r.sharedMaterial=DistortionMaterial;properties.Clear();
            properties.SetFloat(Strength,DistortionStrength);properties.SetFloat(Scale,NoiseScale);
            properties.SetFloat(Noise,NoiseSpeed*Mathf.Lerp(.5f,1.5f,SpeedFactor));
            properties.SetFloat(Offset,RefractionOffset);properties.SetFloat(Mask,OpacityMask);properties.SetFloat(Clock,clock);
            r.SetPropertyBlock(properties);
        }
        void SetVisible(bool value){if(coreRenderer)coreRenderer.enabled=value;if(trailRenderer)trailRenderer.enabled=value;if(pressureRenderer)pressureRenderer.enabled=value;}
        void OnDisable(){
            foreach(var r in new[]{coreRenderer,trailRenderer,pressureRenderer})if(r)Destroy(r.gameObject);
            foreach(var m in new[]{coreMesh,trailMesh,pressureMesh})if(m)Destroy(m);
            coreMesh=trailMesh=pressureMesh=null;samples.Clear();
        }
    }
}
