# 클로리움 G2 작업 기록

2026-11-03 배경제거 통합(사용자 요청: 기존 배경제거 제거, 클로리움 설정에서 켜면 왼쪽 세로 메뉴에 배경제거 아이콘·새 배경제거 사용, 다양한 호출 테스트; 제거 범위는 사용자 선택 '촬영 모드까지 전부'). 제거: PhotoScissors 실행(C:\Program Files (x86)\Clorium\PhotoScissors.exe)과 G2 종료 시 강제 종료, PhotoRoom 클라우드 창(AIRemoveBackgroundWnd·ViewModel, 호출처 없던 코드), '배경 제거 촬영' 모드(조명 ON/OFF 두 장 촬영 CaptureBackground, 기준값 처리 RemoveBackgroundWnd, 메인 F/B 배경 패널·1회/2회·자동, 설정 카드의 PNG/JPG/배경 기준 색), 프리셋 키 ShowRemoveBg·ModeBgPng·ModeBgJpeg·BGTargetColor. Core의 Settings.Background·ShowRemoveBg·ModeBg*·Branding.ControlRemoveBackground·WindowsCmdConsts는 기존 설정 파일/공개 API 호환용으로 남김(G2에서 더 이상 켜지지 않음). 추가: CameraControl/Classes/CloriumBackgroundRemoval.cs(Matting Studio http://127.0.0.1:8765 상태 확인, 꺼져 있으면 Start.ps1 -NoBrowser로 1회만 시작, 모델 로딩 중 느린 응답은 대기, 다른 프로그램의 포트 점유 안내, 선택 사진 또는 그 폴더를 /?mode=file|folder&select= 링크로 기본 브라우저에 엶), Settings UseBackgroundRemoval(기본 꺼짐)·BackgroundRemovalPath(비우면 <G2>\CloriumBackgroundRemoval\outputs\matting-studio\Start.ps1), 설정 '배경제거' 카드, 왼쪽 레일 '배경제거' 버튼. 배경제거 저장소(C:\Users\user\Documents\ChatGPT\클로리움 배경제거)에 링크 대상 미리 올리기(scan만, 시작은 작업자)와 Start.ps1 포트 충돌 보고 수정. 검사: G2 별도 설정 BgRemovalIntegration20261103 빌드 오류 0, SuperfocusRegression 통과, 	ests/BackgroundRemovalRegression 실제 G2 클라이언트↔실제 Start.ps1/app.py 41개(꺼짐·스크립트 없음·시작 실패·포트 충돌·냉시작·시작 중 재클릭·모델 로딩 중 클릭·반복 클릭·서버 종료 후 재시작), Run-UiBinding.ps1 32개(배경제거 15개 포함), Matting Studio 단위 24개, 내장 브라우저에서 파일/폴더/없는 경로/새로고침 링크와 실제 GPU 1장 처리(_bgr PNG 원본 크기 저장) 확인. 시험 중 발견·수정: RAW를 file 모드로 보내던 링크, 모델 로딩 중 '실행 안 됨' 오판, Start.ps1 출력 파이프를 서버가 물려받아 G2 쪽 대기가 끝나지 않던 문제. 미확인: 실제 G2 화면 클릭, 실제 의류 사진 품질, 개발 실행본·설치본 반영과 Matting Studio(venv·모델 포함) 배포 패키징, 깊은 설치 경로의 260자 제한(시험 중 긴 경로에서 PIL DLL 로드 실패 재현).

2026-11-03 오토사이징 통합(사용자 요청: 기존 오토사이징 제거, 클로리움 설정에서 켜면 왼쪽 세로 메뉴에 오토사이징 아이콘 표시·새 기능 사용, 다양한 호출 테스트). 제거: 하드코딩 `C:\Program Files (x86)\Clorium\Clorium AutoSizing.exe` 실행과 G2 종료 시 강제 종료, Branding.ControlAutoSizing 표시 조건(속성은 호환용 유지), 복구 소스 `projects/Clorium.AutoSizing.Installed`·`Standalone`과 provisioning map, Clorium.sln·AllVariants.sln·Build.proj·FinalSourceCheck 참조, 미감 회귀의 구 exe CSV/바코드/API 검사(대신 _M.jpeg Title/Subject/Comment 메타데이터 검사). 유지: 치수 촬영의 `치수측정\<이름>_M.jpeg`(PR 제외)·MetaScale/DiskSize/스케일 보정. 추가: `CameraControl/Classes/CloriumAutoSizing.cs`(Desk의 사용자별 pipe `CloriumAutoSizing.<USERNAME>`로 ping/show/open/browse, 미실행 시 경로 인수로 실행, 시작 중 재클릭은 준비 후 전달), Settings `UseAutoSizing`(기본 꺼짐)·`AutoSizingPath`(비우면 `<G2>\CloriumAutoSizing\Clorium AutoSizing.exe`), 클로리움 설정 '오토사이징' 카드(스위치, 실행 파일, 기존 스케일 행), 왼쪽 레일 '오토사이징' 버튼(켜면 표시, 선택 사진의 _M → 사진 → 치수측정 폴더 순으로 열기). 새 오토사이징 저장소(`C:\Users\user\Documents\ChatGPT\의류 사진으로 부위별 사이즈 자동 측정`, Desk 1.16)에 ControlPipe·단일 실행과 사용자 보고 "Bitmap region is already locked"(배경 제거 스냅샷 Clone 공유) 수정. 검사: G2 별도 설정 AutoSizingIntegration20261103 빌드 오류 0, SuperfocusRegression 통과, `tests/AutoSizingRegression` 실제 G2 클라이언트↔실제 Desk exe 40개(2회), Run-UiBinding.ps1 실제 XAML 바인딩 17개, Desk 기존 검사 전체+control 33+snapshot 8. 미확인: 실제 G2 앱 화면에서 클릭, 실카메라 치수 촬영→Desk 측정 흐름, 미감 회귀/설치 업데이트 검사의 전체 Build.proj 재실행(legacy 릴리스 입력 필요), 개발 실행본(.local/g2-runtime)·설치본 반영, Desk를 G2 배포물 `CloriumAutoSizing\` 폴더로 넣는 패키징.

2026-11-02 클로리움조명조절 통합(사용자 요청: 기존 조명조절 제거, 클로리움 설정에서 켜면 새 조명 기능 사용, 실행+촬영 연동, DMX만 제거). 제거: DMX/ArtNet 조명 컨트롤러(LightControlWnd·LightControlModel·LightSetting·Preset·PresetManager·ArtNetSender), DMX 색온도 설정 행. Arduino USB LED 디머와 절전 시간은 유지. 추가: `CameraControl/Classes/CloriumLightControl.cs`(조명 앱 0.7.9의 사용자별 named pipe `CloriumLightControl.<USERNAME>`로 ping/show/전체 전원), Settings `UseCloriumLight`(기본 꺼짐)·`CloriumLightPath`(비우면 `<G2>\CloriumLightControl\CloriumLightControl.exe`), 클로리움 설정 > 확장 기능 '조명 조절' 카드(켜기 스위치, 촬영 후 조명 끄기=기존 ModeLighting, 실행 파일). 켜면 왼쪽 조명 버튼(앱 열기/앞으로)과 메인 켜기/끄기 패널 표시. 시작 시 앱 실행 후 켜기, 촬영 전 켜기(절전 경과 또는 촬영 후 끄기 모드, 최대 8초 대기), 사진 수신 후 끄기, 절전·종료 시 끄기. 요청은 한 번에 하나, 최신 요청 우선, busy 재시도. 조명 앱 저장소(`C:\Users\user\Documents\Codex\클로리움조명조절`, 커밋 1b48939)에 명령 통로 추가. 검사: G2 별도 설정 LightIntegration20261102 빌드 오류 0, SuperfocusRegression 통과, 조명 앱 119개+스모크/데모, `src/Clorium.G2/tests/CloriumLightRegression`에서 실제 G2 클라이언트 소스↔조명 앱 데모(소스 실행과 PyInstaller exe 모두) 15개 통과. 미확인: 실물 Godox 조명으로 G2 연동, 실카메라 촬영 흐름, 개발 실행본(.local/g2-runtime)·설치본 반영, 조명 앱 exe를 G2 배포물에 넣는 패키징.

2026-11-02 관리형 라이선스 제품 통합 롤백(사용자 요청, 나중에 다시 추가): `src/Clorium.G2`에서 ManagedLicenseRuntime/CaptureAuthorization, 촬영 가드 28개 파일, 시작 분기, 라이선스 메뉴, Client 참조를 제거했다. 레거시 CheckLicense/CompareDate_Ex는 유지했고 modules/Clorium.Licensing과 .local 데이터는 보존했다. 별도 설정 LicenseRollback20261102로 CameraControl 빌드 시 오류 0개였고 산출물에 라이선스 DLL/형식이 없음을 확인했다. SuperfocusRegression은 통과했다. 개발 실행본(.local/g2-runtime)·설치본에는 반영하지 않았고 실카메라는 확인하지 않았다. 재통합 절차는 handoff/licensing/DEVELOPMENT_HANDOFF.md에 있다.

2026-11-01 라이선스 사용자 정책 변경 완료(로컬): 정식 최초1회 인증→동일PC 재접속 없이 영구 사용. 내부 직원 이름/ID·중지상태 등록, Demo/Test는3개월 온라인 재인증 필수(고정기간/오프라인 우회금지), 외부 고객 고정만료 데모는 유지. NextReauthAt 서버영속/한국시간AddMonths3, 조기refresh·activate·반납/이전으로 기한연장 방지. 기한후온라인 재인증으로 다음3개월, 미접속/중지계정은 촬영 차단. 영구권은시계수정으로재인증을요구하지않고기간제시간검사는유지. CSV직원정책/다음인증일과UI직원등록/필수정책/표시반영.

독립 계획PASS→후보검토FAIL3건(직원정책우회,CSV영구오표시,영구권시계복구의접속요구)수정→소스정책PASS. API43/달력정책16/복원4/실제WPF조회검색/최종바이너리48기기진입점+외부셔터PASS. 새배포후보 `delivery/Clorium-Licensing-20261101-104452` 및ZIP, `evidence/licensing-policy-delivery.json`에해시·근거. 시간경계는격리TimeProvider로검증했고실제3개월경과관찰/실카메라노출아님. 사용자설치본/baseline/사진·설정변경없음.

기존로컬회사앱/서버는실행중이아니었음을확인한뒤,같은회사데이터와서명키로새검증버전기동. 별도빌드출력 ReleasePolicy20261101 및 `.local/active-build.txt`로기존바로가기연결,실행중앱덮어쓰기없음. 실제직원계정/라이선스는이름정보가없으므로가상으로회사데이터에추가하지않음(시험직원은격리서버에만생성). 기존30일토큰은새서버/모듈에서한번온라인새로고침해야신정책전환. 운영주소 license.clorium.com(가비아등록/카페24홈페이지)선택,클라우드아직없음. 고객배포실행파일은로컬검증용루프백이므로외부운영배포미완료유지.

2026-10-28 라이선스 관리 신규 작업: 사용자 요청(Windows 영구권, 고객/시리얼/버전/PC, 데모·테스트, 복제 방지, 최소 관리)과 회사·클라우드 상시 운영 선택을 `modules/Clorium.Licensing/PLAN.md`에 연결. 별도 `license_review` 검토자의 계획 FAIL 보완 후 PASS를 받고 구현. .NET10 인증 서버/회사 WPF 관리 앱, .NET48 고객 SDK/인증 창, CameraControl 시작/메뉴/갱신/촬영 및 외부셔터 가드 통합. 새로운 필수모드 빌드는 공개키/서버 주소 내장, 레거시 EXIF/키 파일 방식 미사용. 기본 기존개발 빌드의 정식 발급 경로는 유지. 설치본/baseline/실행 중 기존개발본은 교체하지 않음.

독립 후보 검토에서 외부셔터 가드누락·다른시리얼 오타로 기존권한 차단·초점오류 오안내 3건 발견 후 수정. 최종 실제 .NET48/API33개, 전체백업복원4개, 실제 WPF 서버조회/검색/PC이력/보호백업, 서버/백업ACL, 최종고객바이너리48개 카메라진입점+외부셔터 차단, 기존19개 촬영차단, 수퍼포커스회귀 통과. 실호스트 WMI/레지스트리 PC지문 안정성 확인(원값 미기록). 설치스크립트의 격리대상 모듈복사/기존파일백업, 고객저장소ACL 격리시험, 비전용서버폴더 거부/ACL유지, 복원스크립트 통과. 카메라에 새 노출은 하지 않았음. 별도 검토자는 로컬후보 PASS(독립 재실행이 아닌 소스/실행근거 검토임).

현재 후보 `delivery/Clorium-Licensing-20261028-231526`, manifest29개 SHA256 일치 및 고객배포물의 서버비밀 제외 확인. 근거 `evidence/licensing-delivery.json`, licensing-integration/restore/binary/admin-ui/installer 등 로그. 회사 실제운영용 빈 데이터는 `modules/Clorium.Licensing/.local/company-data`에 제한ACL로 생성(배포물 제외), 키/고객정보를 출력하지 않음. `라이선스관리_실행.cmd`로 회사관리앱과 루프백서버 실행. 데이터/키 백업은 운영 이전 때 함께 복원해야 함. 서버 자동백업 시작/매일/최근30개, 수동백업도 제한ACL.

남은 실제운영 조건: 사용자에게 요청한 운영도메인·배포서버 정보 답변 대기. 실제 HTTPS/프록시/서비스운영/외부 고객PC 모듈적용과 ProgramData 권한·실카메라 회귀는 미검증. 현재 후보의 인증주소는 `http://127.0.0.1:5088`이므로 외부고객 배포본으로 사용하지 않음. 이 로컬결과를 클라우드/고객운영 완료로 표현하지 않는다. 후속은 운영서버에 현재 키/DB를 복원→공개주소설정→고객필수모드 재빌드→고객PC검증. 현재 사용 안내 `modules/Clorium.Licensing/README.md`.

2026-10-27 / 첫 UI 시안
등록 프로젝트: 클로리움G2 (702bcb49-85ad-4a86-8efd-5f9d890fca8a)
목표: 기존 4.7.1.5 촬영·저장 기능을 유지하고 화면·메뉴·폰트를 개선. 사용자가 시안을 먼저 요청했으므로 현재는 제품 적용 전 디자인 검토 단계.
원본: 오토사이징/outputs/Clorium_4.7.1.5_미감개선_전체소스.zip
기준 소스: baseline/Clorium_Source_4.7.1.5, ZIP과 2080개 파일 SHA256 일치.
시안: preview/CloriumG2.Preview.exe, Main.xaml, Theme.xaml. 실제 WPF 실행 및 화면 확인.
렌더: design/01~07 PNG. 1440x900 DIP 2배 / 1200x740 DIP 1.5배.
검사: 시안 상태 규칙 9개 PASS. 실제 UI 클릭으로 ON→OFF 시 주황 테두리 해제 및 _F/_B 표시 확인 후 ON 복원.
검토 후 수정: 메뉴 아이콘을 일관된 벡터로 교체, 긴 상품번호 말줄임과 전체값 툴팁.
제한: 시안 샘플 동작만 구현. 실제 촬영/저장/API/라이선스/AutoSizing/포커스 실패 처리는 연결하지 않음. 기존 설치·데이터·기준 소스 변경 없음. 다른 PC 글꼴 및 실제 다중 모니터 DPI 이동 검증 전.
다음: 사용자 시안 의견 반영 후 WPF 제품 Command/Binding/이벤트 보존 적용, 기존 회귀 검사 및 실장비 확인. 기존 버전 설치파일은 그대로 유지.
프로젝트 시안 작업본은 이 폴더가 기준이며 오토사이징/클로리움 G2는 제작 중 임시 복사본.

2026-10-27 후속: 시안 1 보존, 시안 2 컴팩트(76/252) 및 시안 3 상단메뉴/하단촬영(0/228) 제작. 세 시안 각각 그레이/블랙 선택 가능. 제공 AI 로고 적용, 색/비율 보존 및 밝은 로고 영역 사용. comparison 프로그램 신규 별도 폴더, 47개 UI 상태 검사 PASS. 어두운 테마 제목 대비 및 시안3 썸네일 잘림 수정. 실제 제품 기능 통합 전.
Windows 실제 창에서 시안 1→2 및 그레이→블랙 클릭 전환과 결과 화면 확인. 제공 AI 원본 SHA256 일치 확인.

2026-10-27 수정: preview-frame-presets / design-frame-presets 신규. 메인/확대/썸네일3종에 AspectFrame 적용, 원본 크기와 방향 계산 함수. 6조합 2크기 5방향/비율 5영역 = 300개 크기 검사 및 프리셋/기존시안 합계353 PASS. 프리셋 영역 선택/불러오기/현재조건저장과 예시조건 편집, 실행중 메모리 저장, 중복/빈이름/busy 방지. 기존 CameraPreset.Get/Set 확인했으며 제품통합 시 모든 항목/설정저장 보존 필요. 현 시안 원본픽셀/EXIF 실제연결은 미구현, 예시선택으로 검증. 실제제품 소스 수정없음.

2026-10-27 촬영기능 교체: 사용자 G2/EOS R/RF24–105 및 RF24–240 지정. 기존 고장 난 수퍼포커스/영상촬영을 src/Clorium.G2에서 비활성화(보존 baseline 제외). 연구 근거 2패키지 및 ZIP, 새 Clorium Stack 엔진+UI+C# 어댑터 추가. 빌드와 자동 검사 통과, 실제 카메라·새 촬영 UI 연결 미완료. 촬영기능_교체현황.md 참조.

2026-10-28 수퍼포커스: 기존 Canon 세션/라이브뷰를 이용하는 새 순차 촬영 서비스, SDK 오류를 확인하는 초점 이동, 원본 보존 전송, MainWindow 다운로드 소유권 분기, G2 파일 메뉴의 별도 촬영 창을 추가. 설치본은 변경하지 않고 .local/g2-runtime에서 정식 발급 라이선스 검사와 WPF/WIA 후처리를 유지한 개발본 실행. EOS R + RF24-105mm F4 L IS USM을 실제 연결해 2장/6장 JPEG 다운로드 및 6720×4480 PNG 합성, 촬영 중지 후 4장 부분 원본 보존·합성 미실행 확인. 모의 순차 촬영 검사 8묶음 통과. 가까운 도자기에 시작 AF를 맞춘 8장/Far/Large 최종 후보 시험 진행 중. 사용자 답변으로 오늘 실기는 24–105mm만 시험, 24–240mm는 미검증으로 유지. 세부 근거 evidence/superfocus-20261028.json 및 촬영기능_교체현황.md. 동영상 서비스 미구현, 설치 배포·전체 품질 합격 아님.

후속: 위 8장 시험은 3번째 다운로드 실패로 중단(정상 2장+불완전 1장 보존, 합성 없음). 이후 Device Busy가 남아 새 후보의 재시험 대기. 현재 후보에는 SDK 콜백 수명/실패 시 다운로드 취소/AF OFF/상세 로그를 보완했고 빌드·WPF/WIA/WMPLib 후처리 통과. SHA256은 evidence/superfocus-candidate-final.json. 기존 차단 19항목도 .NET Framework에서 새로 통과. 창 없이 남은 제가 실행한 개발본만 종료했고, 사용자에게 USB 물리 재연결 요청(답변 대기). 현재 개발본은 종료, 설치본 그대로. 마지막 후보의 실기 통과나 오류 원인 확정은 아직 주장하지 않는다.

USB 재연결 후: 현재 보완본 SHA256 4모듈 일치 확인. EOS R 초기화/라이브뷰 복구, 8장/Far/Large 촬영→다운로드→6720×4480 PNG 저장과 JPEG 8개 완전 디코딩 통과(`20261028_101742_a3141440`). 정상 종료도 확인. 다만 원본 간 초점 범위 변화가 뚜렷하지 않아 **수퍼포커스 전체 기능은 미완료**. 기존 G2 동작 감지 AF는 OFF, 본체 Continuous AF 간섭 가능성을 공식 Canon 원격 수동 초점 안내로 확인하고 사용자에게 해당 설정 확인을 요청했다. 현재 앱 종료, 사용자 AF 상태 답변 대기. 전송 실패 직접 원인은 미확정이고 1회 비재발만 확인했다. 동영상 작업은 아직 착수 전.

컨티뉴어스 AF: 사용자가 켜져 있던 본체 설정을 끈 뒤 동일 후보/동일 8장 조건 재시험. `20261028_103543_093cb227`에서 실제 근거리→원거리 초점 변화와 양쪽 디테일을 포함한 PNG 합성 확인. 8 JPEG+PNG 전부 6720×4480 완전 디코딩. **수퍼포커스 촬영·다운로드·초점 이동·합성 통합의 이 장면 실기 통과**. RF24–240mm 및 광범위 상품 품질은 미검증. 결과/원본/실패 자료 모두 보존. G2 새 창에 컨티뉴어스 AF OFF, 렌즈 AF 조건 안내만 추가해 별도 개발 실행본에 반영 중. 동영상 서비스는 별도 후속으로 미구현.

안내 반영본 빌드/후처리 완료(오류 0/기존 경고 4), 개발 실행본 .local/g2-runtime/CameraControl.exe. 해시 evidence/superfocus-candidate-delivery.json, 실제 기능 시험 후보 해시는 별도 유지. 기능 시험 뒤 변경은 준비 안내 문구뿐이다. 앱 정상 종료, 카메라 본체 컨티뉴어스 AF는 사용자 변경대로 OFF. 다음 작업은 별도 동영상 서비스 구현 및 테스트이며, 수퍼포커스의 RF24–240/상품별 품질 검증은 남겨 둔다.

2026-10-28 A/B 요청 완료: 라이브뷰 가까이/멀리 1·5·20단계 버튼, A/B 지정과 공유 범위 창, 간격별 자동 컷수, B→A 복귀 후 양 끝 포함 순차 촬영·원본 다운로드·합성 구현. Python 자동 경로/찾아보기·빈 경로 사전 안내 추가. EOS R RF24-105mm에서 40단계/간격4=11컷 및 재지정 40단계/간격2=21컷 촬영·6720×4480 PNG 저장 완료. 21 JPEG+PNG 완전 디코딩과 근/원거리 변화 확인. 자동 검사11묶음, 기존 차단19항목 PASS. 최종 빌드 오류0/기존 경고4, WPF/WIA/WMPLib 후처리 및 정상 종료·재시작 카메라 재연결 확인. 실기 후보/표시 수정본 해시와 결과 증거 evidence/superfocus-ab-*.json 참조. A/B는 절대 거리/광학 DOF 추정이 아닌 작은 SDK 명령 상대값, 컷수는 선택 간격 기준 예상값. 범위 밖 선명도·모든 상품 품질은 보장하지 않는다. RF24–240mm 미검증, 동영상 후속 유지. 기존 설치본과 baseline은 변경하지 않았다. 최종 개발 앱은 사용을 위해 라이브뷰를 열어 둔다.

2026-10-28 촬영 버튼 후속: 라이브뷰에 별도 파란색 '수퍼포커스 촬영 · 합성 시작' 버튼을 추가하고 기존 열기 버튼은 '촬영 설정'으로 변경. A/B와 200컷 한도 검증에 따른 CanCapture를 창/라이브뷰 공용으로 적용, 기존 RunAsync 경로로 바로 실행. 빌드 오류0/기존 경고4, WPF/WIA/WMPLib 후처리, 기존 차단19항목 통과. Canon 실제 라이브뷰에서 미지정 비활성→A0/B1 지정 시 2컷 표시 및 활성 확인. 이 버튼 변경으로 추가 노출은 하지 않았으며 이전21컷 엔진 검증과 구분. 앱 재시작으로 사용자 A/B는 재지정 필요. evidence/superfocus-start-button.json 참조.

2026-10-28 박스 A/B 후속: 포커스 박스 이동과 AF에 의한 무조건 범위 초기화 대신, 박스 모드 기본값으로 A 카메라AF/원점 기록→B 박스 선명도 정점 탐색(작은 SDK 명령 추적)을 구현. 실제 도자기A0→블라인드B74, 박스 이동 후 A/B 보존, 간격4=20컷 촬영·다운로드·6720×4480 합성 완료. 증거 evidence/superfocus-box-*.json, verify-box-output.py. 자동검사12묶음/기존차단19 PASS, 최종빌드0오류4기존경고. 툴바 RowSpan과 설정 창 Hide/앱 종료 CloseAll로 표시 잘림/같은 세션 A/B 보존도 보완. 최종 개발본 UI 확인 진행. 박스 대비 기반 상대 초점이며 절대거리/DOF가 아님; 일반상품 품질과24–240 미검증.

최종 확인: A 박스 양방향 대비 보정이 포함된 후보에서 흐린 도자기→선명한 도자기 A0 저장, 블라인드 박스로 이동 후 A 유지, B72 저장 및 파란 직접촬영 버튼 활성 확인. 설정 창 열기/닫기 뒤 A0/B72 그대로 보존됨. 자동검사13묶음/기존차단19 PASS. 최종 빌드0오류10기존경고, 해시 evidence/superfocus-box-delivery.json. 이 후보의 A/B 선택 실기와 직전 후보20컷 다운로드/합성 증거를 구분한다. 이전 실행본 정상 종료/재시작도 확인. 최종 개발 앱은 A0/B72(도자기/블라인드), 촘촘한 간격1=73컷 상태로 열어 둠. 다른 장면/피사체는 A/B 재지정 필요.

2026-10-28 품질 선택 완료: 라이브뷰/설정 창에 고·중·저 각 촬영 간격/예상 컷수 버튼 추가, 기본 중. 고=1단위 전체컷, 중=고컷수2/3 반올림, 저=1/3 반올림, 양 끝/최소2컷/200컷한도, 범위 전체 균등배치. A0/B72 기준73/49/24컷 자동검사. 실제 UI에서 시험A0/B5 고6/중4/저2, 선택간A/B 유지/두 창 선택동기화 확인. 자동검사15묶음/기존차단19 통과,0오류10기존경고/후처리 통과. 기존 실행횟수 정리 기능이 개발본→설치본 자동재실행을 일으켜, 개발 표식이 있을 때 공유세션·캐시정리와 하드코딩 재시작을 건너뛰게 분리하고 실제 .NETFramework guard PASS. 설치본 바이너리는 변경 없음. 신규 실물 노출 없이 UI와 주입카메라 계획 순서 검증, 증거 evidence/superfocus-quality-delivery.json. 최종 앱은 시험 범위 정리 후 박스모드ON/기본중으로 준비.

2026-10-28 이동형 팝업 후보: 라이브뷰의 대형 고정 툴바를 수퍼포커스 실행 버튼으로 교체하고, 선택 시 열리는 너비410 DIP 네이티브 이동 창에 화살표/A/B/초기화, 고중저 컷수, 촬영/중지/결과/설정을 압축했다. 설명·경로는 툴팁/접힌 고급 설정으로 이동. A→B 또는 B→A 순서 지정과 양방향 계획을 지원하며 현재 초점에 가까운 끝점부터 시작, 이미 끝점이면 복귀 이동과 첫 안정화 대기 생략. 이후 초점 안정화와 원본 다운로드 대기는 유지. 빌드0오류4기존경고, 자동검사17묶음과 기존차단19항목 PASS. 현재 데스크톱 잠금/화면보호로 네이티브 창 활성화가 두 번 실패해 컴퓨터 지침에 따라 조작 중지 및 사용자 화면 해제 요청. 실행 중 기존 개발본은 교체하지 않았으며, 신규 팝업 실화면/드래그/툴팁과 B선지정 실카메라 검증은 대기. 후보해시/상태 evidence/superfocus-popup-candidate.json. 구현/자동검사 완료와 실행본 반영/실기 완료를 구분한다.

화면 해제 후 후속 완료: 기존 개발본 정상 종료→새 후보 WPF/WIA/WMPLib 후처리→개발 실행본 반영·카메라 재연결 확인. 대형 고정 패널 제거/선택 시 작은 팝업 표시, 제목바 드래그, 흰 설명 툴팁, 닫기·다시열기 때 위치와 A/B 유지 실화면 확인. EOS R RF24-105에서 먼 B 선지정→박스 이동→가까운 A 후지정 성공(고124/중83/저41, 현재A에서 A→B). 화살표 일부 잘림은 폰트10/패딩1로만 보완해 다시 빌드·반영하고 최종 화면 확인. 최종 후보의 수동 A0/B5·저2컷으로 B→A 촬영/다운로드/PNG 합성 완료(20261028_171944_1ce6fdd9), 같은 범위에서 바로 A→B 재촬영/합성 완료(20261028_172028_28fd8e8f). 총4 JPEG+2PNG 완전 디코딩 6720×4480. 이 짧은 시험은 순서·전송·합성 검증이며 배경 인물 이동이 있어 전체광학품질 합격 근거로 쓰지 않는다. 불필요한 시작점 복귀/첫 안정화 대기 생략, 이후 안정화와 다운로드 대기는 유지. 자동17묶음/기존차단19 PASS, 빌드0오류4기존경고, 개발본 정리방지guard PASS. 최종해시/범위 evidence/superfocus-popup-delivery.json, 파일 evidence/superfocus-popup-files.json. 앱은 시험 A/B 정리·박스AF ON·기본중 복원 후 이동 팝업을 열어 둠. 설치본/baseline 변경없음.

2026-10-28 메뉴/3단계/직접컷수 완료: 수퍼포커스 실행 버튼을 라이브뷰 영상 오버레이에서 왼쪽 라이브화면 메뉴 아래·캘리브레이션 바로 위로 이동. 팝업 품질 라벨 추가, 고중저 버튼66 DIP로 압축, 박스AF→포커스박스 변경, 작은 컷수 입력칸 추가(비우면 품질 자동, 품질 선택 시 직접입력 해제). 고는 A에서3작은단계마다 촬영하고 나머지 끝점 B도 포함; 중/저는 새 고의 약2/3·1/3 반올림, 최소2·최대200. 예A0/B72는 고25/중17/저8(종전73/49/24). 직접입력2–200·범위단계초과 거부, 양끝 포함 균등배치, 현재 가까운 끝점 시작 유지. 빌드0오류10기존경고, 자동18묶음·기존차단19 PASS, WPF/WIA/WMPLib 및 유지정리guard PASS. 새 개발본 실행·카메라 라이브뷰 연결 및 왼쪽 메뉴/팝업 라벨·입력칸 잘림없음 확인. 이후 캡처 도구가 window crop is outside captured monitor를 반복해 실기 노출은 하지 않았으며 자동계획검사와 이전 양방향 실기 증거를 구분. 입력 시험 준비 중 포커스박스 체크박스 클릭 후 캡처 실패하여 현재 체크 해제일 수 있음. A/B 미등록. 해시/제한 evidence/superfocus-three-step-delivery.json. 설치본/baseline 그대로.

2026-10-28 팝업 추가 압축 완료: 고중저 버튼을32 DIP/글자만 표시하고 예상컷수·간격은 기존 동적 품질 툴팁으로 이동. 컷수 입력칸을 품질과 같은 둘째줄로 올려 별도 셋째줄 제거, A/B/방향 요약은 버튼/입력/촬영 툴팁으로 이동. A/B 색표시 유지, 비활성 버튼 툴팁 표시 추가, 초기 빈 상태줄 숨김. 조작부3줄(초점/품질·컷수·박스/촬영)로 축소. 빌드0오류4기존경고·후처리 PASS, 최종 개발 실행본 실제 팝업399×154 캡처에서 잘림없음 확인. 영상 바깥 좌측 실행메뉴 유지. 이후 부모창 캡처가 간헐적으로 outside captured monitor로 실패했으며 접근성으로 버튼명/툴팁 확인. 이번은 UI만 변경하여 새 노출·A/B시험·별도회귀검사는 하지 않았고, 기존18묶음 수치/촬영회귀 근거는 별도 유지. 카메라/스태킹로직 변경없음. 실행본을 열어두었으며 A/B 미지정. 증거 evidence/superfocus-compact-delivery.json.

2026-10-28 A/B 대기·숫자입력/고스트 위 메뉴 개선: 종전 A박스 탐색 동안 settings 전체 비활성으로 B도 막히는 코드 경로 확인. 이제 탐색 중 반대쪽 A/B 버튼은 활성이고 클릭 당시 ROI를 불변복사하여 예약, 현재 탐색 후 같은 순차카메라 경로로 자동 이어 실행. B…/A…와 상태로 예약 표시, 중지/연결끊김/실패 시 대기 제거. 실제 렌즈 동작이 동시에 완료되는 것은 아님. A/B 지정 후 고 기본선택·고컷수 자동표시, 중저 선택 즉시 숫자갱신, 수동입력2..고최대(기존200상한) 검증, programmatic TextChanged 가드로 직접입력 되돌림 방지, I빔커서/포커스/전체선택·숫자/붙여넣기 처리. 메뉴는 고스트 바로 위로 이동. 자동19묶음·기존차단19 PASS, 빌드0오류10기존경고·후처리·유지정리guard PASS. 최종 런타임의 실제 WPF창을 별도 숨긴 테스트에서 fake frame camera로 생성하여 고25/중17/저8, 모든2..25 수동값/오류/Refresh유지, A탐색중B활성/ROI예약불변/B선지정/중지취소 PASS(2묶음). 실화면은 고스트 위 메뉴/작은팝업/고선택 확인. 네이티브 좌표조작이 팝업 소유창을 잘못 잡아 실물 A→예약B 연속AF/추가노출은 미검증이며 숨긴WPF 검사와 구분한다. 해시/근거 evidence/superfocus-input-delivery.json 및 G2InputCheck.cs, superfocus-input-wpf.log. 개발앱 실행상태, 설치본/baseline 변경없음.

2026-10-28 사용자 재현 후 재수정: 숫자직접입력 실패 및 A탐색 중 박스이동 지연을 재보고. 실제 SetFocusPos(int,int)가 G2FocusCamera.IsOwned 때 이동요청을 버리는 경로 발견(이전 B버튼/예약검사만으로는 못 잡음). 이제 소유권검사 전에 소프트웨어 미리보기좌표를 저장하고 라이브뷰박스는 즉시 해당좌표로 표시. A측정ROI/하드웨어AF는 유지, B선택은 새좌표를불변복사하여직렬처리. 일반 대기박스는기존SDK경로. 컷수/품질은앵커탐색중에도활성, PreviewMouseDown처리가기본입력포커스를가로채지않게제거하고기본캐럿처리후선택. 숫자실사용실패의유일원인은아직확정하지않음. 제품테마하의기존ContentHost40x22이므로0크기스타일가설배제. ModeVid복구폼/의사코드비교 evidence/superfocus-modevid-comparison.md(원개발소스/실행검사아님). 최종빌드0오류10기존경고·후처리·자동19·기존차단19·정리guard PASS. 추가WPF검사에서실제LiveViewViewModel.SetFocusPos진입→탐색중즉시미리보기이동/하드웨어Focus0회/예약BROI보존/컷수편집/품질/범위검사 PASS. 별도실제키보드검사앱은Computer Use앱허용시간만료로실행못함. 제품UI좌표입력도geometry/outsidecapturedmonitor오류. 실제타이핑/실물박스이동성공으로과장하지않고사용자직접확인질문대기. 개발실행본새로열어둠. 해시 evidence/superfocus-preview-delivery.json. 설치본/baseline변경없음.

2026-10-28 수동 화살표 잠금 개선: 사용자가 직전 후보에서 숫자입력/박스이동 정상 확인. A탐색중 화살표잠금 추가요청에 따라 앵커탐색 및 수동이동 중6개화살표활성 유지. 처음 화살표를 누르면 현재자동탐색과예약반대지점취소→네이티브AF정지→현재초점을진행중A/B로저장→누른수동단계순차실행. 초기A네이티브AF로원점불확실시정지후현재원점재설정. 추가화살표는순서대로누적, 중지는대기제거/작업취소. 촬영/전송중잠금은보존. 조기수동전환시저장된A/B는자동탐색정점완료가아닌현재초점이며SDK명령진행중이면해당명령반환후전환. 실제staged WPF+가짜카메라/어댑터+Dispatcher비동기검사에서A탐색취소/AF정지선행/A0보존/+5후-1=현재4/6회이동/동시소유자1/이동중화살표활성/촬영잠금/중지해제PASS. 기존숫자/박스/예약검사도동일하네스통과. 빌드0오류4기존경고·후처리·기존차단19·정리guardPASS. evidence/superfocus-manual-delivery.json, G2ManualTakeoverCheck.cs, superfocus-manual-wpf.log. 새개발앱재실행, 현재카메라미연결UI로실기초점이동은미검증. 설치본/baseline변경없음.
최종 전달 확인: 독립 검토자는 최종 29파일 후보/설치·실행·전용폴더보호 변경분도 PASS. 격리 실행환경 밖에서 회사관리앱 창(Clorium · 라이선스 관리)과 로컬서버 프로세스가 실행 중임을 확인. 최종 ZIP 경로/해시/한계는 evidence/licensing-delivery.json에 연결. 실제 운영주소 질문은 여전히 답변 대기.

2026-10-30 G2 메인화면 A안(뷰파인더형) 1단계 적용: 사용자가 새 캔버스 시안 A/B 중 A안(밝은 배경)을 확정. src MainWindow.xaml만 재배치(코드비하인드 변경 없음, 코드 참조 x:Name 16개·이벤트 핸들러 전부 보존, 원본 백업 .local/claude-run/backup-ui-20261030). 상단바(상품이름/세션·오늘 건수/작업타이머·시작정지·알람·작업종료/세션명·경로/메뉴/카메라·배터리/설정), 왼쪽 도구 줄(라이브뷰·사진·치수·조명·색보정·배경제거·수퍼포커스·턴테이블+기존 추가버튼), 가운데 기존 레이아웃 호스트+'직전 촬영 이미지' 표시+노출값 바, 오른쪽(조명 카드, 프리셋 목록·등록/적용/보기/삭제·색온도/조리개, 앞뒷면·회전·하자·미감개선, 배경제거, 카메라 설정, 고급, 최근 세션, 촬영 버튼). 앱 테마가 Dark여도 메인창은 창 범위 밝은 브러시로 유지. 빌드 0오류·기존경고4, 격리 검사본 .local/g2-check-20261030-192017(후처리 verify PASS, CameraControl.exe SHA256 CEBFBB77…EF60)에서 수퍼포커스19·기존차단19·WPF3·유지정리·하자모의375·Stack9·라이선스 통합 PASS. 검사본 실행 화면에서 A안 배치·글자 가독성·메인메뉴 표시 확인(카메라 미연결, 촬영·설정변경 없음). 개발 실행본 g2-runtime·설치본·baseline 변경 없음. 미완: 프리셋 단일클릭 적용·이름변경·스트림덱 1–5, D 버튼, 세로/가로 토글, 설정의 도구/의류·하자·미감개선 on/off, 최근순 세션, 조명 상태, 다음 파일명, 세션 썸네일 전용 레이아웃, 실카메라 F/B/D·PR 파일명 실기. 근거 evidence/g2-ui-a-phase1.json.

## 2026-10-30 21:45 · G2 UI A안 2단계 — 시안 그대로 새로 그림 (개발본)

- 요청: 시안과 동일하게, 폰트·아이콘·버튼을 새로 일관되게, 썸네일 위 가로 바 축소, 메인 메뉴를 로고 클릭으로, 상단 줄 정리. 추가 지시: "새로운 UI + 기존 기능만 가져오고, 기존 UI는 가져오지 마라".
- 방식: 기존 XAML 조각을 옮기지 않고 화면을 새로 작성하고, 기존 이벤트·명령·바인딩·코드가 쓰는 이름만 연결.
- 변경 파일
  - `projects/CameraControl/styles/g2theme.xaml` (신규): 색 토큰, 내장 폰트, 선형 아이콘 60여 개, 버튼·리스트·콤보·스위치·슬라이더·스크롤바·메뉴·툴팁 스타일.
  - `projects/CameraControl/fonts/` (신규): Noto Sans KR 400/500/700(한글·라틴만 남긴 서브셋), JetBrains Mono 400/500/600, OFL 라이선스 2개.
  - `CameraControl/MainWindow.xaml`: 전면 재작성.
    - 상단 줄: 로고(메뉴) · 상품이름 · 이 세션/작업 시간(타이머는 팝업) · 세션 · 카메라/배터리 · 설정 · 패널.
    - 왼쪽 도구 8개 + 더보기 팝업, 오른쪽 패널(조명 · 프리셋 1–5 · F/B/D · 미감개선/세로·가로 · 카메라 설정 · 미리보기 · 폴더/자동내보내기 · 최근 세션 · 촬영).
  - `CameraControl/MainWindow.xaml.cs`: `G2LogoMenu_Click`, `G2ClosePluginPopup_Click` 추가(UI 전용).
  - `CameraControl/Layouts/LayoutNormal.xaml`: 재작성.
    - 직전 촬영 이미지 무대, 오버레이(배지 · 미감개선 ON · 배율 · 파일명/노출).
    - 36px 얇은 도구 바, 이미지만 있는 썸네일과 면 태그(PR_F/PR_B/F/B/D).
  - `CameraControl/Controls/Controler.xaml`: 카메라 설정을 라벨+값 행으로 재작성(이름·핸들러 유지).
  - `CameraControl/Classes/G2Converters.cs` (신규): 면 태그, 1부터 시작하는 번호. 표시 전용이며 파일명 규칙은 변경 없음.
  - `CameraControl.csproj`: g2theme Page, 폰트 Resource 6개.
- 백업: `.local/claude-run/backup-ui-20261030/phase2-before/`
- 빌드/회귀: `build-test-20261030-212702.log`
  - 빌드 오류 0, 기존 경고만.
  - 수퍼포커스, legacy guard 19, WPF checks, maintenance, defect harness, stack 9, licensing 33+4 모두 PASS.
- 점검본: `.local/g2-check-20261030-212702`(격리). 화면 확인을 마쳤습니다.
  - 로고 메뉴와 하위 메뉴, 더보기 팝업, 카메라 설정 펼침이 동작했습니다.
  - 폰트가 적용되었습니다.
- 남은 확인
  - 이 테스트 세션의 샘플 이미지가 가운데 무대에 표시되지 않음(1단계 화면에서도 동일). 실제 촬영본으로 확인 필요.
  - 무대 테두리에 얇은 어두운 선이 있음.
  - 실카메라 F/B/D/PR 촬영, AutoSizing/API, 운영 배포 전 회귀는 미실행.
- 커밋: 사용자 지시로 보류 중(미실행).

## 2026-10-30 22:58 · 메인 메뉴 재구성 · 썸네일 체크박스 (개발본)

- 로고 메뉴 순서: 파일 · 세션 · 화면 · 카메라 · 설정 · 종료
  - 파일 > 열기: 현재 세션 폴더를 윈도우 탐색기로 엽니다 (`G2OpenSessionFolder_Click`).
  - 파일 > 다른 이름으로 저장: 선택한 사진을 새 이름으로 복사합니다 (`G2SaveAs_Click`). 원본은 바꾸지 않고, 원본과 같은 경로는 거부합니다.
  - 설정 > 클로리움 설정 / 수퍼포커스 사용(ON/OFF) / 라이선스 · PC 이전 / (플러그인 · 도움말: Branding에서 켜져 있을 때만 표시)
  - 파일 메뉴의 수퍼포커스 항목은 삭제했습니다.
  - 새 설정값 `Settings.UseSuperfocus`(기본 ON, 저장됨)를 추가했습니다 (`CameraControl.Core/.../Settings.G2.cs`). 끄면 왼쪽 수퍼포커스 버튼이 숨겨집니다.
- 썸네일 오른쪽 위에 체크박스(`FileItem.IsChecked`)를 추가했습니다. 휴지통은 기존 `Del_Image`를 그대로 사용합니다. 체크한 사진을 모두 지우고, 체크가 없으면 선택한 사진을 지웁니다. 확인 창과 휴지통으로 보내는 동작은 기존과 같습니다.
- 썸네일 위 아이콘 간격: 보이는 간격을 약 12px에서 24px로 2배 늘렸습니다.
- 빌드/회귀: `build-test-20261030-224327.log` 전 항목 PASS.
- 점검본: `.local/g2-check-20261030-224327`
  - 메뉴 순서, 설정 하위 메뉴, 수퍼포커스 ON→OFF→ON(원상 복구), 열기, 체크박스 체크와 해제를 확인했습니다.
  - 삭제와 다른 이름으로 저장은 실제 파일 조작이라 실행하지 않았습니다.
  - 탐색기 창이 앱 뒤에 열리는 현상이 있습니다.

## 2026-10-30 23:27 · 색온도·밝기 ± 줄, 썸네일 촬영 정보 (개발본)

- 오른쪽 패널: "색온도 · 조리개" 접힘 영역을 없애고 프리셋 아래에 두 줄을 항상 표시합니다.
  - 색온도: −는 `btn_sliderLeft_Click`, +는 `btn_sliderRight_Click`.
  - 밝기: +는 `btn_ApertureLeft_Click`(FNumber.PrevValue), −는 `btn_ApertureRight_Click`. 실카메라에서 방향 확인이 필요합니다.
  - 코드가 쓰는 `slider`는 숨긴 채로 유지했습니다.
- "배경 제거로 전환" 글자 링크를 삭제했습니다. 배경 제거 모드는 클로리움 설정의 `ShowRemoveBg`로 전환합니다.
- 미감개선: 클로리움 설정 > 고객 맞춤 조건의 `UseAestheticImprovement` 체크박스(기존 위치)에 "미감개선 모드" 라벨을 붙였습니다.
  - 켜면 세로·가로 버튼이 절반으로 줄고, 옆에 미감개선 버튼이 나타납니다(UniformGrid). 끄면 세로·가로 버튼이 전체 폭을 씁니다.
- 썸네일에 마우스를 올리면 파일명, 조리개, 셔터, ISO, 초점거리, 노출보정이 표시됩니다(FileItem EXIF 값).
- 빌드/회귀: `build-test-20261030-231205.log` 전 항목 PASS. 점검본은 `.local/g2-check-20261030-231205`입니다.
- 참고: 테스트 세션의 샘플 이미지 2장에는 EXIF가 없어 값이 비어 보입니다. 앱이 실행 중일 때 다시 실행하면 "이미 실행 중입니다" 안내가 뜨며, 이는 기존 동작입니다.

## 2026-10-31 00:12 · 수퍼포커스 설정 위치, 가로/세로 아이콘, 카메라 설정 플로팅 창 (개발본)

- 수퍼포커스 ON/OFF를 로고 메뉴에서 빼고, 클로리움 설정 > Advanced의 체크박스(`UseSuperfocus`, 기본 ON)로 옮겼습니다. 체크하면 왼쪽 도구줄에 수퍼포커스 버튼이 나타납니다.
- 세로/가로 촬영 버튼
  - 아이콘을 회전하던 방식 대신 세로 사진 액자(`G2IcoPortrait`)와 가로 사진 액자(`G2IcoLandscape`) 두 가지 아이콘을 따로 그렸습니다. 아이콘은 글자 왼쪽에 둡니다.
  - 상태 표시는 스타일 DataTrigger(조상 Button의 `Content.Kind`)로 바꿨습니다. 이전 버전의 ControlTemplate 트리거는 TemplatedParent 바인딩이 잘못되어 가로 상태로 바뀌지 않았던 결함이 있었고, 이를 수정했습니다.
- 카메라 설정: 오른쪽 패널의 "카메라 설정" 줄을 누르면 가운데 화면 위에 떠 있는 창이 열립니다(`G2CamPanel`, `controler1` 포함).
  - 머리줄을 끌어 옮길 수 있고(`G2CamPanel_DragDelta`), ×로 닫습니다(`G2CloseCameraPanel_Click`).
- 빌드/회귀: `build-test-20261030-235253.log` 전 항목 PASS. 점검본은 `.local/g2-check-20261030-235253`입니다.
  - 가로↔세로 전환, 플로팅 창 열기·닫기, Advanced 체크박스 표시를 확인했습니다. 설정 창은 취소로 닫았습니다.

## 2026-10-31 00:58 · 이미지 색보정(G2) — Magick.NET 엔진 + 새 G2 창 (개발본)

- 요청: 고해상도 사진의 노출·명도·채도·채널별 색보정 기능. GitHub 오픈소스 중 신뢰성·안정성이 높은 것을 쓰고, 클로리움 G2 새 UI와 통일.
- 엔진 선택: **Magick.NET 14.12.0 (ImageMagick 7.1.2-19 Q8, Apache-2.0)**. 이미 제품 의존성(`dependencies/Managed`, 런타임 `Magick.Native-Q8-x64.dll`)이라 새 바이너리 없음. 제외: ImageSharp(Six Labors Split License), SkiaSharp(보정 연산 부족), OpenCvSharp(대형 네이티브 추가·ICC 별도). 고지: `src/Clorium.G2/licenses/Magick.NET-NOTICE.md`.
- 변경 파일
  - `CameraControl/Classes/G2ImageAdjust.cs` (신규): 설정·곡선·Magick 적용·저장. 노출(선형광 2^EV)·명도(중간톤 감마)·대비(S곡선, 중간회색 고정)·색온도/색조(휘도 정규화 이득)·전체/R/G/B 레벨(검정점·감마·흰점)을 실수로 계산한 256단계 컬러 LUT 1회(ImageMagick CLUT) + 채도(Rec.709 휘도 유지 색행렬). 미리보기와 원본 저장이 같은 함수.
  - `CameraControl/windows/G2ColorAdjustWnd.xaml(.cs)` (신규): g2theme 토큰/스타일 사용. 왼쪽 사진 무대(보정 후/원본 배지, 축소본·저장 해상도 배지), 오른쪽 패널(히스토그램+클리핑 %, 기본 보정, 색 균형, 채널별 레벨, 초기화·다른 이름으로·새 파일로 저장). 원본 비교: 토글/사진 누르고 있기/\ 키. 항목 이름 두 번 클릭=그 값만 초기화, 값 칸 직접 입력, Ctrl+S, Esc. 미리보기 긴 변 2000px, 백그라운드 렌더(마지막 값만 반영).
  - `MainWindow.xaml.cs`: 왼쪽 도구줄 '색보정'(`but_ColorCompensation_Click`)이 새 창을 연다. 사진 미선택·RAW 등은 안내 후 중단. 기존 `ColorCompensationWnd` 소스는 남겨 두었으나 호출하지 않음.
  - `CameraControl.csproj`: Page 1개 추가.
  - `tests/ColorAdjustRegression` (신규, net10 + 실제 Magick.NET), `evidence/G2ColorAdjustCheck.cs` (신규, 격리 런타임의 실제 WPF 창), `.local/claude-run/build-test.ps1`에 두 단계 추가.
- 저장 규칙: **원본은 절대 덮어쓰지 않음**. 기본 `이름_cc.확장자`, 충돌 시 `이름_cc (1).확장자`. 같은 폴더 임시파일(`.~cc-*`)에 쓴 뒤 이름 변경. EXIF 방향은 1회 적용 후 바로 세움, ICC·EXIF 유지, JPEG 품질은 원본 추정값(없으면 95). 같은 폴더 저장본은 세션 목록에 추가·선택(기존 색보정과 같은 `GetNewFileItem/Add/Select_Image`), 자동 내보내기 이벤트(`OnFileTransfered`)는 호출하지 않음. 기존 창은 원본을 삭제 후 다시 썼으므로 동작이 바뀐 점에 유의.
- 발견·수정한 결함: ImageMagick의 BGRA 내보내기가 알파 없는 이미지에 알파 채널을 켜서 이후 복제본 미리보기가 투명해짐 → BGR로 내보내 알파 255를 채우도록 변경, 회귀 검사 추가.
- 빌드/회귀: `.local/claude-run/build-test-20261031-005523.log` 전 항목 PASS(빌드 오류 0).
  - 엔진 12 PASS: 항등 LUT, CLUT=계산 곡선(전 픽셀 최대 오차 0), +1EV ×1.994/−1EV ×0.501, 곡선 단조·끝점, 채도 −100 무채색(편차 0)/+60 휘도 유지(평균 0.07), R 레벨이 G/B 비트 동일, 색온도/틴트, 새 파일·충돌 이름·크기/ICC/품질 유지·원본 바이트 동일·항등 PNG 비트 동일, 원본/기존파일 덮어쓰기·RAW 거부, 흑백→컬러 승격, JPEG→미리보기→보정 경로 불투명, 6720×4480 원본 저장 약 3.9초. EXIF 방향 검사는 테스트 파일에 방향 태그가 기록되지 않아 SKIP.
  - 실제 WPF 6 PASS: 6000×4000 JPEG 열기·2000px 미리보기, 슬라이더/직접 입력/R 채널 선택, 원본 비교, 저장(6000×4000, 원본 해시 동일), 초기화. 화면: `evidence/color-adjust-0[1-4]-*.png`.
  - 점검본: `.local/g2-check-20261031-005523` (CameraControl.exe SHA256 ACDDAFE4…0A6D). 개발 실행본 g2-runtime·설치본·baseline 변경 없음. 백업: `.local/claude-run/backup-colorfix-20261031/`.
- 남은 확인/한계
  - 실제 촬영 사진(EOS R)과 세션 목록 추가·썸네일 갱신은 점검본 앱에서 사람이 직접 확인 필요(자동 검사는 세션 추가를 끈 상태).
  - Q8 엔진이라 16비트 TIFF는 8비트 정밀도로 저장(창에 안내). RAW·CMYK 미지원. 100% 확대 미리보기 없음(저장은 원본 해상도).
  - 보정값은 실행 중에만 '이전 보정값'으로 기억(파일 저장 안 함). 프리셋 저장·일괄 적용은 다음 단계 후보.
  - 고객 배포 전 Magick.NET/ImageMagick 라이선스 원문 동봉 확인.
- 커밋: 하지 않음.

## 2026-10-31 23:45 · 큰 글자 보기 (메인 화면) · 설정 화면 시안 (개발본)

- 큰 글자 보기: 설정값 `Settings.UseLargeText`(기본 OFF, 저장됨)를 추가했습니다(`CameraControl.Core/.../Settings.G2.cs`).
  - 지금 쓰는 설정 창에서는 General 맨 위의 체크박스로 켜고 끕니다(`SettingsWnd.xaml`).
- 동작
  - `Classes/G2TextScale.cs`(신규)가 G2 화면 글자 크기 키(G2Fs9…G2Fs22)와 레이아웃 크기를 앱 전체 범위에 넣습니다. 레이아웃 크기는 왼쪽 도구줄 열·버튼, 오른쪽 패널 폭, 상단 높이, 더보기 타일입니다.
  - 켜면 글자가 ×1.45로 커집니다. 함께 넓어지는 영역: 오른쪽 패널 340→430, 도구줄 74→98, 상단 62→72.
  - 상품이름 글자와 바코드 표시 글자는 고정 크기로 두어 제외했습니다.
  - MainWindow 생성자에서 최초로 적용하고, 설정이 바뀌면 바로 반영합니다.
- XAML: MainWindow, LayoutNormal, Controler, g2theme의 FontSize 숫자를 `DynamicResource G2Fs…`로 바꿨습니다(변환 스크립트 사용). 화면 모양과 배치는 OFF 상태에서 이전과 같습니다.
- 다른 세션 작업과 겹침
  - 다른 Claude 세션이 같은 폴더에서 색보정(G2ColorAdjustWnd) 작업을 하고 GitHub(main 1e15112)에 푸시했습니다. 이번에 처음에는 이전 기준의 MainWindow.xaml.cs를 덮어써 색보정 연결이 빠졌습니다.
  - 푸시본과 비교해 바로 복구했습니다. 푸시본 위에 큰 글자 초기화 코드만 더한 버전으로 다시 저장했습니다.
  - 그 밖의 파일(MainWindow.xaml, LayoutNormal, Controler, g2theme, SettingsWnd, Settings.G2)은 푸시본과 같았고, 그 위에 이번 변경만 더했습니다.
- 빌드/회귀: `build-test-20261031-234139.log` 전 항목 PASS(색보정 엔진·WPF 검사 포함). 점검본은 `.local/g2-check-20261031-234139`입니다.
- 화면 확인 못 함: 다른 세션의 점검본(011953)이 실행 중이라 새 점검본을 띄우지 않았습니다.
- 설정 화면 시안(디자인 캔버스 "클로리움 설정 · G2"): 폭 35% 축소, 확장기능, 설정프리셋 A/B/C(큰 버튼=불러오기, 'A 저장하기'=저장·덮어쓰기, 하나만 선택), 몬스타덱을 장치·연결로 이동, 큰 글자 보기 스위치. 앱 반영 전입니다.

## 2026-11-01 00:25 · 큰 글자 보기 배율 1.35로 조정 (개발본)

- `G2TextScale.LargeFactor` 1.45 → 1.35. 함께 넓어지는 영역: 오른쪽 패널 340→410, 도구줄 74→94, 도구 버튼 60×56→80×66, 상단 62→70, 더보기 타일 76×64→98×76.
- 설정 창 문구: "큰 글자 보기 (화면 글자 약 1.35배 확대보기 · 상품이름 제외)". 설정 화면 시안의 설명은 "화면 글자 약 1.35배 확대보기"로, 시안 미리보기 배율도 1.35로 바꿨습니다.
- 빌드/회귀: `build-test-20261031-234914.log` 전 항목 PASS. 점검본은 `.local/g2-check-20261031-234914`입니다.
- 화면 확인: 설정에서 켜면 메인 화면 글자와 영역이 바로 커지고, 상품이름은 그대로입니다. 취소를 누르면 바로 원래 크기로 돌아옵니다. 확인 후 저장하지 않았습니다.

## 2026-11-01 01:25 · 클로리움 설정 창을 G2 디자인으로 교체 (개발본)

- `windows/SettingsWnd.xaml` 전면 재작성(생성 스크립트 사용). 시안 "클로리움 설정 · G2"를 따랐습니다.
  - 왼쪽 분류: 일반 · 촬영 · 확장기능 · 장치·연결 · 화면 표시 · 단축키 · 데이터·초기화 · 레거시 설정.
  - 위에 설정 검색, 아래에 설정 백업 · 기본값으로 · 상태 문구 · 취소 · 저장을 둡니다.
  - 기존 바인딩과 핸들러(Btn_*_Click, AndroidCameraDevice_Clicked, FTPserver_Clicked, lst_device 등)와 Branding 표시 조건을 그대로 유지했습니다.
  - 숨겨져 있던 미리보기·라이브뷰·외부장치 설정은 레거시 설정 분류에서 상태 표시(유지/재개발/삭제 검토)와 함께 보입니다.
- 확장기능: 기능별 카드에 스위치를 두고, 켜면 세부 설정이 펼쳐집니다. 포함 기능은 의류 F/B, 하자 D, 미감개선 PR, 배경 제거, 치수 측정, 수퍼포커스, 바코드, 외부 API, 작업 타이머·알람, 촬영 후 조명 끄기입니다.
  - 턴테이블 등 Branding으로 정하는 도구 표시는 사용자 설정이 아니라 제외했습니다.
- 몬스타덱(`UseMonstarDeck`)은 장치·연결 › 컨트롤러로 옮겼습니다.
- 설정프리셋 A/B/C: `windows/SettingsWnd.G2.cs`(신규)
  - 큰 버튼은 불러오기(선택 표시), 'A 저장하기'는 지금 값 저장(덮어쓰기)입니다.
  - 설정 73개를 `%DataFolder%\G2SettingsPresets.xml`에 저장합니다.
  - 클로리움 ID, 비밀번호, 테마·언어, 웹/FTP 서버(시작·정지 처리 필요)는 저장 대상에서 제외했습니다.
  - 불러온 값은 창의 저장을 눌러야 확정되고, 취소하면 되돌아갑니다.
- 검색: 각 줄과 카드의 이름·설명으로 찾고, 맞는 분류만 남깁니다. Branding으로 숨긴 항목은 건드리지 않습니다.
- 큰 글자 보기가 설정 창에도 적용됩니다(G2Fs 키, 20pt 추가).
- 결함 수정: 선택된 분류의 파란 글자색·굵기가 본문으로 번지던 문제를 본문 기본 글자색·굵기 지정으로 고쳤습니다.
- 백업: `.local/claude-run/backup-ui-20261030/settings-before/SettingsWnd.xaml`
- 빌드/회귀: `build-test-20261101-010750.log` 전 항목 PASS. 점검본은 `.local/g2-check-20261101-010750`입니다.
- 화면 확인: 일반·확장기능·레거시 화면, 검색, A 저장하기 → 값 변경 → A 불러오기로 원복을 확인했습니다. 끝낸 뒤 취소로 닫았습니다.
- 확인 중 데이터 폴더에 `G2SettingsPresets.xml`(설정 A)이 만들어졌습니다.
- 남은 일: 슬롯 상태 문구("바뀐 값 있음")는 프리셋 버튼을 누를 때만 갱신됩니다. 레거시 항목의 실제 삭제·재개발은 사용자 결정 뒤 진행합니다.


2026-11-01 수퍼포커스 Claude 이관: 현재 전체 개발 변경을 기존 GitHub origin/main에 보존하기 위해 점검. handoff/superfocus에 전용 PRD·인수인계·시작요청·검사기록을 추가하고 root CLAUDE/README 연결. 수퍼포커스19묶음 및 별도 라이선스 정책16 재실행 PASS(정책빌드 오류0/경고0). Claude 새 프로젝트 수퍼포커스개발 생성: https://claude.ai/project/01a1979e-d0c7-763f-a3a5-63d3889f83bd . 첨부/첫인수대화 및 푸시 최종상태는 실제 서비스 결과로 별도 확인. 기존 private local/baseline/research/delivery 제외를 유지한다. 최신 화살표 수동전환의 실렌즈 확인은 후속 과제로 보존.

2026-11-02 라이선스 관리 Claude Code 이관: 사용자 요청으로 handoff/licensing에 PRD·변경 이력·개발 인수인계·검사 명령·첫 요청·실제 인수 응답을 작성했다. 기존 저장소 origin/main의 소스 기준은 2a4b6b9이며 이번에는 제품 코드를 수정하지 않았다. projects/클로리움라이선스관리에 전용 CLAUDE.md를 두고 같은 저장소 소스를 참조한다. Claude Code 2.1.312 새 영구 세션 e21eb88f-704e-441d-b3ec-2a7c154e0206(이름 클로리움라이선스관리)이 문서와 실제 소스 확인 후 인수 응답을 반환했으며 전용 실행 CMD로 재개한다. 달력 정책 16개는 새 임시 데이터로 재실행 PASS. GitHub는 기존 공개 저장소·main 보호 없음·Actions workflow 없음으로 확인했고 .local/비밀/회사 데이터/로컬 배포물은 제외한다. 회사 키 보존·정식 최초 1회 영구권·직원 PC 고정 및 KST 3개월 인증을 유지한다. AWS/DNS/TLS/운영 인증 완료는 아직 별개 미완료이다. 이 기록을 포함한 최종 커밋을 원격 main에 푸시하고 SHA 일치를 확인하는 것이 이관 전달 경계다.
