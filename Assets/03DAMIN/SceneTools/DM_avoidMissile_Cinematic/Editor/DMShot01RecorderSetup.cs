using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.Recorder.Encoder;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Damin.CinematicCopy.EditorTools
{
    // Editor-only preparation. Never starts Play or recording, and never opens a different scene.
    [InitializeOnLoad]
    public static class DMShot01RecorderSetup
    {
        public const string ScenePath="Assets/03DAMIN/DM_avoidMissile_Cinematic.unity";
        const string MovieName="DM_Shot01_FullHD";
        const string Marker="UserSettings/DM_Shot01_RecordPrepare.request";
        const string Done="UserSettings/DM_Shot01_RecordPrepare.done";
        const string Status="UserSettings/DM_Shot01_RecordPrepare.status.txt";
        static double nextCheck;
        static string lastStatus;
        static DMShot01RecorderSetup(){if(!Application.isBatchMode)EditorApplication.update+=AutoPrepare;}
        static void StatusText(string text){
            if(lastStatus==text)return;lastStatus=text;
            Directory.CreateDirectory("UserSettings");File.WriteAllText(Status,text);
        }
        static void AutoPrepare(){
            if(EditorApplication.timeSinceStartup<nextCheck)return;nextCheck=EditorApplication.timeSinceStartup+1;
            if(!File.Exists(Marker)||File.Exists(Done)){EditorApplication.update-=AutoPrepare;return;}
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            string reason=Guard();
            if(reason!=null){StatusText("WAIT: "+reason);return;}
            try{Prepare(true);File.WriteAllText(Done,DateTime.UtcNow.ToString("O"));EditorApplication.update-=AutoPrepare;}
            catch(Exception e){StatusText("ERROR: "+e);Debug.LogException(e);EditorApplication.update-=AutoPrepare;}
        }
        static string Guard(){
            if(EditorApplication.isPlayingOrWillChangePlaymode)return "Play를 정지해 주세요.";
            if(EditorUtility.scriptCompilationFailed)return "스크립트 컴파일 오류를 먼저 확인해 주세요.";
            if(SceneManager.sceneCount!=1||SceneManager.GetActiveScene().path!=ScenePath)return "DM_avoidMissile_Cinematic 씬만 열어 주세요.";
            if(SceneManager.GetActiveScene().isDirty)return "현재 씬을 Ctrl+S로 저장해 주세요. 저장되지 않은 편집은 자동으로 덮어쓰지 않습니다.";
            if(Resources.FindObjectsOfTypeAll<RecorderWindow>().Any(w=>w.IsRecording()))return "현재 녹화를 먼저 정지해 주세요.";
            return null;
        }
        [MenuItem("Tools/DM Cinematic/01 - Prepare Recorder (Full HD)")]
        public static void PrepareFromMenu(){
            try{Prepare(true);}
            catch(Exception e){Debug.LogException(e);EditorUtility.DisplayDialog("01 녹화 준비",e.Message,"확인");}
        }
        public static RecorderControllerSettings Prepare(bool showWindow){
            string reason=Guard();if(reason!=null)throw new InvalidOperationException(reason);
            var scene=SceneManager.GetActiveScene();
            var directors=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DMCinematicDirector>(true)).ToArray();
            if(directors.Length!=1)throw new InvalidOperationException("이 씬의 Director가 정확히 하나여야 합니다.");
            var d=directors[0];if(!d.cameras||d.cameras.shots.Length<1||!d.cameras.shots[0].camera)throw new InvalidOperationException("01 카메라 연결을 확인해 주세요.");
            // Preserve the saved scene and any existing local Recorder preferences before preparation.
            string stamp=DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_ffff");
            string backup="UserSettings/DM_RecordingBackups/"+stamp;
            Directory.CreateDirectory(backup);File.Copy(ScenePath,backup+"/DM_avoidMissile_Cinematic.unity",false);
            const string prefs="Library/Recorder/recorder.pref";
            if(File.Exists(prefs))File.Copy(prefs,backup+"/recorder.pref",false);
            var gameType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")??Type.GetType("UnityEditor.GameView,UnityEditor");
            if(gameType==null)throw new InvalidOperationException("Game View 타입을 찾지 못했습니다.");
            EditorWindow.GetWindow(gameType);
            PlayModeWindow.SetViewType(PlayModeWindow.PlayModeViewTypes.GameView);
            PlayModeWindow.SetCustomRenderingResolution(1920,1080,"DM Full HD 1920x1080");
            PlayModeWindow.GetRenderingResolution(out uint width,out uint height);
            if(width!=1920||height!=1080)throw new InvalidOperationException("Game View가 Full HD로 설정되지 않았습니다.");

            var settings=RecorderControllerSettings.GetGlobalSettings();
            // Local Recorder list only. Keep other recorders, but exclude them from this capture.
            foreach(var existing in settings.RecorderSettings)existing.Enabled=false;
            var movie=settings.RecorderSettings.OfType<MovieRecorderSettings>().FirstOrDefault(m=>m.name==MovieName);
            bool isNew=movie==null;if(isNew){movie=ScriptableObject.CreateInstance<MovieRecorderSettings>();movie.name=MovieName;}
            movie.Enabled=true;movie.CaptureAudio=true;movie.CaptureAlpha=false;
            movie.ImageInputSettings=new GameViewInputSettings(); // Use Game View Resolution, not a separate render camera.
            movie.EncoderSettings=new CoreEncoderSettings{
                Codec=CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality=CoreEncoderSettings.VideoEncodingQuality.Custom,
                TargetBitRate=120,GopSize=60,EncodingProfile=CoreEncoderSettings.H264EncodingProfile.High,NumConsecutiveBFrames=2
            };
            movie.FileNameGenerator.Root=OutputPath.Root.Project;
            movie.FileNameGenerator.Leaf="Recordings/DM_Shot01";
            movie.FileNameGenerator.FileName="DM_Shot01_"+DefaultWildcard.Take;
            Directory.CreateDirectory("Recordings/DM_Shot01");
            int highest=0;
            foreach(string file in Directory.GetFiles("Recordings/DM_Shot01","DM_Shot01_*.mp4")){
                string suffix=Path.GetFileNameWithoutExtension(file).Substring("DM_Shot01_".Length);
                if(int.TryParse(suffix,out int n))highest=Math.Max(highest,n);
            }
            movie.Take=Math.Max(Math.Max(1,movie.Take),highest+1);
            var movieSO=new SerializedObject(movie);movieSO.FindProperty("captureEveryNthFrame").intValue=1;movieSO.ApplyModifiedPropertiesWithoutUndo();
            settings.SetRecordModeToManual();settings.FrameRatePlayback=FrameRatePlayback.Variable;settings.FrameRate=30;
            settings.CapFrameRate=true;settings.ExitPlayMode=true;
            if(isNew)settings.AddRecorderSettings(movie);
            settings.Save();

            Undo.RecordObject(d,"Prepare Shot01 recording");
            d.cameraPlaybackMode=DMCinematicDirector.CameraPlaybackMode.SingleCamera;d.takeCameraNumber=1;
            d.independentGroundPass=true;d.loopTake=false;d.playOnStart=true;
            EditorUtility.SetDirty(d);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("씬 저장에 실패했습니다.");
            if(showWindow){var window=EditorWindow.GetWindow<RecorderWindow>();window.SetRecorderControllerSettings(settings);window.Show();window.Focus();}
            StatusText("READY — 01 only; "+d.groundPassTake.duration+" seconds; 1920x1080; Variable/max30; MP4 H.264 High; 120 Mbps; GOP60; B2; audio ON; Manual. Press START RECORDING, then STOP RECORDING after take. Output: "+Path.GetFullPath("Recordings/DM_Shot01")+". No recording started.");
            Debug.Log("[DM Recorder] 01 Full HD 준비 완료. Recorder의 START RECORDING을 누르고 연출이 끝나면 STOP RECORDING을 누르세요. 아직 녹화는 시작하지 않았습니다.",d);
            return settings;
        }
    }
}
