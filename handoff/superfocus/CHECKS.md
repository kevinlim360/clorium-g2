# 이관 시점 검사와 근거

날짜: 2026-11-01. 이 문서의 재실행과 과거 실기를 구분한다.

| 이번 재실행 | 결과 | 근거 |
|---|---|---|
| 실제 Core를 링크한 SuperfocusRegression | 19묶음 PASS | `superfocus-regression-20261101.log` |
| 라이선스 달력 정책 PolicyChecks | 16 PASS | `licensing-policy-20261101.log` |
| PolicyChecks 및 참조 프로젝트 빌드 | 오류0/경고0 | `licensing-policy-build-20261101.log` |
| 변경 파일 비밀 패턴 검사 | 개인키/GitHub토큰/AWS키/Anthropic키 패턴0 | 이관 작업에서 파일명/종류만 확인, 값 출력 없음 |

PolicyChecks는 최초 일반 병렬 MSBuild 호출이 진단 없는 실패로 종료되어, 저장소 Build.ps1과 같은 `-m:1 -nodeReuse:false -p:UseSharedCompilation=false`로 다시 빌드한 뒤 실제 생성 DLL을 실행했다. 실패를 정책 PASS로 기록하지 않았으며 최종 명령이 통과했다. 검사 상태 데이터는 Git에서 제외된 `.local/handoff-policy-20261101`에만 있다.

```powershell
dotnet run --project src/Clorium.G2/tests/SuperfocusRegression/SuperfocusRegression.csproj -c Release --no-restore
dotnet build modules/Clorium.Licensing/PolicyChecks/PolicyChecks.csproj -c ReleasePolicy20261101 --no-restore -m:1 -nodeReuse:false -p:UseSharedCompilation=false -v:q
dotnet modules/Clorium.Licensing/PolicyChecks/bin/ReleasePolicy20261101/net10.0/PolicyChecks.dll .local/새로운-격리-검사폴더
```

## 이전 검증에서 이어지는 근거

- `evidence/superfocus-manual-delivery.json`, `superfocus-manual-wpf.log`: 마지막 화살표 전환 후보의 빌드와 실제 WPF Dispatcher + fake adapter 검사. 이번에는 하드웨어를 연결해 다시 실행하지 않았다.
- 사용자는 직전 후보의 실제 숫자 입력과 박스 이동이 된다고 확인했다. 최신 화살표 수동 전환은 실렌즈 미검증이다.
- `evidence/superfocus-popup-delivery.json`, `superfocus-popup-files.json`: 이전 양방향 촬영/원본·PNG 디코딩 근거. 이후 UI/수동 전환까지 같은 후보로 합격 처리하지 않는다.
- `evidence/licensing-policy-delivery.json`: 같은 저장소의 별도 라이선스 작업에서 API43/달력16/복원4/진입48 등 기록. 이관 작업에서는 달력16만 재실행했다.

이번 작업은 버전 보존과 개발 이관이며 운영 설치 배포가 아니다. RF24–240, 최종 수동 전환 실기, 고객 상품 전체 광학 품질, 운영 HTTPS 라이선스 배포는 계속 미완료다. GitHub 푸시/Claude 프로젝트 등록 상태는 별도의 이관 결과 파일과 실제 원격을 확인한다. GitHub Actions workflow는 현재 저장소에 없어 원격 CI 통과를 주장하지 않는다.
