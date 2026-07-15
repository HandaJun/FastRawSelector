# FastRawSelector

Windows WPF 앱 — RAW 사진 빠른 선별(culling) 도구.

- **저장소:** https://github.com/HandaJun/FastRawSelector  
- **런타임:** .NET Framework 4.6.2+ / Windows  
- **UI 언어:** 한국어 · 日本語 · English  

## 다운로드 (사용자)

1. [Releases](https://github.com/HandaJun/FastRawSelector/releases) 에서 최신 zip 받기  
2. 압축 해제 후 `FastRawSelector.exe` 실행  
3. (최초) [.NET Framework 4.6.2](https://dotnet.microsoft.com/download/dotnet-framework/net462) 이상 설치  

**업데이트:** 새 zip을 받아 exe를 덮어쓰면 됩니다.  
설정·로그는 `%AppData%\Roaming\FastRawSelector\` 에 남으므로 유지됩니다.  
*(앱 내 자동 업데이트는 아직 없습니다 — 수동 다운로드.)*

자세한 배포 안내: [`doc/배포/포터블-및-설치.md`](doc/배포/포터블-및-설치.md)

## 개발자 빌드

```bat
nuget restore src\FastRawSelector.sln
msbuild src\FastRawSelector.sln /p:Configuration=Release /p:Platform="Any CPU"
```

출력: `src\FastRawSelector\bin\Release\FastRawSelector.exe`

## 버전

| 항목 | 위치 |
|---|---|
| AssemblyVersion | `src/FastRawSelector/Properties/AssemblyInfo.cs` |
| 창 제목 표시 | ` vMajor.Minor.Build` (예: ` v1.0.0`) |

현재 버전: **1.0.0**

### GitHub Release 올리는 법 (요약)

1. `AssemblyInfo.cs` 버전 올리기  
2. Release 빌드 → zip (`FastRawSelector.exe` + 짧은 README)  
3. `git tag v1.0.0` 후 push  
4. GitHub → Releases → 태그 선택 → zip 첨부 · 변경 요약 작성  

템플릿·체크리스트: `doc/배포/포터블-및-설치.md` §8

## 문서

| 문서 | 내용 |
|---|---|
| `doc/배포/포터블-및-설치.md` | 포터블 zip, AppData, 탐색기 연동, Release |
| `doc/계획/전소스-개선-후보.md` | Part 1~7 실행 계획 |
| `doc/계획/추가개선-및-확장-계획서.md` | A~J 기능 로드맵 |
| `AGENTS.md` | 저장소 가이드 (에이전트/기여자) |

## 라이선스 / 고지

- 앱 본체: 저장소 라이선스 따름  
- 네이티브: libraw, exiv2 (QuickLook 계열 바인딩 포함) — 각 라이선스 확인  
