# Building the Windows Installer via GitHub Actions

I cannot execute a Windows build or GitHub Actions workflow from this Linux sandbox. No downloadable installer has been produced.

## Exact automated process

1. Create a GitHub repository and push the contents of the `BankReceiptsManager` folder (including `.github/workflows/build-installer.yml` and `installer/`).

2. On GitHub, go to **Actions → Build Windows Installer → Run workflow**.

3. The workflow (runs on `windows-latest`):
   - Restores and publishes a self-contained win-x64 single-file executable.
   - Installs Inno Setup via Chocolatey.
   - Compiles `installer/BankReceiptsManager.iss`.
   - Uploads `BankReceiptsManager-Setup.exe` as a workflow artifact.

4. Download the artifact from the completed workflow run. It is a standard Windows installer that creates Start menu / optional desktop shortcuts and supports uninstall.

## What the Milestone 1 build contains

- Direct import of the original Barclays `.xls` (columns confirmed earlier).
- Real matching against the four live sources under the configurable `Y:\accounts\All cases reports\` folder:
  - PI All cases.csv (A → D)
  - HDR All cases - Balance review.csv (A → B)
  - All cases Conv.csv (A → C)
  - Mailinfo workbook (A → B)
- Paths are editable in Settings and persisted.
- Occurrence-based duplicate detection.
- Review Allocations screen with filters and manual correction.
- Email sending is hard-disabled.

## Local Windows alternative (if you prefer not to use GitHub)

```bat
dotnet publish src\BankReceiptsManager\BankReceiptsManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\BankReceiptsManager.iss
```

The installer will appear in `installer\Output\`.
