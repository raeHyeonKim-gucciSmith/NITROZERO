# RH_boostOn 캐릭터 차량 탑승·IK 작업표

대상 씬: `Assets/01RAEHYEON/RHScenes/RH_boostOn.unity`  
대상 캐릭터: `주인공_기본자세 (1)`, `라이벌_기본자세 (1)`, `방해자_기본자세 (1)`
첫 검증 대상(사용자 지정): `v1_Driver` + `Final_RedCarCockpit`

이 파일은 작업 순서와 담당자를 기록한다. 끝난 항목만 `- [x] 완료됨`으로 바꾸고, 실제 씬에서 확인하지 않은 항목은 완료로 표시하지 않는다. 각 단계의 결과와 수정이 필요한 점은 해당 항목 아래에 짧게 기록한다.

## 절대 규칙 — IK Target 계층

- [x] 완료됨 · Codex — 이 씬의 모든 `Two Bone IK Constraint`에는 **반드시 `v1_DriverRig` 아래의 내부 IK Target만 연결**한다.
- [x] 완료됨 · Codex — `Final_RedCarCockpit` 및 그 하위의 핸들·페달 Target은 Constraint에 직접 연결하지 않는다. 이들은 위치·회전 참고 또는 내부 Target 추적 원본으로만 사용한다.
- [x] 완료됨 · Codex — 위 규칙을 이미 두 번 잘못 안내했다. 이후 모든 IK 안내와 씬 수정 전에 Target의 부모가 `v1_DriverRig`인지 먼저 확인한다. 절대 같은 실수를 반복하지 않는다.
- [x] 완료됨 · Codex — 사용자가 수동으로 맞춘 IK Target의 Position·Rotation·Scale은 편집 상태에서 스크립트가 절대 변경하지 않는다. `DriverIKTargetFollower`는 Play 상태에서만 Target을 움직인다.

## 1. 작업 준비

- [x] 완료됨 · 사용자 — Unity Package Manager에서 Animation Rigging 설치. 프로젝트 설정 파일에서 버전 `1.4.1` 확인.
- [x] 완료됨 · 사용자 — 첫 검증 대상을 `v1_Driver`와 `Final_RedCarCockpit`으로 지정.
- [ ] 진행 전 · 사용자 — 나머지 캐릭터와 차량의 배정은 첫 검증 후 지정.
- [x] 완료됨 · Codex — 저장된 RH_boostOn 씬에서 `v1_Driver`, `Final_RedCarCockpit`, `v1_DriverSeatAnchor` 확인. 좌석 기준점은 Cockpit의 자식임.
- [x] 완료됨 · 사용자 — `v1_Driver`를 Cockpit 좌석에 맞게 배치하고 배율을 현재 크기에서 6.5배로 조정. 화면에서 좌석 크기에 맞는 것을 확인.
- [x] 완료됨 · Codex — `Final_RedCarCockpit`의 핸들 구조와 `v1_Driver`의 오른팔 본 계층 확인.
- [ ] 진행 전 · Codex — RH_boostOn 씬의 작업 전 상태를 복구할 수 있게 보존하고, 기존 라이벌 Timeline 연결을 기록.

## 2. 첫 캐릭터의 좌석 배치

- [ ] 진행 전 · Codex — 차량에 `DriverSeatAnchor`를 만들고 첫 캐릭터를 좌석에 맞춰 배치. 차량·캐릭터 배율과 부모 관계를 정리.
- [ ] 진행 전 · 사용자 — Unity 화면에서 골반, 머리, 무릎, 차량 내장재와의 간섭을 확인하고 원하는 앉은 위치를 알려주기.
- [ ] 진행 전 · Codex — 확인 결과를 반영해 앉은 기본 자세를 구성하고, 차량 이동 중에도 탑승 위치가 유지되는지 검증.

## 3. 오른손 핸들 IK 시험

