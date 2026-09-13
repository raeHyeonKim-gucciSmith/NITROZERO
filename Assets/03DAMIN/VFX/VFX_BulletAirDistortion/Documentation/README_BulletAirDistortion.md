# 총알 공기 왜곡 테스트

## 테스트 씬

`Scenes/BulletAirDistortion_Test.unity`를 열고 Play를 누릅니다. 배경·전용 카메라·조명·왜곡 프리팹만 있는 씬입니다. 기존 `Assets/03DAMIN/VFX.unity`에서는 총알 테스트 인스턴스와 배경판만 빼서 이 씬으로 옮겼습니다. 다른 차량과 VFX는 그대로입니다.

## 프리팹은 하나

`Prefabs/PF_BulletAirDistortion.prefab`을 사용합니다. `TestMissile_VisualOnly`는 기존 미사일의 메시와 재질만 복사한 작은 테스트 모형입니다. 원본 미사일 프리팹은 수정하지 않았습니다. 발사 컨트롤러, 연기, 파티클, 충돌, Rigidbody, Animator는 포함하지 않습니다. LOD0 외형만 사용합니다.

기존 임시 이동 스크립트 `DemoCubeFlight`가 이 모형을 천천히 움직입니다. 클래스 이름은 그대로지만 더 이상 큐브를 표시하지 않습니다. `Preview Playback Speed`로 테스트 이동 속도를 조절합니다. VFX 강도는 별개라 슬로모션에서도 약해지지 않습니다.

## 실제 발사되는 총알 연결

루트의 `Bullet Air Distortion VFX > 따라갈 총알`에 씬의 실제 총알 Transform을 넣습니다. 테스트 모형은 자동으로 멈추고 숨습니다. VFX는 실제 총알을 이동시키지 않습니다. 런타임 생성 총알은 생성 후 `SetTarget(projectile.transform)`으로 연결하세요. 한 인스턴스는 한 총알을 추적합니다.

## 왜곡 설정

- `Distortion Strength`, `Refraction Offset`: 굴절 강도.
- `Distortion Width`, `Distortion Length`: 폭과 짧은 후류 길이.
- `Trail Fade`: 남은 월드 공간 흔적의 수명, 기본 0.18초.
- 영화 슬로모션은 `실제 이동 속도로 왜곡 조절`을 끈 상태로 사용합니다. 이때 VFX의 `Bullet Speed`는 강도 반응에만 사용합니다.

배경은 불투명 이미지판이며 카메라는 Opaque Texture On입니다. 다른 씬에서도 Opaque Texture가 필요합니다. 압력층은 기본 꺼져 있습니다. 전역 Time.timeScale이나 차량 연출 시간은 바꾸지 않습니다.
