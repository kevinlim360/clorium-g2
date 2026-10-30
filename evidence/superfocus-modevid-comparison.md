# ModeVid 참조와 G2 박스/입력 수정 — 2026-10-28

사용자 재현: A 선택 후 초점 탐색이 끝날 때까지 라이브뷰 포커스 박스를 옮기기 어려움. 컷수 직접 입력도 불가하다고 보고.

## 직접 확인한 G2 원인

LiveViewViewModel.SetFocusPos(int,int)는 G2FocusCamera.IsOwned이면 즉시 반환했다. A 탐색이 어댑터를 소유하므로 화면 위치 변경도 버렸다. 직전 수정은 B 버튼 활성/예약만 검사했으며 이 진입 경로는 빠졌다. 기존 WPF 수치 검사에서 TextBox.Text를 설정한 것은 실제 마우스/키보드 재현 검사가 아니었다.

수정: 카메라 소유권 검사보다 먼저 SuperfocusWindow.TrySelectPreviewBox를 호출한다. 앵커 탐색 중 선택 좌표를 불변 FocusBoxContrast에 기록하고 DrawFocusPoint는 이 좌표로 박스를 그린다. 현재 A 측정 ROI와 SDK 렌즈 이동은 바꾸지 않는다. B 선택은 표시한 소프트웨어 박스를 복사하여 현재 A 탐색 완료 뒤 직렬 실행한다. 사용자 미리보기 이동을 위해 추가 SDK AF/초점 명령을 보내지 않는다. 일반 대기 상태 박스 클릭은 기존 카메라 경로로 돌아간다.

입력: 컷수/품질을 앵커 탐색 중에도 활성화했다. TextBox PreviewMouseLeftButtonDown에서 e.Handled=true로 기본 포커스 처리를 차단하던 코드를 없애고, 기본 클릭/캐럿 동작 뒤 GotKeyboardFocus에서 선택하도록 변경했다. 이 클릭 경로가 사용자 숫자 입력 실패의 유일 원인이라고 확정하지 않는다. 제품 MaterialDesign TextBox 테마로 기존 필드 크기를 측정한 결과 PART_ContentHost40x22로, 0 크기 템플릿 가설은 배제했다(evidence/superfocus-edit-themed-before.log).

## ModeVid 자료

research/modevid-feature-modules/superfocus/README.ko.md 및 forms/TSUPERFOCUSTOOLBARFORM.lfm: A/B, 별도 AutoFocus, SHOTS 트랙바, Start/Stop/Reset 이벤트가 있다. 복구된 Ghidra 의사코드이며 원 개발 소스/독립 빌드 SDK가 아니다.

pseudocode/ModeViD/capture-and-ui/FUN_10010e710.c: 누적 상대 위치(0x858) 갱신과 표시. FUN_10010e950.c: 상대 위치를0x860에 저장하고 컷수/UI 갱신 호출. FUN_10010e8b0.c: 상태 초기화와 UI 갱신. FUN_10010eb70.c: 상대 이동을 더하고 내부 명령 전달. FUN_10010e240.c: 촬영 매수와 상대 이동에 기반한 촬영 계획. 이 자료에서는 상태/지점 지정과 촬영 시퀀스가 구분된다. 폼 이벤트와 모든 함수 주소의 대응, 실제 ModeVid의 모든 SDK/GUI 비동기 동작은 이번 정적 읽기만으로 확정하지 않았다. ModeVid 실행/토큰 생성/합성 프로그램 호출은 하지 않았다.

## 검사와 한계

신규 G2PreviewInputCheck는 실제 staged SuperfocusWindow + fake frame camera를 사용. 즉시 미리보기 위치 변경, 현재 A와 독립된 B ROI 복사, 탐색 중 컷수 편집, 반대 버튼 활성/예약/취소 검사를 통과했다. 숫자 범위/품질 회귀와 별도로 보존한다.

실제 키보드 전용 검사 G2PreviewKeyboardLiveCheck는 도구의 앱 허용 시간 만료로 시작되지 못했다. 제품 창의 원격 클릭도 outside captured monitor/coordinate input geometry unavailable로 불안정했다. 이를 제품 입력 성공으로 기록하지 않는다. 수정 개발 실행본을 열고 사용자에게 실제 입력과 박스 이동 확인을 요청했다. 대답 전까지 실사용 합격은 보류한다.
