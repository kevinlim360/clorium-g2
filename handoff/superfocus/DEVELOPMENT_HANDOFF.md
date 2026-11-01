# 수퍼포커스개발 인수인계

2026-11-01 · https://github.com/kevinlim360/clorium-g2 · main

## 읽기 순서

1. 이 폴더의 `PRD.md`, `CHECKS.md`, `START_HERE.txt`.
2. 루트 `CLAUDE.md`, `work-status.md`, `촬영기능_교체현황.md`.
3. 아래 현재 소스와 최종 근거. 과거 PRD의 고=1단계/항상 A 복귀/고정 대형 패널 문구를 다시 적용하지 않는다.

## 핵심 소스

| 파일(루트 기준) | 역할 |
|---|---|
| `src/Clorium.G2/projects/CameraControl/CameraControl/windows/SuperfocusWindow.cs` | 팝업, 숫자 입력, 앵커 탐색, ROI/반대 앵커 예약, 화살표 수동 전환·큐, 촬영/합성 연결 |
| `src/Clorium.G2/projects/CameraControl/CameraControl/ViewModel/LiveViewViewModel.cs` | `SetFocusPos`의 소프트웨어 박스 우선 분기, `DrawFocusPoint` 표시, 기존 AF/촬영 연동 |
| `src/Clorium.G2/projects/CameraControl/CameraControl/windows/LiveViewWnd.xaml` 및 `.cs` | 고스트 위 실행 메뉴, 모델리스 팝업 수명/종료 |
| `src/Clorium.G2/projects/CameraControl.Core/CameraControl/Core/Classes/FocusRangePlan.cs` | 상대 위치, 3단계 품질, 최대/직접 컷수, 가까운 끝점 시작 |
| 같은 Core 폴더 `FocusBoxSearch.cs`, `FocusBoxContrast.cs` | 박스 선명도 탐색과 불변 정규화 ROI |
| 같은 Core 폴더 `FocusCaptureSequence.cs`, `G2FocusCamera.cs`, `CloriumStackProcess.cs` | 순차 촬영·다운로드 게이트·소유권·합성 프로세스 |
| `src/Clorium.G2/projects/CameraControl.Devices/CameraControl/Devices/Canon/CanonSDKBase.cs` | Canon SDK 초점·전송 어댑터 |
| `modules/Clorium.Stack/clorium_stack.py` | 합성 엔진 |
| `src/Clorium.G2/tests/SuperfocusRegression/` | 실제 Core를 링크한 회귀 검사 |

Windows는 경로 대소문자를 무시한다. 소스 디렉터리는 실제 `windows` 표기이고 일부 과거 문서는 `Windows`로 기록한다.

## 유지해야 할 구현 경계

- `G2FocusCamera`가 촬영/초점 SDK 소유권을 관리한다. 원본 다운로드 이벤트는 기존 MainWindow 진입점에서 소유자에게 라우팅한다.
- A 탐색 중 미리보기 이동은 카메라에 경쟁 AF 명령을 보내지 않는다. 화면용 ROI를 바꾸고 현재 A의 측정 ROI는 고정한다. B 선택 시 해당 ROI를 복사한다.
- 화살표 전환은 진행 중 SDK 명령이 반환된 뒤 AF 정지와 소유권 해제를 거쳐 실행한다. 여러 모터 소유자를 만들지 않는다. 조기 전환한 A/B는 현재 위치이지 자동 탐색 정점이 아니다.
- `automaticCuts`/`updatingCuts`로 품질 숫자 갱신과 직접 편집을 구분한다. 직접 입력 상한은 고 기준 및 기존 200이다.
- private `focusCameraFactory`/`stopNativeAutofocus`는 WPF 비동기 검사 경계다. 기본값은 정식 Canon 어댑터이며 라이선스 우회나 새 SDK 세션을 만들지 않는다.

## 빌드·검사·실행

개발 루트에서 PowerShell, .NET 10 SDK, .NET Framework 4.8 입력을 사용한다.

