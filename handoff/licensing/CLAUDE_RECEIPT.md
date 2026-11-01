# Claude Code 실제 인수 확인

2026-11-02 · 세션명: 클로리움라이선스관리 · Session ID: `e21eb88f-704e-441d-b3ec-2a7c154e0206`

다음은 새 Claude Code 세션이 문서와 실제 소스를 읽고 반환한 응답이다. 제품 파일 변경/운영 배포는 실행하지 않았다. 인수 시점 Git 기준은 2a4b6b9이며 아래 응답 뒤 Codex가 최종 문서를 커밋·푸시한다. CLI 2.1.312의 --name/--session-id로 생성했으며 웹 Claude 프로젝트를 생성한 것은 아니다.

## 인수 확인: 클로리움라이선스관리

**프로젝트명:** 클로리움라이선스관리 (Claude Code 작업 폴더 `projects/클로리움라이선스관리`, 소스 루트 `../..`)

**읽은 문서:** 프로젝트 `CLAUDE.md`, `handoff/licensing/`의 `START_HERE.txt`, `PRD.md`, `DEVELOPMENT_HANDOFF.md`, `CHECKS.md`

**실제 확인한 소스 (4개):**
- `modules/Clorium.Licensing/Server/Store.cs:85-91`: 영구권은 `Wire.Forever`로 발급됩니다. 직원 3개월권은 다음 인증일(`NextReauthAt`)이 지났을 때만 KST(+9) 기준 `AddMonths(3)`로 다시 계산하므로, 기한 전에 갱신해도 기간이 늘지 않습니다.
- `modules/Clorium.Licensing/Server/Store.cs:74-75`: 직원 계정이 중지되면 `ACCOUNT_DISABLED`로 인증을 거부합니다.
- `modules/Clorium.Licensing/Common/Protocol.cs:14, 104-105`: `Forever` 상수, 허용 주요 버전 검사, 영구권 판정 조건이 있습니다.
- `src/Clorium.G2/.../CameraControl/ManagedLicenseRuntime.cs`: 필수 관리 모드(`CLORIUM_MANAGED_LICENSE` 내장 설정), 1분 주기 확인, 촬영 검사 연결(`CaptureAuthorization.Validate`)을 확인했습니다.
- `src/Clorium.G2/.../Devices/Classes/CaptureAuthorization.cs`: 촬영 직전 권한 검사 진입점(`AssertAllowed`)이 있습니다.

**최신 인증 정책:**
- **정식 영구권:** 같은 PC에서 최초 1회 인증하면 이후 인터넷 없이 영구 사용합니다. 인증한 PC와 허용 주요 버전에서만 작동합니다.
- **내부 직원 테스트·데모:** PC에 고정되며, 한국시간 달력 기준 3개월마다 온라인으로 다시 인증해야 합니다. 오프라인 응답 파일로는 발급·갱신할 수 없습니다.
- **외부 고객 고정 만료 데모·테스트:** 지정 만료일을 따르고, 일일 확인과 최대 30일 유예를 유지합니다.
- 과거의 하루 1회 확인 정책으로 되돌리지 않습니다.

**아직 완료되지 않은 운영 배포:**
- Lightsail 서버(`54.153.232.149`)에 Linux 앱 배포, 기존 키·데이터 복원, TLS, 외부 `/health`, 실제 인증 검증이 모두 남아 있습니다.
- 현재 고객 배포 후보 ZIP은 루프백 주소로 구성되어 있어 외부 배포가 불가합니다. 운영 HTTPS 주소와 기존 공개키를 넣어 다시 빌드하고 검증해야 합니다.

**DNS: 이전 검사 결과와 현재 사실 구분**
- **이전 검사 결과 (문서 기록):**
  - `clorium.com`의 권한 네임서버는 카페24였습니다.
  - `license.clorium.com`은 홈페이지 CNAME을 거쳐 `222.122.198.189`로 연결되어 있었습니다.
  - `.co.kr`의 네임서버는 hostcocoa였고, `license.clorium.co.kr`은 NXDOMAIN(존재하지 않는 주소)이었습니다.
- **현재 사실:** 이번 세션은 읽기 전용이라 DNS를 다시 조회하지 않았습니다. 현재 상태는 확인되지 않았습니다.

**다음 첫 진단 작업:**
1. `license.clorium.com`의 NS·A·CNAME을 공개 DNS에서 다시 조회합니다.
2. `54.153.232.149`의 80/443 접속 상태를 확인합니다.
3. 결과에 따라 카페24 DNS에 `A / license / 54.153.232.149`만 설정하도록 안내합니다. 가비아 네임서버 전체 변경은 하지 않고, 기존 @/www/MX 설정은 보존합니다.

**지킨 범위:**
- `.local`, 비밀 값, 고객 데이터에는 접근하지 않았습니다.
- 제품 변경, 배포, 커밋, 다른 에이전트 생성은 하지 않았습니다.
- `CHECKS.md`가 세션 응답을 확인하는 곳으로 지정한 `handoff/licensing/CLAUDE_RECEIPT.md`는 파일 쓰기가 금지된 모드라 만들지 않았습니다. 필요하면 Codex가 이 보고 내용으로 기록하면 됩니다.

GitHub 푸시는 이 인수 확인을 받은 뒤 Codex가 수행합니다.