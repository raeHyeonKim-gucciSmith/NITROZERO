# DM 카메라 사용

전체 자동 재생과 구간 시간은 **DM_CinematicDirector**에서 조절합니다. 상세 내용은 같은 폴더의 **DM_FilmGuide.md**를 확인하세요.

Main Camera 1대가 화면을 출력하며 Cinemachine 카메라 10개를 13개 촬영 구간에서 재사용합니다.

|카메라|용도|
|---|---|
|01|고정 지면 통과|
|02|고정 후방 이탈|
|03|파랑 창문 근접|
|04|카키 창문 근접|
|05|카키 후퇴|
|06|보넷 준비→측면 발사|
|07|미사일 추적→상승|
|08|빨간 차 드리프트·회전 완료·재가속|
|09|탑뷰 접근·이탈·실내 대체|
|10|실내 모델 준비 후 사용할 구도|

Play 정지 후 DM_CinematicCameras를 선택하면 개별 구도를 확인할 수 있습니다. 추적 카메라의 위치는 Shots의 Position Offset, 보는 곳은 Aim Offset, 화각은 각 CM의 Lens에서 수정합니다. 수정 후 구도 버튼을 누르면 Main Camera 미리보기가 반영됩니다.

전체 Director가 Play 중인 동안 개별 카메라 버튼은 비활성화됩니다. 자동 시간표와 수동 샷 제어가 서로 덮어쓰지 않도록 한 것입니다.

첫 고정 카메라 01/02는 Director의 Fit Opening To Timing을 켜면 첫 구간 길이와 주행 속도에 맞춰 재생 시작 때 배치됩니다. Ground Camera Offset / Rear Camera Offset으로 보정하세요. 재생 중에는 고정입니다. 이 기능을 끄면 씬에서 직접 배치한 Transform을 그대로 사용합니다.

공용 프리팹·Graph·Material은 직접 수정하지 마세요. 전체 구성은 DM_avoidMissile 씬 전용입니다.
