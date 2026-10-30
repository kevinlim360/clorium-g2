# Clorium 유지보수 소스

제공받은 설치 파일의 관리형 제품 모듈 9개와 별도 AutoSizing 대안 판본 1개를 소스·프로젝트로 복구했습니다. 새 기능 업데이트는 포함하지 않았습니다. 고객 Windows의 실행 판단은 고객이 담당합니다. 잃어버린 개발 이력 전체나 모든 외부 SDK의 소스가 복원된 것은 아닙니다.

처음 검토·빌드·설정 준비·후속 수정은 [고객용 Codex 프롬프트](CUSTOMER_CODEX_PROMPTS.md)를 순서대로 복사해 사용할 수 있습니다.

## 빌드와 코드 위치

검증 환경은 macOS의 .NET SDK 10.0.301입니다. 프로젝트는 .NET Framework 4.8 또는 4.7.2를 대상으로 합니다. 필요한 Framework 참조·WinRT 빌드 계약·제공된 managed DLL은 상대 경로로 묶었습니다. NuGet 원격 소스를 사용하지 않습니다.

```powershell
dotnet msbuild Build.proj -m:1 -nodeReuse:false
# 별도 AutoSizing 대안도 함께 빌드하려면:
dotnet msbuild Build.proj -m:1 -nodeReuse:false -p:BuildAllVariants=true
```

`Clorium.sln`은 설치본 9프로젝트이고 `Clorium.AllVariants.sln`은 대안 판본까지 10프로젝트입니다. Windows Visual Studio에서 해당 솔루션을 열 수 있도록 `.sln`을 제공하지만 실제 Windows IDE 열기/빌드는 이번 작업에서 실행하지 않았습니다. Build.proj는 빌드한 Main·Devices의 COM/WinRT 메타데이터를 정적 보정하고 `build-output/installed`에 9개 제품 결과물을 모읍니다. 보정에는 원본 제품 EXE/DLL이 필요하지 않습니다. 빌드 도구 이외에 제품·설치 파일을 실행하지 않습니다. 별도 AutoSizing은 `build-output/standalone`에 따로 모읍니다. 두 판본은 배포 파일명이 같으므로 설치 폴더에서 서로 덮어쓰지 마세요.

| 프로젝트 | 역할 | 대상 |
| --- | --- | --- |
| CameraControl | 주 화면·촬영·자체 연동 | net48/x64 |
| CameraControl.Core | 설정·작업 흐름·공통 기능 | net48/x64 |
| CameraControl.Devices | 카메라·Bluetooth/WIA 연결 | net48/x64 |
| CameraControl.Plugins | 화면·이미지 변환·확장 기능 | net48/x64 |
| Canon.Eos.Framework | Canon managed wrapper | net48/x64 |
| PortableDeviceLib | Windows 휴대기기 wrapper | net48/x64 |
| ZoomAndPan | 화면 확대/이동·색상 선택 | net48/x64 |
| Clorium.AutoSizing.Installed | 설치본 크기 측정 도구 | net472/AnyCPU+Prefer32Bit |
| Clorium.Centering | 물체 영역·중심 계산 | net48/x64 |
| Clorium.AutoSizing.Standalone | 별도 대안 크기 측정 도구 | net472/x64 |

원래 의미가 있는 함수·타입·공개 매개변수 이름은 유지했습니다. 모호한 private 선언 325개를 근거에 따라 정리하고 대응·보류 이유를 `evidence/AutoSizing.*`, `evidence/Centering`에 남겼습니다. 확인된 공개 원문의 주석·이름을 활용하고 자체 계산·장치/통신 경계에는 복구 설명 주석을 추가했습니다. `Recovery inference:` 등의 설명은 원래 개발자 의도를 되찾았다는 주장이 아닙니다.

## 화면과 디버깅

