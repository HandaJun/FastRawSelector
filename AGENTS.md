# Repository Guidelines

## Project Overview

FastRawSelector is a Windows WPF desktop application (.NET Framework 4.6.2, C#) for fast RAW photo culling and selection. It decodes RAW camera files via embedded native `libraw.dll`, reads EXIF metadata via `exiv2-ql-{32,64}.dll`, and presents a grid/single/full-screen image viewer with selection state, thumbnail export, and RAW-copy workflows. UI is Korean-only with no localization pipeline.

## Architecture & Data Flow

**Not MVVM.** The app uses a code-behind-centric architecture where static "service" classes hold all application state and controls are updated imperatively via method calls dispatched through `Common.Invoke()` (a `Dispatcher.Invoke` wrapper).

### Core pipeline

```
Disk file
  → Common.IsRawFile/IsBitmapFile (extension filter)
  → LoadImage.ImageList (SortedDictionary<string, ImageData>)
  → Decode:
      RAW  → RawManager.GetThumbnailArray → MetaProvider → exiv2 (XML EXIF + thumbnail bytes) → BitmapImage → orientation-aware rotate/scale → JPEG byte[]
      BMP  → BitmapManager.GetBitmapArray → MetadataExtractor + System.Drawing → ImageResizing → JPEG byte[]
  → ImageData.ImageArray (cached byte[])
  → LoadImage.SetImage/SetExif → Common.Main.SingleViewCtrl or FullViewCtrl (per Common.NowView)
```

### Prefetch

`LoadImage.NearLoad` spawns a `Thread` (Highest priority) to prefetch ±10 neighbors. `LoadImage.AllLoad` uses `Parallel.ForEach` over the remaining `ImageList` with `Interlocked.Increment` for progress bar updates. Neither is cancellable.

### Settings (YAML via YamlDotNet)

- **`ApplicationSetting`** (`LOGIC/ApplicationSetting.cs`) — global app config at `<exeDir>/FastRawSelectorSetting.yaml`. Props: `AllowNotRawImage`, `IsExifVisible`. `Location` is `readonly`.
- **`SelectorSetting`** (`LOGIC/SelectorSetting.cs`) — per-folder selection state at `<dir>/FastRawSelector.yaml`. Holds `HashSet<string> SelectedSet` of selected filenames. `Location` is mutable static, reassigned on each `Load(path)`.

Both use `DeserializerBuilder().IgnoreUnmatchedProperties()`, create a default file if missing, and swallow deserialize exceptions.

### Dialog/navigation pattern

Dialog windows (`ExportThumbnailWindow`, `RawCopyWindow`) use **lazy singleton** + **hide-on-close** (`Window_Closing` cancels and calls `Hide()`). They read global state directly and write to the filesystem — no return values or events. Alerts use MaterialDesign `DialogHost.Show()` with `NotificationMessage` subclasses rendered via `DataType`-matched `DataTemplate`s in `STYLE/DialogStyle.xaml`.

### View switching

`MainWindow.ViewChange(ViewEnum)` toggles `Visibility` on three `UserControl` instances (`GridViewCtrl`, `SingleViewCtrl`, `FullViewCtrl`). `ViewEnum`: `Grid, Single, Full`. Grid view is incomplete/non-functional.

## Key Directories

All source under `src/FastRawSelector/`:

| Directory | Purpose |
|---|---|
| `LOGIC/` | Core services, native P/Invoke bindings, settings, helpers |
| `MANAGER/` | Decode pipelines: `RawManager` (RAW), `BitmapManager` (non-RAW) |
| `MODEL/` | POCO data models, enums, notification message types |
| `VIEW/` | Dialog windows (export, copy, EXIF change, folder division, settings) |
| `CONTROL/` | Reusable WPF `UserControl`s (Single, Full, Grid views) + converter |
| `STYLE/` | XAML resource dictionaries (dialog templates, borders, text styles) |
| `Properties/` | AssemblyInfo, Annotations, Resources, Settings |
| `IMG/` | Embedded folder/drive icons |
| `bin/`, `obj/` | Build output (gitignored) |

## Development Commands

### Build (CLI)

```bash
# 1. Restore NuGet packages (packages.config — NOT PackageReference)
nuget restore src/FastRawSelector.sln

# 2. Build
msbuild src/FastRawSelector.sln /p:Configuration=Release /p:Platform="Any CPU"

# Output: src/FastRawSelector/bin/Release/FastRawSelector.exe
# (Costura.Fody embeds all managed DLLs → single exe)
```

### Build (Visual Studio)

Open `src/FastRawSelector.sln` (VS 2019+), NuGet auto-restores on open, F5 to debug or Ctrl+Shift+B for Release.

### No tests, lint, or CI

There are no test projects, no linting config, and no CI manifests in this repository.

## Code Conventions & Common Patterns

### Naming

- **Public methods/properties**: PascalCase (`SetArg`, `GetThum`, `ImageList`, `NowIndex`)
- **Locals**: camelCase, sometimes terse (`thum`, `bm`, `flg`, `act`)
- **XAML controls**: PascalCase with type suffix — `MainImg` (Image), `ExportPathTb` (TextBox), `CountTb` (TextBlock), `ExportPb` (ProgressBar), `SelectCb` (CheckBox), `PrevBt` (Button), `SelectedBd` (Border), `SingleViewCtrl` (UserControl)
- **Event handlers**: `<Control>_<Event>` (`ExportBt_Click`, `Window_Loaded`, `Window_Closing`)
- **ComboBox mode discrimination**: `Tag` property as string (`Tag="All"`, `Tag="Selected"`, `Tag="SpecifiedFolder"`), read via `cbi.Tag.ToString()` in switch statements
- **Known typo**: `LoadImage.NextIamge` (should be `NextImage`) — load-bearing, called in multiple places

### State management

All application state is **static mutable global** with no synchronization:
- `LoadImage`: `ImageList`, `NowImage`, `NowIndex`, `LastIndex`, `processingSet` (ConcurrentDictionary), `NowDir`, `IsImageLoaded`
- `Common`: `Main` (MainWindow ref), `NowSelectorSetting`, `NowView`/`AgoView`, `IsOnlySelectedShow`, `Loading`, predefined `SolidColorBrush`es, paths
- `App.Setting`: `ApplicationSetting` instance

`NowIndex` is read/written across UI + background threads with no locks — torn-read risk.

### Async/concurrency

- **No `async/await`** in the load pipeline (except `Alert` using `DialogHost.Show` which returns `Task<object>`)
- Concurrency via raw `Thread` (NearLoad, GetThum with `Thread.Sleep(10)`), `Task.Run` (AllLoad, SelectEx save), `Parallel.ForEach` (AllLoad)
- UI marshalling uniformly via `Common.Invoke(this Action, FrameworkElement, DispatcherPriority)` — unusual extension-method-on-`Action` pattern
- **No `CancellationToken`** anywhere

### Error handling

**Pervasive swallow-catch**: `RawManager`, `BitmapManager`, `MetaProvider` native wrappers, `Common.FileDelete`, and both settings `Load()` methods catch `Exception` and return null/empty/default with at most `Debug.WriteLine`. `Log.Exception` is used in only one place (`LoadImage.SelectEx`). Transient decode failures permanently mark valid RAW files as `IsNotImage=true` with no log and no retry.

### P/Invoke

Two native DLL families:
- **libraw** via `LOGIC/RAWLib.cs` — ~40 `DllImport("libraw")`, `CharSet.Ansi` (Unicode variants for `open_wfile*`), marshalled sequential structs, unmanaged delegates for callbacks. Relies on Costura/manual extraction to place `libraw.dll` in the exe directory.
- **exiv2** via `LOGIC/MetaProvider.cs` `NativeMethods` — dual 32/64 `DllImport` of `exiv2-ql-32.dll`/`exiv2-ql-64.dll`, selected at runtime via `Environment.Is64BitProcess`. Two-pass string/buffer pattern (query length with null, then fill).
- **kernel32** `IsWow64Process` in `Common.cs` for OS bitness detection.

### Native DLL lifecycle

`libraw.dll`, `exiv2-ql-{32,64}.dll`, and `log4net.dat` are embedded as WPF `<Resource>` and **extracted to the exe directory on every launch** (`App.xaml.cs` → `Common.FileNameToRoot`) then **deleted on window close** (`MainWindow.xaml.cs`). This requires write permission to the install directory and leaves stale DLLs on crash.

### Logging

`LOGIC/Log.cs` — thin log4net wrapper with `[CallerMemberName]`/`[CallerFilePath]` formatting. Configured via `[assembly: log4net.Config.XmlConfigurator(ConfigFile="log4net.dat", Watch=true)]` in `AssemblyInfo.cs`. The `log4net.dat` file (deliberate `.dat` extension camouflage) defines three `RollingFileAppender`s writing to `.\logs\` (DEBUG/INFO+WARN/ERROR+FATAL, date-rolled). Root level is INFO. **Barely used** — most catch blocks swallow silently instead of logging.

### XAML conventions

- **No `DataContext` binding** except `DialogStyle.xaml` templates. All control-to-control communication is via code-behind.
- **MaterialDesign styles**: `MaterialDesignFlatDarkButton`, `MaterialDesignFlatLightButton`, `MaterialDesignComboBox`, `MaterialDesignMenu`, etc., referenced as `StaticResource`.
- **Icons**: MahApps `PackIconBoxIcons` with `Kind` enum, 25x25 or 20x20, `#EEEEEE`/`White` foreground.
- **Transparent hit-test borders**: `Background="#01000000"` for mouse-hover trigger areas.
- **Custom window chrome**: Dialog windows use `WindowStyle="None"`, `AllowsTransparency="True"`, `Background="Transparent"`, `DragMove()` for dragging.
- **Korean UI text** throughout (`"섬네일추출"`, `"출력대상"`, `"선택한 사진만"`). Comments mix Korean and English.
- **Only one `IValueConverter`**: `HeaderToImageConverter` — currently unused (commented out in `GridViewControl.xaml`).

### Namespace

Folder-based: `FastRawSelector.LOGIC`, `FastRawSelector.MANAGER`, `FastRawSelector.MODEL`, `FastRawSelector.VIEW`, `FastRawSelector.CONTROL`.

## Important Files

| File | Role |
|---|---|
| `src/FastRawSelector.sln` | VS 2019 solution, single project, Debug/Release \| Any CPU |
| `src/FastRawSelector/FastRawSelector.csproj` | Build config, NuGet refs, native DLL resources, post-build cleanup |
| `src/FastRawSelector/App.xaml.cs` | Entry point — extracts native DLLs, forces `ko-KR` culture, loads `ApplicationSetting` |
| `src/FastRawSelector/MainWindow.xaml.cs` | Main shell — keyboard shortcuts, view switching, fullscreen, DLL cleanup on close |
| `src/FastRawSelector/MainWindow.xaml` | Main UI layout — `DialogHost`, menu bar, three view controls |
| `src/FastRawSelector/LOGIC/LoadImage.cs` | **Orchestration hub** (~707 lines) — image loading, navigation, prefetch, selection, EXIF toggle |
| `src/FastRawSelector/LOGIC/Common.cs` | **Global helpers** (~566 lines) — `Invoke`, `Try`, `IsRawFile`, `IsBitmapFile`, file ops, paths, brushes |
| `src/FastRawSelector/LOGIC/RAWLib.cs` | **libraw P/Invoke binding** (~569 lines) — ~40 DllImport, enums, marshalled structs |
| `src/FastRawSelector/LOGIC/MetaProvider.cs` | **exiv2 P/Invoke** — EXIF XML parse, thumbnail, size, orientation (GPL-3.0 header from QuickLook) |
| `src/FastRawSelector/MANAGER/RawManager.cs` | RAW decode pipeline — thumbnail extraction + orientation-aware rotate/scale |
| `src/FastRawSelector/MANAGER/BitmapManager.cs` | Non-RAW decode pipeline — MetadataExtractor + System.Drawing resize |
| `src/FastRawSelector/MODEL/ImageData.cs` | Core domain model — POCO with `ImageArray`, `Exif`, `IsNotImage`, `IsRaw`, `IsBitmap` |
| `src/FastRawSelector/LOGIC/Alert.cs` | MaterialDesign `DialogHost` wrappers — `Info()`, `Show()` |
| `src/FastRawSelector/LOGIC/Log.cs` | log4net wrapper with caller info |
| `src/FastRawSelector/LOGIC/ApplicationSetting.cs` | Global YAML settings (`AllowNotRawImage`, `IsExifVisible`) |
| `src/FastRawSelector/LOGIC/SelectorSetting.cs` | Per-folder YAML selection state (`SelectedSet`) |
| `src/FastRawSelector/packages.config` | NuGet package list (packages.config style) |
| `src/FastRawSelector/App.config` | Binding redirects + supportedRuntime (stripped from output by post-build) |
| `src/FastRawSelector/FodyWeavers.xml` | Costura.Fody config (bare `<Costura/>`, all defaults) |
| `src/FastRawSelector/log4net.dat` | log4net XML config (3 rolling file appenders) |
| `src/FastRawSelector/TODO.txt` | Korean TODO list — planned features and completed items |

## Runtime/Tooling Preferences

- **Framework**: .NET Framework 4.6.2 (Windows-only). Not portable to Linux/macOS (WPF, WinForms, `System.Web`, native P/Invoke).
- **SDK**: VS 2019+ / MSBuild 15+ (ToolsVersion 15.0). .NET Framework 4.6.2 targeting pack required.
- **NuGet restore**: `nuget restore` (NOT `msbuild -t:Restore` — packages.config doesn't support it). `packages/` is gitignored.
- **Package manager**: NuGet (packages.config style, NOT PackageReference).
- **Single-exe distribution**: Costura.Fody embeds all managed Copy-Local DLLs. The exe must have **write permission to its own directory** (native DLL extraction on launch). Installing under `%ProgramFiles%` breaks startup.
- **Fody version constraint**: Stay on Fody 4.2.1 / Costura.Fody 3.3.3 while on .NET Framework — Fody 5+ dropped older toolchains.
- **Culture**: App forces `CultureInfo.CurrentCulture = ko-KR` at startup. All UI strings are hardcoded Korean.
- **Log output**: `.\logs\` relative to working directory, date-rolled files.

### Key NuGet packages

| Package | Version | Purpose |
|---|---|---|
| Costura.Fody | 3.3.3 | Embeds managed DLLs into single exe |
| Fody | 4.2.1 | IL-weaving host for Costura |
| log4net | 2.0.15 | Logging |
| MaterialDesignThemes | 4.7.1 | Material Design XAML UI toolkit |
| MaterialDesignColors | 2.1.1 | Material Design color palettes |
| MahApps.Metro.IconPacks.BoxIcons | 4.11.0 | BoxIcons icon set |
| MetadataExtractor | 2.7.2 | EXIF/IPTC/XMP metadata for bitmap files |
| YamlDotNet | 13.0.0 | YAML serialization for settings |
| WindowsAPICodePack-Core/Shell | 1.1.1 | CommonFileDialog, shell APIs |
| Microsoft.Xaml.Behaviors.Wpf | 1.1.39 | XAML Blend behaviors |
| XmpCore | 6.1.10.1 | XMP metadata parsing (MetadataExtractor dep) |
| System.ValueTuple | 4.5.0 | ValueTuple backport for net462 |

### Native DLLs

| DLL | Role | Embedding |
|---|---|---|
| `libraw.dll` | RAW decoding (CR2, NEF, ARW, etc.) | WPF `<Resource>`, extracted on launch |
| `exiv2-ql-32.dll` | EXIF read (32-bit) | WPF `<Resource>`, extracted on launch |
| `exiv2-ql-64.dll` | EXIF read (64-bit) | WPF `<Resource>`, extracted on launch |

## Testing & QA

**No tests exist.** There are no unit test projects, integration tests, or test frameworks referenced anywhere in the solution. No CI/CD pipelines are configured. Verification is manual (run the app, open a RAW folder, navigate/export/copy).

### Known incomplete/non-functional features

- **GridViewControl** — incomplete. Has debug `MessageBox.Show` on folder selection, 9 hardcoded placeholder `GridItemControl`s, no data binding. View button is `Visibility="Collapsed"`.
- **ExifChangeWindow**, **FolderDivisionWindow** — non-functional stubs (all logic commented out, menu items disabled).
- **SettingWindow** — near-empty stub (empty event handlers, menu item commented out).
- **GridItemControl** — empty shell (Border + empty Grid, no properties).
- **HeaderToImageConverter** — both branches return identical icon, `[ValueConversion]` attribute wrong, unused at runtime.
- **NewProjectViewModel** — only `INotifyPropertyChanged` impl in the project, never instantiated (dead code).
- **MiniImgItem** — DTO for mini-thumbnail ListView binding, but `SetMiniImg()` is commented out (unused at runtime).

See `src/FastRawSelector/TODO.txt` for the author's Korean-language feature roadmap.

## Agent Coding Guidelines

Derived from [Andrej Karpathy's observations](https://github.com/multica-ai/andrej-karpathy-skills) on LLM coding pitfalls. Apply these four principles when working on this codebase.

### 1. Think Before Coding

Don't assume. Don't hide confusion. Surface tradeoffs.

- **State assumptions explicitly** — if uncertain, ask rather than guess. This codebase has static mutable global state and native P/Invoke boundaries where assumptions are dangerous.
- **Present multiple interpretations** — don't pick silently when ambiguity exists (e.g., "fix the prefetch" could mean NearLoad index bug or AllLoad progress bug).
- **Push back when warranted** — if a simpler approach exists than what was asked, say so.
- **Stop when confused** — name what's unclear and ask for clarification.

### 2. Simplicity First

Minimum code that solves the problem. Nothing speculative.

- No features beyond what was asked — this codebase already has 5+ incomplete stub windows; don't add more.
- No abstractions for single-use code — the existing code is deliberately code-behind-heavy with static classes, not MVVM. Match that style.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios — but DO note that existing swallow-catch patterns hide real bugs (see Error Handling above).
- If 200 lines could be 50, rewrite it. `LoadImage.cs` (~707 lines) is a candidate to split, but only if the task requires it.

**The test:** Would a senior engineer say this is overcomplicated? If yes, simplify.

### 3. Surgical Changes

Touch only what you must. Clean up only your own mess.

- Don't "improve" adjacent code, comments, or formatting — this repo has Korean/English mixed comments and terse naming (`thum`, `bm`, `flg`); match it, don't "fix" it.
- Don't refactor things that aren't broken — the `NextIamge` typo is load-bearing (called in multiple places); don't rename it as a side effect.
- Match existing style, even if you'd do it differently — extension-method-on-`Action` for `Common.Invoke`, `Tag`-string discrimination in ComboBoxes, hide-on-close singleton windows.
- If you notice unrelated dead code (`NewProjectViewModel`, `MiniImgItem`, stub windows), mention it — don't delete it.
- Remove imports/variables/functions that YOUR changes made unused. Don't remove pre-existing dead code unless asked.

**The test:** Every changed line should trace directly to the user's request.

### 4. Goal-Driven Execution

Define success criteria. Loop until verified.

| Instead of... | Transform to... |
|---|---|
| "Fix the prefetch" | "Write a scenario that reproduces the wrong-image-load, then fix `NearLoad` to use `index` not `i`, then verify neighbors load correctly" |
| "Fix the memory leak" | "Identify the `MetaProvider.GetSize` leak, add `libraw_close`/`libraw_dcraw_clear_mem` in finally, verify no handle growth" |
| "Add a feature" | "State acceptance criteria, implement, build with `msbuild /p:Configuration=Debug`, run and verify the specific workflow" |

This repo has **no tests**. Verification means: build the solution, run the app, open a RAW folder, and exercise the specific workflow you changed. For multi-step tasks, state a brief plan:

```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

For native P/Invoke changes (`RAWLib.cs`, `MetaProvider.cs`), verify there are no access violations or handle leaks by loading multiple RAW files in sequence.
