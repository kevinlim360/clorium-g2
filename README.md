# 클로리움 G2

**라이선스 개발 이관(2026-11-02):** [전용 PRD](handoff/licensing/PRD.md), [개발 인수인계](handoff/licensing/DEVELOPMENT_HANDOFF.md), [검사 기록](handoff/licensing/CHECKS.md). Claude Code 전용 폴더는 `projects/클로리움라이선스관리`이며 `Claude_클로리움라이선스관리_개발시작.cmd`로 이어갑니다. 정식 최초 1회 영구권/직원 PC 고정·3개월 재인증 정책과 미완료 AWS 운영 배포를 보존합니다.

기존 Clorium 4.7.1.5 촬영·저장 동작을 보존하며 Windows WPF 화면, 메뉴, 글꼴을 개선하는 별도 프로젝트입니다.

현재 단계: G2 개발 소스, 수퍼포커스 순차 촬영·합성, 색보정·라이선스 및 UI 개발 후보를 보관합니다. 정식 설치 배포 완료를 의미하지 않습니다. 아래 초기 시안 설명은 이력이며 최신 상태는 개별 모듈과 evidence를 따릅니다.

**수퍼포커스 개발 이관(2026-11-01):** [전용 PRD](handoff/superfocus/PRD.md), [개발 인수인계](handoff/superfocus/DEVELOPMENT_HANDOFF.md), [검사 기록](handoff/superfocus/CHECKS.md). Claude 프로젝트명은 `수퍼포커스개발`입니다. 기존 전체 G2 PRD는 [handoff/PRD.md](handoff/PRD.md)에 보존합니다.

- `baseline/Clorium_Source_4.7.1.5`: 배포된 전체 소스 ZIP에서 가져온 수정하지 않은 기준 소스.
- `preview/CloriumG2.Preview.exe`: Windows에서 직접 열 수 있는 오프라인 WPF 시안. 같은 폴더의 XAML 파일이 필요합니다.
- `design/`: 동일한 WPF 화면을 2배 해상도로 렌더링한 시안과 검증 기록.
- 시안에는 샘플 상품과 벡터 의류 일러스트를 사용합니다. 실제 카메라, 라이선스, API, 사진 저장, 설정 파일에 접근하지 않습니다.

방향: 밝은 중성색, 넓은 이미지 영역, 정돈된 왼쪽 메뉴, 오른쪽 촬영 조작, 일관된 모서리와 여백. 중요한 촬영 조작은 문자로 표시합니다. 애플의 글꼴/아이콘/브랜드 자산을 복제하지 않습니다. 글꼴은 설치된 Segoe UI 및 NanumBarunGothic, 없으면 Malgun Gothic으로 표시합니다. 글꼴 파일은 배포하지 않았습니다.

보존 기준: F/B/D와 PR_F/PR_B 규칙, 하자·치수사진 PR 제외, 폴더명/경로, 다음 상품의 모드 유지, 시작 시 Custom 조건부 ON, 포커스 실패 후 상태, 촬영 중 변경 방지, 턴테이블 전용 이름, 기존 API·라이선스·AutoSizing·업데이트/롤백. Monsterdeck 기능을 새로 추가하지 않습니다.

다음 단계: 시안 의견 반영 후 실제 화면의 기존 Command/Binding/이벤트를 유지하며 적용하고 제품 회귀 검증을 수행합니다. 현재 시안의 버튼 동작 검사는 실제 촬영·파일 저장 통과 증거가 아닙니다.

## 촬영 기능 교체 개발 소스

2026-10-27부터 `src/Clorium.G2`를 개발 기준으로 추가했습니다. 기존 고장 난 수퍼포커스/동영상은 비활성화하고 새 순차 수퍼포커스와 `modules/Clorium.Stack`를 연결했습니다. EOS R 24–105의 특정 장면 촬영/전송/합성 실기 기록이 있으며, 최신 화살표 전환과 RF24–240 실기는 남아 있습니다. 상세 상태는 [촬영기능 교체현황](촬영기능_교체현황.md)을 참고하세요. `research/modevid-feature-modules`는 제3자 복구 참조라 로컬에만 보존하며 GitHub에서 제외합니다.

## 라이선스 관리

2026-11-01 정책 변경: 정식 영구권은 최초1회 인증 후 같은 PC에서 주기 재접속 없이 사용합니다. 내부 직원 계정을 등록해 데모/테스트를 발급하면 한국시간 달력3개월마다 온라인 재인증합니다. 새 후보 `delivery/Clorium-Licensing-20261101-104452`, 근거 `evidence/licensing-policy-delivery.json`. 기존 실행 바로가기는 검증된 `ReleasePolicy20261101` 버전으로 연결했습니다. 자세한 직원 등록과 기존 토큰 전환은 아래 사용 안내를 따릅니다. 외부 인증 서버 운영 배포는 아직 준비 전입니다.

2026-10-28: `modules/Clorium.Licensing`에 회사용 Windows 관리 앱·인증 서버·.NET Framework 고객 인증 모듈을 구현했습니다. `라이선스관리_실행.cmd`로 이 PC의 로컬 서버와 관리 앱을 함께 엽니다. 사용법은 [라이선스 관리 안내](modules/Clorium.Licensing/README.md)를 참고하세요.

CameraControl 소스에 최초 인증·라이선스 메뉴·자동 갱신·실행 중/촬영 전 검사를 연결했습니다. 영구/데모/테스트, 고객/시리얼/버전/PC, 반납/강제회수, 오프라인 파일, 변경 이력, CSV, 자동/수동 전체 백업을 지원합니다. 새 방식 필수 빌드는 공개 연결 설정을 실행파일에 내장하고 기존 날짜/키 파일 검사로 내려가지 않습니다. 기존 설치본과 실행 중 개발본은 교체하지 않았습니다.

로컬 배포 후보: `delivery/Clorium-Licensing-20261028-231526`. 인증 주소가 루프백이므로 외부 고객 운영 배포 전 실제 HTTPS 주소를 넣어 다시 빌드해야 합니다. 현재 검증 범위와 남은 고객 PC/카메라·운영 배포 검사는 `evidence/licensing-delivery.json`에 기록했습니다.
