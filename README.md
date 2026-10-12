# AES — Android Engineering Suite

> All-in-one Windows toolset for Android reverse engineering — APK decompilation, firmware unpacking, ADB device control, and static analysis in a single Avalonia UI.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-11.3.9-8B4CFF?logo=avalonia)](https://avaloniaui.net/)
[![Platform](https://img.shields.io/badge/platform-Windows%20x64-0078D4?logo=windows)](https://github.com/K1LLLAGT/AES/releases)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

---

## Modules

| Module | Description |
|---|---|
| **APK Lab** | Decompile APKs, extract manifests, inspect DEX bytecode |
| **Firmware Lab** | Unpack and inspect Android firmware images |
| **Device Bridge** | Live ADB device listing, shell commands, logcat streaming, fastboot |
| **Analyzer** | Static DEX analysis and artifact extraction |
| **Workspace** | Project management — open, save, and organise engineering sessions |

---

## Tech Stack

| Component | Library | Version |
|---|---|---|
| UI Framework | Avalonia + Fluent Theme | 11.3.9 |
| Reactive UI | ReactiveUI + Fody | 20.1.1 / 19.5.41 |
| Observables | System.Reactive | 6.1.0 |
| ADB / Process | CliWrap | 3.10.2 |
| Runtime | .NET | 8.0 |
| Tests | xUnit | 2.x |

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) (for building from source)
- [Android Platform Tools](https://developer.android.com/tools/releases/platform-tools) — place `adb.exe` and `fastboot.exe` in `tools\platform-tools\`
- Windows 10 x64 or later

No .NET runtime required for the self-contained published build.

---

## Getting Started

### Clone & build

```powershell
git clone https://github.com/K1LLLAGT/AES.git
cd AES
dotnet restore AES.sln
dotnet build AES.sln --configuration Release
```

### Run (dev mode — rebuilds on launch)

```powershell
# Double-click launch.bat, or:
dotnet run --project src\AES.UI\AES.UI.csproj
```

### Run (prebuilt binary)

```powershell
# Double-click launch-exe.bat, or:
.\publish\win-x64\AES.UI.exe
```

---

## Publish — Self-Contained Executable

Produces a single `AES.UI.exe` with no .NET runtime dependency:

```powershell
dotnet publish src\AES.UI\AES.UI.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  --output publish\win-x64
```

Output: `publish\win-x64\AES.UI.exe`

---

## Project Structure

```
AES/
├── src/
│   ├── AES.Contracts/           # Shared interfaces and models
│   │   └── Models/              # Device, Artifact, Job, Project, LogEntry
│   ├── AES.Core/                # CoreEngine, EventBus, JobScheduler, Logger, ArtifactStore
│   ├── AES.Modules.ApkLab/      # APK decompilation module
│   ├── AES.Modules.FirmwareLab/ # Firmware unpacking module
│   ├── AES.Modules.DeviceBridge/# ADB controller + device monitor
│   ├── AES.Modules.Analyzer/    # Static DEX analysis
│   ├── AES.Modules.Workspace/   # Project management
│   ├── AES.UI/                  # Avalonia desktop shell + ViewModels
│   └── AES.Tests/               # xUnit test suite
├── tools/
│   └── platform-tools/          # Place adb.exe + fastboot.exe here
├── publish/                     # Self-contained build output (git-ignored)
├── launch.bat                   # Dev launcher (dotnet run)
├── launch-exe.bat               # Production launcher (prebuilt exe)
└── REBUILD-AES.ps1              # Full directory + file bootstrap script
```

---

## Running Tests

```powershell
dotnet test src\AES.Tests\AES.Tests.csproj --configuration Debug --logger "console;verbosity=normal"
```

---

## ADB Setup

1. Download [Android Platform Tools](https://developer.android.com/tools/releases/platform-tools)
2. Copy `adb.exe` and `fastboot.exe` into `tools\platform-tools\`
3. Enable **USB Debugging** on your Android device
4. Run `adb devices` to confirm the device is authorised
5. Launch AES — the Device Bridge panel will detect connected devices automatically

---

## Launchers

| File | Purpose |
|---|---|
| `launch.bat` | Development — calls `dotnet run`, rebuilds on every launch |
| `launch-exe.bat` | Production — starts the prebuilt Release or published exe directly |

---

## Contributing

```
feat/your-feature-name    # New features
fix/your-fix-description  # Bug fixes
chore/task-name           # Maintenance tasks
```

1. Fork the repository
2. Create a branch: `git checkout -b feat/my-feature`
3. Commit your changes: `git commit -m "feat: add my feature"`
4. Push: `git push origin feat/my-feature`
5. Open a Pull Request

---

## License

MIT — see [LICENSE](LICENSE) for details.
