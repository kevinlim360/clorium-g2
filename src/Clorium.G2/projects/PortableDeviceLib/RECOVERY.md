# digiCamControl managed wrapper 복원 빌드 후보

Canonical 프로젝트는 projects/Canon.Eos.Framework, projects/PortableDeviceLib, projects/ZoomAndPan이다. 입력은 모두 배포본 2.1.6.0/net48/x64이며 원본 wrapper DLL 세 개는 인계 폴더에 넣지 않았다. 다른 공급 컴파일 의존성은 dependencies에 별도로 포함되어 있다. SDK 10.0.301에서 원시 리소스 보존 빌드가 각각 오류/경고 0이고 공개 API는 1184/748/425개로 원본과 일치한다. 세 모듈의 검사한 어셈블리 식별값과 원시 resource가 일치한다. 실제 Windows/native Canon SDK/COM/WPF 제품 실행은 하지 않았다.

최종 빌드 루트에는 framework-refs/net48/.NETFramework/v4.8 참조와 PortableDeviceLib용 dependencies/Managed/Interop.PortableDeviceApiLib.dll 및 Interop.PortableDeviceTypesLib.dll이 포함되어 있다. 각 wrapper 프로젝트는 ../../framework-refs 및 ../../dependencies/Managed 상대 경로로 참조한다. NuGet.Config는 package source를 비워 동봉한 오프라인 참조만 사용한다. 파일별 출처·해시·확인 상태는 ../../evidence/dependency-inventory.json에 있다.

    dotnet restore projects/Canon.Eos.Framework/Canon.Eos.Framework.csproj --configfile projects/Canon.Eos.Framework/NuGet.Config
    dotnet build projects/Canon.Eos.Framework/Canon.Eos.Framework.csproj --no-restore -c Release
    dotnet restore projects/PortableDeviceLib/PortableDeviceLib.csproj --configfile projects/PortableDeviceLib/NuGet.Config
    dotnet build projects/PortableDeviceLib/PortableDeviceLib.csproj --no-restore -c Release
    dotnet restore projects/ZoomAndPan/ZoomAndPan.csproj --configfile projects/ZoomAndPan/NuGet.Config
    dotnet build projects/ZoomAndPan/ZoomAndPan.csproj --no-restore -c Release
    dotnet build projects/ZoomAndPan/ZoomAndPan.csproj --no-restore -c Release -p:BuildEditableXaml=false

ZoomAndPan은 C# 업무 소스 한 벌을 공유한다. 최종 기본 BuildEditableXaml=true는 themes/generic.xaml을 새 BAML로 컴파일하므로 XAML 편집이 빌드에 반영되며 리소스 바이트가 달라진다. -p:BuildEditableXaml=false를 명시하면 ../../resources/ZoomAndPan/ZoomAndPan.g.resources를 보존하고 XAML은 참고용으로 남긴다. 두 모드의 재디컴파일 C# 84파일은 전부 같으며 새 리소스의 themes/generic.baml 키/타입을 보존했다. 첫 BorderThickness를 합성 변경해 빌드 리소스 hash가 달라지는 것을 확인했고, 소스·리소스/entry hash·어셈블리 바이트까지 원복됐다. 현재 테마는 public local 타입만 사용하므로 원본에 없던 SDK GeneratedInternalTypeHelper를 Compile Filename metadata로 제외한다. 앞으로 internal local 타입을 XAML에 추가한다면 이 제외 설정을 재검토해야 한다. 실제 Visual Studio 열기/저장/화면 렌더링은 미확인이다.

PDB 문서 72개 중 pinned upstream commit 5785e2b3ec29de153ad894e3b925ddeddb4ef76e의 원문 68개가 SHA256 일치했다. Canon EosCamera의 미일치 main/property partial과 충돌하는 exact partial 2개는 보류하여 66개(25/25/16)를 안전히 적용했다. 정확한 원문 bytes는 RecoveredUpstream에 있다. 나머지는 설치본 디컴파일 소스를 보존했다. Canon SDK의 배포본 추가 P/Invoke 3개는 원문 partial과 별도의 Edsdk.DeployedAddons.cs에 설치본 attributes/호출 규약/매개변수를 그대로 보존했다. 공개 이름·매개변수·문자열·함수본문을 임의로 정리하거나 빈 구현으로 바꾸지 않았다. 원래 배포본에 있는 Portable 진단 로그 2개와 Zoom Assert 5개의 조건부 호출도 DEBUG 심볼을 두 프로젝트에 공급해 보존했다. API/resource 검사와 별도로 전체 본문 대조에서 확인한 빌드 조건이다.

원문 적용 과정에서는 checksum 검증을 거친 원문이 덮는 top-level 타입만 제거하고 fallback 선언을 보존했다. 현재 checkout 파일 대신 pinned git show bytes를 검증해 사용했다. 최종 프로젝트에는 그 결과 소스와 적용 맵을 제공하며, 원본 입력 DLL과 사설 후처리 작업 디렉토리는 포함하지 않는다.

원본/재빌드 같은 ILSpy 11.1.0.9782 project/nested 설정으로 만든 차이 목록, API·identity·resource 검사, exact-source 맵과 합성 XAML 검사는 ../../evidence/Upstream-Wrappers에 있다. 빌드/API/resource만으로 함수본문 동일성을 확대 주장하지 않는다. 사설 baseline/원시 로그/전체 roundtrip 작업 디렉토리는 인계에 넣지 않았으며 차이 전체는 독립 리뷰 결과와 함께 판단한다.

라이선스는 ../../licenses/THIRD-PARTY-NOTICES.md, ../../licenses/upstream-wrappers의 전문과 원문 헤더를 모듈별로 확인한다. native EDSDK/COM 장치, 실제 Windows 동작, 최종 재배포 조건과 실제 최종 ZIP 해제본의 hash/경로/민감정보/통합 빌드 확인은 별도 단계다.
