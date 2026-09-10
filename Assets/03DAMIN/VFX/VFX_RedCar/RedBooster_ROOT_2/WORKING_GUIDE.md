# 작업용 / BackUp 분리 — 2026-09-10

앞으로 작업할 폴더: `Assets/03DAMIN/VehicleBooster_Working`

## 두 오브젝트의 연결

| 씬 오브젝트 | 프리팹 | 그래프·텍스처·재질·셰이더 |
|---|---|---|
| BlueBooster_ROOT_Working | VehicleBooster_Working/Prefabs/PF_BlueBooster_Working.prefab | VehicleBooster_Working 내부 복사본 |
| BlueBooster_ROOT_Backup | VehicleBooster_Backup/Prefabs/PF_BlueBooster_Backup.prefab | VehicleBooster_Backup 내부 원본 |

이름 통일: 기존 VehicleBooster 폴더는 VehicleBooster_Backup으로 변경했습니다. 폴더·프리팹·씬 루트에 Working / Backup 표기를 맞췄으며, 효과 값·참조 GUID·위치·회전·활성 상태는 유지했습니다. 아래의 분리·복원 기록에서 기존 VehicleBooster 폴더는 현재 VehicleBooster_Backup을 뜻합니다.

원래는 같은 프리팹과 8개 VFX Graph를 공유했습니다. 작업용 프리팹은 독립 프리팹이며 원본의 Variant가 아닙니다. 작업용 그래프, 텍스처, 재질, Shader Graph, HLSL 함수 파일까지 복사하고 Unity API로 참조를 다시 연결했습니다. 작업용 프리팹에서 기존 VehicleBooster 폴더로 연결되는 의존성이 없음을 검사했습니다. Unity 패키지·기본 제공 자원은 정상적으로 공유합니다.

BackUp의 Outer만 `outer-refine/Outer.before.vfx`에 보관한 화염 질감 변경 직전 상태로 복원했습니다. 기존 TEX_PlasmaNoise, Face Camera Plane, 폭 1.25 / 길이 1.05, 원래 Size 곡선과 Z 랜덤 회전이 복구됩니다. 작업용은 TEX_OuterFlameFlow와 변경 후 설정을 유지합니다. BackUp의 위치·회전·활성 상태와 다른 원본 자산은 변경하지 않았습니다.

중요: Global Volume/Bloom과 카메라는 같은 씬의 전역 설정입니다. 이 설정을 나중에 변경하면 두 오브젝트의 화면상 표현 모두에 영향을 줄 수 있습니다. 이번 작업에서는 변경하지 않았습니다.

## Outer가 뒤에 따로 붙어 보이는 이유

검사한 열린 씬에서 Core, Inner, Outer의 로컬 위치는 모두 `(0,0,0)`, 회전은 모두 `(0,0,0)`, 스케일은 모두 `(1,1,1)`입니다. BackUp 루트는 비활성이고 작업용 Distortion도 비활성입니다. 따라서 이 검사 시점에는 위치 오프셋이나 활성 BackUp 중복이 원인이 아닙니다.

| 현재 작업용 값 | Core | Inner | Outer |
|---|---:|---:|---:|
| Speed | 15~17 | 10~12 | 8~10 |
| Lifetime | 0.11~0.16 | 0.22~0.30 | 0.38~0.52 |
| Intensity 배율 | 10 | 4.5 | 2.5 |
| 초기 Gradient Alpha | 1 | 0.8 | 0.5 |

1. Outer는 속도가 느려도 훨씬 오래 살아 뒤에 더 길게 남습니다. 단순 Speed×Lifetime의 이동 거리 범위는 Core 약 1.65~2.72, Inner 약 2.2~3.6, Outer 약 3.04~5.2입니다. 현재 Diameter/Length가 1일 때의 계산이며 Outer는 TailLimit 4.6으로 제한됩니다. 텍스처 크기와 Alpha를 포함한 실제 가시 길이와는 다릅니다.
2. 앞쪽에서는 밝은 Core/Inner와 Additive 합성되므로 낮은 밝기의 Outer 질감이 구별되기 어렵습니다. 뒷부분에서만 파란 질감이 두드러져 보입니다. Intensity는 그래프의 곱셈 배율이며 HDR Color Picker의 EV와 다릅니다.
3. 새 화염 마스크는 같은 Quad 크기라도 밝은 부분의 폭이 좁고 길쭉합니다. 기존 둥근 입자보다 옆을 감싸는 면적이 작아졌습니다. 앞쪽 겹침을 약하게, 뒤쪽 흐름을 강하게 보이게 하는 추가 요인입니다.

이번에는 원인 분석만 했으며 작업용의 외형 값을 추가로 조절하지 않았습니다. 후속 수정은 작업용 폴더에서만, 노즐·중간의 감싸는 폭과 Outer 꼬리의 가시 길이를 함께 조절하는 방향이 적합합니다. 단순 Z 위치 이동이나 전체 밝기 증가만으로 해결하려고 하면 합성이 더 어긋날 수 있습니다.

## 검증 / 복구 자료

자료 폴더: `C:/Users/PC/.codex/visualizations/2026/09/08/01a08037-f3ed-7521-90b6-bbc674952c5d/booster-isolation`

- VFX.before.unity: 열린 씬의 분리 전 사본
- source-disk-backup: 시작 당시 원본 자산 사본
- backup-before.json / backup-after.json: BackUp 계층·컴포넌트 비교
- result.txt: 분리 및 의존성 검사 결과
- restoration.txt: Outer 복원 및 작업용/다른 원본 자산 보존 검사 결과
- trace.txt: 실제 위치, 활성 상태, 참조 및 그래프 값 기록

두 세트의 VFX 출력이 컴파일 가능한지 확인했습니다. 이 단계에서는 외형 튜닝이나 새로운 Play Mode 카메라 테스트는 하지 않았습니다. 이전 Outer 변경 전후 재생 캡처는 `outer-refine` 폴더에 있습니다.
