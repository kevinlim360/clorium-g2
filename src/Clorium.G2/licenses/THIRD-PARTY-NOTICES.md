# 제삼자 소스·의존성 고지

이 문서는 최종 소스 후보에 실제 포함된 파일의 출처와 확인 상태를 구분한다. 파일 바이트가 제공 자료나 특정 패키지와 일치한다는 확인은 그 파일의 모든 재배포 조건을 확인했다는 뜻으로 확대하지 않는다. 제품 소스 전체에 단일 MIT 라이선스를 적용하거나 고객의 사용·재배포 권리를 새로 보증하지 않는다.

전체 파일 목록·크기·SHA-256과 확인 상태는 [dependency-inventory.json](../evidence/dependency-inventory.json)에 있다. `dependencies`의 103파일은 공급 바이너리 102파일과 생성한 빌드 계약 1파일이다. 별도로 프레임워크·WinRT 참조 614파일과 도구에 동봉된 Mono.Cecil 3파일을 기록했다. `bin`·`obj`의 생성 산출물은 이 인벤토리 범위에서 제외한다.

## 복원한 관리형 wrapper와 파일별 고지

| 대상 | 확인한 근거 | 포함된 고지와 남은 조건 |
|---|---|---|
| digiCamControl upstream 원문 | 후보 커밋 `5785e2b3ec29de153ad894e3b925ddeddb4ef76e`의 원문과 배포 PDB 문서 체크섬을 비교했다. 최종 적용된 원문 66파일의 근거는 `evidence/Upstream-Wrappers`에 있다. | [digiCamControl MIT 전문](digiCamControl-MIT.txt)을 제공한다. 이 루트 고지는 아래 파일별 고지·별도 라이브러리·native SDK의 조건을 대체하지 않는다. |
| `projects/Canon.Eos.Framework`의 managed wrapper | 원저자 Sascha Kiefer(esskar)의 [Canon.Eos.Framework 라이선스](https://github.com/esskar/Canon.Eos.Framework/blob/master/LICENSE.TXT)는 MIT다. checksum 일치 원문과 배포본 디컴파일 fallback을 구분해 보존했다. | [Canon managed wrapper MIT 전문](upstream-wrappers/Canon-Eos-Framework-LICENSE.TXT)과 파일별 저작권 고지를 보존한다. Canon native EDSDK 조건은 별개다. |
| `projects/PortableDeviceLib/RecoveredUpstream` | Vincent Lainé의 파일별 헤더에 LGPL 2.1 또는 이후 버전 선택이 명시되어 있다. | [GNU LGPL 2.1 전문](upstream-wrappers/LGPL-2.1.txt)과 해당 원문 헤더를 제공한다. 해당 소스·변경 내용·고지 제공 및 라이브러리 교체/재링크 등 실제 배포 방식에 적용되는 조건은 최종 사용·배포 구성에 따라 확인해야 한다. |
| `projects/ZoomAndPan` | digiCamControl 원문 고지와 `RecoveredUpstream/RelayCommand.cs`, `RelayCommandGeneric.cs`의 GalaSoft Laurent Bugnion 2009–2013 MIT 고지를 보존했다. | RelayCommand 파일의 MIT를 다른 모든 파일의 권리로 확대하지 않는다. 파일별 고지와 upstream 루트 고지를 함께 유지한다. |

PortableDeviceLib용 `dependencies/Managed/Interop.PortableDeviceApiLib.dll`과 `Interop.PortableDeviceTypesLib.dll`은 공급 설치 자료와 바이트가 일치하는 별도 COM interop 바이너리다. 이 두 DLL의 원본 소스·정확한 생성 도구/배포 조건은 확인하지 못했으며 PortableDeviceLib 소스의 LGPL 확인과 별개로 기록한다.

Canon wrapper는 native `EDSDK.dll`을 호출하는 선언을 포함한다. 최종 `dependencies`에는 native EDSDK DLL을 넣지 않았다. managed wrapper의 MIT 확인으로 native Canon SDK 권리가 확정되지는 않는다. [Canon SDK 안내](https://www.usa.canon.com/support/sdk)를 기준으로 고객 설치본에 적용되는 실제 SDK 계약을 별도로 확인해야 하며, 이 복구에서 해당 계약을 확인하지 못했다.

## 공급된 컴파일 의존성

| 최종 폴더 | 실제 파일 수 | 확인한 바이트 출처 | 확인하지 못한 사항 |
|---|---:|---|---|
| `dependencies/Managed` | 61 DLL | 제공 설치 자료 `app`의 동일 이름 파일과 SHA-256 일치 | 정확한 upstream 소스/패키지 릴리스·공급자 변경 여부·파일별 재배포 조건 |
| `dependencies/AutoSizing.Installed` | 18 | 설치판 AutoSizing의 Costura 추출본과 일치; DLL 17개와 제삼자 `costura.pdb` 1개 | 각 추출 바이너리의 정확한 upstream 및 재배포 조건 |
| `dependencies/AutoSizing.Standalone` | 18 | 별도 제공판 AutoSizing의 Costura 추출본과 일치; DLL 17개와 제삼자 `costura.pdb` 1개 | 각 추출 바이너리의 정확한 upstream 및 재배포 조건 |
| `dependencies/Centering` | 5 DLL | Centering의 Costura 추출본과 일치 | 각 추출 바이너리의 정확한 upstream 및 재배포 조건 |

각 제품 판본의 의존성은 별도 폴더에 유지했다. 이름이 같은 파일도 판본 간에 임의 교체하거나 하나의 라이선스 확인 결과로 합치지 않았다. Newtonsoft.Json, Accord, Magick.NET, Microsoft/Owin/System 계열 등 파일명만으로 정확한 버전의 라이선스를 확정하지 않았다. 공급 자료의 소유나 로컬 빌드 성공을 고객 재배포 권리의 근거로 취급하지 않는다.

## Windows 참조 팩과 WinRT 빌드 계약

- `framework-refs/net472` 369파일과 `framework-refs/net48` 238파일은 각각 `Microsoft.NETFramework.ReferenceAssemblies.net472`·`.net48` 1.0.3 패키지 자산과 바이트 단위로 일치한다. 패키지의 라이선스 링크는 [Microsoft dotnet LICENSE](https://github.com/Microsoft/dotnet/blob/master/LICENSE)이며 현재 연결되는 원문은 MIT다. 역사적 패키지별 라이선스 원문은 별도로 고정하지 않았고 해당 전문 파일도 이 후보에 별도로 포함하지 않았다.
- `framework-refs/winrt`·`winrt-devices`의 WinMD 파일은 보관한 `Microsoft.Windows.SDK.Contracts` 패키지 자산과 일치한다. 일부 동일 바이트가 여러 SDK 패키지 버전에 있으므로 특정한 한 버전에서 왔다고 단정하지 않는다. 패키지의 [Windows SDK 라이선스 링크](https://aka.ms/WinSDKLicenseURL)는 기록했으나 정확한 적용 약관과 재배포 조건은 확인하지 못했다.
- `System.Runtime.WindowsRuntime.dll`은 `System.Runtime.WindowsRuntime` 4.6.0의 `ref/netstandard2.0` 자산과 일치한다. 포함된 [WindowsRuntime 라이선스 전문](WindowsRuntime-LICENSE.TXT)은 그 패키지의 `LICENSE.TXT`와 SHA-256이 일치한다.
- `System.Runtime.InteropServices.WindowsRuntime.dll`은 `System.Runtime.InteropServices.WindowsRuntime` 4.3.0 자산과 일치한다. 포함된 [WindowsInterop .NET Library 라이선스 전문](WindowsInterop-dotnet_library_license.txt)은 그 패키지의 `dotnet_library_license.txt`와 SHA-256이 일치한다. 이 별도 Microsoft .NET Library 약관을 다른 Windows 참조 파일 전체의 조건으로 확대하지 않는다.
- `dependencies/BuildSupport/Windows.AsyncOnly.winmd`는 포함된 `tools/WinmdSubset/Program.cs`로 만든 부분 계약이다. Windows.Foundation의 비동기 관련 타입과 TypedEventHandler를 남기고 runtime 생성자 선언을 추가했다. 입력 `Windows.winmd`의 SHA-256은 인벤토리에 기록했지만 정확한 upstream 원출처와 약관은 확인하지 못했다. 입력 계약에 적용되는 조건을 함께 확인해야 한다.

이 파일들은 복원 프로젝트의 오프라인 컴파일 참조·계약으로 구분했다. 실제 Windows 런타임과 장치 환경은 별도 구성 대상이다.

## Mono.Cecil 도구 의존성

`tools/InteropMetadataRestore/Mono.Cecil.dll`, `tools/WiaMetadataFix/Mono.Cecil.dll`, `tools/WinmdSubset/Mono.Cecil.dll`은 `Mono.Cecil` 0.11.6 패키지의 `lib/netstandard2.0/Mono.Cecil.dll`과 SHA-256이 일치한다. [해당 0.11.6 태그의 라이선스](https://github.com/jbevain/cecil/blob/0.11.6/LICENSE.txt)는 MIT이며 Jb Evain과 Novell, Inc.의 저작권 고지를 포함한다. 도구의 재배포에도 원래 저작권·허가 고지 보존 조건이 적용된다. 해당 태그의 공식 원문을 [Mono.Cecil 0.11.6 라이선스 전문](Mono.Cecil-0.11.6-LICENSE.txt)으로 동봉했고 출처 URL과 SHA-256을 인벤토리에 기록했다.

## 확인 상태의 경계

확인된 사항은 실제 파일 목록/해시, 공급 자료 또는 패키지 자산과의 바이트 일치, 위에 명시한 원문 헤더와 라이선스 파일이다. 공급 DLL 102파일의 정확한 upstream·고객 계약, Canon native EDSDK, Windows SDK 계약, 생성 부분 계약의 입력 출처, .NET Framework 참조 팩의 역사적 라이선스 원문은 미확인 항목으로 남겼다. 이 문서는 빌드·정적 대조의 증거와 권리 확인 상태를 함께 전달하며 실제 Windows/COM/native 장치 실행을 검증했다는 주장을 하지 않는다.
