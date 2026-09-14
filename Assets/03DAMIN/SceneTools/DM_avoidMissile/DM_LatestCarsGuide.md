# 최신 차량 교체 — DM_avoidMissile 전용

2026-09-14 Pull 이후 Assets/Prefabs/Final_Cars의 최신 차량 6대를 새로 배치하고 모두 Unpack Completely하여 사용합니다. 원본 프리팹 자산은 수정하지 않습니다.

- 활성 차량 이름은 Red_Car_Final, Blue_Car_Final, Green_Car_Final, extraCar1_Final, extraCar2_Final, extraCar3_Final입니다.
- 최신 프리팹의 루트 Scale을 그대로 유지합니다. 이전 씬 차량 대비 약 3.315배입니다.
- 최신 차량의 TrailerCruiseMotion과 분리된 Steering / Rolling 피벗을 사용합니다. DM Race Wheel Visual의 Use Cruise Rig가 켜져 있으면 이 최신 리그가 바퀴를 구동합니다.
- DM_CinematicDirector, 카메라 10개, DM Race Performance, 연기·미사일 준비, 빨간 차 부스터와 창문 경로를 새 차량으로 다시 연결했습니다.
- 도로 위 타이어 높이, 차량 간격, 카메라 거리는 큰 차량에 맞추고, 촬영 구간의 초 단위 길이는 보존합니다.
- 운전자 눈빛과 실내 촬영은 기존처럼 별도 제작이 필요한 임시 구간입니다.

## 이전 차량 백업

`DM_Backup_PreLatestCars_DO_NOT_PLAY`는 이전에 작업하던 차량 6대를 보관하는 비활성 그룹입니다. 삭제하지 않았습니다. 촬영 중에는 켜지 마세요. 새 차량과 같은 이름의 이전 차량이 이 그룹 안에 있으므로 카메라/주행 대상은 Hierarchy 최상위의 활성 차량을 선택하세요.

추가 파일 백업은 C:/UnityProject_Backups/DMLatestCars_20260914/Before.unity에 있습니다.

Unpack은 프리팹 연결만 해제합니다. 머티리얼·VFX Graph·메시·스크립트 자산은 여전히 공유될 수 있으므로, 이들 자산 자체를 바꾸려면 개인 복사본을 먼저 만들어 연결해야 합니다.
