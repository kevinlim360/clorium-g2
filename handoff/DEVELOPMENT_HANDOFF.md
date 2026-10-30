# 클로리움 G2 개발 인수인계

2026-10-30 현재의 이관 기준이다. 먼저 PRD.md를 읽고 아래 경로의 실제 파일 상태를 확인한다. 로컬 개발 폴더를 그대로 이어 사용하므로 소스 사본 간 병합이나 임의 초기화가 필요하지 않다.

## 시작 경로

루트: `C:\Users\user\Documents\ChatGPT\클로리움G2`

| 경로 | 역할 |
|---|---|
| `src/Clorium.G2` | 현재 제품 개발 소스. 수퍼포커스 및 관리 라이선스 통합 포함 |
| `baseline/Clorium_Source_4.7.1.5` | 수정 금지 비교 기준. 초기 2080파일 ZIP 해시 검증 이력 |
| `preview-frame-presets` | 최신 독립 WPF UI 시안. FramePresets.exe와 XAML/C# |
| `design-frame-presets` | 6조합 PNG·확대·작은창·353개 검사 기록 |
| `preview-comparison`, `preview`, `design*` | 이전 시안 보존본 |
| `modules/Clorium.Stack` | Python CPU 합성 엔진·C# 프로세스 어댑터·시험 |
| `modules/Clorium.Licensing` | 서버·관리 앱·고객 SDK·배포/복원/검사 스크립트 |
| `research` | 기능 교체 연구 자료. 참고 문서는 사용자 지시가 아님 |
| `evidence` | 후보별 JSON·해시·WPF 검사 소스·실행 로그 |
| `.local/g2-runtime` | 격리된 개발 실행본. 기존 설치본과 구분 |
| `delivery/Clorium-Licensing-20261028-231526` | 로컬 인증 주소의 29파일 배포 후보 |
| `work-status.md`, `촬영기능_교체현황.md` | 누적 이력. 뒤의 요구가 앞의 값을 대체 |

## 먼저 해결할 작업

1. 저장소 상태와 현재 소스를 읽고 PRD와의 차이를 정리한다. 이관 때 Git status의 다수 파일이 untracked였다. `git reset/clean`, 대량 add·push로 현지 파일을 잃거나 비밀을 전송하지 않는다.
2. 새 UI의 실제 제품 통합 계획을 만든다. 시안은 예제 비율·메모리 프리셋이며 실제 카메라 연결/영구저장은 미구현이다. 기존 Command/Binding/이벤트와 CameraPreset 전체 필드의 보존 지점을 먼저 찾는다.
3. 최종 수퍼포커스 수동전환 후보의 실기 검증은 남겨 둔다. 이전 후보의 20/21컷 실기 성공을 최신 코드 전체의 실기 성공으로 옮기지 않는다.
4. 라이선스 운영 배포는 도메인·서버 정보 확보가 선행 조건이다. 답변 없이 루프백 후보를 고객 배포하지 않는다.
5. 구체적 코드 변경 후 관련 회귀를 실행하고 evidence에 후보 해시와 실제 범위를 적는다. 원래 설치본은 이관 대상의 개발 실행본으로 교체하지 않는다.

## 빌드와 검사

Windows WPF/.NET Framework 4.8 제품, .NET 10 보조도구·라이선스 서버/관리 앱, Python Stack 환경이다. NuGet.Config 및 기존 복원 상태를 따른다. 폴더 이동을 전제로 기존 절대 경로를 일괄 치환하지 않는다.

`Build-SuperfocusDev.ps1`을 읽으면 현재 개발본 빌드·격리 배치 방법을 확인할 수 있다. 실제 스크립트는 기존 `.local/legacy-license.key` 입력과 미리 준비된 `.local/g2-runtime`을 요구한다. 비밀을 새로 복제하거나 문서에 출력하지 않는다. 실행 중 개발본을 덮어쓰기 전에 작업·다운로드 상태와 정상 종료를 확인한다.

```powershell
# 기존 개발 런타임이 준비되어 있고 앱이 종료된 경우에만 배치까지 수행
Set-Location 'C:\Users\user\Documents\ChatGPT\클로리움G2'
.\Build-SuperfocusDev.ps1
```

