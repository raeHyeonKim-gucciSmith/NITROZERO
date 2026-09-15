using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// One-shot, opt-in authoring. Does not run in builds or touch reference assets.
[InitializeOnLoad]
public static class DMMatchRHRacingLighting
{
    public const string Target="Assets/03DAMIN/DM_avoidMissile_Cinematic.unity";
    public const string Reference="Assets/01RAEHYEON/RHScenes/RH_racing.unity";
    public const string ReferenceHash="1D8B5A10BC7AC90CE98912B969A21F625EDCA40779B039F36BA0294CAFDDBA27";
    public const string SkySource="Assets/Stagit/SkyboxEarthPlanets/skyboxes/skyboxv1.mat";
    public const string SkyCopy="Assets/03DAMIN/SceneTools/DM_avoidMissile_Cinematic/Materials/DM_RH_RacingSkybox.mat";
    public const string RootName="DM_RH_RacingLighting";
    const string Request="UserSettings/DM_MatchRHRacing_v1.request";
    const string Status="UserSettings/DM_MatchRHRacing_v1.status.txt";
    static double next;
    static DMMatchRHRacingLighting(){EditorApplication.update+=Poll;}
    public static string Hash(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
    static bool Wait(string text){if(File.Exists(Request)&&(!File.Exists(Status)||File.ReadAllText(Status)!=text))File.WriteAllText(Status,text);return false;}
    static void Poll(){
        if(EditorApplication.timeSinceStartup<next||!File.Exists(Request))return;
        next=EditorApplication.timeSinceStartup+2;
        try{ApplyNow();}catch(Exception e){File.WriteAllText(Status,"ERROR: "+e);Debug.LogException(e);EditorApplication.update-=Poll;}
    }
    public static bool ApplyNow(){
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return Wait("Play 정지 / 컴파일 완료 대기");
        if(EditorSceneManager.sceneCount!=1||PrefabStageUtility.GetCurrentPrefabStage()!=null)return Wait("DM_avoidMissile_Cinematic 씬만 열고 프리팹 편집에서 나와 주세요.");
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!=Target)return Wait("DM_avoidMissile_Cinematic 씬을 열어 주세요.");
        if(scene.isDirty)return Wait("현재 씬을 Ctrl+S로 저장해 주세요. 미저장 편집은 덮어쓰지 않습니다.");
        if(Hash(Reference)!=ReferenceHash)throw new InvalidOperationException("RH_racing이 검증 이후 변경되었습니다. 재비교가 필요합니다.");
        if(scene.GetRootGameObjects().Any(g=>g.name==RootName)||File.Exists(SkyCopy))throw new InvalidOperationException("이름이 같은 전용 조명/하늘이 이미 있습니다. 덮어쓰지 않았습니다.");
        var lights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToArray();
        if(lights.Any(l=>l.type==LightType.Directional&&l.enabled&&l.gameObject.activeInHierarchy))throw new InvalidOperationException("새 방향광이 감지되었습니다. 중복 조명을 피하려고 중단했습니다.");
        var cameraData=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UniversalAdditionalCameraData>(true)).ToArray();
        if(cameraData.Any(c=>c.renderPostProcessing))throw new InvalidOperationException("후처리가 새로 켜졌습니다. 사용자 편집 확인이 필요합니다.");
        var source=AssetDatabase.LoadAssetAtPath<Material>(SkySource);
        if(!source)throw new InvalidOperationException("기준 하늘 재질을 찾지 못했습니다.");
        string sourceHash=Hash(SkySource);
        string backup="UserSettings/DM_RecordingBackups/RHLighting_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        Directory.CreateDirectory(backup);File.Copy(Target,backup+"/DM_avoidMissile_Cinematic.unity");
        // A private material keeps future local adjustments separate from team assets.
        var sky=new Material(source){name="DM_RH_RacingSkybox"};
        AssetDatabase.CreateAsset(sky,SkyCopy);AssetDatabase.SaveAssetIfDirty(sky);
        var root=new GameObject(RootName);Undo.RegisterCreatedObjectUndo(root,"Match RH racing lighting in DM scene");
        CreateLight(root.transform,"Directional Light",new Vector3(0,3,0),new Quaternion(.40821788f,-.23456968f,.10938163f,.8754261f),new Color(1,.95686275f,.8392157f,1),1,LightShadows.Soft,1);
        CreateLight(root.transform,"Earthlight",new Vector3(-1920,-18,-115),new Quaternion(.0560187f,.9430295f,-.2090646f,.252684f),new Color(.4392157f,.6117647f,1,1),.35f,LightShadows.None,0);
        // All other RenderSettings already match RH_racing, verified against scene YAML.
        RenderSettings.skybox=sky;
        DynamicGI.UpdateEnvironment();
        EditorSceneManager.MarkSceneDirty(scene);
        if(Hash(Reference)!=ReferenceHash||Hash(SkySource)!=sourceHash)throw new IOException("참조 파일 변경 감지");
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("작업 씬 저장 실패");
        string message="READY: RH_racing sky clone + two directional lights; post-processing remains OFF; existing vehicles/cameras/local VFX lights/map unchanged. Backup="+backup;
        File.WriteAllText(Status,message);
        if(File.Exists(Request))File.Move(Request,Request+"."+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+".done");
        Debug.Log(message);return true;
    }
    static void CreateLight(Transform parent,string name,Vector3 position,Quaternion rotation,Color color,float intensity,LightShadows shadows,float bounce){
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.SetPositionAndRotation(position,rotation);
        var light=go.AddComponent<Light>();light.type=LightType.Directional;light.color=color;light.intensity=intensity;
        light.range=10;light.spotAngle=30;light.innerSpotAngle=21.80208f;
        light.shadows=shadows;light.shadowStrength=1;light.shadowBias=.05f;light.shadowNormalBias=.4f;light.shadowNearPlane=.2f;
        light.bounceIntensity=bounce;light.colorTemperature=6570;light.useColorTemperature=false;
        light.cullingMask=-1;light.renderingLayerMask=1;light.lightmapBakeType=LightmapBakeType.Realtime;
        var data=go.AddComponent<UniversalAdditionalLightData>();
        // Mirror the reference's URP shadow resolution/cookie/layer settings.
        var so=new SerializedObject(data);
        so.FindProperty("m_UsePipelineSettings").boolValue=true;
        so.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=2;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
