<p align="center">
  <img src="src/FastRawSelector/FastRawSelector.ico" width="96" height="96" alt="FastRawSelector icon"/>
</p>

<h1 align="center">FastRawSelector</h1>

<p align="center">
  <strong>Fast RAW photo culling for Windows</strong><br/>
  Browse, select, and export — without waiting on a heavy editor.
</p>

<p align="center">
  <a href="README.md"><b>English</b></a> ·
  <a href="README.ko.md">한국어</a> ·
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
  <a href="https://github.com/HandaJun/FastRawSelector/releases/latest"><img src="https://img.shields.io/badge/⬇%20Download%20Latest-1.0.0-00BCD4?style=for-the-badge"/></a>
</p>

---

## Why FastRawSelector?

Culling hundreds of RAW files in Lightroom or Capture One is slow when you only need **select / reject / next**.  
FastRawSelector is a **lightweight WPF viewer** built for that loop:

| | |
|:--|:--|
| **Speed** | Embedded JPEG previews via libraw / exiv2 — not full demosaic for every click |
| **Focus** | Keyboard-first navigation, selection, fullscreen |
| **Light** | Single portable `exe` (Costura) + settings in AppData |
| **Safe** | Selection state saved per folder; originals stay untouched unless you export/copy |

---

## Features

### Viewing & selection
- **Single / Fullscreen / Grid** views  
- **← →** step, **PgUp / PgDn** jump 10, **Home / End**  
- **Select toggle** (default `B`), selection counter, “selected only” filter  
- **EXIF panel** (toggle, customizable hotkey)  
- **Dark / Light** themes  

### Workflows
- **Thumbnail export** (JPG) — all / selected / folder  
- **RAW copy** by selection or JPEG-name match  
- **Folder sort** by extension, date, camera, lens  
- **Batch file-time** adjust from EXIF-related times  

### App
- UI languages: **English · Korean · Japanese**  
- Explorer **Send to** / Open with (optional)  
- **Update check** against GitHub Releases (Settings toggle)  
- Remember window size/position, last folder  

---

## Quick start

1. Download **`FastRawSelector-v*.zip`** from  
   **[Releases → Latest](https://github.com/HandaJun/FastRawSelector/releases/latest)**  
2. Extract anywhere and run **`FastRawSelector.exe`**  
3. If prompted, install  
   **[.NET Framework 4.6.2+](https://dotnet.microsoft.com/download/dotnet-framework/net462)**  
4. Open a folder (**Ctrl+O**), drag a RAW file, or use Explorer Send to  

> **Updating:** download a newer zip and replace the `exe`.  
> Settings live in `%AppData%\Roaming\FastRawSelector\` and are kept.

---

## Default shortcuts

| Key | Action |
|:--|:--|
| `←` / `→` | Previous / next |
| `PgUp` / `PgDn` | Jump ±10 |
| `Home` / `End` | First / last |
| `B` * | Toggle selection |
| `I` * | Toggle EXIF |
| `F` * | Fullscreen |
| `G` | Grid view |
| `Ctrl+O` | Open folder |
| `Ctrl+,` | Settings |
| `F1` | Help |
| `Esc` | Exit fullscreen |

\* Customizable in **Settings → Hotkeys**

---

## Requirements

| Item | Detail |
|:--|:--|
| OS | Windows 7 SP1+ (WPF) |
| Runtime | .NET Framework **4.6.2** or later |
| Disk | Portable zip — no installer required |
| Network | Optional (update check only) |

---

## Build from source

```bat
msbuild src\FastRawSelector.sln /t:Restore /p:Configuration=Release
msbuild src\FastRawSelector.sln /p:Configuration=Release /p:Platform="Any CPU"
```

Output:

```text
src\FastRawSelector\bin\Release\FastRawSelector.exe
```

---

## Project layout

```text
src/
  FastRawSelector.sln
  FastRawSelector/          # WPF app
```

Only the **`src`** tree is the product source published here.

---

## License & third-party

- Application: see [LICENSE](LICENSE)  
- Native stacks: **libraw**, **exiv2** (and related) — respect their licenses  
- UI: MaterialDesignThemes, MahApps icon packs, etc. (NuGet)

---

## Support

- Issues & ideas: [GitHub Issues](https://github.com/HandaJun/FastRawSelector/issues)  
- Releases: [github.com/HandaJun/FastRawSelector/releases](https://github.com/HandaJun/FastRawSelector/releases)

<p align="center">
  <sub>Made for photographers who want to cull first, edit later.</sub>
</p>
