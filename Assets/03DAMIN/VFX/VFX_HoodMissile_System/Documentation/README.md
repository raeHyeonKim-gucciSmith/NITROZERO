# Hood Missile — 독립 차량/미사일 VFX 시스템

## 사용

1. `Scenes/SC_HoodMissile_Preview.unity`를 엽니다. 기존 씬에 저장되지 않은 변경이 있다면 먼저 저장합니다.
2. Play 후 Game 창을 클릭합니다.
3. **Shift 한 번**: 내부 연기 축적 → 낮은 보넷 개방 → 좌우 압력 누출 → 장착구 상승 → Burst와 미사일 발사 → 추진 Trail → 연기 소멸.
4. **R**: 현재 연출/미사일을 지우고 닫힌 상태로 초기화합니다. 다시 Shift로 실행합니다.

시작 시 자동으로 발사하지 않습니다. 실행 중 Shift를 반복해도 미사일이 중복 생성되지 않습니다. 연출이 끝나면 다시 Shift로 실행할 수 있습니다.

## 생성 자산

- `Graphs/VFX_HoodVehicle.vfx`: 내부 연기, 왼쪽 누출, 오른쪽 누출, Launch 연기 Burst, Launch 섬광의 **5개 시스템 / 그래프 1개**.
- `Graphs/VFX_MissileTrail.vfx`: 짧은 추진 화염 + 연기 Trail의 **2개 시스템 / 그래프 1개**.
- `Prefabs/PF_HoodMissile_System.prefab`: 큐브 보넷 모형, 차량 그래프, Transform 연결점, 전체 연출 컨트롤러.
- `Prefabs/PF_Missile_Exhaust.prefab`: 단순 미사일 모형, ExhaustPoint, 미사일 그래프, 이동/Trail 바인더.
- `Scripts/HoodMissileVFXController.cs`: 한 번의 입력에 따른 보넷·장착구·연기 강도·발사·초기화 관리.
- `Scripts/MissileVFXFlight.cs`: 테스트용 미사일 이동, 배기 위치/방향 전달, 남은 Trail 소멸 후 자기 정리.

연기 모양은 VFX Graph의 기본 블록/오퍼레이터로 구성합니다. Custom HLSL 및 런타임 그래프 생성은 사용하지 않습니다. 스크립트는 입력, 타이밍, Transform 바인딩, 테스트 이동만 담당합니다. 별도 게임 무기/데미지/타격 시스템은 없습니다.

## 기본 타이밍

| 시간 | 동작 |
|---|---|
| 0.00초 | 연출 시작, 내부 밀도 상승 |
| 0.20초 | 보넷 개방 시작 |
| 0.35초 | 내부 밀도 목표 도달 |
| 0.48초 | 좌우 누출 시작 |
| 0.93초 | 좌우 누출 최고 강도 |
| 1.00초 | 보넷 개방 완료 |
| 1.10~1.80초 | 장착구 0.025m 상승 |
| 1.92초 | Launch 이벤트 한 번 + 미사일 생성 |
| 2.04초부터 | 좌우 누출 감소 |
| 2.32초부터 | 내부 연기 감소 |
| 이후 | 이미 나온 연기 자연 소멸, 미사일은 독립적으로 비행 |

기존 모형의 개방 0.8초 / 대기 0.1초 / 상승 0.7초를 유지하므로, 처음 제시한 예시의 0.75초 발사 대신 **장착구 전개 완료 후 발사**합니다. Inspector에서 각 시간을 바꾸면 발사 시점이 함께 바뀝니다.

기본 보넷 개방은 `Hood Open Amount 0.25 × Reference Open Angle 30 = 7.5도`입니다. 미사일은 닫힌 지붕을 관통해 수직 발사하지 않고 **앞쪽 ClearancePoint로 짧게 빠져나온 뒤 FlightDirection을 향해 상승**합니다. 실제 차량에 이식할 때는 미사일 크기와 통과 공간에 맞게 두 기준점을 조절해야 합니다. 이 모형은 물리 충돌로 보넷을 자동 회피하는 시스템이 아닙니다.

## Inspector 조절

루트의 `HoodMissileVFXController`가 재생 중 그래프의 아래 값을 전달합니다. 루트 Inspector에서 값을 바꾸거나, 바인딩 없는 별도 Visual Effect 인스턴스에서 그래프를 조정합니다. 연결된 인스턴스의 그래프 기본값만 바꾸면 컨트롤러 값이 우선합니다.

- 보넷: HoodOpenAmount, ReferenceOpenAngle, HingeLocalAxis, HoodOpenDuration.
- 장착구: BayDeployLocalOffset, DelayAfterHatch, BayDeployDuration.
- 타이밍: HoodStartDelay, InteriorFillTime, SideLeakStart, SideLeakRiseTime, LaunchDelayAfterBay, SideHoldAfterLaunch, InteriorHoldAfterLaunch, FadeOutTime.
- 내부 연기: InteriorSmokeDensity, InteriorSmokeTurbulence, SmokeColor.
- 누출: SideLeakSpawnRate, SideLeakSpeed, SideLeakSpread, SideLeakTurbulence, SideAsymmetry.
- 발사: LaunchBurstIntensity.
- Trail: TrailSpawnRate, TrailSpeed, TrailLength, TrailWidth, TrailTurbulence, TrailColor.
- 미사일 프리팹: LaunchSpeed, CruiseSpeed, AccelerationTime, TurnDuration, FlightDuration, MaxSmokeLifetime.