Main·Core·Plugins의 XAML은 실제 빌드 입력입니다. AutoSizing 두 판본은 각각 4개 폼의 `.Designer.cs`를 분리했습니다. 속성 합성 수정→빌드→재디컴파일 또는 BAML 대조→복원 검증은 수행했으며 실제 Windows 디자이너/.resx 시각 편집은 미확인입니다.

Centering·ZoomAndPan의 기본 모드는 XAML을 다시 컴파일합니다. 두 모듈만 원본 화면 리소스 보존 모드로 빌드하려면 해당 프로젝트에 `-p:BuildEditableXaml=false`를 지정합니다. 업무 C#은 공유하지만 보존 모드에서는 XAML 수정이 반영되지 않습니다. 편집 모드의 BAML 바이트는 원본과 달라질 수 있으며, 키·연결 URI와 실제 화면은 별도 검사 항목입니다. 상세 조건은 프로젝트 `RECOVERY.md`와 최종 비교 보고서를 참조하세요.

Main·Devices 보정 결과에는 해당 재빌드의 PDB도 유지합니다. 기본 고객 빌드는 고객 PC의 소스 경로를 PDB에 남겨 디버거에서 찾을 수 있게 합니다. 이동 가능한 검증용 PDB가 필요한 경우에만 `-p:PortableSymbols=true`로 경로를 `/CloriumSource`로 매핑합니다. 이 옵션을 쓰면 디버거에 소스 위치를 별도로 연결할 수 있습니다. 최종 소스 ZIP에는 bin/obj/build-output/PDB 빌드 결과가 없습니다.

## 설정과 기존 실행 환경

하드코딩된 키·계정·프리셋 기본 비밀번호는 값 없는 환경변수로 분리했습니다. [설정 안내](README-settings.md)와 `config/*provisioning-map.json`을 사용하면 고객이 자기 원본의 기존 직접 초기값을 로컬 설정으로 옮길 수 있습니다. 실제 원본 EXE와 `.local` 설정은 ZIP에 없습니다. 원래 라이선스·인증 판정을 삭제하거나 우회하지 않았습니다. 필요한 값이 없으면 관련 기능이 실패할 수 있으며 SMTP 값은 고객이 직접 제공합니다. 두 로그 경로는 키/JSON을 기록하지 않는 일반 실패 문구로 바꿨고 의도한 변경으로 기록합니다.

이 소스 빌드는 고객이 보유한 설치 환경의 vendor/native runtime을 대신하지 않습니다. Canon EDSDK, PhotoScissors, ffmpeg·enblend 등 원래 native 도구와 설치 리소스는 기존 정품 배포 환경에서 유지해야 합니다. 해당 바이너리의 소스를 복원하거나 새 재배포 권리를 보증하지 않습니다. managed 의존성은 제공된 DLL을 사용하고 각 출처·조건·미확인 상태는 `licenses/THIRD-PARTY-NOTICES.md`와 `evidence/dependency-inventory.json`에 표시합니다.

## 검증 범위

최종 전체 빌드·입력/출력 해시·공개 API·원시 리소스·화면 키·진단 호출 결과는 `evidence/final-build.json`과 `evidence/final-assembly-comparison.json`에 있습니다. 복원 모듈의 재빌드 후 디컴파일 차이를 전부 검토하고, 실제 설정 변경·생성 코드 차이·남은 불확실성을 모듈별로 기록했습니다. PDB 원문 일치는 빌드 심볼 보존을 대신하지 않으므로 원본 Debug/Trace 호출도 따로 대조했습니다.

GoPro 관련 디컴파일 실패 3개는 빈 본문으로 두지 않았습니다. 공개 원문/원본 IL을 근거로 회수했지만 일부 local-cache 대 field-read 차이와 분기 정렬 한계는 남아 있습니다. 공개 API·리소스 일치와 차이 검토는 모든 함수의 실행 동등성 증명이 아닙니다. Windows 실행, 카메라·턴테이블·Bluetooth/WIA 장치, 라이선스·API·서버 연결, 설치/배포 및 고객 업무 흐름은 이번 작업에서 확인하지 않았습니다.
