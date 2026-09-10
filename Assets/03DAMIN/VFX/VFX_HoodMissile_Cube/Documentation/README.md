# 보넷 미사일 VFX용 독립 큐브 모형

이 단계는 **보넷/장착구 큐브 모형과 전개 동작, 향후 VFX 연결점**입니다. 연기 VFX Graph, Launch Burst, 미사일 발사/이동 및 Trail은 아직 포함되지 않습니다.

## 실행

같은 폴더의 `Scenes/SC_HoodMissile_CubePreview.unity`를 열고 Play 후 Game 창에 포커스를 둡니다.

- **Shift**: 보넷 열기 → 짧은 대기 → 장착구 상승.
- **R**: 닫힌 상태로 초기화. Shift로 다시 테스트할 수 있습니다.
- 독립 프리팹: `Prefabs/PF_HoodMissile_Cube.prefab`.
- 루트의 `CubeHoodDeploymentController`에서 개방량/시간/상승량을 조정합니다.

## 원본에서 읽은 값

저장된 `Assets/03DAMIN/DaminCar.unity`의 `Interferer_Car_ROOT`에 연결된 `TaurusHatchRotateAroundDeploy`를 읽었습니다. 실제 차량 메시, 머티리얼, 공용 스크립트는 이 모형에 복사하거나 연결하지 않았습니다.

단위는 원본 루트 좌표의 Unity unit(일반적인 1 unit = 1 m 가정)입니다. 정확한 측정치는 `SourceMeasurements.json`에 있습니다. 메시 정점을 루트 좌표로 변환해 외곽 범위를 측정했습니다.

| 항목 | 측정값 / 반영 |
|---|---|
| 보넷 외곽 | X 0.857636 / Y 0.173343 / Z 0.719584 |
| 보넷 큐브 | X/Z 외곽 유지, 굴곡 대신 두께 0.03의 평판. 상단 높이를 원본 외곽 상단에 맞춤 |
| 장착구 외곽 | X 0.814081 / Y 0.235457 / Z 0.631446 |
| 힌지 루트 위치 | (0, 1.023, 1.405) |
| 원본 개방각 | 30도 |
| **큐브 기본 개방각** | **Hood Open Amount 0.25 × Reference Open Angle 30 = 7.5도**. 좁은 틈 연출용이며 Amount 1이면 30도 |
| 개방 시간 | 0.8초 |
| 장착구 상승 시작 | 개방 후 0.1초 대기, 총 0.9초 시점 |
| 장착구 상승 | 로컬 Y +0.025, 0.7초 동안 |
| 전개 완료 | 1.6초 |
| 회전축 | 원본의 월드 +X를 차량 루트에서 해석한 로컬 -X. 새 루트가 회전해도 함께 회전 |

장착구는 큐브 바닥/벽으로 외곽을 재현했습니다. 내부 세 개의 `TubePlaceholder`는 공간 확인용 직육면체이며, 실제 차량 미사일 개수나 발사구 형상의 복제가 아닙니다. 외곽 프레임/받침대는 새 테스트용 치수입니다. 충돌체와 물리 시뮬레이션은 넣지 않았습니다.

## 구조와 연결점

```text
HoodMissile_Cube_ROOT [CubeHoodDeploymentController]
├─ Frame_CubeProxy
├─ HoodHinge
│  ├─ HoodPanel_Cube
│  └─ HingeMarker
├─ EquipmentBay_Cube
│  ├─ BayFloor / BayLeft / BayRight / BayFront / BayRear
│  ├─ TubePlaceholder_1 / 2 / 3
│  ├─ MissileSpawnPoint
│  └─ LaunchBurstPoint
├─ LeftVentPoint
├─ RightVentPoint
└─ InteriorSmokeVolume
```

컨트롤러의 Transform 참조는 모두 프리팹 내부에 연결되어 있으므로 별도 수동 연결은 필요 없습니다. 좌우 Vent의 로컬 +Z는 각각 바깥을 향하고, Spawn/Burst의 +Z는 위를 향합니다. 루트 선택 시 Gizmos로 연결점을 확인할 수 있습니다. InteriorSmokeVolume은 크기를 표시하는 빈 Transform이며 실제 연기를 생성하지 않습니다.

향후 미사일의 ExhaustPoint와 Trail은 별도 미사일 프리팹의 자식으로 구성해야 합니다. 전개 완료 이벤트(On Bay Ready)와 시작/초기화 이벤트를 제공하며, 기존 원본의 1.6초 동작이 끝나기 전 0.75초에 발사하려면 타이밍 설계를 별도로 조정해야 합니다.

## 독립성과 검증

- 새 폴더 안의 스크립트/머티리얼/프리팹/씬만 참조합니다. Unity 기본 Cube 메시 및 URP/Input System 패키지는 사용합니다.
- 기존 DaminCar/VFX 씬, 블루/레드 부스터, 타이어 연기와 공용 프로젝트 설정은 수정하지 않습니다.
- Unity 6000.3.18f1 / URP 환경에서 생성하고 새 컨트롤러를 컴파일합니다.
- 네이티브 Unity에서 지연 상승, 개방각, 상승량, 중복 시작 방지, 초기화, 루트 회전 후 재사용을 검증합니다. 실제 키보드 Shift 입력은 자동 테스트에서 누르지 않았습니다.
- HDRP로 옮기면 새 Lit 머티리얼을 해당 파이프라인용으로 바꿔야 합니다. 모형과 전개 코드에는 VFX Graph 의존성이 없습니다.
