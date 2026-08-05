# Windock Walkthrough

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
