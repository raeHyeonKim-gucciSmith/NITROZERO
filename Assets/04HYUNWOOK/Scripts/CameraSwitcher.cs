using UnityEngine;
using UnityEngine.Playables; // Timeline 제어를 위한 네임스페이스
using Unity.Cinemachine;     // Unity 6 / Cinemachine v3 기준

public enum CutsceneCameraType
{
    Camera1,
    Camera2,
    Camera3,
    Camera4,
    Camera5
}

[ExecuteAlways] // 에디터(플레이 전) 상태에서도 실시간 작동
public class CutsceneManager : MonoBehaviour
{
    [Header("4개의 시네마틱 카메라 등록")]
    public CinemachineCamera camera1;
    public CinemachineCamera camera2;
    public CinemachineCamera camera3;
    public CinemachineCamera camera4;
    public CinemachineCamera camera5;

    [Header("플레이 전 시점 선택 (드롭다운)")]
    public CutsceneCameraType activeCamera = CutsceneCameraType.Camera1;

    [Header("자동 플레이 종료 설정")]
    [Tooltip("체크하면 컷씬(Timeline) 재생 완료 시 플레이 모드가 자동으로 꺼집니다.")]
    public bool autoStopOnFinish = true;

    [Tooltip("연동할 Timeline의 PlayableDirector (비워두면 이 오브젝트에서 자동으로 찾습니다)")]
    public PlayableDirector playableDirector;

    [Tooltip("Timeline이 없을 경우 사용할 백업 타이머(초)")]
    public float fallbackDuration = 3.0f;

    private void OnEnable()
    {
        // 게임 실행(Play 모드) 시에만 자동 종료 타이머/이벤트 작동
        if (Application.isPlaying && autoStopOnFinish)
        {
            if (playableDirector == null)
            {
                playableDirector = GetComponent<PlayableDirector>();
            }

            if (playableDirector != null)
            {
                // Timeline 재생 종료 이벤트 등록
                playableDirector.stopped += OnTimelineStopped;
            }
            else
            {
                // Timeline이 지정되지 않았을 때 설정된 시간 후 종료
                Invoke(nameof(StopPlayMode), fallbackDuration);
            }
        }
    }

    private void OnDisable()
    {
        // 이벤트 해제
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnTimelineStopped;
        }
    }

    private void OnValidate()
    {
        UpdateActiveCamera();
    }

    private void Awake()
    {
        UpdateActiveCamera();
    }

    public void UpdateActiveCamera()
    {
        SetCameraPriority(camera1, activeCamera == CutsceneCameraType.Camera1);
        SetCameraPriority(camera2, activeCamera == CutsceneCameraType.Camera2);
        SetCameraPriority(camera3, activeCamera == CutsceneCameraType.Camera3);
        SetCameraPriority(camera4, activeCamera == CutsceneCameraType.Camera4);
        SetCameraPriority(camera5, activeCamera == CutsceneCameraType.Camera5);
    }

    private void SetCameraPriority(CinemachineCamera cam, bool isActive)
    {
        if (cam != null)
        {
            cam.Priority = isActive ? 20 : 10;
        }
    }

    // Timeline 재생 완료 시 호출
    private void OnTimelineStopped(PlayableDirector director)
    {
        StopPlayMode();
    }

    // 유니티 플레이 모드 자동 종료 함수
    private void StopPlayMode()
    {
        Debug.Log("🎬 컷씬 재생이 완료되어 플레이 모드를 자동으로 종료합니다.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 에디터 플레이 종료
#else
        Application.Quit(); // 빌드된 게임 종료
#endif
    }
}