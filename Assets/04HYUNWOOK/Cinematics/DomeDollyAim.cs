using UnityEngine;
using Unity.Cinemachine;
namespace Nitrozero.Cinematics
{
// Only aim is authored here; position is evaluated by CinemachineSplineDolly.
[ExecuteAlways]
public sealed class DomeDollyAim : CinemachineExtension
{
    [Header("Inspector controls")]
    [InspectorName("Transform 좌표 직접 사용")]
    [Tooltip("켜면 이 카메라의 Transform Position을 그대로 사용합니다. 스플라인 및 위치 오프셋보다 우선하며, 대상 바라보기는 유지됩니다.")]
    public bool useTransformPosition;
    [InspectorName("Transform 회전 직접 사용")]
    [Tooltip("Transform Rotation을 기준 방향으로 사용하고, Right Pan Degrees와 Rotation Offset을 화면에만 더합니다. 대상 추적과 샘플 회전보다 우선합니다.")]
    public bool useTransformRotation;
    [InspectorName("카메라 이동")]
    [Tooltip("체크하면 스플라인과 Timeline 이동을 함께 켭니다. 해제하면 현재 위치에 멈추고 Transform으로 직접 이동할 수 있습니다. 시선 설정은 바뀌지 않습니다.")]
    public bool topViewMovement = true;
    [InspectorName("원근감 제거 (직교 투영)")]
    public bool keepOverheadOrthographic;
    [Tooltip("켜면 Timeline이 이동 진행률을 제어합니다. 끄면 Spline Dolly의 Camera Position을 직접 수정합니다.")]
    public bool timelineDrivesDolly = true;
    [Tooltip("켜면 Timeline이 회전 진행률을 제어합니다. 끄면 Position을 직접 수정합니다.")]
    public bool timelineDrivesAim = true;
    [Range(0,1)] public float pathStart;
    [Range(0,1)] public float pathEnd = 1f;
    public AnimationCurve movementCurve = AnimationCurve.Linear(0,0,1,1);
    public AnimationCurve rotationCurve = AnimationCurve.Linear(0,0,1,1);
    [Tooltip("샘플 회전 대신 아래 Start/End Euler 각도를 사용합니다.")]
    public bool useEulerAngles;
    public Vector3 startEuler;
    public Vector3 endEuler;
    [Tooltip("추적 또는 기존 회전에 더할 각도입니다. Y는 좌우, X는 위아래, Z는 기울기입니다.")]
    public Vector3 rotationOffset;
    [Tooltip("컷이 끝날 때 추가로 오른쪽으로 돌아볼 각도입니다. 음수는 왼쪽입니다.")]
    public float rightPanDegrees;
    public AnimationCurve panCurve = AnimationCurve.EaseInOut(0,0,1,1);
    [Tooltip("스플라인 결과에 더할 월드 위치 오프셋입니다.")]
    public Vector3 positionOffsetWorld;
    [Tooltip("지정하면 이 오브젝트의 이동만큼 스플라인 카메라도 함께 이동합니다.")]
    public Transform positionFollowTarget;
    [Tooltip("스플라인 작성 시 추적 오브젝트의 월드 위치입니다.")]
    public Vector3 positionFollowReference;
    [Header("Authored aim and target")]
    public Quaternion[] rotations;
    [Range(0,1)] public float position;
    public Transform redTarget;
    public bool trackRed;
    [InspectorName("거리 기반 느린 시작 / 빠른 추적")]
    public bool distanceBasedTracking;
    public DomeCinematicSequence trackingSequence;
    [InspectorName("초반 추적 비율"), Range(0,1)]
    public float initialTrackingWeight=.12f;
    [InspectorName("통과 전 추적 완료 비율"), Range(.1f,1)]
    public float trackingCatchupFraction=.85f;
    public Vector3 targetOffsetWorld;
    public bool overhead;
    [System.NonSerialized] public bool holdOrientation;
    Quaternion lastOrientation;
    bool hasOrientation;
    Vector3 lastFollowPosition;
    bool hasFollowPosition;
    public void SyncTopViewMovement()
    {
        if(!overhead)return;
        useTransformPosition=!topViewMovement;
        timelineDrivesDolly=topViewMovement;
        var dolly=GetComponent<CinemachineSplineDolly>();
        if(dolly && dolly.enabled!=topViewMovement)dolly.enabled=topViewMovement;
    }
    public override void PrePipelineMutateCameraStateCallback(
        CinemachineVirtualCameraBase vcam, ref CameraState state, float deltaTime)
    {
        SyncTopViewMovement();
        if(overhead && keepOverheadOrthographic)
        {
            var lens=state.Lens;
            lens.ModeOverride=LensSettings.OverrideModes.Orthographic;
            state.Lens=lens;
        }
    }
    public void ApplyTimelineProgress(float progress, CinemachineSplineDolly dolly)
    {
        SyncTopViewMovement();
        if(timelineDrivesAim)position=Mathf.Clamp01(progress);
        if(useTransformPosition || (overhead && !topViewMovement) || !timelineDrivesDolly || !dolly || !dolly.enabled || !dolly.Spline) return;
        float u=Mathf.Lerp(pathStart,pathEnd,Mathf.Clamp01(movementCurve.Evaluate(progress)));
        dolly.CameraPosition=UnityEngine.Splines.SplineUtility.ConvertIndexUnit(
            dolly.Spline.Spline,u*(dolly.Spline.Spline.Count-1),UnityEngine.Splines.PathIndexUnit.Knot,dolly.PositionUnits);
    }
    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if(stage!=CinemachineCore.Stage.Finalize)return;
        if(useTransformPosition)state.RawPosition=vcam.transform.position;
        else state.RawPosition+=positionOffsetWorld;
        if(!useTransformPosition && positionFollowTarget)
        {
            if(holdOrientation && hasFollowPosition)state.RawPosition=lastFollowPosition;
            else
            {
                state.RawPosition+=positionFollowTarget.position-positionFollowReference;
                lastFollowPosition=state.RawPosition;hasFollowPosition=true;
            }
        }
        // Top-view look-at is independent of movement and temporarily overrides
        // the authored rotation without erasing it when the checkbox is toggled.
        if(useTransformRotation && !(overhead && trackRed && redTarget))
        {
            // Cinemachine writes RawOrientation back into the virtual camera
            // Transform. Keep it authored and apply the pan only to final output,
            // avoiding accumulated rotation and preserving Scene gizmo edits.
            var authored=vcam.transform.rotation;
            state.RawOrientation=authored;
            state.OrientationCorrection=Quaternion.Inverse(authored)*Decorate(authored)*state.OrientationCorrection;
            return;
        }
        if(holdOrientation && hasOrientation){state.RawOrientation=Decorate(lastOrientation);return;}
        if(trackRed && redTarget)
        {
            var direction=redTarget.position+targetOffsetWorld-state.RawPosition;
            if(direction.sqrMagnitude>.000001f)
            {
                state.RawOrientation=overhead ? GetStableOverheadAim(direction,vcam.transform.rotation)
                    : Quaternion.LookRotation(direction,Vector3.up);
                if(distanceBasedTracking && trackingSequence && !trackingSequence.accelerateLastShot)
                {
                    var origin=trackingSequence.GetRedPositionAtTime(trackingSequence.LastShotStartTime)+targetOffsetWorld;
                    var initialDirection=origin-state.RawPosition;
                    float speed=Mathf.Max(.001f,trackingSequence.fastSpeedMetresPerSecond);
                    // The authored vehicle path runs along world +X. Recompute from
                    // the live camera position so Scene edits keep the pass timing aligned.
                    float passSeconds=(state.RawPosition.x-origin.x)/speed;
                    float shotSeconds=Mathf.Max(.001f,trackingSequence.PlaybackDuration-trackingSequence.LastShotStartTime);
                    float catchup=Mathf.Clamp(passSeconds*trackingCatchupFraction,.05f,shotSeconds);
                    float u=Mathf.Clamp01(position*shotSeconds/catchup);
                    float eased=u*u*u*(u*(u*6-15)+10);
                    float weight=Mathf.Lerp(initialTrackingWeight,1,eased);
                    if(initialDirection.sqrMagnitude>.000001f)
                        state.RawOrientation=Quaternion.Slerp(Quaternion.LookRotation(initialDirection,Vector3.up),state.RawOrientation,weight);
                }
            }
            lastOrientation=state.RawOrientation;hasOrientation=true;
            if(overhead && useTransformRotation)
            {
                var authored=vcam.transform.rotation;
                state.RawOrientation=authored;
                state.OrientationCorrection=Quaternion.Inverse(authored)*Decorate(lastOrientation)*state.OrientationCorrection;
                return;
            }
            state.RawOrientation=Decorate(lastOrientation);
            return;
        }
        float progress=Mathf.Clamp01(rotationCurve.Evaluate(position));
        if(useEulerAngles)
        {
            lastOrientation=Quaternion.Euler(Vector3.Lerp(startEuler,endEuler,progress));hasOrientation=true;
            state.RawOrientation=Decorate(lastOrientation);return;
        }
        if(rotations==null||rotations.Length==0){state.RawOrientation=Decorate(state.RawOrientation);return;}
        float index=progress*(rotations.Length-1);
        int a=Mathf.Min(Mathf.FloorToInt(index),rotations.Length-1),b=Mathf.Min(a+1,rotations.Length-1);
        float fraction=index-a;
        state.RawOrientation=Quaternion.Angle(rotations[a],rotations[b])>100
            ? (fraction<.5f?rotations[a]:rotations[b]) : Quaternion.Slerp(rotations[a],rotations[b],fraction);
        lastOrientation=state.RawOrientation;hasOrientation=true;
        state.RawOrientation=Decorate(lastOrientation);
    }
    Quaternion Decorate(Quaternion rotation) => Quaternion.AngleAxis(rightPanDegrees*panCurve.Evaluate(position),Vector3.up)
        * rotation * Quaternion.Euler(rotationOffset);
    Quaternion GetStableOverheadAim(Vector3 direction, Quaternion authoredTransform)
    {
        // A fixed world +Z up vector twists the image as an off-axis target moves.
        // Swing from the authored top view instead, preserving its roll reference.
        Quaternion reference=Quaternion.Euler(90,0,0);
        float progress=Mathf.Clamp01(rotationCurve.Evaluate(position));
        if(useTransformRotation)reference=authoredTransform;
        else if(useEulerAngles)reference=Quaternion.Euler(Vector3.Lerp(startEuler,endEuler,progress));
        else if(rotations!=null && rotations.Length>0)
        {
            float sample=progress*(rotations.Length-1);
            int a=Mathf.Min(Mathf.FloorToInt(sample),rotations.Length-1);
            reference=Quaternion.Slerp(rotations[a],rotations[Mathf.Min(a+1,rotations.Length-1)],sample-a);
        }
        var forward=reference*Vector3.forward;
        var target=direction.normalized;
        // Deterministic fallback for a target exactly behind the reference view.
        var swing=Vector3.Dot(forward,target)<-.99999f
            ? Quaternion.AngleAxis(180,reference*Vector3.right)
            : Quaternion.FromToRotation(forward,target);
        return swing*reference;
    }
}
}
