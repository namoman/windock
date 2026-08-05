# Windock

Windows에서 열린 창을 **작업표시줄에서 숨기고**, 필요할 때 **독 패널에서 다시 꺼내 쓰는** 경량 보조 프로그램입니다.

창이 너무 많아 작업표시줄이 복잡해질 때, 당장 쓰지 않는 창을 내려놓고 집중할 수 있게 도와줍니다.

## 주요 기능

| 기능 | 설명 |
|------|------|
| 창 보관 (Park) | 현재 활성 창을 작업표시줄에서 숨김 |
| 창 복원 (Restore) | 독 패널에서 더블클릭으로 이전 위치·크기 복원 |
| 글로벌 핫키 | `Ctrl+Shift+P`로 즉시 보관 |
| 창 선택 보관 | 열린 창 목록에서 직접 선택 |
| 시스템 트레이 | 백그라운드 상주, 독 표시/숨김 |
| 자동 정리 | 닫힌 창은 2초마다 목록에서 제거 |
| 폴백 처리 | 작업표시줄 숨김 실패 시 일반 최소화로 보관 |

## 요구 사항

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## 설치 및 실행

```powershell
git clone https://github.com/namoman/windock.git
cd windock
dotnet build
dotnet run --project WindowDock.App
```

빌드 후 실행 파일 위치:

```
WindowDock.App\bin\Debug\net8.0-windows\Windock.exe
```

Release 빌드:

```powershell
dotnet publish WindowDock.App -c Release -r win-x64 --self-contained false
```

## 사용 방법

### 1. 창 보관하기

1. Windock을 실행합니다.
2. 정리할 창을 활성화합니다.
3. `Ctrl+Shift+P`를 누르거나, 독 패널의 **현재 창 보관** 버튼을 클릭합니다.

또는 **창 선택...** 버튼으로 열린 창 목록에서 직접 고를 수 있습니다.

### 2. 창 복원하기

독 패널에 표시된 항목을 **더블클릭**하면 이전 위치와 크기로 복원됩니다.

### 3. 트레이 메뉴

시스템 트레이 아이콘에서 다음을 사용할 수 있습니다.

- 독 표시/숨김
- 현재 창 보관
- 창 선택해서 보관
- 종료

## 단축키

| 단축키 | 동작 |
|--------|------|
| `Ctrl+Shift+P` | 현재 활성 창 보관 |

## 동작 원리

Windock은 Win32 API를 사용해 창 핸들(`HWND`) 단위로 동작합니다.

1. **보관**: 숨김 Owner 창에 parent를 지정해 작업표시줄 버튼을 제거하고, 창을 숨깁니다.
2. **복원**: parent를 원래대로 되돌리고, 저장해 둔 위치·크기로 창을 다시 표시합니다.
3. **폴백**: parent 변경이 실패하면 일반 최소화(`SW_MINIMIZE`)로 보관합니다.

## 프로젝트 구조

```
windock/
├── WindowDock.App/       # 앱 진입점, 트레이, 글로벌 핫키
├── WindowDock.Core/      # Win32 연동, 창 열거, Park/Restore 로직
├── WindowDock.UI/        # WPF 독 패널, 창 선택 UI
└── WindowDock.sln
```

| 프로젝트 | 역할 |
|----------|------|
| `WindowDock.Core` | Win32 P/Invoke, `WindowEnumerator`, `WindowParker` |
| `WindowDock.UI` | `DockWindow`, `WindowPickerWindow`, ViewModel |
| `WindowDock.App` | 트레이 아이콘, 핫키 등록, 앱 수명 주기 관리 |

## 앱 아이콘

아이콘 파일: `WindowDock.App/Assets/app-icon.png`, `app-icon.ico`

Stitch에서보낸 이미지로 교체하려면 `WindowDock.App/Assets/README.md`를 참고하세요.

## 설정 저장

보관한 창의 메타데이터는 아래 경로에 JSON으로 저장됩니다.

```
%AppData%\Windock\parked-windows.json
```

## 제한 사항

| 상황 | 설명 |
|------|------|
| 관리자 권한 앱 | 일반 권한으로 실행한 Windock은 관리자 권한 창을 조작할 수 없습니다 |
| 일부 앱 | parent 변경을 거부하는 앱은 최소화 폴백으로 처리됩니다 |
| UWP/Store 앱 | `ApplicationFrameHost` 구조로 인해 지원이 제한될 수 있습니다 |
| 전체화면 게임 | 독점 전체화면 창은 OS 보호로 조작이 어렵습니다 |

대부분의 일반 데스크톱 앱(브라우저, IDE, 탐색기, 메신저 등)은 정상 동작합니다.

## 기술 스택

- C# / .NET 8
- WPF
- Win32 API (P/Invoke)

## 라이선스

MIT License