- [x] 완료됨 · 사용자 — 새 `v1_RightHandTarget`을 핸들 림의 오른손 접점에 배치.
- [x] 완료됨 · 사용자 — `Rig Builder`, `v1_DriverRig`, `Rig`, `v1_RightArmIK`, 오른팔 `Two Bone IK Constraint`, 팔꿈치 Hint를 구성.
- [x] 완료됨 · 사용자 — `RightArm → RightForeArm → RightHand` 연결과 위치 영향도 적용. 손목 회전 영향도는 `0`으로 유지.
- [x] 완료됨 · 사용자 — 오른손 목표점을 드라이버 쪽 핸들 표면으로 보정해 관통 없이 접점 확인.
- [ ] 진행 전 · Codex — 팔꿈치 굽힘과 차량·핸들 이동 중 손 접촉 검증.
- [ ] 진행 전 · Codex — 차량·핸들이 움직일 때 손이 따라붙는지 검증하고, 확인된 수정점을 반영.

## 4. 나머지 손발과 동작

- [x] 완료됨 · 사용자 — `v1_SteeringPivot`, `v1_BoosterPivot`, `v1_AccelPivot`, `v1_ClutchPivot` 배치 완료.
- [x] 완료됨 · 사용자 — 핸드브레이크와 시프터는 기존 회전 기준점이 맞아 별도 Pivot을 만들지 않기로 확인.
- [x] 완료됨 · 사용자/Codex — 오른손 내부 IK Target을 핸들 목표점에서 시프터 Grip 목표점으로 전환. Timeline 눈금 40→60에서 Blend 0→1, 시프터 레버는 60→75에서 회전하고 75 자세를 유지. Play에서 손 접촉과 레버 추적 확인.
- [ ] 진행 전 · 사용자/Codex — 기본상태는 양손 핸들 + 오른발 엑셀 밟음 + 왼발 클러치 위 대기(미작동). 눈금 0~30 기본상태 유지, 30~50 엑셀 해제와 클러치 밟기를 동시에 진행, 오른손은 40~60에 핸들에서 시프터로 이동, 60~75 기어 레버 조작 중 클러치를 유지. 80~100에 오른손은 핸들로, 클러치는 해제·엑셀은 다시 밟힌 기본자세로 복귀한다. 시프터 레버는 75의 변경된 기어 각도를 유지한다. 두 발과 두 페달의 접촉을 Play에서 확인한 뒤 전체를 `v1_GearShift`로 묶는다.
- [ ] 검증 전 · Codex/사용자 — 발 추적 스크립트에 재생 시작마다 간격을 다시 계산하지 않는 고정 간격 모드를 추가. 엑셀·클러치의 이전에 맞았던 기본 자세에서 각 발의 `Lock Current Offset For Future Plays`를 한 번 실행해 보정값을 저장하고, 새 페달 회전 키에서 발 접촉을 Play로 검증한다. 내부 발 IK Transform은 임의 수정하지 않는다.
- [ ] 진행 전 · Codex — 왼손 핸들 IK와 오른발 가속·왼발 클러치 IK를 추가하고 손목·발목 방향, 팔꿈치·무릎 Hint를 조정. 브레이크는 사용하지 않음.
- [ ] 진행 전 · 사용자 — 손잡이·페달 접촉 위치와 최종 앉은 자세를 화면에서 확인.
- [ ] 진행 전 · 사용자 — 손이 이동할 A·B 지점과 각 이동이 일어날 컷·시간을 지정.
- [ ] 진행 전 · Codex — 손 목표점이 A에서 중간 경로를 거쳐 B로 이동하도록 Timeline을 구성하고 IK 영향도를 자연스럽게 전환.
- [ ] 진행 전 · Codex — 라이벌의 기존 5초 Timeline과 새 자세·IK가 충돌하지 않는지 확인하고 수정.

## 5. 세 캐릭터 적용 및 최종 확인

