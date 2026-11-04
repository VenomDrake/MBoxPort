# MboxPort

**Free, local MBOX to PST conversion for Windows. No Outlook required.**

[Italiano](README.it.md) · [Changes](CHANGELOG.md) · [License](LICENSE) · [Third-party notices](THIRD_PARTY.md)

[Download the latest release](https://github.com/VenomDrake/MBoxPort/releases/latest)

MboxPort converts one standard MBOX archive at a time into Unicode PST files. Messages and attachments stay on your computer. The Windows application includes its .NET runtime.

## Use

1. Extract the entire `MboxPort-1.0.1-Portatile-x64.zip` archive and run `MboxPort.exe`, or use the installer when one is provided.
2. Choose a file, or drop one MBOX into the window. Archives without a filename extension are supported.
3. Select an existing destination folder and an approximate PST part size: 10, 20 or 40 GiB.
4. Start the conversion. Each run creates its own results folder.
5. Open the results and review the report before relying on an archive with warnings or skipped messages.

Requires Windows 10/11 x64. The executable is not digitally signed.

## Languages

The language selector provides **10 languages**, including Italian. The initial choice follows the system language when supported; otherwise it uses English. A selected language is remembered locally.

| Language | Code |
| --- | --- |
| English | `en` |
| Italiano | `it` |
| 简体中文 | `zh-Hans` |
| हिन्दी | `hi` |
| Español | `es` |
| العربية | `ar` |
| Français | `fr` |
| বাংলা | `bn` |
| Português | `pt` |
| Bahasa Indonesia | `id` |

The interface, application messages and human-readable conversion report use the selected language. Arabic uses a right-to-left layout. Low-level engine and operating-system diagnostic details can remain in their original language. JSON property names and report filenames remain stable across languages.

## Results and cancellation

- PST files are split automatically. The selected size is approximate: overhead and an individual large message can exceed it.
- `rapporto.txt` is the human-readable report; `rapporto-dettagliato.json` contains engine details; `sessione.json` records the run status and final counters.
- A source read failure stops the conversion. Completed PST parts may remain, but they are partial results.
- Cancellation discards the current incomplete PST and retains earlier completed parts. Saved-message counts refer only to retained files; messages discarded with the incomplete part are reported separately.
- The application checks final message accounting and reopens completed PSTs to verify their stored item counts. These checks do not guarantee the visual rendering of every message in Outlook.
- Each conversion reads the original file without modifying it. There is no automatic resume.

## Compatibility and limits

Only standard MBOX/MIME email is imported by this interface. Extract compressed exports before selecting the MBOX. Provider-specific variants require testing with a sample. An isolated MBOX does not necessarily contain the source folder structure, labels, read states, contacts or calendars.

Input files can exceed 20 GiB, but a complete real-world conversion above 20 GiB has not been validated. Individual messages above approximately 1900 MiB are skipped and reported. Large-message parsing can require substantial memory, and cancellation can wait for the current parsing operation. Allow disk space for both PST output and temporary attachments. Use a filesystem that supports large files.

## Build from source

Requires Windows x64, .NET SDK **8.0.427** or a later patch allowed by `global.json`, and NuGet access for the first restore.

```powershell
./build.ps1
```

The script performs locked `win-x64` restores, publishes the self-contained app into a clean staging directory, runs application and selected engine regression tests, and converts 240 synthetic messages. It creates source and portable ZIP packages and `SHA256SUMS.txt` under `artifacts/`. Inno Setup is used to create an installer when available:

```powershell
./build.ps1 -InnoCompiler 'C:\path\to\Inno Setup 6\ISCC.exe'
```

Product name and version are defined in `src/Directory.Build.props`. Full source for ContinuMail and its PST engine is included. NuGet dependency versions are recorded in lock files. Build tools and NuGet packages are not bundled as an offline SDK.

After `build.ps1` has restored dependencies, individual checks can be run as follows:

```powershell
dotnet test tests/PostaBridge.Tests/PostaBridge.Tests.csproj -c Release -r win-x64 --no-restore
dotnet build third_party/ContinuMail/src/Mail2Pst.Cli/Mail2Pst.Cli.csproj -c Release --no-restore
dotnet test third_party/ContinuMail/tests/Mail2Pst.Core.Tests/Mail2Pst.Core.Tests.csproj -c Release -r win-x64 --no-restore
```

The separate CLI build supplies the path expected by upstream end-to-end tests. It does not change the app's restore configuration. See `VALIDAZIONE.txt` for checks actually performed and their limits.

An optional independent read-back script is included in `tools/verify_independent.py`. It requires Python 3.12+ and `pypstreader==1.0.0`; it operates on the synthetic SmokeTest output.

## Origin and license

MboxPort was previously named **PostaBridge**. It is an independent integration based on [ContinuMail](https://github.com/ContinuMail/continumail-converter), with [PSTFileFormat](https://github.com/ROM-Knowledgeware/PSTFileFormat). It is distributed under **GNU GPL v3 or later**, without warranty. The PST library retains its LGPL terms. Existing copyright notices, licenses and source attribution are preserved in [THIRD_PARTY.md](THIRD_PARTY.md) and the included upstream source.

Release archives include the corresponding source code and license notices.
