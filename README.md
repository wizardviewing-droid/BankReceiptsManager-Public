# Bank Receipts Manager – Milestone 1

## Status of this delivery

The Windows installer (`BankReceiptsManager-Setup.exe`) is produced by the GitHub Actions workflow
`.github/workflows/build-installer.yml` on a Windows runner and published as the `BankReceiptsManager-Setup` artifact.

**What has been delivered**  
- Complete source code for **Milestone 1** with **real** (not demo) reference matching against the four live sources.  
- Configurable paths (Settings screen) defaulting to `Y:\accounts\All cases reports\`.  
- Confirmed Barclays `.xls` import structure.  
- GitHub Actions Windows workflow (`.github/workflows/build-installer.yml`) and Inno Setup script that produce a standard installer.  
- Email sending is disabled.

**To obtain the installer**  
Push this repository to GitHub and run the “Build Windows Installer” workflow. Download the artifact. See `docs/Build-Instructions.md` for the exact steps.

## Confirmed Barclays export structure

From the supplied `BACS CHAPS RECEIPTS (1).xls`:

| Field                  | Column (0-based) | Notes                                      |
|------------------------|------------------|--------------------------------------------|
| Statement / Entry Date | 1                | dd/MM/yyyy                                 |
| Transaction Type       | 3                |                                            |
| Transaction Details    | 7                | May wrap to following row                  |
| Value Date             | 12               |                                            |
| Bank Reference         | 17               | Often empty                                |
| Customer Reference     | 20               |                                            |
| Receipt Amount         | 33               | Use decimal arithmetic                     |

Header row is the first row containing “Statement / Entry Date”. Non-transaction rows (totals, blanks, headers) are skipped.

## Building on Windows

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or later).  
2. Open `BankReceiptsManager.sln` in Visual Studio 2022 (or later) **or** use the command line:

```bat
cd BankReceiptsManager
dotnet restore
dotnet build -c Release
dotnet publish src\BankReceiptsManager\BankReceiptsManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

3. The self-contained executable will be in the `publish` folder.  
4. For a proper installer, use Inno Setup or WiX on the published output.

## Next steps (Milestone 2+)

- Replace the JSON history with ClosedXML Excel writes to `Y:\accounts\Receipts Manager\`.  
- Implement processing lock.  
- Add Outlook COM or Microsoft.Office.Interop.Outlook for drafts.  
- Build remaining navigation items.

## Testing notes

The import service was validated against the structure of the supplied anonymised file. Automated unit tests for the regex extractor and duplicate key logic should be added before production use.
