# 클로리움라이선스관리 개발 인수인계

2026-11-02 · 기존 GitHub `kevinlim360/clorium-g2` · 기존 소스 보존 기준 커밋 `2a4b6b92fe9079367fe11f647b8b999d4a96625d`.

## 시작과 소스 위치

전용 Claude Code 작업 폴더는 `projects/클로리움라이선스관리`이며 저장소 루트는 두 단계 위이다. 실제 소스는 복제하지 않고 같은 저장소의 `modules/Clorium.Licensing` 및 `src/Clorium.G2`를 사용한다. 루트 `Claude_클로리움라이선스관리_개발시작.cmd`로 같은 이름의 세션을 이어간다. Claude 웹 프로젝트와 Claude Code 로컬 작업 폴더/세션은 서로 다른 기능이다. 이번 이관 대상은 Claude Code이다.

먼저 이 폴더의 PRD, 이 문서, CHECKS, START_HERE를 읽고 모듈 README 및 실제 소스와 대조한다. 루트의 과거 PRD에 있는 모든 라이선스 하루 1회 확인 정책을 다시 적용하지 않는다. G2 UI·수퍼포커스의 독립 개발 과제를 임의로 변경하지 않는다.

| 경로(저장소 기준) | 역할 |
|---|---|
| `modules/Clorium.Licensing/Common/Protocol.cs` | 서명 프로토콜·만료/갱신·Forever 값·PC/버전 검사 |
| `modules/Clorium.Licensing/Server/{Program,Store,Models,PrivateFiles,BackupWorker}.cs` | HTTP API·정책·직원 계정·달력 계산·단일 쓰기·접근권한·백업 |
| `modules/Clorium.Licensing/Client/{LicenseClient,LicenseWindow}.cs` | net48 인증 저장/검사·온라인/오프라인·PC 이전 UI |
| `modules/Clorium.Licensing/Admin/Program.cs` | 회사 WPF 관리 UI·직원 발급·CSV·백업 |
| `modules/Clorium.Licensing/PolicyChecks/Program.cs` | 실제 Store 기반 달력 정책 16개 검사 |
| `modules/Clorium.Licensing/Tests/{Program,BinaryChecks}.cs` | API·복원·실제 모듈/장치 가드 검사 |
| `src/Clorium.G2/projects/CameraControl/CameraControl/ManagedLicenseRuntime.cs` | 필수 관리 모드·주기 확인·촬영 경계 |
| `src/Clorium.G2/projects/CameraControl.Devices/CameraControl/Devices/Classes/CaptureAuthorization.cs` | 장치 촬영 권한 진입점 |
| `modules/Clorium.Licensing/*ps1` | 빌드·테스트·배포·회사 실행·설치·복원 |

경로는 `rg --files`로 실제 존재를 확인한다. 장치 통합은 SDK 구현들 및 외부 셔터에도 분산되어 있다. 다음 촬영 진입을 막되 진행 중 다운로드/저장/닫기는 중단하지 않는 경계를 유지한다.

## 로컬 실행과 비밀 데이터

현재 활성 빌드 설정은 `modules/Clorium.Licensing/.local/active-build.txt`에서 확인한다. 이전 후보는 `ReleasePolicy20261101`이었다. `라이선스관리_실행.cmd`가 회사 로컬 서버/관리 앱 실행 경로이다. 운영 서버가 배포되었다고 가정하지 않는다.

회사 데이터는 `modules/Clorium.Licensing/.local/company-data`에 있다. 서명 개인키·관리자 토큰·고객 DB/백업을 화면/대화/로그에 출력하거나 Git/Claude 컨텍스트로 올리지 않는다. 실제 인증 키를 새로 만들지 않는다. AWS 이전은 기존 전체 백업을 보호된 새 폴더로 복원하고 같은 서명키를 사용한다. 고객용 공개키와 짝이 바뀌면 기존 배포물을 사용할 수 없다.

`.local`, `baseline`, `research`, `delivery`, bin/obj는 Git 제외이다. CameraControl 전체 배포 빌드는 로컬 baseline의 호환 메타데이터와 기존 실행 환경이 필요해 새 Git clone만으로 완결되지 않는다. 서버/관리 앱/정책 검사와 전체 CameraControl 배포의 전제조건을 구분한다. 설치본·기존 개발 런타임·사진·설정·공유 서버/카메라 프로세스를 사용자 지시 없이 덮어쓰거나 종료하지 않는다.

## 다음 작업 순서

1. **진단:** 실제 DNS A/CNAME/NS와 AWS 80/443 접속 상태를 새로 확인한다. 홈페이지/메일의 기존 DNS를 보존한다. 카페24 DNS의 `license` A만 `54.153.232.149`로 설정한다. 사용자가 가비아 네임서버 변경 화면에 있으면 취소하도록 안내한다. 정답 네임서버를 추측해서 전체 변경하지 않는다.
2. **배포 준비:** SSH 접근 경로와 Ubuntu 버전/메모리/디스크를 확인하고 .NET 10 설치 경로를 검증한다. 512MB에서 프로세스·업데이트·부하를 측정한다. 메모리 부족 시 swap/1GB 플랜 전환을 구체적으로 검토하되 자동 요금 변경을 하지 않는다.
3. **보호된 이전:** 기존 키/관리자 설정/데이터를 출력하지 않고 전송·복원한다. 서버 단일 인스턴스, 전용 실행 계정/권한, 부팅 자동 시작, 재시작 정책, 로그 보관을 구성한다. Linux에서 기본 루프백 5088로 서비스한다. Windows 전용 회사 관리 앱은 회사 PC에 둔다.
4. **공개 HTTPS:** 역방향 프록시와 `license.clorium.com` TLS 인증서를 구성한다. HTTP 80은 인증서 발급/리디렉션 용도로 보존하고 HTTPS 443을 추가한다. 관리 접근은 필요한 범위로 제한하고 앱의 내부 포트를 외부에 열지 않는다. 프록시가 Forwarded 헤더를 덮어쓰고 앱의 신뢰 프록시 설정과 일치해야 한다.
5. **수용 검사:** 외부 HTTPS `/health`, 관리자 인증 실패/성공, 시험 시리얼 발급, PC 고정, 최초 영구 인증 후 네트워크 없이 실행, 다른 PC 토큰 거부, 직원 3개월 재인증·기한 경계·계정 중지·오프라인 거부를 검증한다. 날짜 변경은 시험 시계/격리 테스트로 검증하고 실제 업무 PC 시간을 바꾸지 않는다.
6. **고객 후보:** 회사 앱에서 실제 운영 HTTPS 주소/기존 공개키만 내보내 `Build-Distribution.ps1 -ClientConfig ...`로 새 후보를 만든다. 배포 manifest/비밀 제외/필수 모드/서명 검증/복원 후 인증을 확인한다. 실제 고객 설치는 기존 실행 중 앱과 파일 보존 조건을 지킨다.
7. **운영:** 같은 서버의 자동 백업 외에 별도 위치의 보호된 백업과 복원 검사를 마련한다. 모니터링/업데이트/인증서 갱신 실패 대응 절차를 정리한다. 실제 운영 수용 결과를 기록한다.

이번 요청은 개발 이관이며 이관 과정 자체에서 운영 배포나 고객 PC 변경을 실행하지 않는다. 이후 사용자의 개발/배포 요청 범위 안에서 필요한 작업을 이어간다. 실제 접근 권한이 없으면 완료로 보고하지 말고 필요한 접속 경로만 요청한다. 개인키·암호를 채팅에 붙여넣도록 요구하지 않는다.
