# 현재 배포: Clorium 4.7.1.5

직전 미감개선 기능을 유지한 버전 변경본입니다. 최신 설치·검증 안내는 RELEASE_4.7.1.5.txt를 먼저 읽으세요. 아래 4.7.1.4 문서는 이전 이력입니다. 내부 어셈블리 참조 2.1.6.0과 기존 비공개 빌드 입력 방식은 유지합니다.

# 현재 배포: 4.7.1.4 / 2026-10-23-aesthetic-mode

미감개선 기능은 Custom에서 켜며 실행 시 ON, 제품 전환 시 유지됩니다. 촬영 원본만 _PR_F/_PR_B, 하자와 치수사진은 기존 이름입니다. 적용·검증·한계는 RELEASE_AESTHETIC_20261023.txt를 먼저 읽으세요. 몬스타덱은 제외했습니다.

전체 빌드는 기존 비공개 키 입력을 CloriumLegacyLicenseKeyFile 속성으로 참조하며 복제하지 않습니다. Build.proj는 CoreWpfCompatibility 도구로 기존 WPF 공개 보조 타입을 보존하고 업무 메서드 IL 불변을 확인합니다. template은 기존 릴리스 Core DLL이며 이 소스의 releases/Clorium_4.7.1.4_20261023/files에 포함됩니다.

아래 내용은 이전 라이선스 수정본의 이력입니다.

# 현재 배포: 4.7.1.4 / 2026-10-23-license-match

라이선스 인식 오류 수정과 Windows 더블클릭 업데이트 EXE가 추가되었습니다. 사용자 적용은 `RELEASE_4.7.1.4_20261023.txt`, 개발자 재빌드와 검사 범위는 `deployment/windows-update/README.md`를 따르세요. 아래 2026-10-22/19 기록은 이전 릴리스의 이력입니다.

라이선스 호환 키는 비공개 빌드 입력으로 공급합니다. `.local` 및 실제 키 값을 소스 ZIP/Git에 추가하지 마세요. `tools/LicenseCompatibilityRecovery`는 정식 기존 실행기의 원본 모듈 SHA-256을 확인해 이전 발급 방식의 설정만 회수합니다. 이번 배포의 선택 파일과 매니페스트는 `releases/Clorium_4.7.1.4_20261023`에 있습니다.
# 2026-10-22 제품 전환 수정본 (Clorium 4.7.1.4)

현재 릴리스는 2026-10-22-session-reset입니다. 자세한 변경·검증 범위는 RELEASE_4.7.1.4_20261022.txt를 참조하세요. 이전 4.7.1.4 소스는 FILE_MANIFEST.base-20261019.json으로 구분합니다. 원본 20261005 기록도 보존합니다.

전체 빌드: dotnet msbuild Build.proj -m:1 -nodeReuse:false -p:BuildAllVariants=true
새 회귀 검사: dotnet run --project tests/DefectCaptureRegression/DefectHarness.csproj
바이너리/WPF 검사: tests/DefectCaptureRegression/Run-BinaryChecks.ps1
실제 전달 파일은 releases/Clorium_4.7.1.4_20261022_Update에 포함합니다. Core 소스가 변하지 않아 기존 검증 Core를 재사용했으므로 직접 재빌드한 Core와 배포 파일의 바이트는 다를 수 있습니다.

---

# Clorium 최종 전체 유지보수 소스 — 2026-10-05

최초 제공된 `Clorium_maintainable_source_20261002_v2.zip` 전체를 기반으로, 고객 PC에서 확인한 CameraControl 시작 수정과 Centering 폴더 열기 수정을 통합했습니다. 패치만 있는 묶음이 아니라 기존 전체 제품 소스, 솔루션, 참조 DLL, 리소스, 빌드 도구 및 최신 전체 설치 프로그램 소스를 포함합니다.

## 처음 사용할 때

이 README와 `Build.proj`가 있는 `Clorium_Source_Final_20261005` 폴더를 프로젝트 루트로 사용하세요. .NET SDK **10.0.301**을 설치한 뒤 루트에서 다음 명령을 실행합니다.

```powershell
dotnet msbuild Build.proj -m:1 -nodeReuse:false -p:BuildAllVariants=true
```

기본 `Clorium.sln`은 설치본 9프로젝트, `Clorium.AllVariants.sln`은 별도 AutoSizing 대안을 포함한 10프로젝트입니다. `Build.proj`는 Main/Devices의 COM/WIA 메타데이터 후처리도 수행합니다. 제품 프로젝트만 개별 빌드한 raw 결과로 배포하지 말고 후처리된 `build-output/installed`를 사용하세요.

