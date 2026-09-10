# Final 타이어 연기 — 차량 연결용

기존 `TireSmoke_Test_ROOT Final`의 저장된 `02_TireSmoke` 연기와 Inspector 오버라이드를 독립 복제했습니다. 테스트 바퀴, 바닥, 조명, 카메라, Animator는 포함하지 않습니다. 기존 테스트 프리팹, 원본 그래프, 차량 프리팹, 씬은 수정하지 않습니다.

## 연결

1. **Final → 02_TireSmoke**를 선택합니다. 스크립트는 차량이 아닌 이 연기 부모에 붙습니다.
2. **Tire Smoke Wheel Controller → Vehicle Root**에 사용할 차량의 최상위 Root를 넣습니다.
3. 같은 컴포넌트의 Front Left / Front Right / Rear Left / Rear Right를 펼칩니다.
4. 각 **Wheel** 칸에 그 위치의 타이어 오브젝트를 드래그합니다. 차량 전체나 림과 타이어를 중복으로 넣지 않습니다.
5. 각 **Radius**에 실제 바퀴 반지름(월드 단위 m)을 넣고 Play로 확인합니다.

현재 Final에는 직접 부착합니다. 다른 연기 부모에 추가할 때는 해당 연기 오브젝트를 선택하고 **NITRO ZERO → VFX → Attach Wheel Controller to Selected Smoke**를 사용하세요. 차량에는 컴포넌트를 붙이지 않습니다. Smoke Prefab은 미리 연결됩니다. 여러 차량에는 연기 오브젝트를 각각 복제하고 각자의 Vehicle Root와 Wheel을 연결하세요. 기존 차량에 Apply All을 하지 않습니다.

차량/바퀴가 비어 있으면 기존 테스트 연기는 그대로 보입니다. 바퀴를 연결한 뒤 Play하면 원래 자리의 연기 Renderer만 잠시 숨기고 바퀴별 연기를 생성합니다. 연결 해제/컴포넌트 비활성화 시 원래 표시 상태가 복원됩니다. 테스트 바퀴·바닥·조명·카메라 자체는 건드리지 않습니다.

## 조절

- Emit Smoke: 전체 연기 켜기/끄기. Intensity: Final 대비 전체 방출량 배수.
- 각 바퀴 Emit / Intensity: 해당 바퀴만 켜기/강도 조절.
- Radius: 바퀴 아래 접지 위치와 연기 크기를 함께 맞춤.
- Use Renderer Center: 타이어 메시에 붙은 Renderer의 중심을 사용. 가져온 모델의 Transform 원점이 바퀴 중심이 아닐 때 유용.
- Center Point: 더 정확하게 맞추려면 별도의 바퀴 중심 Transform 연결. 연결하면 다른 중심 계산보다 우선.
- Position Offset: 차량 기준 X(좌우), Y(높이), Z(앞뒤), 월드 m 단위 보정.
- Vehicle Forward Axis: 기본 +Z, 연기는 반대 방향. 차량 모델 앞쪽 축이 다르면 변경.
- World Up: 켜면 위는 월드 +Y. 끄면 차량 기준 Vehicle Up Axis 사용.
- Direction Reference: 선택 사항. 조향 너클처럼 **굴림 회전이 없는** Transform만 연결. 회전하는 타이어를 넣으면 안 됩니다.
- Reference Wheel Radius: 원본 그래프의 기준 반지름(기본 0.65). 각 차량 크기 조절에는 바퀴별 Radius를 사용.
- Size Multiplier: 전체 연기 크기 보정. 크게 바꾸면 WheelWrap 중심도 달라지므로 작은 보정에 사용.
- Fade Time: 방출량 변화 보간. 방출 중지 후 이미 생성된 연기는 그래프 수명에 따라 사라짐.

## 동작과 범위

Play 중에 연결된 바퀴마다 독립 VisualEffect 인스턴스를 만듭니다. 생성된 오브젝트는 Hierarchy 최상위 `TireSmoke_* (Runtime)`으로 보입니다. 타이어의 굴림 회전이나 차량 부모의 비균일 Scale을 물려받지 않게 하기 위한 구조입니다. 위치와 차량 진행 방향만 매 프레임 추적하며, 컴포넌트를 끄거나 제거하면 생성물만 정리합니다. 기존 차/바퀴 Transform은 읽기만 합니다.

빈 바퀴 칸은 건너뛰며 같은 바퀴를 중복 연결하면 한 번만 생성합니다. Play 중 Wheel/Smoke Prefab 연결을 바꾸면 다시 생성됩니다. 기본 설정은 연출 확인용으로 정지 차량에서도 연기가 나옵니다. 게임 코드에서는 `SetEmission(bool)`과 `SetIntensity(float)`로 연결할 수 있습니다.

**현재 Final의 Local-space VFX를 그대로 유지합니다.** 기존 입자도 차량을 따라 이동합니다. 속도·슬립·브레이크·지면 접촉 판정, 역주행 방향 전환, 실제 도로 경사/충돌, 움직이는 차량 뒤에 월드 공간으로 연기를 남기는 기능은 이번 연결 작업에 포함하지 않습니다. 일반 주행에서 자동으로 연기가 발생해야 한다는 물리 판정도 하지 않습니다.

## 독립성

`SourceAssets`는 차량 연결본 전용 그래프/텍스처입니다. 기존 `VFX_TireSmoke`와 프로젝트 에셋 참조를 공유하지 않습니다. 이 차량용 프리팹을 쓰는 여러 차량끼리는 같은 차량용 그래프를 사용하며, 인스턴스별 위치·방출량 조절은 서로 영향을 주지 않습니다. 차량마다 그래프 디자인까지 다르게 바꾸려면 차량용 에셋 세트도 추가 복제해야 합니다.

텍스처 원본은 프로젝트에 이미 복제되어 있던 Unity VFX Graph 17.3 WispySmoke03b 샘플입니다. 원래 Unity 샘플 라이선스가 적용됩니다. Unity 6000.3.18f1 / URP + VFX Graph 17.3 환경에서 검증합니다.