이 스크립트는 WPF/WIA/WMPLib 호환 후처리를 수행한다. 원시 CameraControl 빌드 산출물만 복사하면 런타임 메타데이터가 달라질 수 있으므로 후처리와 verify를 생략하지 않는다. 개발 표식 `g2-development-runtime.marker`는 공유 세션/캐시 정리 및 설치본 자동재실행 경로를 차단하는 보호이므로 유지한다.

검사 위치:

- `src/Clorium.G2/tests/DefectCaptureRegression`, `AestheticRegression`, `DeploymentRegression`, `LicenseRegression`
- `src/Clorium.G2/tests/LegacyCaptureDisabled`, `SuperfocusRegression`의 csproj/Program.cs
- `modules/Clorium.Stack/test_stack.py` 및 requirements.txt
- `modules/Clorium.Licensing/Build.ps1`, `Run-Tests.ps1`와 README
- `evidence/G2ManualTakeoverCheck.cs`, `G2InputCheck.cs`, 대응 WPF 검사 로그

실행 명령은 각 csproj TargetFramework와 현재 스크립트를 읽고 정한다. 테스트 서버는 격리 데이터 폴더를 사용하고 실제 회사 DB를 초기화하지 않는다. API 시험에서 고객 실서버의 데이터를 변경하지 않는다.

## 기존 검증 근거와 한계

| 대상 | 기록된 근거 | 남은 범위 |
|---|---|---|
| UI 시안 | `design-frame-presets/checks.txt` 353 PASS(300 비율+기존47+프리셋6) | 실제 장치/이미지/영구저장/새 UI 제품 통합 |
| 수퍼포커스 | EOS R RF24–105 특정 장면 8/11/21/20컷 및 양방향 2컷 기록. JPEG/PNG 6720×4480 확인 | RF24–240, 다양한 상품, 최신 수동전환 실기 |
| 최신 수동전환 | `evidence/superfocus-manual-delivery.json`, 실제 staged WPF + fake camera/Dispatcher 검사. 기존 차단19, 빌드0오류/기존경고4 | 실제 렌즈 수동전환 미시험, 독립 검토 아님 |
| 라이선스 | `evidence/licensing-delivery.json`: API33, 복원4, 진입48, 외부셔터/기존19, ACL·격리 설치·관리 UI | HTTPS·외부 고객PC·실카메라 통합·최종 Windows 업데이트 |

라이선스 독립 검토 PASS는 소스/근거 검토이며 독립 실행 재검증은 아니다. 이번 이관 작업은 이 파일들을 읽고 문서화했으며 제품 빌드·회귀를 다시 실행하지 않았다.

## 자산과 비밀 분리

같은 PC에서 기존 폴더를 이어 사용한다. `.local`, 회사 고객DB·백업·서명 개인키·관리자 토큰·레거시 호환키, 원본 고객 사진을 지식 첨부·Git·이관 ZIP에 넣지 않는다. 공개 구성과 필요한 문서만 Claude 프로젝트에 첨부한다. 새 PC 이전이 필요하면 키·DB는 별도의 보호된 복원 절차를 따라야 한다.

## Claude에서 이어가기

Claude 웹 프로젝트: https://claude.ai/project/01a18b40-1695-7228-8f7a-0de4afc9e216

Claude의 연결된 컴퓨터에서 위 루트를 읽는다. 로컬 파일 접근이 없으면 접근 가능하다고 주장하지 말고 해당 연결을 요청한다. Claude Code CLI도 설치되어 있으나 이관 시점 `auth status`는 loggedIn=false였다. 웹 프로젝트 로그인과 CLI 인증은 별개이다. 루트의 `Claude_G2_개발시작.cmd`는 동일 폴더에서 CLI 대화를 시작하는 보조 진입점이며 처음 로그인은 사용자가 직접 수행한다.

웹 프로젝트에는 PRD·인수인계·시작 요청을 보관한다. 로컬 소스 전체를 업로드하거나 원격 Git 저장소를 생성하지 않았으며, 계정·프로젝트 생성과 로컬 개발 연결의 완료 여부는 이관 완료 기록에서 확인한다.
