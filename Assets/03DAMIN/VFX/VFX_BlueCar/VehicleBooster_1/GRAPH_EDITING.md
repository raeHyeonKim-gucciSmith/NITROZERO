# Blue Booster — 그래프 편집 안내

작업 씬: `Assets/03DAMIN/VFX.unity`

`Graphs` 폴더의 `.vfx` 파일을 더블클릭하면 VFX Graph에서 편집할 수 있습니다. 게임 연동용 런타임 제어 스크립트는 사용하지 않습니다.

## 화염 그래프

- `Core`: 밝은 중심 화염
- `Inner`: 시안색 안쪽 화염
- `Outer`: 파란 외곽 플라즈마
- `Streaks`: 가늘고 빠른 줄기
- `Sparks`: 작은 불꽃 입자

조절 위치:

- 입자 수: Spawn → Constant Spawn Rate → Rate
- 색과 크기 변화: Output → Color / Size 곡선 블록
- 속도: Blackboard → SpeedMin / SpeedMax
- 수명: Blackboard → LifetimeMin / LifetimeMax
- 폭과 길이: Blackboard → ParticleWidthMin / ParticleLengthMin (Streaks/Sparks에는 Max도 있음)
- 밝기: Blackboard → Intensity
- 외곽 흔들림: Outer Blackboard → NoiseStrength / NoiseFrequency / NoiseSpeed

Blackboard에서 속성을 선택하고 기본값을 변경합니다. 연결된 회색 입력칸은 잠긴 것이 아니라 선으로 연결된 노드의 값을 받는 칸입니다. 기본 조절은 위 속성과 곡선에서 하며 연결선을 끊을 필요가 없습니다. 변경 후 그래프를 저장하세요.

왼쪽 `NATIVE MATH` 그룹은 기존 화염 모양을 유지하기 위한 일반 연산 노드입니다. Custom HLSL VFX 블록은 제거했습니다. 값을 바꿀 때 Core/Inner/Outer/Glow를 함께 켜고 합성 결과를 확인하세요.

## 후광 그래프

- `Hotspot`: 노즐의 밝은 중심점, 카메라 방향 Billboard
- `Glow`: 부드러운 외곽 후광, 카메라 방향 Billboard
- `Distortion`: 진행축에 정렬된 열 왜곡

세 그래프는 고정 입자 1개를 사용합니다. Output의 Scale 블록에서 크기를, Hotspot/Glow의 Blackboard Intensity에서 밝기를 조절할 수 있습니다. Distortion의 Output에는 Strength, NoiseSpeed, NoiseScale이 있습니다.

기존 후광·왜곡 재질의 외형을 유지하기 위해 Shader Graph와 그 내부 픽셀 셰이더 함수는 남아 있습니다. 이것은 입자값을 덮어쓰는 C# 런타임 스크립트나 Custom HLSL VFX 블록과는 별개입니다.

## 검증

변환 전후 Play Mode에서 측면, 위/아래 사선, 뒤쪽 사선, 거의 정후면을 촬영해 비교했습니다. 현재 저장된 외형을 기준으로 변환했으며 움직이는 입자의 프레임별 배치는 다를 수 있습니다. Bloom과 다른 씬은 이번 그래프 전환에서 변경하지 않았습니다.
