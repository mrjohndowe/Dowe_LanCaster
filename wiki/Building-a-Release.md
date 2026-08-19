# Building a Release

The release automation builds the Windows x64 application, packages the Roku receiver, includes required media tools, and creates distributable ZIP files.

## Local release build

Close any running Dowe LanCaster instance, then run:

```powershell
.\BUILD-RELEASE.cmd
```

The underlying `scripts\Build-Release.ps1` process:

1. Confirms the .NET SDK is installed.
2. Installs missing FFmpeg and yt-dlp dependencies.
3. Packages the Roku receiver.
4. Publishes a self-contained Windows x64 .NET 8 build.
5. Copies FFmpeg, ffprobe, optional ffplay, yt-dlp, the Roku ZIP, and startup instructions into the Windows distribution.
6. Creates `dist\DoweLanCaster-Windows-x64.zip`.

## Release artifacts

- `dist\DoweLanCaster-Windows-x64.zip`: complete Windows distribution.
- `dist\DoweLanCaster-Roku.zip`: receiver channel for Roku developer-mode sideloading.

## CI artifacts

The `Build Dowe Lan Caster` GitHub Actions workflow produces two artifacts:

- `DoweLanCaster-Windows-x64`
- `DoweLanCaster-Roku`

The workflow currently builds artifacts; publishing a tagged GitHub Release is a separate repository-maintainer action.

## Version checklist

Before publishing a version:

- Update the project `Version`, `AssemblyVersion`, and `FileVersion` together.
- Update the README release heading and feature summary.
- Ensure startup instructions match the current features and version.
- Run the end-to-end checklist in [Development](Development).
- Sideload the newly generated Roku ZIP and verify all launch modes.
- Confirm the Windows package works after extraction on a clean x64 Windows environment.
- Verify the proprietary license remains in the distributed package as required by the maintainer.