- [ ] 진행 전 · Codex — 첫 캐릭터에서 검증한 구조를 주인공·라이벌·방해자의 지정 차량에 각각 맞춰 적용.
- [ ] 진행 전 · Codex — 정지 화면, 차량 이동, 핸들 회전, Timeline 재생에서 손발 접촉·관절 꺾임·차체 관통을 확인.
- [ ] 진행 전 · 사용자 — 실제 촬영 구도에서 세 캐릭터의 자세와 손발 움직임을 최종 확인하고 수정점을 알려주기.
- [ ] 진행 전 · Codex — 최종 수정, 씬 저장 및 변경 사항 검증. 각 완료 항목과 남은 제한 사항을 이 파일에 기록.

현재 상태: Animation Rigging 설치, `v1_Driver` 좌석 배치, 손발 IK 접점의 사용자 조정, 네 개의 새 조작계 Pivot 배치 완료. 핸드브레이크·시프터는 기존 Pivot 사용. Timeline 동작과 최종 접촉 검증은 남아 있음.

## 부스터 사용 동작 (`v1_Booster`)

- [ ] 진행 중 · 사용자/Codex — `v1_GearShift`와 별개의 Timeline으로 촬영 컷에 따라 선택해 사용한다. 양손 핸들·오른발 엑셀의 기본자세에서 오른손이 `v1_BoosterGripTarget`을 잡고, `v1_BoosterPivot`을 앞쪽 끝까지 회전시킨다. 손잡이 타겟은 움직이는 `Ctrl_Booster_Grip`의 자식이다. 이 동작에는 손·부스터의 복귀 키를 넣지 않고 끝 자세를 유지한다.
- [ ] 진행 중 · 사용자/Codex — 별도 `v1_RightArmIK` Animator 트랙에서는 회전 영향도 키가 기록됐으나 실제 Rig 계산에 반영되지 않았다. `DriverHandTargetBlend`가 Timeline의 `Booster Blend` 값으로 오른팔 `Two Bone IK Constraint`의 `Target Rotation Weight`를 직접 조절하도록 수정했다. 내부 `v1_RightHandIKTarget`의 `Right Arm IK` 참조를 연결하고, 기존 회전 영향도 트랙을 Mute한 뒤 Play에서 0→1 변화를 검증한다. 손잡이 접점의 Rotation을 Play에서 확인한 뒤 Edit 상태에 저장한다. 기존 내부 IK Transform 보정값은 수정하지 않는다.

## 드리프트 진입 동작 (`v1_Drift`)

- [x] 완료됨 · 사용자 — 별도 타임라인과 빨간 차 핸드브레이크의 `v1_HandbrakeGripTarget`을 만들고 손잡이 접점 Position·Rotation을 맞춤(1~4단계).
- [x] 완료됨 · Codex — `DriverHandTargetBlend`에 `Handbrake Source`, `Handbrake Blend`를 추가하고 핸드브레이크 블렌드가 오른손 위치·회전과 오른팔 IK 회전 영향도를 함께 조절하도록 확장. 기존 시프터·부스터 필드와 키는 유지.
- [ ] 진행 전 · 사용자 — 내부 `v1_RightHandIKTarget`의 `Handbrake Source`에 빨간 차의 `v1_HandbrakeGripTarget`을 연결. 기존 내부 IK Transform 값은 수정하지 않음.
- [ ] 진행 전 · 사용자 — 타임라인에 오른손 내부 타겟, 빨간 차 핸드브레이크 레버, 핸들 피벗, 엑셀·클러치 피벗 트랙을 연결하고 기본자세→엑셀 해제·클러치 작동→손잡이 접촉→핸드브레이크 당김·핸들 시계방향 90°를 키로 기록. 복귀 키 없음.

## 권총 조준과 시선 (`v1_Drift` 250번 이후)

