<p align="center">
  <img src="src/FastRawSelector/FastRawSelector.ico" width="96" height="96" alt="FastRawSelector 아이콘"/>
</p>

<h1 align="center">FastRawSelector</h1>

<p align="center">
  <strong>Windows용 빠른 RAW 선별(culling) 도구</strong><br/>
  무거운 후보정 프로그램 없이 고르고, 넘기고, 내보내기.
</p>

<p align="center">
  <a href="README.md">English</a> ·
  <a href="README.ko.md"><b>한국어</b></a> ·
  <a href="README.ja.md">日本語</a>
</p>

<p align="center">
  <a href="https://github.com/HandaJun/FastRawSelector/releases/latest"><img alt="Release" src="https://img.shields.io/github/v/release/HandaJun/FastRawSelector?style=for-the-badge&color=00BCD4"/></a>
  <a href="https://github.com/HandaJun/FastRawSelector/releases"><img alt="Downloads" src="https://img.shields.io/github/downloads/HandaJun/FastRawSelector/total?style=for-the-badge&color=26A69A"/></a>
  <img alt="Platform" src="https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white"/>
  <img alt=".NET" src="https://img.shields.io/badge/.NET%20Framework-4.6.2+-512BD4?style=for-the-badge&logo=dotnet&logoColor=white"/>
  <img alt="UI" src="https://img.shields.io/badge/UI-EN%20%7C%20KO%20%7C%20JA-FF7043?style=for-the-badge"/>
</p>

<p align="center">
  <a href="https://github.com/HandaJun/FastRawSelector/releases/latest"><img src="https://img.shields.io/badge/⬇%20최신판%20다운로드-1.0.3-00BCD4?style=for-the-badge"/></a>
</p>

---

## 왜 FastRawSelector인가요?

수백 장의 RAW를 후보정 프로그램에서만 넘기면 **선택·탈락**만 할 때도 느립니다.  
FastRawSelector는 그 루프에 맞춘 **가벼운 WPF 뷰어**입니다.

| | |
|:--|:--|
| **속도** | libraw / exiv2 임베디드 미리보기 — 매번 full demosaic 하지 않음 |
| **집중** | 키보드 중심 이동·선택·전체화면 |
| **가벼움** | 포터블 단일 `exe` + 설정은 AppData |
| **안전** | 폴더별 선택 저장, 원본은 내보내기/복사 전까지 그대로 |

---

## 주요 기능

### 보기 · 선택
- **싱글 / 전체화면 / 그리드**  
- **← →** 한 장, **PgUp / PgDn** 10장, **Home / End**  
- **선택 토글** (기본 `B`), 선택 수, “선택한 사진만”  
- **EXIF 패널** (단축키 변경 가능)  
- **다크 / 라이트** 테마  

### 작업
- **썸네일 JPG 추출** — 전체 / 선택 / 폴더  
- **RAW 복사** — 선택 또는 JPEG 이름 매칭  
- **폴더 분류** — 확장자·날짜·카메라·렌즈  
- **파일 시간 일괄 변경**  

### 앱
- UI: **한국어 · 日本語 · English**  
- 탐색기 **보내기 / 연결 프로그램** (선택)  
- **자동 업데이트** (GitHub Releases, 설정에서 ON/OFF)  
- 창 위치·크기, 최근 폴더 기억  

---

## 빠른 시작

1. **[Releases → Latest](https://github.com/HandaJun/FastRawSelector/releases/latest)** 에서  
   **`FastRawSelector-v*.zip`** 받기  
2. 압축 해제 후 **`FastRawSelector.exe`** 실행  
3. 필요 시  
   **[.NET Framework 4.6.2+](https://dotnet.microsoft.com/download/dotnet-framework/net462)** 설치  
4. **Ctrl+O**, RAW 드래그, 또는 탐색기 보내기로 폴더 열기  

### 업데이트

| | |
|:--|:--|
| **앱 안** | **설정 → 지금 업데이트 확인** (또는 시작 시 자동 확인). zip 다운로드 → 앱 종료 → `exe` 교체 → 자동 재실행 |
| **수동** | 새 zip을 받아 `exe`만 덮어쓰기 |
| **설정** | `%AppData%\Roaming\FastRawSelector\` 에 저장되며 업데이트 후에도 유지 |

> 쓰기 가능한 폴더에 두는 것을 권장합니다.  
> `Program Files` 등은 권한 때문에 자동 교체가 실패할 수 있습니다.

---

## 기본 단축키

| 키 | 동작 |
|:--|:--|
| `←` / `→` | 이전 / 다음 |
| `PgUp` / `PgDn` | ±10장 |
| `Home` / `End` | 처음 / 마지막 |
| `B` * | 선택 토글 |
| `I` * | EXIF |
| `F` * | 전체화면 |
| `G` | 그리드 |
| `Ctrl+O` | 폴더 열기 |
| `Ctrl+,` | 설정 |
| `F1` | 도움말 |
| `Esc` | 전체화면 해제 |

\* **설정 → 단축키** 에서 변경 가능

---

## 요구 사항

| 항목 | 내용 |
|:--|:--|
| OS | Windows 7 SP1 이상 (WPF) |
| 런타임 | .NET Framework **4.6.2** 이상 |
| 설치 | 포터블 zip (인스톨러 불필요) |
| 네트워크 | 선택 (업데이트 확인 시) |

---

## 소스 빌드

```bat
msbuild src\FastRawSelector.sln /t:Restore /p:Configuration=Release
msbuild src\FastRawSelector.sln /p:Configuration=Release /p:Platform="Any CPU"
```

출력:

```text
src\FastRawSelector\bin\Release\FastRawSelector.exe
```

---

## 구성

```text
src/
  FastRawSelector.sln
  FastRawSelector/          # WPF 앱
```

공개 저장소의 제품 소스는 **`src`** 입니다.

---

## 라이선스

- 앱: **[GNU GPL v3](LICENSE)**  
- 네이티브: **libraw**, **exiv2** 등 — 각 라이선스 준수  
- UI: MaterialDesignThemes, MahApps 아이콘 등 (NuGet)

---

## 지원

- 이슈: [GitHub Issues](https://github.com/HandaJun/FastRawSelector/issues)  
- 배포: [Releases](https://github.com/HandaJun/FastRawSelector/releases)

<p align="center">
  <sub>먼저 고르고, 나중에 보정하세요.</sub>
</p>