```powershell
dotnet run --project src/Clorium.G2/tests/SuperfocusRegression/SuperfocusRegression.csproj -c Release
./Build-SuperfocusDev.ps1
```

`Build-SuperfocusDev.ps1`은 기존 `.local/g2-runtime`과 baseline의 호환 입력, `.local/legacy-license.key`를 필요로 한다. 비밀값을 읽어 출력하거나 Git/Claude에 올리지 않는다. 후보 빌드가 끝나고 앱이 완전히 종료된 뒤에만 `-SkipBuild`로 재배치한다. 실행 중 DLL 잠금을 무시한 부분 복사는 하지 않는다.

WPF/WIA/WMPLib 후처리와 `g2-development-runtime.marker`를 유지한다. 이 표식은 개발본이 공유 세션 정리나 하드코딩된 설치본 재시작을 수행하지 않도록 한다. `baseline/`, `research/`, `delivery/`, `.local/`은 로컬 보존이며 공개 GitHub에는 없다.

개발 실행: `.local/g2-runtime/CameraControl.exe`. Python 실행 파일은 `stack-python.txt` 또는 `CLORIUM_STACK_PYTHON`, 엔진은 `modules/Clorium.Stack/clorium_stack.py`를 사용한다. 다른 PC의 경로를 그대로 복사하지 않는다.

추가 실제 WPF 검사 소스: `evidence/G2ManualTakeoverCheck.cs`, 이전 입력/박스 검사 `evidence/G2PreviewInputCheck.cs`. .NET Framework csc + 실제 staged 모듈 + WPF 참조가 필요하다. 로그의 fake/실기 구분을 보존한다.

## 마지막 확인과 남은 실기

- 사용자가 직접 숫자 입력과 A 탐색 후 박스 이동이 정상이라고 확인했다.
- 최신 화살표 전환은 `evidence/superfocus-manual-delivery.json`, `superfocus-manual-wpf.log` 기준. 실제 카메라 미연결 상태여서 최종 렌즈 전환은 미시험이다.
- 이전 광학/전송 성공 후보: `superfocus-popup-delivery.json`, `superfocus-popup-files.json`. 마지막 소스 변경과 동일한 실기 후보로 표현하지 않는다.
- ModeVid 참조 비교: `evidence/superfocus-modevid-comparison.md`. 보관된 자료는 복구 의사코드이며 원본 개발 소스가 아니다. SDK 원시 상수나 토큰을 추정해 복제하지 않는다.
- UI 자동화의 좌표/소유 창 문제가 반복됐고 별도 입력 검사 앱의 허용 시간이 만료됐다. 도구 실패를 제품 합격/불합격 근거로 바꾸지 않는다.

## 같은 저장소의 다른 작업

현재 미커밋분에는 2026-11-01 라이선스 정책 변경도 포함됐다. 기존 검증 근거는 `evidence/licensing-policy-delivery.json`: 정식 영구권 최초 인증 후 같은 PC 영구 사용, 내부 직원 테스트/데모 한국시간 달력 3개월 재인증. 운영 HTTPS 배포는 미완료다. 기존 광범위 PRD의 이전 일일 확인 정책은 모듈 README의 최신 정책으로 대체된다. 수퍼포커스 첫 작업에서 이 별도 정책을 되돌리지 않는다.

## Claude 시작 조건

Claude 프로젝트의 컨텍스트 파일은 개발 소스 전체가 아니다. 연결된 컴퓨터에서 위 로컬 루트를 실제 읽을 수 있는지 먼저 확인한다. 읽을 수 없으면 GitHub 공개 소스와 첨부 PRD를 기준으로 분석하고, 실제 카메라/비공개 로컬 입력 접근은 사용자에게 연결을 요청한다. 파일을 읽지 않고 읽었다고 보고하지 않는다.

첫 인수 응답은 현재 소스·근거 일치, 최우선 실기 3개와 필요한 연결을 보고하는 것으로 한다. 이어서 사용자가 지시한 개발을 수행한다. 운영 서버·설치본·고객 사진은 인수 작업만으로 변경하지 않는다.
