# Final 차량 — 보넷 / 미사일 VFX

## 풍성한 연기 / 느린 발사 개정

현재 Final 차량 기준: 내부 연기는 4초에 걸쳐 차오르고, 좌우 연기는 2.5초부터 2초 동안 증가합니다. 첫 발사는 보넷 시작 후 최소 6.5초에 발생하며, 초기 속도는 6m/s, 가속도는 2m/s²입니다. 기존 개별 미사일 상승·대기와 발사 후 연기 유지 Override는 보존했습니다.

- `08_보넷미사일VFX`: 내부 차오름 시간, 내부 밀도·덩어리 크기, 좌우 밀도·덩어리 크기, 공통 세로 볼륨을 조절합니다. 현재 내부 발생량600/밀도2/크기0.30, 좌우 발생량450/밀도1.8/크기0.28, 수명2.6초/팽창0.65/세로볼륨1입니다.
- `PF_MissileVFXSystem`: 흰 연기 밀도2.2, 시작폭0.16m, 팽창1.8m, 목표길이12m, 기본발생량900, 이동1m당40 추가, 상한2400으로 설정했습니다. 밀도는 틈이 비치는 정도, 폭·팽창은 기둥 굵기, 길이는 근사 꼬리 길이에 영향을 줍니다.
- `Trailer Missile Launcher`: 새 `보넷 시작 후 첫 발사 최소 시간 (초)`가 연기가 쌓일 최소 시간을 확보합니다. 0이면 기존 전개+지연+미사일상승+대기 합만 사용하고, 그 합이 최소 시간보다 길면 더 긴 쪽을 따릅니다. 다음 Shift 발사는 이 최소 시간으로 다시 지연되지 않습니다. 속도를 낮추려면 초기 속도와 가속도를 함께 낮추세요.
- 아래 표의 타이밍은 Final 차량의 현재 Override까지 반영한 값입니다. VFX 프리팹을 단독 사용하면 차량 Override가 없으므로 발사 후 유지/감소는 해당 프리팹 기본값을 사용합니다.
- 4개의 연기 그래프에 볼륨과 밀도를 반영하고 용량을 늘렸습니다(보넷 각 시스템2048, 미사일8192). 투명 입자가 많이 겹치므로 저사양/다중 차량에서는 발생량·크기·수명을 낮춰 GPU 부담을 줄이세요. 뒷바퀴 연기와 다른 사람 파일은 수정하지 않았습니다.

2026-09-11 흰 연기 개정: 미사일용 프리팹은 `01_WhiteSmokeTrail` 하나만 사용합니다. 코어·불꽃 자식은 제거했고, 기존 그래프 파일은 복구용으로 남겼습니다. 보넷의 발사 Burst는 변경하지 않았습니다.

## 바로 사용

`FINAL_PF_Interferer_MissileTrailer`에 차량 VFX를 연결하고, 재장전용 `PF_TrailerMissile`의 ExhaustPoint에 미사일 VFX를 연결했습니다. 기존 네 장착 미사일도 같은 미사일 프리팹을 사용하므로 VFX를 함께 상속합니다. 별도 수동 연결 없이 Play 후 Game 뷰에서 Shift를 누릅니다.

보넷·발사대의 움직임, 4번 미사일의 상승/대기, 바퀴 회전은 보존했습니다. 첫 발사 최소 시간과 비행 속도·가속도는 위 개정값으로 조정했습니다. 타이어 연기, 부스터, 씬과 프로젝트 렌더 설정은 수정하지 않았습니다.

## Inspector 위치

- 차량의 `03_보넷_미사일발사시스템/08_보넷미사일VFX`: `HoodMissileVFXController_Final`에서 연기 시점·발생량·밀도·흐름을 조정합니다.
- `MissileVFX/Prefabs/PF_MissileVFXSystem` 루트: `MissileExhaustVFXController`에서 모든 미사일의 흰 연기 Trail을 공통 조정합니다. 사용하지 않는 코어·화염 필드는 Inspector에서 숨깁니다.
- 보넷 각도/시간과 발사대 상승은 기존 `Trailer Hood Deployment`에서 조정합니다.
- 4번 미사일의 개별 상승 높이·상승 시간·대기 시간·발사 순서·속도는 기존 `Trailer Missile Launcher`에서 조정합니다.

차량에서 특정 VFX 인스턴스만 바꾸면 Prefab Override가 됩니다. 공통 기본값을 바꾸려면 위의 VFX 프리팹 자체를 편집하세요.

## 타이밍 기준

Shift를 VFX에서 중복 감지하지 않습니다. 기존 Launcher의 `onSequenceStarted`, `onMissileLaunched`, `onReset` 이벤트에 런타임 구독합니다.