- [ ] 진행 중 · 사용자 — 씬의 `Gun`에 `v1_GunGripTarget`·`v1_GunMuzzleTarget`, Cockpit에 독립적인 `v1_WindowAimTarget`을 배치한다. 씬 저장 후 배치 검증.
- [x] 완료됨 · Codex — `DriverHandTargetBlend`에 `Gun Source`·`Gun Blend`를 추가했다. Gun Blend가 손 위치·손목 방향의 마지막 전환을 제어하며 기존 시프터·부스터·핸드브레이크 블렌드는 유지한다.
- [ ] 진행 전 · 사용자 — 내부 `v1_RightHandIKTarget`에 `Gun Source`를 연결하고, 권총 Animator·Timeline 트랙을 연결한다. 권총은 300번까지 홀스터에 고정, 340번에 꺼낸 중간 자세, 380번에 창문 조준, 420번에 유지한다.
- [ ] 진행 중 · 사용자/Codex — 기존 Rig의 `V1_HeadAim` Multi-Aim Constraint가 `v1_WindowAimTarget`을 바라보게 구성한다. 현재 저장된 씬에서는 Source Objects 첫 항목 Weight가 0이므로 이를 1로 맞춰야 한다. `DriverHeadAimBlend`를 내부 `v1_RightHandIKTarget`에 추가하고 `Head Aim`에 `V1_HeadAim`을 연결한다. 기존 오른손 Animator 트랙에서 `Head Aim Blend`를 기록해 Play 중 Constraint Weight를 제어한다. 시선 타겟은 권총 자식으로 두지 않는다.

## 다른 운전자에게 적용

- [x] 완료됨 · 사용자 확인 — v1 운전자의 드리프트·권총 조준·고개 회전 동작을 1차로 완성했다. 접촉·관통의 최종 파일 기반 검증은 별도다.
- [x] 완료됨 · Codex — v2는 파란 차, v3는 초록 차의 좌석 기준점 아래로 배치했다. 기존 배율과 IK 보정값은 유지했다.
- [x] 완료됨 · Codex — v1의 내부 IK Target 규칙을 따르는 v2·v3 손 IK 설치 도구를 추가했다. v2는 핸들·시프터·`Ctrl_SwitchPanel_Toggles`의 빨간 스위치 손 목표점, v3는 양손 핸들 목표점을 생성한다. 설치 도구의 Unity 메뉴 실행과 화면 접점 보정은 아직 하지 않았다.
- [x] 완료됨 · Codex — `DriverHandTargetBlend`에 빨간 스위치 접촉용 Button Source·Button Blend를 추가했다. 기존 v1 블렌드 필드와 수동 IK Transform은 유지했다.
- [ ] 진행 전 · 사용자/Codex — Unity에서 `NITROZERO > Drivers > Set up v2 and v3 hand IK`를 실행하고 RigBuilder·양팔 Constraint 연결을 확인한다. 설치 도구는 기존 작업을 보존하도록 이름으로 중복 생성을 피한다.
- [ ] 진행 전 · 사용자/Codex — v2의 `v2_RedSwitchPressTarget`을 패널 가운데가 아니라 **빨간 스위치 표면**에 맞춘다. 현재 `Ctrl_SwitchPanel_Toggles`는 단일 씬 오브젝트이므로 빨간 스위치 자체의 독립 운동 가능 여부를 Unity 화면에서 확인한다.
- [ ] 진행 전 · 사용자/Codex — v2 시프터 접점, v3 양손 핸들 접점을 각각 맞추고 Play 상태에서 팔꿈치 굽힘·관통을 확인한다.
- [ ] 진행 전 · Codex — v2 기어 변경 및 빨간 스위치 누르기 Timeline 클립을 v1 동작을 참고해 별도로 생성·바인딩한다. v3는 양손 핸들 기본자세만 사용한다.
- [ ] 진행 전 · Codex/사용자 — v2·v3 운전자의 Rig 구성, 차량 조작계와 권총 배치를 개별 확인한다. v1의 코드와 눈금 배치는 재사용하되 IK 타겟 Position·Rotation·Scale과 팔·발 접점은 각 캐릭터/차량에 맞춰 별도 보정한다.
- [ ] 진행 전 · Codex/사용자 — 각 차량용 Timeline 복사본을 만들고 해당 차량의 Animator·조작계·권총·시선 타겟으로 바인딩한다. v1 씬의 바인딩을 그대로 쓰지 않는다.
