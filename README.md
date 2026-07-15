# FastRawSelector

Windows WPF app for fast RAW photo culling.

- **Repository:** https://github.com/HandaJun/FastRawSelector  
- **Runtime:** .NET Framework 4.6.2+ / Windows  
- **UI languages:** Korean · Japanese · English  

## Download

1. Get the latest zip from [Releases](https://github.com/HandaJun/FastRawSelector/releases)  
2. Extract and run `FastRawSelector.exe`  
3. Install [.NET Framework 4.6.2](https://dotnet.microsoft.com/download/dotnet-framework/net462) or later if needed  

**Update:** Replace the exe with a newer zip. Settings stay under `%AppData%\Roaming\FastRawSelector\`.  
The app can check GitHub Releases for updates (toggle in Settings).

## Build

```bat
msbuild src\FastRawSelector.sln /t:Restore /p:Configuration=Release
msbuild src\FastRawSelector.sln /p:Configuration=Release /p:Platform="Any CPU"
```

Output: `src\FastRawSelector\bin\Release\FastRawSelector.exe`

## Version

- `AssemblyVersion`: `src/FastRawSelector/Properties/AssemblyInfo.cs`  
- Current: **1.0.0**

## License

- App: see repository license  
- Native: libraw, exiv2 (and related) — see each project’s license  
