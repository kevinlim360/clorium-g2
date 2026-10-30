# 실행 설정을 소스 밖에서 유지하기

소스에는 실제 키·계정 값이 들어 있지 않습니다. AutoSizing의 기존 라이선스 키와 벤더 API 키는 원본 후보별 환경변수로 분리했습니다. 원래 라이선스 판정·암호화를 없애거나 새 키로 바꾸지 않았습니다. 필요한 값을 제공하지 않으면 관련 기능을 사용할 수 없으며, 독립 AutoSizing 판본의 두 라이선스 파일 중 하나를 인정하는 OR 조건은 유지했습니다.

고객이 이미 보유한 **정확한 원본 EXE**로 다음 도구를 실행하면 메타데이터에 포함된 기본 키를 로컬 설정으로 옮길 수 있습니다. 제품 EXE를 실행하지 않고 입력 전체 SHA-256과 매핑 필드를 확인합니다. 설치본과 별도 AutoSizing은 서로 다른 입력이며 바꿔 사용할 수 없습니다. 직접 문자열 필드 초기값 또는 명시적으로 매핑된 문자열 상수만 읽습니다. 다른 설정은 추정하지 않습니다.

```powershell
dotnet build tools/InitializeDeploymentSettings/InitializeDeploymentSettings.csproj --configfile NuGet.Config -m:1 -nodeReuse:false
# ORIGINAL_AUTOSIZING_EXE와 MAP_PATH를 해당 후보로 바꿉니다. OUTPUT은 새 파일이어야 합니다.
dotnet tools/InitializeDeploymentSettings/bin/Debug/net10.0/InitializeDeploymentSettings.dll MAP_PATH ORIGINAL_AUTOSIZING_EXE .local/settings.json
. ./config/LoadLocalSettings.ps1 -SettingsPath .local/settings.json
```

`.local`은 고객 PC에서만 관리합니다. Git·소스 ZIP·고객지원 로그에 넣지 마세요. 도구는 값을 콘솔에 출력하지 않고 기존 파일도 덮어쓰지 않습니다. macOS에서는 새 파일 권한을 소유자 읽기/쓰기(600)로 만들며 Windows에서는 고객 폴더 접근 권한을 사용합니다. 설정을 올린 같은 PowerShell에서 제품을 실행하면 해당 자식 프로세스에 환경변수가 전달됩니다. 이 스크립트 자체가 제품을 실행하지는 않습니다. `LoadLocalSettings.ps1`은 코드 정적 검토만 수행했으며 이 작업의 macOS 환경에서 PowerShell 실행은 확인하지 않았습니다.

Main의 암호 키·벤더 설정과 Core의 프리셋 기본 비밀번호는 `config/main-provisioning-map.json`, `config/core-provisioning-map.json`을 각각 해당 원본 모듈에 적용해 회수할 수 있습니다. 각 설정 JSON을 같은 PowerShell에서 차례로 올리면 함께 적용됩니다. Main의 SMTP 설정은 빈 템플릿의 환경변수에 고객 값으로 제공합니다. 필드 초기값 추출 매핑이 제공되지 않은 항목은 사용 중인 배포 설정과 담당자 기록에서 확인해야 합니다. 누락 시 동작은 각 프로젝트 인계 문서를 참조하세요. 값의 교체는 별도 운영 결정입니다.

현재 값 없는 매핑은 Main 3필드, Core 1필드, Plugins 2필드, 설치 AutoSizing 3필드, 별도 AutoSizing 4필드입니다. AutoSizing VendorID도 후보별 설정으로 분리했습니다. 원래 암호 키와 라이선스 파일을 함께 유지해야 하며 도구가 새 라이선스를 만들거나 유효기간을 바꾸지는 않습니다.

Dropbox 앱 식별자·비밀값은 `CLORIUM_DROPBOX_APP_KEY`와 `CLORIUM_DROPBOX_APP_SECRET`로 분리했습니다. `config/plugins-provisioning-map.json`을 정확한 원본 `CameraControl.Plugins.dll`에 적용하면 두 문자열 상수를 로컬로 옮길 수 있습니다. 로그인과 기존 토큰 재연결 호출 모두 이 설정을 사용합니다. 기존 사용자 토큰/secret 보관 계약은 유지했고 실제 사용자 설정 파일은 포함하지 않았습니다. 앱 설정이 비어 있으면 Dropbox 로그인/연결이 실패할 수 있습니다. 실제 Dropbox 통신은 미검증입니다.
