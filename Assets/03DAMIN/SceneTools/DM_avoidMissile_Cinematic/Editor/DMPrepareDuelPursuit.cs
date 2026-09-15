using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Damin.CinematicCopy;

// Only an explicit one-shot request or menu command can write the working scene.
// This does not change Recorder, packages, shared assets or another loaded scene.
[InitializeOnLoad]
public static class DMPrepareDuelPursuit
{
    const string ScenePath="Assets/03DAMIN/DM_avoidMissile_Cinematic.unity";
    const string Request="UserSettings/DM_Shot02_SpeedContrastV4.request";
    const string Status="UserSettings/DM_Shot02_SpeedContrastV4.status.txt";
    static double next;
    static DMPrepareDuelPursuit(){EditorApplication.update+=Poll;}
    static void Poll(){
        if(EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+2;
        if(!File.Exists(Request))return;
        try {Prepare(false);}catch(Exception e){File.WriteAllText(Status,"ERROR: "+e.Message);Debug.LogException(e);EditorApplication.update-=Poll;}
    }
    [MenuItem("Tools/DM Cinematic/02 - Preview Pursuit Take")]
    public static void Menu(){Prepare(true);}
    static bool Wait(string reason,bool menu){
        if(menu)Debug.LogWarning("02 준비 보류: "+reason);
        if(File.Exists(Request)&&(!File.Exists(Status)||File.ReadAllText(Status)!=reason))File.WriteAllText(Status,reason);
        return false;
    }
    static bool Prepare(bool menu){
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)
            return Wait("Play 정지 / 컴파일 완료 대기",menu);
        if(EditorSceneManager.sceneCount!=1||PrefabStageUtility.GetCurrentPrefabStage()!=null)
            return Wait("작업 씬 하나만 열고 프리팹 편집에서 나와 주세요.",menu);
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!=ScenePath)return Wait("DM_avoidMissile_Cinematic 씬을 열어 주세요.",menu);
        if(scene.isDirty)return Wait("현재 편집을 Ctrl+S로 저장해 주세요. 저장되지 않은 내용은 변경하지 않습니다.",menu);
        var list=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DMCinematicDirector>(true)).ToArray();
        if(list.Length!=1)return Wait("작업 씬의 전용 Director가 정확히 하나여야 합니다.",menu);
        var d=list[0];
        if(d.opening==null||d.opening.vehicles.Length!=6||d.opening.vehicles.Any(c=>c==null||c.gameObject.scene!=scene||PrefabUtility.IsPartOfPrefabInstance(c)))
            return Wait("이 씬의 언팩된 차량 6대 연결을 먼저 확인해야 합니다.",menu);
        string backup="UserSettings/DM_RecordingBackups/02_Pursuit_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        Directory.CreateDirectory(backup);
        File.Copy(ScenePath,Path.Combine(backup,"DM_avoidMissile_Cinematic.unity"));
        Undo.RecordObject(d,"Prepare independent Shot02 pursuit");
        d.independentDuel=true;
        d.cameraPlaybackMode=DMCinematicDirector.CameraPlaybackMode.SingleCamera;
        d.takeCameraNumber=2;
        // Preserve the user\'s pursuit settings. Only the explicit V4 request lowers the camera.
        if(File.Exists(Request)&&File.ReadAllText(Request).Contains("SpeedContrastV4")){
            d.duelTake.cameraHeight=.45f;
            d.duelTake.cameraSeparation=1;
        }
        d.loopTake=false;
        // Keep the user's saved duration, vehicle speed, lens, pursuit and all other shots.
        EditorUtility.SetDirty(d);EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("작업 씬 저장 실패");
        string message="READY: camera2 independent pursuit; "+d.duelTake.duration+" seconds; "+d.duelTake.speedKph+" km/h; "+d.duelTake.focalLength+" mm; height="+d.duelTake.cameraHeight+"m; separation="+d.duelTake.cameraSeparation+". Only target Director settings saved. No Play/recording started. Backup="+backup;
        if(File.Exists(Request))File.Move(Request,Request+"."+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+".done");
        File.WriteAllText(Status,message);
        Debug.Log(message);
        return true;
    }
}