- 설치본: `build-output/installed` — 9개 모듈과 심볼.
- 별도 AutoSizing 대안: `build-output/standalone` — 설치본 AutoSizing과 서로 다른 판본입니다.
- 제품은 기존 .NET Framework 4.8/4.7.2 대상을 유지합니다. SDK 10은 빌드 도구입니다.
- 참조 DLL/Framework 참조는 상대 경로로 포함되어 있으며 NuGet 원격 소스를 요구하지 않습니다.

## 반영한 수정

1. `projects/CameraControl/CameraControl/App.xaml`: `Startup="Application_Startup"` 연결. 프로세스는 남아 있지만 창이 나오지 않던 시작 문제를 수정했습니다.
2. `projects/Clorium.Centering/Clorium_Centering/App.cs`: UI 생성 전에 `Costura.AssemblyLoader.Attach(true)`를 호출. 폴더 선택창 라이브러리를 로드하지 못하던 문제를 수정했습니다.
3. Centering의 `MainWindow.xaml.cs`: 폴더 선택 취소 시 목록 유지, 새 목록 연결 순서 수정, 폴더/미리보기 오류 표시 및 `%LOCALAPPDATA%\Clorium\Logs\Centering.log` 기록.
4. Centering 프로젝트: Windows API Code Pack 기본 DLL 참조 추가.
5. `deployment/`: 20261005 전체 설치 프로그램, 설정 전달 실행기, 설치 검증/백업/복원 코드와 재빌드 스크립트 통합.

Clorium/AutoSizing 설정 전달은 설치 프로그램의 실행기를 통해 처리합니다. 라이선스 값을 제품 소스에 다시 하드코딩하지 않았습니다. 디버깅 시 설정을 준비하는 방법은 `README-settings.md`, 정상 바로가기 구성은 `deployment/README.md`를 읽으세요. 설정 없이 제품 EXE를 직접 실행한 결과가 설치 패키지의 실행 환경과 같지는 않습니다.

## 설치 프로그램 재빌드

`deployment/README.md`를 따르세요. 설치 경로는 기존 요청대로 고정합니다.

- 프로그램: `C:\Program Files (x86)\Clorium`
- 설정: `C:\ProgramData\Clorium`
- 데이터: `D:\Clorium Data`

전체 설치를 만들려면 이미 제공한 세 실행환경 ZIP 및 Microsoft VC++ 배포 파일을 별도 입력으로 준비해야 합니다. 이 소스 묶음은 촬영 데이터·실제 라이선스·개인 API 설정·원본 실행환경 ZIP을 포함하지 않습니다. 외부/native SDK의 제공되지 않은 소스를 새로 복원한 것은 아닙니다. 기존 managed 참조와 리소스 및 제삼자 고지는 모두 유지했습니다.

## 이번 검증

- 최초 소스 1,890개 파일을 원래 manifest로 검증한 뒤 수정했습니다.
- Linux / .NET SDK 10.0.301에서 설치본 9프로젝트와 별도 대안까지 전체 빌드 및 COM/WIA 후처리가 성공했습니다.
- 재빌드된 Main의 시작 연결과 Centering의 초기화 순서·내장 리소스를 검사했습니다.
- 고객 확인: 이전 전체 설치 성공, Clorium/AutoSizing 정상 실행, Centering 수정 적용 후 정상 작동.
- 최종 전체 재빌드 결과를 Windows에서 새로 실행하거나 장치 연결을 다시 시험한 것은 아닙니다. 설치 빌드 스크립트로 실제 실행 구성요소를 사용한 전체 패키지 제작을 완료했습니다. 추가 검증은 `evidence/Win11_20261005_v2`를 참조하세요.

이번 자료는 `evidence/Final_20261005`에 있습니다. 다른 `evidence/final-*`와 `README-original-20261002.md`는 최초 복구 시점의 기록입니다. 이번 수정 전 보고서와 현재 결과를 구분하세요.

`FILE_MANIFEST.json`은 최종 ZIP의 파일 목록·길이·SHA-256입니다(manifest 자신은 목록에서 제외). `bin`, `obj`, `build-output` 등의 빌드 산출물은 ZIP에서 제외합니다. `.local`, 실제 비밀번호·키 및 생성된 `deployment/seed.zip`을 소스 저장소에 넣지 마세요.

2026-10-05 추가 보완: 설치 제작 시 `CloriumLauncher.exe.config`가 함께 복사되도록 수정했습니다. Windows 11 신규 설치용 패키지 2026.10.05.2의 구성 파일 400개를 검증했습니다.
