# Progressive Tire Smoke — 트레일러 시간 연출용

기존 Final/Burnout/타이어 프리팹을 수정하지 않은 독립 버전입니다. 이 폴더의 그래프·텍스처·스크립트는 기존 버전과 공유하지 않습니다.

## 사용할 프리팹

`Prefabs/PF_TireSmoke_Progressive.prefab`을 씬으로 드래그하세요. 루트의 `Progressive Tire Smoke Controller`가 5개 레이어를 제어합니다.

관리할 프리팹은 `PF_TireSmoke_Progressive.prefab` 하나입니다. 스크립트가 이 프리팹 내부의 5개 연기 레이어만 복제해 뒷바퀴마다 생성합니다. 별도 Emitter 프리팹이나 연결칸은 없습니다. 그래프·텍스처·스크립트는 이 Progressive 폴더에 그대로 유지해야 합니다. 새 버전을 제거할 때는 이 폴더 전체를 제거하면 되며 기존 버전은 이 폴더에 의존하지 않습니다.

## Inspector의 트레일러 시간표

기본 시간표는 Play 시작 또는 시작 버튼을 누른 시점을 0초로 봅니다.

| Inspector 항목 | 기본값 | 의미 |
|---|---:|---|
| Play 시작 시 자동 실행 | ON | Play에서 시간표를 시작 |
| 연기 시작 지연 | 2초 | 연기를 만들기 전 대기 |
| 연기가 커지는 시간 | 5초 | 약한 연기에서 최대 강도까지 증가 |
| 최대 연기 유지 | 3초 | 최대 강도를 유지 |
| 발산 감소 시간 | 3초 | 새 연기 생성량을 0까지 감소 |
| 증가 곡선 | Ease In/Out | 천천히 쌓이다 빠르게 증가하는 타이밍을 편집 |
| 반복 재생 | OFF | 반복 미리보기 선택 |

즉 2초에 발생 시작 → 7초에 최대 강도 → 10초에 감소 시작 → 13초에 새 연기 생성 종료입니다. 기존 입자는 수명이 끝날 때까지 남으므로 화면에서 연기가 완전히 사라지는 시각은 더 늦습니다. 시간은 Unity 게임 시간 기준입니다.

Inspector 버튼은 Play 모드에서 사용합니다. ‘트레일러 연출 시작’으로 시간표를 재시작하고, ‘발산 중지’로 자연스럽게 잦아들게 할 수 있습니다. 재시작은 남은 입자를 강제로 지우지 않습니다. Play 중 바꾼 값은 일반적인 Unity 동작에 따라 Play 종료 시 원래 값으로 돌아가므로, 최종 수치는 Play를 멈춘 상태에서 저장하세요.

자동 실행을 끄면 Smoke Power(0~1)를 수동 조절합니다. 수동 입력에는 Build Up Time/Fade Out Time이 적용됩니다. 시간표 실행에는 이 지연을 중복 적용하지 않습니다.

## 레이어와 증가 방식

- Contact: 낮은 강도부터 접지점에 생성.
- WheelWrap: 타이어 원주를 따라 감기고, Wrap Amount에 따라 머무르는 시간 변경.
- Trailing: 더 높은 강도에서 뒤쪽 후류 증가.
- FineWisps: 얇은 가장자리 디테일.
- GroundTrail: 낮고 넓은 바닥 연기.

Layers의 Response 커브와 Max Strength를 따로 편집합니다. Surge Threshold 이후 Surge Boost가 곱해져 ‘부왕’ 하는 증가를 만듭니다. Smoke Lifetime은 새 입자의 수명, Smoke Expansion은 입자가 나이 들며 부푸는 정도입니다. 전체 오브젝트 Scale을 키워 연출하는 방식이 아닙니다.

## 뒷바퀴 연결

스크립트는 연기 루트에 둡니다. 차량에 붙이지 않습니다.

- Vehicle Root: 사용할 차량 루트.
- Rear Left / Right > Wheel: 뒷바퀴 Transform.
- Center Point: 선택 사항. 회전하지 않는 바퀴 중심 마커.
- Ground Contact Point: 선택 사항. 접지 위치 마커.
- Wheel Radius / Width: 실제 월드 미터 단위.
- Direction Reference: 선택 사항. 회전하지 않는 조향/차축 기준.
- Wheel Axis: 위 기준 프레임 안에서의 차축 방향.
- Rotation Direction: 감기는 방향(+1/-1).
- Wheel Angular Speed: 연출용 각속도(rad/s). 자동 측정하지 않음.

바퀴를 연결하지 않으면 원래 배치한 위치에서 연기를 재생합니다. 연결하면 뒷바퀴에 각각 생성하고 소스 연기만 숨깁니다. 앞바퀴 슬롯은 없습니다. 바퀴별 랜덤 시드는 서로 다르게 설정합니다.

트레일러 연출에서는 Use Slip Amount를 OFF로 두면 됩니다. 나중에 실제 주행 코드가 필요하면 SetSmokePower, SetWheelSlip, SetVehicleSpeed 또는 노출된 필드로 값을 전달할 수 있습니다.

## 현재 범위

차량 속도·접지·슬립·회전은 자동으로 읽지 않습니다. 휠하우스/차체 Collision Volume, Depth Collision, SDF 및 주행 후 월드 공간에 남는 연기는 아직 구현하지 않았습니다. 현재는 Local-space 다층 연출 프리팹입니다. 실제 차량별 차체 관통 방지와 주행 물리 연동은 별도 적용 단계입니다.
