# CameraControl 유지보수 소스 인계

이 폴더의 `projects/CameraControl`과 `projects/CameraControl.Core`가 고객이 편집할 기본 소스다. 두 프로젝트는 .NET Framework 4.8을 대상으로 하며, Main은 Core를 프로젝트 참조한다. WPF XAML은 실제 Page 항목으로 컴파일된다. 설치본에서 복구한 화면의 BAML 키 이름은 원본과 같지만 새로 컴파일한 BAML 바이트와 실제 Windows 화면 동작은 별도 검증이 필요하다.

전체 폴더를 최종 루트에 복사하고 그 루트에 `framework-refs/net48/` 및 `dependencies/Managed/`를 함께 둔다. 프로젝트의 참조 경로는 모두 이 루트 기준 상대 경로다. `projects/CameraControl/RecoveredResources/`의 작은 `.resources` 파일은 Main의 기존 리소스 매니저 계약을 유지하기 위한 비밀값 없는 리소스다. 원본 `CameraControl.exe`나 `CameraControl.Core.dll`은 이 소스 폴더에 포함하지 않는다.

```sh
dotnet restore projects/CameraControl/CameraControl.csproj --configfile NuGet.Config
dotnet build projects/CameraControl/CameraControl.csproj --no-restore -m:1 -nodeReuse:false
dotnet restore tools/InteropMetadataRestore/InteropMetadataRestore.csproj --configfile NuGet.Config
dotnet build tools/InteropMetadataRestore/InteropMetadataRestore.csproj --no-restore -m:1 -nodeReuse:false
dotnet tools/InteropMetadataRestore/bin/Debug/net10.0/InteropMetadataRestore.dll restore metadata/wmplib-metadata-spec.json projects/CameraControl/bin/Debug/net48/CameraControl.exe projects/CameraControl/bin/Debug/net48/CameraControl.normalized.exe
dotnet tools/InteropMetadataRestore/bin/Debug/net10.0/InteropMetadataRestore.dll verify metadata/wmplib-metadata-spec.json projects/CameraControl/bin/Debug/net48/CameraControl.normalized.exe
```

Main의 WMPLib COM interop 메타데이터는 C# 컴파일러가 원본과 다르게 내보낸다. 마지막 두 명령은 **공개 WMPLib 메타데이터 명세 JSON만** 사용해 타입 플래그, 인터페이스, 메서드·매개변수 메타데이터를 보정한다. 원본 EXE는 필요하지 않다. 보정 도구는 비-WMPLib 메서드 본문이 작업 중 바뀌지 않았는지 검사한다. 배포용 후보를 만들 때는 보정된 결과를 사용하되, 이는 제품 실행 검증을 대신하지 않는다. `Mono.Cecil.dll`은 이 빌드 도구의 의존성이다.

환경변수 이름과 빈 설정 예시는 `config.template.env`에 있다. SMTP 계정 값이 없으면 지원 메일 전송 시 예외가 발생한다. 벤더 API 키·식별자가 없으면 해당 필드는 빈 문자열이며 외부 인증이 불가능하다. 프리셋 기본 비밀번호도 값이 없으면 빈 문자열이다. 이 템플릿 파일을 읽어 환경변수로 올리는 코드는 포함하지 않으므로 배포 환경이나 실행 스크립트에서 값을 제공해야 한다. 값은 소스·Git·로그에 기록하지 않는다.

`config/main-provisioning-map.json`과 `config/core-provisioning-map.json`에는 원본 모듈 SHA·타입·필드·환경변수 계약만 기록했습니다. Main 3필드와 Core 1필드는 같은 해시의 고객 원본에서 로컬로 옮길 수 있습니다. SMTP 두 설정은 전송 메서드 인자이므로 고객 배포 설정에서 별도 공급합니다. 상세 방법은 최종 루트 `README-settings.md`를 참조하세요. 원본 모듈과 실제 설정값은 소스 ZIP에 없습니다. API 실패 로그는 인증키·요청 URL·요청 데이터를 기록하지 않습니다.

`Recovery inference:`로 시작하는 주석은 디컴파일한 메서드 본문을 근거로 추가한 설명이다. 라이트, 턴테이블, 배경 제거, FTP 트리거, Android HTTP 경계 함수에 붙어 있다. 주석은 실제 장치·서비스 동작을 확인했다는 뜻이 아니다.

원본과의 정적 비교에서 Main은 WMPLib 정규화 후 공개 API 누락·추가가 0개, Core의 앞선 화면 합성 검사 단계에는 WPF 생성 helper 공개 API 7개 추가를 관찰했지만, 최종 깨끗한 통합 빌드에서는 Main·Core 모두 공개 API 누락·추가 0개였다. Main 리소스 키 93개와 Core BAML 키 6개는 원본과 같은 이름이며 생성 URI도 원본과 일치한다. 테스트는 빌드와 합성 XAML 편집·복원에 한정된다. Windows 디자이너, 실제 화면, 카메라·턴테이블, 서버 통신 및 고객 업무 흐름은 확인하지 않았다.

Main의 `App.Encryption_key`도 최종 통합 단계에서 `CLORIUM_MAIN_LICENSE_KEY`로 분리했습니다. 모듈별 초기 영수증은 해당 단계의 기록이며 최종 소스/결과물은 루트 `evidence/final-*` 증거를 우선 확인합니다. 배포용 빌드는 루트 `Build.proj`로 실행해 Main과 Devices 후처리 및 해당 PDB 수집을 함께 수행하세요.