TrailLength는 직선 비행 시의 **대략적인 시각적 길이**입니다. 그래프는 `TrailLength / (MissileSpeed + TrailSpeed)`로 생성 시 수명을 정하고 랜덤 변화를 더합니다. 가속/선회 중에는 정확한 고정 길이가 아닙니다. 이미 생성된 입자의 수명은 새 설정으로 소급 변경하지 않습니다.

그래프의 제어용 노출값도 있습니다.

- 차량: InteriorCenter / InteriorSize / LeftVentPosition / RightVentPosition / LeftVentDirection / RightVentDirection / BurstPosition / InteriorLevel / SideLevelLeft / SideLevelRight.
- 미사일: EmitterPosition / EmitterDirection / MissileSpeed / TrailEmission / MaxSmokeLifetime.

그래프만 따로 미리 볼 때는 차량의 `InteriorLevel`, `SideLevelLeft/Right` 또는 미사일의 `TrailEmission`을 1로 올립니다. 기본값 0은 씬 로드 때 자동 분출을 방지하기 위한 값입니다. Burst는 `Launch` 이벤트로 한 번 발생합니다.

## Hierarchy / Transform 참조

```text
HoodMissile_ROOT [HoodMissileVFXController]
├─ Frame_CubeProxy
├─ HoodHinge
│  └─ HoodPanel_Cube
├─ EquipmentBay_Cube
│  ├─ TubePlaceholder_1 / 2 / 3
│  ├─ MissileSpawnPoint
│  └─ LaunchBurstPoint
├─ InteriorSmokeVolume
├─ LeftVentPoint
├─ RightVentPoint
├─ ClearancePoint
├─ FlightDirection
└─ VehicleVFX_Interior_Sides_Burst [VFX_HoodVehicle]

Missile_Runtime [독립 월드 오브젝트 / MissileVFXFlight]
├─ MissileVisual
└─ ExhaustPoint
   └─ MissileTrailVFX [VFX_MissileTrail]
```

모든 참조는 프리팹에 연결되어 있으므로 이 테스트에는 수동 연결이 필요 없습니다. 다른 차량에서는 HoodHinge, EquipmentBay, InteriorSmokeVolume, LeftVentPoint, RightVentPoint, MissileSpawnPoint, LaunchBurstPoint, ClearancePoint, FlightDirection을 해당 차량 위치에 맞게 연결합니다. Vent/Exhaust/FlightDirection의 로컬 +Z가 방향 기준입니다. InteriorSmokeVolume은 회전 없는 루트 기준 박스 공간으로 사용합니다.

미사일 프리팹은 차량의 자식으로 생성하지 않습니다. Trail 오브젝트는 미사일 ExhaustPoint의 자식이며, **화염은 Local simulation으로 노즐에 붙고 연기는 World simulation으로 공간에 남습니다.** 추진 효과는 미사일이 ClearancePoint를 통과한 뒤 시작합니다. 차량 쪽 연기 시스템은 Local simulation으로 차량 쪽 모형에 붙어 있습니다.

## 보존 범위와 한계

- 새 폴더의 자산만 사용합니다. 블루/레드 부스터, 타이어 연기, 기존 큐브 모형, DaminCar/VFX 씬을 수정하거나 참조하지 않습니다.
- Unity 기본 프리미티브 및 설치된 URP/VFX Graph/Input System 패키지는 공통 기반입니다. 이는 기존 부스터 파일과의 공유가 아닙니다.
- 연기/노멀/소프트 입자 텍스처는 설치된 Unity VFX Graph 패키지의 자산을 새 폴더에 별도 복사한 것입니다.
- 전용 프리뷰 씬의 작은 Bloom만 새로 만듭니다. 기존 씬/프로젝트의 Bloom, Renderer 설정, 패키지 설정은 바꾸지 않습니다.
- Unity 6000.3.18f1 / URP 17.3 / VFX Graph 17.3 기준. HDRP에는 Lit Output/머티리얼을 해당 파이프라인용으로 바꿔야 합니다.
- 연기는 조명 반응을 가진 다중 입자 표현이며 실제 유체/볼륨 시뮬레이션은 아닙니다. 보넷의 메시는 깊이로 가리고, 위치/속도를 제한해 낮은 내부 공간과 누출을 연출합니다. 임의 차량 형태를 자동으로 분석하거나 입자가 틈을 물리적으로 찾아가는 기능은 아닙니다.
- 자동 Bounds는 이동하는 미사일과 남은 Trail의 컬링 누락을 줄이기 위한 설정입니다. 성능 최적화는 실제 게임의 동시 미사일 수와 목표 GPU에서 별도로 측정해야 합니다.
