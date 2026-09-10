# HUD 파츠별 편집

Play를 종료하고 Tools → HYUNWOOK → Open TPS UI Builder 또는 Open FPS UI Builder로 연다.

Hierarchy에서 각 파츠를 선택한 뒤 Inspector의 Position(Left/Top), Size(Width/Height), Text/Font Size를 수정하고 Save한다. 숫자 값 자체는 Play 중 차량 데이터로 갱신된다.

- rpm-title: RPM x1000 글자
- rpm-bars: RPM 막대 영역 / rpm-bar-0~39: 개별 막대
- rpm-ticks 안 rpm-tick~rpm-tick-9: 개별 눈금 숫자
- speed-value / gear-value / time-value: 속도·기어·시간
- art-timer-frame / art-map-frame: 타이머·지도 테두리 이미지
- FPS art-ranking-frame / art-instrument-frame / art-fuel-frame / art-cooling-frame: 독립 프레임
- TPS group-art 이름이 붙은 요소: 랭킹·계기판의 기존 개별 프레임
- FPS fps-frame: 외곽 헬멧 장식. 내부 타일은 원본을 빈틈없이 그리기 위한 조각이므로 일반 편집 시 펼칠 필요가 없다.

프레임과 텍스트는 별도 요소다. 이제 프레임을 이동해도 다른 프레임이나 숫자가 같이 움직이지 않는다. 관련 값 묶음은 패널 하위에 유지했으며 개별 요소를 직접 선택해 수정할 수 있다. 레이아웃 좌표는 해당 부모 패널 기준이다.

원본 PNG 파일은 변경하지 않았다. HudArtworkPart가 원본 이미지의 지정 영역만 표시하므로 기존 그림을 그대로 재사용하며 각 파츠의 위치·크기·색상 틴트를 따로 편집할 수 있다. 이미지 내부 픽셀/선의 모양 자체를 바꾸는 것은 별도의 이미지 편집이다.

검증: C# 컴파일, UXML 구조/이름, 참조 텍스처, FPS 전체 이미지 영역의 중복·누락 없는 분할 확인. 실제 Unity UI Builder 조작은 미검증. Tools → HYUNWOOK → Validate TPS UI / Validate FPS UI로 Unity 내부 역직렬화 검사를 실행할 수 있다.
