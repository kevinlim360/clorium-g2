# 클로리움 G2

개발 루트는 이 폴더이며 한국어로 작업한다. 제품 소스는 `src/Clorium.G2`, 보존 기준은 `baseline/Clorium_Source_4.7.1.5`이다.

## 먼저 읽을 문서

- 공통: `handoff/PRD.md`, `handoff/DEVELOPMENT_HANDOFF.md`, 최신 `work-status.md`. 예전 README의 첫 시안 상태만 보고 최신 수퍼포커스·라이선스 구현을 누락하지 않는다.
- 수퍼포커스 작업: `handoff/superfocus/PRD.md`, `DEVELOPMENT_HANDOFF.md`, `CHECKS.md`가 동작 규칙과 검증 상태의 기준이다.
- 라이선스 작업: 전용 세션 `projects/클로리움라이선스관리`(루트 `Claude_클로리움라이선스관리_개발시작.cmd`)에서 하며 `handoff/licensing/PRD.md`, `DEVELOPMENT_HANDOFF.md`, `CHECKS.md`, `START_HERE.txt`가 정책·AWS/DNS·배포 상태의 기준이다.

## 보존할 제품 규칙

- F/B/D 파일명(미감개선 ON/OFF와 관계없이 동일), 턴테이블 예외, 폴더/API/AutoSizing 호환성을 보존한다.
- Monsterdeck 신규 구현은 제외한다.
- 수퍼포커스 확정 동작은 바꾸지 않는다: 고=3작은단계, A0/B72=25/17/8컷, 직접입력 2..고최대(200 상한), 현재 가까운 끝점부터 양방향 촬영, 고스트 위 메뉴와 3줄 소형 팝업.

## 상태를 구분해 보고

- UI 시안과 제품 통합, 모의 검사와 실카메라 검사, 로컬 라이선스 후보와 운영 배포를 구분한다.
- 미검증 항목과 남은 제품 통합 과제(`preview-frame-presets` 실제 비율 자동 연결, 프리셋 영구저장 등)는 위 handoff 문서와 `work-status.md`의 최신 기록을 따른다.

## 금지·보호

- 설치본·사진·설정·baseline을 덮어쓰지 않는다.
- `.local`의 개인키/토큰/회사DB/백업은 출력·업로드·커밋하지 않는다. 기존 로컬 입력은 필요 범위에서만 참조한다.
- Git의 untracked 파일을 버리지 않는다.
- 사용자 지정 없이 실행 중인 앱/카메라/서버를 종료하거나 운영 배포하지 않는다.
- 개발본 배치는 `Build-SuperfocusDev.ps1`로 하며 호환 후처리와 개발 표식을 유지한다.

## 작업 기록

- 승인된 개발은 검증 근거와 남은 한계를 `work-status.md`에 기록한다.
