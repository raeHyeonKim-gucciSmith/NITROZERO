using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CustomEditor(typeof(TrailerCruiseMotion))]
public sealed class TrailerCruiseMotionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var car = (TrailerCruiseMotion)target;
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("자동 조향: Steering Spline에 이동 경로를 연결하세요.\n" +
            "Manual Steering Weight: 0 = 자동 / 1 = 수동. 조향 실행 버튼은 수동으로 전환합니다.\n" +
            "Look Ahead는 커브 예측 거리, Smoothing Distance는 공간 평균 거리입니다.\n" +
            "교차 경로는 Use Spline Progress를 켜고 이동 컨트롤러의 진행도(0~1)를 연결하세요.", MessageType.Info);
        if (car.automaticSteering && car.steeringSpline == null)
            EditorGUILayout.HelpBox("스플라인이 연결되지 않아 기존 수동 조향을 사용합니다.", MessageType.Warning);
        EditorGUILayout.HelpBox("Wheel Speed Kph: 바퀴 회전만 조절 (0.1 = 슬로우, 최대 600).\n" +
            "핸들 360° → 앞바퀴 " + car.wheelAngleAt360.ToString("0.##") + "°. 양수: 우회전 / 음수: 좌회전.\n" +
            "일반 0.8초 / 드리프트·카운터 0.35초 / 정렬 1초. Override Duration으로 시간 변경.", MessageType.Info);
        EditorGUILayout.LabelField("현재 조향 시간", car.steering.Duration.ToString("0.##") + " s");
        float handle = Application.isPlaying || car.IsTimelineControlled ? car.CurrentHandleAngle :
            Mathf.Clamp(car.steeringWheelAngle, Mathf.Min(car.minHandleAngle, car.maxHandleAngle), Mathf.Max(car.minHandleAngle, car.maxHandleAngle));
        EditorGUILayout.LabelField("핸들 / 실제 앞바퀴", handle.ToString("0.##") + "° / " +
            (handle / 360f * car.wheelAngleAt360).ToString("0.##") + "°");
        using (new EditorGUI.DisabledScope(!Application.isPlaying || car.IsTimelineControlled))
        {
            if (GUILayout.Button("조향 실행 (Play 모드)")) car.StartSteering();
            if (GUILayout.Button("현재 각도에서 조향 멈춤")) car.StopSteering();
        }
        using (new EditorGUI.DisabledScope(Application.isPlaying || car.IsTimelineControlled || EditorUtility.IsPersistent(car)))
            if (GUILayout.Button("이 차량의 촬영용 Timeline 만들기")) CreateShot(car);
    }

    private static void CreateShot(TrailerCruiseMotion car)
    {
        string path = EditorUtility.SaveFilePanelInProject("촬영용 Timeline 저장", car.name + "_CruiseShot", "playable", "저장 위치를 선택하세요.");
        if (string.IsNullOrEmpty(path)) return;
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, AssetDatabase.GenerateUniqueAssetPath(path));
        var track = timeline.CreateTrack<TrailerCruiseTrack>(null, "Wheel Speed & Steering");
        var clip = track.CreateClip<TrailerCruiseClip>();
        clip.displayName = "기본 주행";
        clip.duration = 5;
        var asset = (TrailerCruiseClip)clip.asset;
        asset.wheelSpeedKph = car.wheelSpeedKph;
        asset.manualSteeringWeight = 0f;
        asset.steering.startHandleAngle = car.steeringWheelAngle;
        asset.steering.targetHandleAngle = car.steeringWheelAngle;
        var shot = new GameObject(car.name + "_CruiseShot");
        Undo.RegisterCreatedObjectUndo(shot, "Create cruise shot");
        var director = Undo.AddComponent<PlayableDirector>(shot);
        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.Hold;
        director.playableAsset = timeline;
        director.SetGenericBinding(track, car);
        EditorUtility.SetDirty(timeline);
        EditorUtility.SetDirty(track);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = shot;
        EditorGUIUtility.PingObject(timeline);
    }
}

[CustomEditor(typeof(TrailerCruiseClip))]
public sealed class TrailerCruiseClipEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var clip = (TrailerCruiseClip)target;
        EditorGUILayout.HelpBox("조향 시간: " + clip.steering.Duration.ToString("0.##") + "초\n" +
            "클립 시작부터 시작각 → 목표각으로 이동하고 이후 목표각을 유지합니다.\n" +
            "Recovery도 목표각을 직접 지정합니다 (직진 복귀는 0°).\n" +
            "다음 클립의 시작각을 이전 목표각에 맞추세요. 클립을 겹치지 않게 배치하세요.\n" +
            "클립이 조향 시간보다 짧으면 해당 지점까지만 진행합니다.", MessageType.Info);
        EditorGUILayout.HelpBox("Manual Steering Weight: 일반 주행 0 / 드리프트 수동 조향 1.\n" +
            "Automatic Blend In/Out은 스플라인 자동 조향과 전환하는 시간입니다.\n" +
            "끝에서는 0도 대신 현재 커브의 자동 조향으로 복귀합니다.\n" +
            "클립 사이에서는 차량의 Manual Steering Weight 값을 사용합니다 (기본 0).", MessageType.Info);
    }
}
