# Centering 유지보수

.NET Framework 4.8 / x64. `projects/Clorium.Centering/centering.csproj`의 기본 빌드는 실제 XAML을 다시 컴파일합니다. `MainWindow.xaml.cs`, `ProgressRingControl.xaml.cs`에는 화면을 사용하는 업무 코드가 한 벌만 있습니다. `*.PreservedUi.g.cs`는 원본 화면 리소스 보존 모드에서만 컴파일합니다.

기본 화면 수정 빌드: `dotnet build projects/Clorium.Centering/centering.csproj -c Release -m:1 -nodeReuse:false`
원본 화면 보존 빌드: 위 명령에 `-p:BuildEditableXaml=false`를 추가합니다. 결과는 각각 `bin/Release/Editable/net48/`, `bin/Release/Preserved/net48/`입니다. 원본 모드는 화면 XAML 수정 내용을 반영하지 않습니다.

의미가 확인된 private 매개변수·지역변수 61개(식별자 토큰 248곳)의 이름을 바꿨습니다. 명칭 대응은 `evidence/naming-map.*`에 있습니다. 공개 함수·공개 매개변수, UI 필드·컨트롤 이름, P/Invoke, 문자열은 유지했습니다. 이미지 처리 주요 함수 6곳에 복구 설명 주석을 별도 추가했습니다. 원본 주석을 되찾았다고 주장하지 않습니다. 영역 좌표, 색상 채널, 연결 요소 큐, 경계 너비의 기존 계산과 예외 처리 순서를 임의 수정하지 않았습니다.

원본 화면 보존 모드의 공개 API 25개 및 원시 리소스 11개는 정적 대조에서 일치했습니다. 기본 편집 모드는 공개 API 누락 0개, WPF가 생성한 helper API 7개 추가, BAML 재컴파일로 화면 리소스 바이트 차이가 있습니다. 원래 3개 리소스 키·2개 생성 URI와 비BAML 아이콘 바이트는 유지됩니다. ProgressRingControl의 Opacity를 0.99로 합성 수정해 빌드에 반영됨을 확인하고 원상복구 후 원래 후보 해시도 확인했습니다. Windows Visual Studio 디자이너 열기·저장과 제품 실행은 확인하지 않았습니다.

실제 이미지·운영 설정·원본 EXE·키와 작업자 로컬 경로는 이 인계 폴더에 없습니다. 공급사 DLL은 제공된 참조를 사용하며, 공급사 소스를 복원했다고 주장하지 않습니다.
