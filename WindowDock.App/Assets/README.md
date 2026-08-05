Windock 앱 아이콘은 Google Stitch에서보낸 이미지를 사용합니다.

## Stitch 원본
- 프로젝트: `web application/stitch/projects/16915130007013591920/screens/ee5e179a45da402f83f81265a0aa93d7`

## 교체 방법
1. Stitch 화면을 **PNG (512×512 이상)** 로보냅니다.
2. 아래 파일을 덮어씁니다:
   - `app-icon.png` — UI·트레이용
   - `app-icon.ico` — Windows 실행 파일 아이콘용 (선택, 권장)

ICO 변환 (PowerShell, ImageMagick 설치 시):
```powershell
magick app-icon.png -define icon:auto-resize=256,128,64,48,32,16 app-icon.ico
```

3. 프로젝트를 다시 빌드합니다.
