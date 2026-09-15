using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Damin.CinematicCopy;

[InitializeOnLoad]
public static class DMKhakiWindowTintSetup
{
    public const string TargetScene="Assets/03DAMIN/DM_avoidMissile_Cinematic.unity";
    public const string SourceMaterial="Assets/ImportedBlenderAsset/Materials/MAT_Taurus_Glass_LightDust_v24.mat";
    public const string PrivateFolder="Assets/03DAMIN/SceneTools/DM_avoidMissile_Cinematic/Materials";
    public const string PrivateMaterial=PrivateFolder+"/DM_Khaki_WindowTint.mat";
    const string Request="UserSettings/DM_KhakiWindowTint_v1.request";
    const string Status="UserSettings/DM_KhakiWindowTint_v1.status.txt";
    public const float Opacity=.985f;
    static double next;
    static DMKhakiWindowTintSetup(){EditorApplication.update+=Poll;}
    static void Poll(){
        if(EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+2;
        if(!File.Exists(Request))return;
        try {ApplyNow();}catch(Exception e){
            File.WriteAllText(Status,"ERROR: "+e.Message);
            Debug.LogException(e);EditorApplication.update-=Poll;
        }
    }
    static bool Wait(string message){
        if(File.Exists(Request)&&(!File.Exists(Status)||File.ReadAllText(Status)!=message))File.WriteAllText(Status,message);
        return false;
    }
    public static bool IsCabinGlass(Renderer renderer){
        string n=renderer.name;
        // In this imported model GlassFront is the HEADLAMP cover, not the windshield.
        // Windows_LOD0 contains front/rear cabin panes; four other Windows renderers are doors.
        return n.Contains("RMCar26_Windows");
    }
    static string Hash(string path){
        using(var sha=System.Security.Cryptography.SHA256.Create())
            return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
    }
    public static bool ApplyNow(){
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)
            return Wait("Play 정지 / 컴파일 완료 대기");
        if(EditorSceneManager.sceneCount!=1||PrefabStageUtility.GetCurrentPrefabStage()!=null)
            return Wait("DM_avoidMissile_Cinematic 씬 하나만 열고 프리팹 편집에서 나와 주세요.");
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!=TargetScene)return Wait("DM_avoidMissile_Cinematic 씬을 열어 주세요.");
        if(scene.isDirty)return Wait("현재 씬을 Ctrl+S로 저장해 주세요. 미저장 배치는 변경하지 않습니다.");
        var directors=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DMCinematicDirector>(true)).ToArray();
        if(directors.Length!=1||!directors[0].khakiCar)throw new InvalidOperationException("전용 카키 연결을 확인할 수 없습니다.");
        var khaki=directors[0].khakiCar;
        if(khaki.gameObject.scene!=scene||PrefabUtility.IsPartOfPrefabInstance(khaki))
            throw new InvalidOperationException("카키 차량이 이 씬의 언팩된 인스턴스인지 먼저 확인해야 합니다.");
        var source=AssetDatabase.LoadAssetAtPath<Material>(SourceMaterial);
        if(!source||!source.HasProperty("_BaseColor"))throw new InvalidOperationException("원본 유리 재질이 예상과 다릅니다.");
        var windows=khaki.GetComponentsInChildren<Renderer>(true).Where(IsCabinGlass).ToArray();
        if(windows.Length!=5||windows.Any(r=>r.sharedMaterials.Length!=1))
            throw new InvalidOperationException("예상한 창문 렌더러 5개가 아닙니다. 다른 부품은 변경하지 않았습니다.");
        var existing=AssetDatabase.LoadAssetAtPath<Material>(PrivateMaterial);
        if(existing&&windows.All(r=>r.sharedMaterial==existing)){
            Complete("ALREADY APPLIED: private material on this Khaki's six panes (five renderers).");
            return true;
        }
        if(existing)throw new InvalidOperationException("전용 재질이 이미 있으므로 덮어쓰지 않았습니다.");
        if(windows.Any(r=>r.sharedMaterial!=source))throw new InvalidOperationException("창문에 다른 편집이 감지되어 중단했습니다.");
        string sourceHash=Hash(SourceMaterial);
        string backup="UserSettings/DM_RecordingBackups/KhakiGlass_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        Directory.CreateDirectory(backup);
        File.Copy(TargetScene,backup+"/DM_avoidMissile_Cinematic.unity");
        File.Copy(SourceMaterial,backup+"/OriginalGlass.mat");
        if(!AssetDatabase.IsValidFolder(PrivateFolder)){
            string parent=Path.GetDirectoryName(PrivateFolder).Replace('\\','/');
            AssetDatabase.CreateFolder(parent,"Materials");
        }
        if(!AssetDatabase.CopyAsset(SourceMaterial,PrivateMaterial))throw new IOException("유리 재질 복사 실패");
        var tint=AssetDatabase.LoadAssetAtPath<Material>(PrivateMaterial);
        tint.name="DM_Khaki_WindowTint";
        // Preserve the original hue, smoothness, reflection/specular and shader setup.
        // Only opacity changes: the empty cockpit is hidden without painting a black panel.
        foreach(string property in new[]{"_BaseColor","_Color"}){
            if(!tint.HasProperty(property))continue;
            Color color=tint.GetColor(property);color.a=Opacity;tint.SetColor(property,color);
        }
        EditorUtility.SetDirty(tint);
        AssetDatabase.SaveAssetIfDirty(tint);
        if(Hash(SourceMaterial)!=sourceHash)throw new IOException("공유 원본 재질 변경이 감지되었습니다. 씬 연결 전에 중단합니다.");
        Undo.RecordObjects(windows.Cast<UnityEngine.Object>().ToArray(),"Tint only working-scene Khaki windows");
        foreach(var window in windows){window.sharedMaterials=new[]{tint};EditorUtility.SetDirty(window);}
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("작업 씬 저장 실패");
        if(Hash(SourceMaterial)!=sourceHash)throw new IOException("공유 원본 재질을 다시 확인해야 합니다.");
        Complete("READY: 6 Khaki cabin panes on 5 renderers only; opacity="+Opacity+"; private material="+PrivateMaterial+"; original glass/headlamps/spoiler/body/prefabs/cameras unchanged. Backup="+backup);
        return true;
    }
    static void Complete(string message){
        if(File.Exists(Request))File.Move(Request,Request+"."+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+".done");
        File.WriteAllText(Status,message);Debug.Log(message);
    }
}
