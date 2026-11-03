# 검사와 이관 기준

기존 근거는 `evidence/licensing-policy-delivery.json` 및 그 파일이 연결한 로그이다. API 43, 달력 16, 복원 4, 장치 48+외부 셔터, WPF 회사 UI, 배포 manifest 29개가 과거 최종 후보에서 확인되었다. 운영 Linux·외부 HTTPS 인증·실고객 설치 검사는 미완료이다.

저장소 루트에서 실행한다. 별도 빌드 설정을 쓰며 실행 중인 이전 산출물을 덮어쓰지 않는다.

```powershell
./modules/Clorium.Licensing/Build.ps1 -Configuration Approval20261103
dotnet run --project modules/Clorium.Licensing/PolicyChecks/PolicyChecks.csproj -c Approval20261103 --no-build -- <새 임시 폴더>
./modules/Clorium.Licensing/Run-Tests.ps1 -Configuration Approval20261103 -Port 5128
```

2026-11-03 결과(새 정책·제품별 관리): 정책 21/21, 통합 101/101, 관리 앱 WPF 연결·제품 필터, 데이터·백업 ACL, 복원 7/7 통과. 시험 서버는 `--rate-limit 5000`으로 띄운다(운영 기본 분당 120회). 시나리오 8은 제품별 관리이다: 기본 제품 등록, 오토사이징 '라이선스 없음', 제품이 다른 시리얼 거부, 같은 PC의 클로리움·오토사이징 독립 인증, 제품별 버전 라인, 저장소 교차 사용 거부, 오토사이징 PC 변경 승인·임시 데모, 새 제품 등록·발급·인증, 제품 인증 중지/재개, CSV 제품 열, 복원 후 오토사이징 유지. 통합 시나리오는 1) 영구 최초 1회·100년 무접속, 2) 패치 무확인·4.7→4.8 자동 1회 확인·오프라인 시 차단 안내·5.x 거부 후 기존 권한 유지, 3) 복사된 인증/다른 PC 저장소 '라이선스 없음', 4) PC 변경 승인 대기·좌석 초과 승인 거부·기존 PC 회수·반납 후 다른 PC 승인·거절 사유·재설치 같은 PC, 5) 임시 승인 전 사용 불가·목적/기간 검증·1/3/12개월·만료 경계·시계 역행·기간 중 무접속·재승인·정식 전환·두 번째 PC 승인, 6) 오프라인 파일(영구·임시 승인 후 재처리·변조/이전 거부), 7) 계정 중지·동시 요청 초과 방지, 감사 기록·CSV·백업이다. 로그는 `evidence/licensing-integration.log`, `licensing-restore.log`, `licensing-admin-ui.log`.

전체 API 검사는 임시 서버·가짜 PC 지문·시험 고객을 쓰지만 evidence 파일을 갱신한다. 기존 WPF 창과 다른 포트를 쓰고 회사 실데이터로 실행하지 않는다. API/복원 검사를 다시 실행할 필요는 해당 코드/환경 변경에 맞춰 판단한다. 이번 이관은 문서/실행 진입점 변경이므로 이전 근거는 재사용했다. 2026-11-02에는 기존 `ReleasePolicy20261101` 빌드의 달력 정책 16개를 새 임시 데이터 폴더를 지정해 재실행하여 통과했으며 `handoff/licensing/policy-check-20261102.log`에 보존했다. 최초 인자 없는 실행은 검사용 데이터 폴더 인자가 필요해 실패했으며 위 사용 명령에 필수 인자를 반영했다. 제품 코드는 변경하지 않았다.

전체 CameraControl 고객 배포는 `Build-Distribution.ps1`에 기존 baseline/호환 후처리 전제조건이 있다. 공개 저장소에는 해당 로컬 비교본과 비밀이 없으므로 빌드 성공을 임의로 보장하지 않는다. 현재 ZIP은 루프백 구성으로 외부 배포 불가이다.

이관 완료 기준: 전용 PRD/인수인계가 커밋되어 원격 main의 SHA와 일치하고, Claude Code 전용 작업 폴더에서 이름 있는 세션이 실제 문서를 읽고 다음 작업을 확인한다. 웹 프로젝트 생성 여부와 혼동하지 않는다. 세션 응답은 `handoff/licensing/CLAUDE_RECEIPT.md`에서 확인한다.
