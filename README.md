# WebSnap

[![Build](https://github.com/UBGarza/websnap/actions/workflows/build.yml/badge.svg)](https://github.com/UBGarza/websnap/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

WebSnap is a small .NET 10 console app that opens a web page in headless Chromium using [Microsoft Playwright](https://playwright.dev/dotnet/), dismisses a cookie/GDPR consent popup if you ask it to, and saves a PNG screenshot.

```text
dotnet run -- https://www.msn.com "Accept all"
# -> 20261125_193012_www_msn_com_443.png
```

## Features

- Full-page or viewport-only screenshots.
- Optional dismissal of a cookie consent banner by the button's accessible name (for example, `Accept all`).
- Waits for the network to go idle before capturing, so late-loading content is included.
- Layered configuration: built-in defaults, an optional `appsettings.json`, environment variables, and command-line arguments.

## Requirements

| Requirement | Notes |
| --- | --- |
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | Needed to build and run. The repo's `global.json` asks for SDK 10.0.100 or a later 10.0 feature band. |
| [PowerShell 7+](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) (`pwsh`) | Needed once, to run the Playwright browser install script. This works on Windows, macOS, and Linux. |
| Internet access | Needed for the one-time downloads listed below. |

## Downloads

Two things get downloaded. Neither one is committed to the repository.

1. **NuGet packages.** These download automatically the first time you run `dotnet restore`, `dotnet build`, or `dotnet run`. They go into your NuGet cache (`~/.nuget/packages`):
   - `Microsoft.Playwright`
   - `Microsoft.Extensions.Configuration` (with its `.Json`, `.EnvironmentVariables`, `.CommandLine`, and `.Binder` packages)
2. **Playwright browser binaries.** These are **not** installed automatically. Run the install script that the build generates (see [Getting started](#getting-started)). It downloads a Chromium build matched to the Playwright package version, which is a few hundred MB, into a shared per-user cache:
   - Windows: `%USERPROFILE%\AppData\Local\ms-playwright`
   - macOS: `~/Library/Caches/ms-playwright`
   - Linux: `~/.cache/ms-playwright`

   To put the browsers somewhere else, set `PLAYWRIGHT_BROWSERS_PATH`. After you upgrade the `Microsoft.Playwright` package, run the install script again, because each Playwright version expects a specific browser build.

## Getting started

```bash
# 1. Clone
git clone https://github.com/UBGarza/websnap.git
cd websnap

# 2. Build (this also restores the NuGet packages)
dotnet build

# 3. Install the Chromium browser for Playwright (one time)
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
#    On Linux, add --with-deps to install the OS libraries Chromium needs (uses sudo):
#    pwsh bin/Debug/net10.0/playwright.ps1 install --with-deps chromium

# 4. Take a screenshot
dotnet run -- https://www.msn.com
```

The screenshot is written to the **current working directory**. With `dotnet run`, that's the folder you run the command from. The file name follows this pattern:

```text
{yyyyMMdd_HHmmss}_{host with dots replaced by underscores}_{port}.png
```

For example: `20261125_193012_www_msn_com_443.png`.

## Configuration

All settings are in the `WebSnap` section. **Every setting is optional.** If nothing is configured, the defaults below apply.

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `Url` | string | `http://localhost:5173` | Page to capture. Must be an absolute URL. |
| `CookieButtonName` | string or `null` | `null` | Accessible name of the consent button to click (for example, `Accept all`). Leave it empty to skip this step. If the button isn't found before the timeout, WebSnap continues without clicking anything. |
| `FullPage` | bool | `true` | `true` captures the whole scrollable page. `false` captures only the viewport. |
| `TimeoutMs` | int | `3000` | How long, in milliseconds, to wait for the consent button. |

### Where settings come from

Settings are read from the sources below, in this order. **When the same setting appears in more than one source, the later source wins.**

1. **Built-in defaults** in [`WebSnapOptions.cs`](WebSnapOptions.cs).
2. **`appsettings.json`** (optional). The build copies it next to the executable. If you delete it or leave a setting out, nothing breaks; the setting just falls back to the default.

   ```json
   {
     "WebSnap": {
       "Url": "https://www.msn.com",
       "CookieButtonName": "Accept all",
       "FullPage": true,
       "TimeoutMs": 3000
     }
   }
   ```

3. **Environment variables** that start with `WEBSNAP_`. WebSnap removes that prefix and reads the rest as a setting name. Use a double underscore (`__`) where JSON would use a nested section, so the full name is `WEBSNAP_` + `WebSnap__<Setting>`:

   ```bash
   # bash / zsh
   export WEBSNAP_WebSnap__Url=https://www.msn.com
   export WEBSNAP_WebSnap__FullPage=false
   ```

   ```powershell
   # PowerShell
   $env:WEBSNAP_WebSnap__Url = "https://www.msn.com"
   ```

4. **Named command-line arguments.** These use the full setting path. Any of these forms works:

   ```bash
   dotnet run -- --WebSnap:Url https://www.msn.com --WebSnap:FullPage false
   dotnet run -- --WebSnap:Url=https://www.msn.com
   dotnet run -- WebSnap:TimeoutMs=5000
   ```

5. **Positional command-line arguments.** This is a shortcut for the two most common settings. The first plain value sets `Url` and the second sets `CookieButtonName`:

   ```bash
   dotnet run -- https://www.msn.com "Accept all"
   ```

   You can mix positional and named arguments, but positional values have to come first. WebSnap stops treating values as positional at the first one that starts with `-` or `/` or contains `=`.

   > **URLs with query strings:** a URL that contains `=` (for example, `https://www.msn.com/?q=1`) can't be passed as a positional argument because WebSnap treats it as a named argument. Pass it with `--WebSnap:Url "https://www.msn.com/?q=1"` instead.

### Examples

```bash
# Only the viewport, with a longer wait for the consent button
dotnet run -- https://www.msn.com "Accept all" --WebSnap:FullPage false --WebSnap:TimeoutMs 8000
```

## Running the compiled app

`dotnet build` puts two runnable files in `bin/Debug/net10.0/`:

- **`websnap.exe`**: a native launcher. On Linux and macOS, it's an extensionless file named `websnap`.
- **`websnap.dll`**: the compiled app, which you run with `dotnet`.

Both take the same arguments and both need the .NET 10 runtime installed.

```powershell
# Windows
.\bin\Debug\net10.0\websnap.exe https://www.msn.com "Accept all"
```

```bash
# Linux / macOS
./bin/Debug/net10.0/websnap https://www.msn.com "Accept all"

# Any OS
dotnet bin/Debug/net10.0/websnap.dll https://www.msn.com "Accept all"
```

### Checking the version

```bash
websnap --version
# websnap 1.0.0+1837e7c
```

The part after `+` is the git commit the app was built from. On Windows, you can also see the version in File Explorer: right-click `websnap.exe` → **Properties** → **Details**.

The version is set by `<Version>` in `WebSnap.csproj`. To override it for a release build, pass `-p:Version=1.2.0` to `dotnet build` or `dotnet publish`.

## Publishing a release build

### Option 1: Framework-dependent (smaller; needs the .NET 10 runtime)

```bash
dotnet publish -c Release -o out
pwsh out/playwright.ps1 install chromium   # browsers are a separate download
```

```powershell
.\out\websnap.exe https://www.msn.com
```

### Option 2: Self-contained (no .NET install needed)

This bundles the .NET runtime with the app, so it runs on a machine that doesn't have .NET installed. The output is larger (roughly 70 MB or more), and you have to publish separately for each OS and CPU type, using [runtime identifiers](https://learn.microsoft.com/dotnet/core/rid-catalog) like `win-x64`, `win-arm64`, `linux-x64`, and `osx-arm64`.

```bash
dotnet publish -c Release -r win-x64 --self-contained -o out/win-x64
```

```powershell
pwsh out\win-x64\playwright.ps1 install chromium
.\out\win-x64\websnap.exe https://www.msn.com
```

> **Note:** Even when self-contained, the output is a **folder**, not a single file. Copy the whole folder, including the `.playwright` subfolder (Playwright's driver), to the target machine. Chromium isn't bundled either, so run `playwright.ps1 install chromium` once on each machine. That step needs PowerShell 7 (`pwsh`).

## Project layout

```text
.
├── Program.cs            # Builds the configuration, runs Playwright, takes the screenshot
├── WebSnapOptions.cs     # Strongly typed settings with defaults
├── appsettings.json      # Optional settings file (copied to the output folder)
├── WebSnap.csproj
└── global.json           # Pins the .NET SDK major version
```

## Contributing

Issues and pull requests are welcome. Before you open a PR, please:

1. Run `dotnet build` and make sure it finishes with no warnings.
2. Run `dotnet format` so the code follows the style in [`.editorconfig`](.editorconfig).

## License

[MIT](LICENSE)
