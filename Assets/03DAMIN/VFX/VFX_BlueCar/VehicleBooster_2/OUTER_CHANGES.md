# Outer Plasma 변경 기록 — 2026-09-10

대상: `Graphs/VFX_BlueBooster_Outer.vfx`

기존 둥근 연기 덩어리에서 진행 방향으로 흐르는 파란 화염으로 변경했습니다. AI 시안의 방향을 실제 파티클에 적용한 것이며 픽셀 단위로 동일한 결과를 의미하지 않습니다.

## 변경한 부분

| 위치 | 항목 | 이전 → 이후 | 이유 |
|---|---|---|---|
| Output Particle Quad | Main Texture | TEX_PlasmaNoise → TEX_OuterFlameFlow | 둥근 얼룩 대신 방향성 있는 부드러운 화염 질감 |
| Output → Orient | Mode | Face Camera Plane → Along Velocity | 화염 마스크의 길이축을 진행 방향에 정렬 |
| Initialize → Set Angle Z | 입력 | 기존 0~360° 해시 기반 회전 → 0° | 마스크가 제각각 돌아 구름처럼 겹치는 현상 방지 |
| Initialize → Set Angle X Random Uniform | A / B | 블록 없음(기본 0°) → -35° / +35° | 완전히 같은 평면을 피하고 후면에서 화염이 사라지는 현상 완화 |
| Blackboard → Output Scale X 계산 | ParticleWidthMin | 1.25 → 1.45 | 새 마스크의 검은 여백을 보정; 실제 밝은 영역은 기존 둥근 연기보다 좁음 |
| Blackboard → Output Scale Y 계산 | ParticleLengthMin | 1.05 → 1.65 | 개별 화염 조각을 길쭉하게 연결. 입자의 이동 속도/수명과는 별개 |
| Output → Size over Life | 곡선 | 아래 참조 | 중간이 크게 부푸는 형태를 줄이고 노즐 쪽부터 꼬리로 점차 축소 |

곡선 좌표는 `(정규화 수명, 크기 배율)`입니다.

- 이전: `(0,0.35), (0.15,0.9), (0.35,1), (0.65,0.65), (0.85,0.3), (1,0)`
- 이후: `(0,0.82), (0.12,1), (0.35,0.78), (0.65,0.4), (0.85,0.15), (1,0)`
- 이후 곡선 보간: Clamped Auto. 이 Size 곡선은 폭과 길이 양쪽에 영향을 줍니다.

## 변경하지 않은 설정

Outer Spawn Rate, SpeedMin/Max, LifetimeMin/Max, Length, Color/Alpha Gradient, Intensity, NoiseStrength/Frequency/Speed는 튜닝하지 않았습니다. Core/Inner/Glow/Hotspot/Streaks/Sparks의 설정이나 Bloom, Distortion도 이 편집 도구에서 변경하지 않았습니다. 기존 TEX_PlasmaNoise는 그대로 남겨 다른 그래프에 영향을 주지 않습니다.

Unity에서 기존에 열려 있던 그래프들의 저장 기록은 초기 재불러오기 시점에도 관찰되었습니다. 초기 디스크 해시와 모든 그래프 파일이 완전히 같다고 주장하지 않으며, 해당 기존 편집 내용은 되돌리지 않았습니다. Outer 수정은 현재 Unity에 열린 내용을 백업한 후 진행했습니다.

## 검증 및 백업

- Play Mode에서 변경 전후 측면, 위 사선, 아래 사선, 뒤 사선, 거의 정후면을 촬영했습니다.
- 전체 합성과 Outer 단독 화면을 별도로 확인했습니다. 비교 중 Distortion은 양쪽 모두 꺼 두었습니다.
- 검증 중 변경한 활성 상태와 카메라 위치는 되돌렸습니다. 원래 상태는 Outer만 켜짐입니다.
- Outer 출력의 컴파일 가능 여부, 실제 GPU 입자 생성, 유지 대상으로 지정한 값과 Color/Alpha Gradient 보존을 확인했습니다.
- 실제 씬 파일은 저장·수정하지 않았으며 백업 사본만 만들었습니다.
- 백업 및 실제 캡처: `C:/Users/PC/.codex/visualizations/2026/09/08/01a08037-f3ed-7521-90b6-bbc674952c5d/outer-refine/`
- `Outer.disk-before.vfx`: 시작 당시 디스크 버전. `Outer.before.vfx`: Unity에 열려 있던 Outer 내용을 저장한 수정 직전 버전.

## 새 텍스처 제작

기본 제공 이미지 생성 도구(imagegen)로 만든 `Textures/TEX_OuterFlameFlow.png`를 사용했습니다. 가져오기: Clamp, Mipmap 켜짐, Alpha Source None, sRGB 켜짐, 무압축, 최대 1024. 검은 배경은 Additive 합성에서 밝기를 더하지 않으며 회색/흰색 질감에 기존 파란 Gradient가 적용됩니다.

생성 프롬프트:

> Use case: stylized-concept. Asset type: production game VFX particle opacity texture, single grayscale flame mask, square 1024x1024. One centered vertically elongated soft white/gray flame wisp on pure black opaque background. This is NOT a complete booster, not a colorful rendered scene, not UI. Bottom end is softly rounded broad base near y=80%, taper upward into feathered narrow wisps near y=15%. Occupy center 65 percent width and 80 percent height, with all four image edges completely black and generous fade margins. Smooth flowing vertically stretched turbulent filaments inside a cohesive flame body; low-contrast wispy gray details, medium gray broad body, brightest parts white but not a solid white slab. Soft translucent feathered edges, no hard outline. Avoid round smoke bubbles, cotton, granular soot, cloud lumps, lightning, sparks, lens flares, text, checkerboard, color. Grayscale luminance will drive additive particle appearance in Unity. Shape should overlap smoothly when many identical particles are emitted, slightly irregular asymmetry, energetic yet soft hot gas flow.
