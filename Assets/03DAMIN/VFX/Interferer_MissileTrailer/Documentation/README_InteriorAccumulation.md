# HoodInteriorSmoke — 발사대 안쪽에서 차오르는 내부 축적

차량 최상위 `보넷 · 미사일 통합 연출 → 05 내부 연기만`에서 조절합니다.
이 효과는 유체 시뮬레이션이 아닌, 3D 공간에 겹쳐지는 연기 파티클입니다.

## 수정 원인과 범위

이전 연기 중심은 차량 로컬 y=0.973m, 공유 바닥 충돌은 y=0.910m였지만 미사일 중심은 약 y=0.884m입니다. 입자 세로 크기도 6cm로 제한되어, 미사일 위에 얇은 띠가 생겼습니다.

이번에는 `01_HoodInteriorSmoke` 안에 내부 전용 앵커 두 개를 추가했습니다.

- `InteriorFillVolume_Only`: (0, 0.875, 1.69)m. 미사일 아래와 사이를 포함하는 생성 공간의 중심.
- `InteriorFloorProxy_Only`: (0, 0.735, 1.69)m. 내부 전용 바닥 충돌면.

기존 공유 InteriorVolume/Floor와 보넷을 따라가는 Roof는 그대로입니다. 좌우/잔연기/발사/미사일 그래프, 보넷·발사대 동작과 발사 시간표는 변경하지 않았습니다. 보넷은 0.8초에 0→30도, 중간 5도 대기 없음 상태를 유지합니다.

## Inspector — 내부 전용 12개 값

| 항목 | 기본값 | 역할 |
|---|---:|---|
| InteriorSmokeStartTime | 0.3초 | 전체 연출 시간 기준 생성 시작 |
| InteriorBuildUpDuration | 2.2초 | 최소→최대 생성량 증가 시간 |
| InteriorSpawnRateMin | 100개/초 | 초기 생성량. 첫 0.15초는 0부터 부드럽게 진입 |
| InteriorSpawnRateMax | 1500개/초 | 최대 연속 생성량 |
| InteriorLifetimeMin | 3.4초 | 최소 수명 |
| InteriorLifetimeMax | 5.2초 | 최대 수명. 축적 후반에는 더 오래 머무는 새 입자 생성 |
| InteriorStartSize | 0.035m | 처음 작은 크기. 입자별 0.8~1.2배 변형 |
| InteriorEndSize | 0.18m | 수명 동안 서서히 자라는 목표 크기 |
| InteriorOpacity | 0.85 | 불투명도 상한. 입자는 태어날 때 값을 저장하고 서서히 나타남 |
| InteriorTurbulence | 0.012m | 작고 느린 흔들림 크기 |
| InteriorUpwardForce | 0.015m/s² | 매우 약한 상승 가속. 상승 거리는 Volume 높이의 12% 이내로 부드럽게 제한 |
| InteriorVolumeSize | (0.76, 0.28, 0.58)m | 내부 가로·높이·깊이 범위 |

시간표는 재생 중 고정됩니다. Play 종료 후 수치를 바꾸고 저장하세요. 기존 09번 성장량/공통 내부 흔들림 등은 새 내부 효과의 제어값이 아닙니다. 새 내부는 05번 값을 사용합니다.

## 변경한 VFX Graph 노드/파라미터

| 구간 | 노드/처리 | 변경 내용 |
|---|---|---|
| Spawn | Constant Spawn Rate | Emission × SpawnRate. 연속 생성만 사용하며 Burst/Enable/prewarm으로 채우지 않음 |
| Initialize | Set Lifetime / Size / Alpha | 작은 시작 크기, 긴 수명, 출생 시 불투명도 저장. 기존 입자 전체를 동시에 진하게 하지 않음 |
| Initialize | Set Mass | 이 위치 해석식 전용 출생 축적 상태 저장(0.2~1). 물리적 질량 기반 힘 계산에는 사용하지 않음 |
| Initialize/Update | Set Position, Sine, Add, Multiply | X/Y/Z 입체 분포. 초반 입자는 아래쪽 중심, 후반 새 입자는 높이까지 분포. 기존 입자가 새 분포로 순간 이동하지 않도록 출생 상태 사용 |
| Update 위치 | UpwardForce, age², Divide | 약한 상승을 내부 높이의 12% 이내로 제한. 한 방향 분출 없음 |
| Update 충돌 | Collision Shape — Plane × 2 | 기존 움직이는 보넷 면 유지, 내부 전용 바닥 면. 입자 크기를 고려한 충돌 반경 |
| Output 크기 | Set Size / ScaleY / AngleZ | 6cm 납작한 제한 제거. 수명에 따른 성장, 0.85~1.1 세로 비율, 임의 회전으로 겹친 puff 형성 |
| Output 알파 | Smoothstep 구성 연산 | 0.3초 동안 부드럽게 출현, 수명 마지막 22%에서만 감소 |
| Output 재질 | URP Lit Planar, Alpha, Flipbook, Normal Map | 기존 8×8 연기 텍스처 유지, 약한 normal bending(0.12), 회백색과 부드러운 가장자리 |

새 그래프 입력: `BirthFill`, `UpwardForce`. 기존 `VolumeSize`, `EmitterPosition`, `FloorPosition`, `RoofPosition/Normal`, `SpawnRate`, `LifetimeMin/Max`, `StartSize/EndSize`, `BirthOpacity`, `Turbulence`, `SmokeColor`, `CollisionMargin`도 사용합니다. 본체/좌우/미사일의 공유 설정 값은 덮어쓰지 않습니다.

## 검증

테스트 복사본에서 실제 차량, 동일 프레임의 연기 없는 차량, 연기 단독 화면을 비교했습니다. 정면과 사선 화면을 0.5/1.0/1.5/2.0/2.5초에 저장했습니다.

| 시간 | 실제 차량에서 확인한 모습 | 연기로 변화한 픽셀 수(800×600 사선 화면) |
|---|---|---:|
| 0.5초 | 거의 비어 있고 매우 옅은 작은 입자 시작 | 124 |
| 1.0초 | 발사대와 미사일 사이가 조금 흐려짐 | 4,557 |
| 1.5초 | 여러 틈에서 연기가 겹쳐 내부 일부를 가림 | 18,548 |
| 2.0초 | 깊이 있는 회백색 연기로 내부가 자욱해짐 | 35,574 |
| 2.5초 | 미사일 사이와 아래쪽 대부분이 연기로 채워짐 | 51,212 |

30fps/75프레임 검사: 2.5초까지 입자 수명 종료 없음, 연속 생성 증가, 큰 밝기 급변 없음(최대 프레임 변화는 마지막 밝기의 약2.38%). GPU 입자 수 1,777개. 프레임 내 GameObject/VFX Enable 전환 없음. SideLevel=0, Burst=0, 미사일 4발 장착 상태 유지. 테스트 로그와 원본 이미지는 `C:/UnityProject_Backups/InteriorDepth_20260911`에 있습니다.

좌우 Overflow 시간표는 이번에 수정하지 않았습니다. 현재 기본값은 2.5초까지 꺼져 있지만, 내부 축적 시간을 더 길게 바꿨을 때 좌우 시작 시간이 자동으로 따라 이동하지는 않습니다.
