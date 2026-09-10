# 05_GroundTrailSmoke

타이어 테스트의 기존 01~04 레이어를 유지하고, 접지점 뒤에서 바닥을 따라 낮게 흐르는 연기 한 개만 추가했습니다.

추가 위치: `TireSmoke_Test_ROOT / 02_TireSmoke / 05_GroundTrailSmoke`.

`PF_TireSmoke_TestRig.prefab`에 새 자식으로 추가합니다. 원본 `PF_TireSmoke.prefab`, 기존 네 그래프, 바퀴/애니메이션, 바닥, 조명, 카메라, 씬은 수정하지 않습니다. 테스트 프리팹 인스턴스에는 새 자식이 상속됩니다.

전용 그래프: `Graphs/VFX_TireSmoke_GroundTrail.vfx`. 텍스처도 이 폴더의 `Textures`에 별도 복사해 기존 그래프/텍스처를 수정하지 않고 조절할 수 있게 했습니다. 새 런타임 스크립트는 없습니다.

## Inspector 조절

새 `05_GroundTrailSmoke`의 Visual Effect Properties에서 조절합니다.

| 항목 | 기본값 | 역할 |
|---|---:|---|
| EmissionRate | 100 | 연기 양 |
| FlowSpeed | 1.8 | 바닥을 따라 뒤로 이동하는 속도 |
| Lifetime | 1.85 | 기준 수명. 파티클마다 약간 다름 |
| Opacity | 0.42 | 불투명도 |
| GroundHeight | 0.10 | 바닥 기준 입자 중심 높이 |
| Spread | 0.35 | 뒤로 갈수록 퍼지는 폭 |
| Turbulence | 0.045 | 작은 흔들림의 크기 |
| SizeMultiplier | 1 | 입자 크기 배율 |
| SmokeColor | (0.80, 0.82, 0.85) | 기존 연기와 어울리는 회백색 |
| ContactWidth | 0.34 | 접지점 생성 폭 |

테스트 모형 좌표에서 바닥은 Y=0, 바퀴 축은 X, 뒤쪽은 -Z입니다. 강한 상승/바퀴 회전 궤도는 넣지 않았습니다. 프리미티브 파티클을 얇게 겹쳐 입체적으로 분포시킨 연출이며, 유체 시뮬레이션이나 자동 지형 충돌은 아닙니다.

`FlowSpeed`와 `Lifetime`을 함께 올리면 연기가 테스트 바닥 길이를 벗어날 수 있습니다. 바닥이나 다른 레이어는 자동 변경하지 않습니다. 실제 이동 차량의 속도/슬립과 연동하는 기능은 이번 범위에 포함하지 않습니다.

확인 시 기존 네 레이어는 그대로 켜두고 새 레이어만 껐다 켜면 추가된 바닥 연기를 비교할 수 있습니다. 제거할 때도 새 자식과 이 GroundTrail 폴더만 대상으로 삼으면 됩니다. 프리팹에서 새 자식을 먼저 제거한 뒤 그래프를 삭제해야 Missing 참조가 남지 않습니다.

텍스처 원출처: Unity Visual Effect Graph의 WispySmoke03b 샘플. Unity Companion License 적용 자산이며, 이번에는 기존 로컬 타이어 텍스처에서 독립 복사했습니다.