| 항목 | 기준 / 기본값 |
|---|---|
| 내부 연기 시작 지연 | 보넷 열림 시작 기준 0초 |
| 내부 연기가 차오르는 시간 | 시작 이후 4초 |
| 좌우 누출 시작 지연 | 보넷 열림 시작 이후 2.5초 |
| 좌우 누출 최대까지 | 누출 시작 이후 2초 |
| Launch Burst | 선택한 미사일의 실제 발사 순간, 해당 발사구에서 한 번 |
| 추진 시작 지연 | 실제 발사 이후 0.02초 |
| 좌우 연기 감소 | 발사 후 3초 유지, 5초 동안 발생량 감소 (차량 Override 보존) |
| 내부 연기 감소 | 발사 후 3초 유지, 1.2초 동안 발생량 감소 (차량 Override 보존) |
| 잔연기 감소 | 발사 직후 생성, 0.1초 유지 후 1.8초 동안 발생량 감소 |

이미 생성된 입자는 발생량이 0이 된 뒤에도 자신의 수명에 따라 사라집니다. 따라서 감소 시간은 모든 입자가 즉시 없어지는 시간이 아닙니다. Burst 섬광은 약 0.06~0.13초, Burst 연기는 약 0.35~0.7초 남습니다.

발사 준비가 길어져도 내부/좌우 연기는 실제 발사까지 유지합니다. 발사 지연을 수정해도 Burst와 추진이 먼저 켜지지 않습니다. 후속 발사에서도 해당 슬롯의 Burst만 재생합니다. 일반 비행 종료 시 미사일 모델은 사라지고, 월드 Trail은 별도로 분리되어 남은 수명을 마친 뒤 정리됩니다. 전체 재장전/초기화는 연출을 즉시 초기화합니다.

## 저장한 프리팹과 그래프

```text
PF_HoodMissileVFXSystem
├─ 01_HoodInteriorSmoke             [VFX_HoodInteriorSmoke]
├─ 02_SideOverflowSmoke_L           [VFX_SideOverflowSmoke]
├─ 03_SideOverflowSmoke_R           [같은 SideOverflow 그래프]
├─ 04_LaunchBurst
│  └─ Slot_1~4_Burst                [VFX_LaunchBurst]
├─ 05_ResidualSmoke                 [VFX_ResidualSmoke]
├─ 06_EmissionPoints
│  └─ InteriorVolume / LeftVentPoint / RightVentPoint
└─ 07_VFXCollisionProxies_연출전용
   └─ HoodRoof_FollowsHatch / InteriorFloor / BodyBlocker_Box

PF_TrailerMissile
└─ 02_ExhaustPoint_추진VFX연결
   └─ PF_MissileVFXSystem
      └─ 01_WhiteSmokeTrail         [VFX_MissileSmokeTrail]
```

새 재사용 VFX 프리팹은 **2개**, 새 VFX Graph는 **7개**입니다. LaunchBurst 그래프 하나에 짧은 연기와 섬광 두 시스템이 있습니다. 슬롯별 Burst는 같은 그래프를 사용하지만 별도 인스턴스라 연속 발사 시 이전 Burst가 다음 발사구로 순간이동하지 않습니다.

런타임 스크립트: `HoodMissileVFXController_Final.cs`, `MissileExhaustVFXController.cs`.
Inspector 스크립트: `Editor/FinalMissileVFXEditors.cs`.

## 주요 조절값

- 내부: 발생량 600, 밀도 2, 흔들림 0.04, 덩어리 크기 0.30.
- 좌우: 발생량 450, 밀도 1.8, 덩어리 크기 0.28, 속도 0.5, 퍼짐 0.22, 난류 0.07, 0.3m 옆으로 나온 후 상승, 외부 상승 속도 0.25, 뒤쪽 흐름 0.1, 좌우 강도 차이 0.1.
- 공통: SmokeColor, SmokeLifetime, SmokeExpansion, SmokeRoundness, CollisionMargin. 보넷 연기 색은 (0.55,0.57,0.59)로 조정하고 출력 알파는 0~1로 제한해 겹침 부분의 과한 밝기를 줄였습니다.
- Burst: LaunchBurstIntensity. 발사 시점은 기존 실제 발사 이벤트를 따릅니다.
- 흰 연기 기본값: 시작 폭 0.16m, 팽창량 1.8m, 목표 길이 12m, 밀도 2.2, 난류 0.06, 색 (0.98, 0.98, 0.98). 기본 발생량 900, 이동 1m당 40 추가, 상한 2400. 가는 시작부에서 뒤로 갈수록 굵어지는 흰 연기 기둥을 목표로 합니다.
- Trail: 발생량·배출 속도·목표 길이·폭·팽창·난류·색·최대 수명·이동 거리당 추가 발생량·발생량 상한.

