# AutoSizing installed 유지보수 후보

이 후보는 설치 payload 판본(AnyCPU + Prefer32Bit)만 포함한다. 원본 입력 SHA-256은 f47cfe92b4330dfaac102c70cc192462f8a16178e138456e92fcfdd9d84ea0ff이며 다른 판본의 프로그램·키·리소스를 혼합하지 않는다.

projects/Clorium.AutoSizing.Installed에 업무 코드와 네 화면의 .Designer.cs를 분리했다. 공개 함수/매개변수, 의미 있는 기존 함수명, UI Control.Name, 원시 리소스 이름과 JSON/통신 헤더 계약은 유지했다. 지역 선언 130개를 선언별로 정리했으며 이름 맵·CSV·보류 목록은 evidence에 있다. 목적·좌표/길이·필터·설정/API 계약 설명 38곳을 복구 후 추가한 주석으로 표시했다.

## 빌드

.NET SDK 10과 제공한 .NET Framework 4.7.2 참조를 사용한다. 실제 제품 실행에는 해당 Windows/장치 환경 확인이 필요하다. 프로젝트 상대 경로로 복원/빌드한다:

    dotnet restore projects/Clorium.AutoSizing.Installed/autosizing-installed.csproj --configfile projects/Clorium.AutoSizing.Installed/NuGet.Config -m:1 -nodeReuse:false
    dotnet build projects/Clorium.AutoSizing.Installed/autosizing-installed.csproj --no-restore -c Release -m:1 -nodeReuse:false

## 기존 값 외부 공급

소스에 있던 기본 라이선스 키와 API 키를 환경변수로 분리했다. 새 키로 교체한 것이 아니며 원래 배포 값과 일치하는 값을 프로그램 시작 전에 공급해야 한다. environment.example은 값 없는 이름 목록이며 앱이 이 파일을 자동으로 읽지는 않는다. 값을 채운 자료는 .local 등 로컬 위치에서만 보관하고 소스 인계 ZIP에 넣지 않는다. 원본 PE 기반 InitializeDeploymentSettings 준비 도구는 고객이 보유한 해당 판본의 기존 EXE를 입력으로 받아 필요한 필드 초기값을 로컬 설정 자료로 회수한다. 실제 값을 화면·로그에 출력하지 않고 소스 ZIP에는 포함하지 않는다. 최종 필드 대응은 루트 config/autosizing-installed-provisioning-map.json에 있다. 생성한 로컬 자료는 프로그램을 시작하는 프로세스의 환경변수로 공급해야 하며 environment.example 자체를 읽는 기능을 앱에 추가하지 않았다.

- CLORIUM_AUTOSIZING_INSTALLED_LICENSE_KEY: CloriumAutoSizing.Program::Encryption_key
- CLORIUM_AUTOSIZING_INSTALLED_VENDOR_API_KEY: CloriumAutoSizing.F_Measure+SendDataInfo::Vendor_aip_key

설치 판본의 기존 라이선스 키가 없으면 설정 필요 메시지를 표시하고 검사를 실패 처리한다.
API 키는 기존 선택적 API GET/POST 경로를 사용할 때만 누락을 안내한다. 공개 가변 Vendor_aip_key 필드에 외부 호출자가 값을 대입하는 방식은 그대로다. ApiHostName의 빈 기본값과 이미지 메타데이터 Comment의 호스트 선택은 변경하지 않았다.

## 검증 범위

로컬 SDK 빌드: 오류 0, 기존 경고 3. 원본과 공개 API 223개, 원시 리소스 24개의 바이트/해시와 검사한 어셈블리 식별값이 일치한다. 이름 정리에서 선언/참조 결속과 비식별자·미선택 식별자·주석/공백 보존을 확인했고, 기능 주석은 별도 단계다. 원본→비밀 분리 전 재빌드→분리 후 재빌드를 같은 ILSpy 11.1.0.9782 설정으로 디컴파일해 모든 차이 파일의 내용을 검토했다. 비밀 분리와 누락 처리는 의도한 본문 변경이므로 원본 동작 전체 동일성을 주장하지 않는다.

각 화면의 .Designer.cs ClientSize 폭 +1을 별도 합성 검사로 바꿔 재빌드/재디컴파일에서 반영을 확인한 뒤 원복했다. 이 근거는 C# 화면 속성 편집의 빌드 연결이다. 실제 Windows Visual Studio 디자이너 열기/저장, .resx 이미지·아이콘의 시각 편집, 화면 표시/장치/네트워크/고객 흐름은 미실행이다. 빌드는 원시 .resources를 보존하며 읽기용 .resx 편집을 자동 반영하는 별도 경로까지 검증한 것은 아니다.

원래 CSV 헤더/빈 행 처리, 캡처 파일명 배열 상한, 작은 양수 측정 길이의 기존 문자열 유지, GrayScale 미적용 등 확인한 유지보수 지점은 코드 주석과 변경 보고서에 설명했다. 이번 이름/주석/비밀 설정 단계에서 알고리즘을 몰래 수정하지 않았다. 제3자 코드/DLL 및 라이선스 재배포 조건은 별도의 인계 조건 확인 대상이다.

원본 비밀값을 보존한 baseline/snapshots/roundtrip과 원본 로그는 이 후보에 포함하지 않았다. 최종 ZIP은 이 후보를 포장한 뒤 다시 해제해 목록·해시·민감정보·상대 경로 빌드를 확인해야 한다.

최종 통합 감사에서 SendDataInfo.VendorID 초기값도 `CLORIUM_AUTOSIZING_INSTALLED_VENDOR_ID`로 분리했습니다. 기존 공개 필드·API는 유지하며 `config/autosizing-installed-provisioning-map.json`으로 고객 원본의 값을 로컬로 옮길 수 있습니다. 앞선 모듈별 영수증 이후 추가된 의도적 설정 변경은 `evidence/final-settings-changes.json`과 최종 전체 재검증을 기준으로 확인합니다.
