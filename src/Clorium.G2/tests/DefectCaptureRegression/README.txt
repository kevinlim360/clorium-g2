하자 촬영 회귀 검사
1. .NET SDK 10에서 dotnet run --project DefectHarness.csproj
2. 결과: bin/Debug/net10.0/results.json
3. 제품 소스에서 메서드를 다시 추출하려면 Node.js로 node generate.mjs [소스루트/projects] 실행.
   전체 소스 ZIP의 tests/DefectCaptureRegression 위치에서 인수를 생략하면 ../../projects를 사용.
4. Program.cs는 추출한 실제 메서드 본문과 가짜 장치/WPF/조명/템플릿 의존성으로 구성.
   앞/뒤 핸들러의 async void 서명만 완료 대기가 가능한 async Task로 바꿈.
5. 파일 저장 검사는 합성 문자열을 기록하며 카메라 JPEG 인코딩 검사가 아님.
6. BinaryChecks.cs는 별도 .NET Framework 4.8 검사. 실제 배포 DLL과 WPF 바인딩을 실행.
   배포본 DLL과 관리 의존성, Windows WPF 환경 필요. 전체 CameraControl 앱 시작이나 실제 장치 촬영은 하지 않음.

2026-10-22: 제품 전환 검사 추가, 총 163개. baseline-session-results.json은 이전 수정본에서의 18개 실패 기록, results.json은 추가 수정본 결과입니다. 생성기는 실제 MainMenuViewModel의 세션 생성/가드/초기화 메서드도 추출합니다. WPF·장치·설정 저장 대역 범위는 그대로입니다. 전체 WPF 프로그램·장치 실기 시험은 별도입니다.