그래프 Blackboard의 주요 Exposed Property: `Emission`, `SpawnRate`, `SmokeDensity`, `Lifetime`, `ParticleSize`, `Expansion`, `Turbulence`, `SmokeColor`, `EmitterPosition`, `EmitterDirection`, `VolumeSize`, `Speed`, `Spread`, `RiseAfterDistance`, `RiseSpeed`, `BackflowSpeed`, `BackDirection`, `RoofPosition`, `RoofNormal`, `FloorPosition`, `FloorNormal`, `BodyCenter`, `BodySize`, `CollisionMargin`, `Intensity`, `FlameColor`, `PreviousEmitterPosition`, `MissileSpeed`, `TrailLength`, `MaxSmokeLifetime` (해당 레이어에 필요한 속성만 존재).

연결된 VFX는 컨트롤러가 값을 전달하므로 일반적인 튜닝은 컨트롤러 Inspector에서 합니다. 그래프만 단독으로 미리 보려면 `Emission`을 1로 올리고 Emitter/충돌 영역을 맞춥니다. Burst는 `Launch` 이벤트를 전송합니다.

## 공간과 충돌

- 차량 연기: 차량 로컬 공간. 내부는 낮고 느린 흐름, 좌우는 옆으로 나온 뒤 상승/후류가 추가됩니다.
- 추진 코어/화염은 현재 미사일 프리팹에서 사용하지 않습니다. 보넷의 짧은 Launch Burst는 별개입니다.
- 미사일 연기: **World 공간**. 발생 위치만 ExhaustPoint를 따라가고 생성된 연기는 따라가지 않습니다. 이전/현재 배기 위치 사이에 분산 생성해 고속 이동 시 프레임별 점선처럼 끊기는 현상을 줄입니다.
- TrailLength는 속도와 입자 수명으로 계산하는 근사 길이입니다. 가속/선회/낮은 프레임/발생량 상한에 따라 달라집니다.
- 내부에는 VFX Graph 네이티브 지붕·바닥 Plane Collision, 좌우에는 Body Box Collision을 넣었습니다. 지붕 프록시는 기존 보넷 움직임을 따라갑니다. 게임 물리 Collider나 프로젝트 Physics 설정은 추가/수정하지 않았습니다.
- 충돌 프록시는 **현재 차량을 위한 단순화된 영역**입니다. 실제 차체 전체의 SDF/유체 해석이 아니며, 임의 형상이나 모든 카메라 각도에서 완전한 메시 비관통을 보장하지 않습니다. 보넷 형태·각도·연기 크기를 크게 바꾸면 프록시와 CollisionMargin을 다시 맞추세요. 프록시를 옮겼으면 Inspector의 지붕 위치 기억 버튼을 누릅니다.

## 다른 차량으로 재사용

차량용 VFX의 Launcher에 호환되는 `TrailerMissileLauncher`를 연결하고, 내부 Volume, 좌우 Vent, 지붕/바닥/차체 프록시를 해당 차량 기준으로 맞춥니다. 기본 방향은 차량 로컬 +Z가 전방이며 Vent의 +Z가 배출 방향입니다. 미사일용 VFX는 `TrailerMissileFlight`의 자식 ExhaustPoint(+Z 배기 방향)에 배치합니다. 다른 게임플레이 컨트롤러를 사용한다면 해당 시작/발사/초기화 이벤트를 연결하는 어댑터가 필요합니다.

## 호환성과 보존

Unity 6000.3.18f1 / 설치된 URP·VFX Graph 기준입니다. Heat Distortion와 Sparks는 선택 사항으로 이번 구성에는 넣지 않았습니다. HDRP에서는 Lit Output과 왜곡 방식을 해당 파이프라인에 맞춰 변경해야 합니다. Soft Particle은 카메라 Depth Texture가 있어야 효과가 나며, 없어도 기본 연기는 그려집니다. Bloom은 기존 씬 설정을 사용하며 전역 Volume/Renderer/패키지 설정을 변경하지 않습니다.

기존 보넷/타이어/부스터 그래프와는 별도 그래프·텍스처를 사용합니다. Unity 패키지 샘플 텍스처 라이선스는 함께 있는 `UnityVFXGraph_LICENSE.md`를 참고하세요. 새 폴더 삭제 전 Final 차량 및 미사일 프리팹에서 VFX 참조부터 제거해야 합니다.

교체 전 프리팹 및 검증: `C:/UnityProject_Backups/FinalMissileVFX_20260911`.
