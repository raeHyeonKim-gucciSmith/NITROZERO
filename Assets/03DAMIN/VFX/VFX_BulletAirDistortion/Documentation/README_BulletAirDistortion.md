# 총알 공기 왜곡 / URP

연기·발광 없이, 총알 주변과 짧은 후류의 **불투명 배경 화면**을 굴절시키는 독립 효과입니다.
실제 유체/Schlieren 물리 해석이 아니라 게임용 화면 굴절 연출입니다.
기존 차량, 미사일, 보넷 연기, 프로젝트 렌더 설정은 변경하지 않습니다.

## 바로 보기

1. Scenes/Demo_BulletAirDistortion.unity를 열고 Play.
2. PF_DemoCubeAirShot의 DemoCubeFlight가 큐브를 반복해서 날립니다.
3. 그 아래 PF_BulletAirDistortion의 BulletAirDistortionVFX에서 왜곡을 조절합니다.
4. 단색/검은 배경보다 선과 무늬가 있는 배경에서 확인하세요.

테스트 큐브 이동만 PreviewPlaybackSpeed로 느리게 보여 줍니다.
실제 입력 속도는 DemoCubeFlight.BulletSpeed이며 VFX에 자동 전달됩니다.
테스트에서도 잔상 수명은 기본 0.18초입니다. 프로젝트 Time.timeScale은 바꾸지 않습니다.

## 런타임 스크립트는 정확히 두 개

- DemoCubeFlight.cs: 버려도 되는 큐브 발사/반복 테스트. 실제 게임의 이동·충돌·데미지 로직이 아닙니다.
- BulletAirDistortionVFX.cs: 재사용할 VFX. 총알을 움직이지 않고 위치/속도를 읽어서 굴절만 그립니다.

PF_BulletAirDistortion.prefab은 VFX만 있는 프리팹.
PF_DemoCubeAirShot.prefab은 테스트 큐브 + 테스트 발사 스크립트 + 연결된 VFX.

## 실제 총알 연결

1. VFX 프리팹을 별도 오브젝트로 둡니다.
2. Target에 실제 총알 Transform, ViewCamera에 게임 카메라를 연결합니다.
3. MeasureTargetSpeed를 켜면 이동 거리/시간으로 속도를 측정합니다.
4. 실제 발사 스크립트는 그대로 사용하고 DemoCubeFlight는 필요 없습니다.

코드로 연결할 때:

    airVFX.SetTarget(realBullet.transform); // 새 발사/풀링 재사용 때 이전 잔상도 초기화
    airVFX.Detach(); // 충돌/회수 직전 호출. Core 제거, 월드 잔상은 짧게 유지 후 소멸

총알이 사라진 뒤 잔상을 남기려면 VFX를 총알과 함께 Destroy하지 마세요.
풀링으로 VFX를 즉시 숨겨야 한다면 ClearTrail() 후 비활성화하세요.
필요하면 MeasureTargetSpeed를 끄고 BulletSpeed를 게임 코드에서 직접 전달할 수 있습니다.
테스트 발사 중에는 DemoCubeFlight가 BulletSpeed를 전달하므로 속도는 DemoCubeFlight에서 조절합니다.

## Inspector

| 값 | 역할 |
|---|---|
| DistortionStrength | 전체 굴절 강도 |
| DistortionLength | 짧은 월드 후류의 최대 길이(m), 속도에 따라 축소 |
| DistortionWidth | 굴절 영역 폭(m) |
| NoiseScale | 미세 흔들림의 공간 빈도 |
| NoiseSpeed | 흔들림 변화 속도 |
| TrailFade | 월드 잔상 수명(초), 기본 0.18 |
| BulletSpeed | 수동 모드의 속도 신호(m/s), 이동 자체를 제어하지 않음 |
| RefractionOffset | 최대 화면 UV 이동을 픽셀 단위로 조절 |
| OpacityMask | 굴절 영역 가중치. 연기의 불투명도가 아님 |

CoreLength, MinimumSpeed, FullStrengthSpeed로 총알 크기와 속도 반응 범위를 맞춥니다.
EnablePressureLayer는 선택형 압력 굴절선이며 기본 꺼짐입니다.
SimulationTimeScale은 VFX 자체 재생 배속이고 기본 1입니다.

## 구성 / Shader Graph

- Core: 총알을 따라가는 작은 카메라 방향 Quad.
- Trail: 월드 위치 이력을 사용하는 짧은 리본. 지속 생성되는 연기 파티클이 아닙니다.
- Pressure: 선택형 얇은 굴절 호. 흰 링/푸른 빛 출력 없음.
- SG_BulletAirRefraction.shadergraph: URP Transparent Unlit / Alpha blend / ZWrite off / 양면.
- AirRefraction.hlsl: Shader Graph Custom Function. UV0는 형태, UV1은 종류·수명/속도 가중치,
  Screen Position은 배경 샘플 좌표. 굴절 방향을 화면 픽셀 크기로 계산합니다.
- MaterialPropertyBlock으로 개별 VFX 값만 변경하며 공유 Material 원본은 런타임에 수정하지 않습니다.
- 런타임 생성 Mesh는 비활성화할 때 정리합니다. 이력은 최대 96개 위치로 제한합니다.

## 카메라 조건과 한계

URP 카메라에서 **Opaque Texture**가 필요합니다. 테스트 씬의 전용 카메라에만 켜 두었습니다.
기존 게임에 옮길 때 사용할 카메라의 Rendering/Opaque Texture를 확인하세요.
스크립트는 공용 Pipeline Asset이나 다른 카메라 설정을 자동 변경하지 않습니다.

배경에 투명 파티클/유리/UI만 있다면 그 부분은 이 방식으로 굴절되지 않습니다.
URP Scene Color / Camera Opaque Texture는 불투명 렌더 결과를 사용하기 때문입니다.
현재 리본은 ViewCamera 기준 billboarding이며 XR/다중 카메라 동시 렌더용 검증은 별도입니다.

Unity 문서:
https://docs.unity.cn/Packages/com.unity.shadergraph@17.0/manual/Scene-Color-Node.html
