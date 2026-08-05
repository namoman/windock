# Windock Walkthrough

## 2026-08-06 — 숨긴 창이 목록에서 사라지는 문제 수정

### 배경
이전 세션에서 Park한 창이 재실행 후 **활성 창 / 숨겨진 창** 양쪽 목록에 안 보였다.

### 원인
- 숨김 목록은 프로세스 메모리에만 유지되고, JSON 메타(`parked-windows.json`)는 저장만 하고 로드하지 않음
- 활성 목록은 `IsWindowVisible` 창만 열거 → Park된 숨김 창 제외
- 앱 강제 종료 시 Owner가 사라져 창은 OS에 숨김으로 남지만 어떤 목록에도 속하지 않는 고아 상태가 됨

### 변경 사항
- 시작 시 JSON 메타와 숨김 창을 매칭해 `AdoptHidden`으로 숨김 목록에 재등록
- 정상 종료(`OnExit`) 시 `RestoreAll`로 숨긴 창을 모두 복원해 고아 창 방지
- `GetHiddenTitledWindows` 열거 추가

### 접근
새 저장 포맷/HWND 영속화 대신, 기존 메타(제목+프로세스명)로 회수하고 종료 시 복원하는 최소 수정.

## 2026-08-06 — 단축키 페이지에서 가짜 항목 제거

### 배경
Shortcuts 화면에 "모든 창 숨기기", "독으로 보내기"가 단축키처럼 표시되어 있었지만, 실제 글로벌 핫키가 아니거나(대시보드 토글) 미구현 기능이었다.

### 변경 사항
- 실제 핫키인 `Ctrl+Shift+P`(현재 창 보관)만 목록에 유지
- 페이지 문구를 "단축키 설정/할당" → "등록된 글로벌 핫키"로 수정

### 접근
존재하지 않는 단축키를 UI로 포장하지 않고, 실제 등록된 항목만 남기는 최소 수정.

## 2026-08-06 — 실행 시 XAML 크래시 수정

### 배경
`dotnet run` 시 앱이 바로 종료됐다.

### 원인 / 수정
1. **GlassCardStyle 누락** — 테마가 `MainWindow.Resources`에만 있어 하위 `UserControl`의 `StaticResource` 해석이 실패 → `App.xaml` `Application.Resources`로 테마 병합
2. **ToggleSwitchStyle TargetType 불일치** — `WindowsView`에서 `Button`에 `ToggleButton` 전용 스타일을 적용 → `ToggleButton`으로 교체

### 접근
새 스타일/폴백을 만들지 않고, 리소스 스코프와 컨트롤 타입만 바로잡는 최소 수정.

## 2026-08-05 — UI 목업 기반 메인 화면 재구성

### 배경
공유된 HTML 목업(Dashboard / Windows / Shortcuts / Settings)에 맞춰 기존 단순 독 패널(`DockWindow`)을 4탭 구조의 `MainWindow`로 교체했다.

### 변경 사항
- `MainWindow` + 하단 네비게이션 셸 추가
- `DashboardView`, `WindowsView`, `ShortcutsView`, `SettingsView` 페이지 분리
- `MainViewModel`에서 창 보관/복원, 통계, 설정 바인딩 통합
- `AppSettings` / `AppSettingsStore`로 시작 프로그램 등록·독 투명도 저장
- 다크 테마 리소스 `Resources/WindockTheme.xaml` 추가

### 제거
- `DockWindow.xaml` (기능은 MainWindow로 이전)

### 미구현(목업 대비)
- 레이아웃 프리셋, 창 바둑판 배열, 작업표시줄 자동 숨기기
- 단축키 커스터마이즈(현재는 안내 목록만 표시)
