# Bank Receipts Manager – Milestone 1

## Version 1.2 (completed bulk allocation UI)

- Clicking **Split** expands a fully functional allocation panel under the receipt.
- Add / edit / remove allocation lines; enter reference + amount; Match each line.
- Running total, remaining balance, and over-allocation warning displayed.
- Fully Allocated only when remaining is exactly £0.00; drafts allowed for partials.
- Manual file handler and Manual Reference are protected on re-run; **Clear** resets them.
- Unit tests cover full / partial / over-allocation totals.
- Email sending remains disabled.

Push to GitHub and run **Build Windows Installer**. The workflow now runs the unit tests before packaging.

- Per-row **Match** button for Unmatched / Requires Review (uses Manual Reference).
- **Clear Manual Allocation**.
- **Expand / Split Receipt** for bulk remittances: parent bank transaction + child allocation lines.
- Continuous reconciliation (original − allocated = remaining). Fully / Partially Allocated statuses. Decimal arithmetic. Over-allocation warning.
- Each allocation line matched independently (Conveyancing override applies). Manual choices preserved.
- Duplicate detection remains on the original parent transaction.
- Email sending still disabled (notifications will be per allocation line when enabled).

Push to GitHub and run **Build Windows Installer** for the v1.2 setup executable. Settings/history in `%LocalAppData%\BankReceiptsManager` are preserved on upgrade.

- Dashboard is now the central daily workspace (Import & Match button, summary cards, full receipt table on one screen).
- UK GBP formatting (£1,929,722.28) and en-GB culture.
- Conveyancing receipts always notify `team_conveyancing@true.co.uk` (configurable in Settings); fee earner still shown.
- Editable File Handler dropdown populated from Mailinfo; Manual Reference column that takes priority.
- Improved extraction (finds 675427 inside LHA675427, etc.).
- Remove from batch with confirmation (does not delete history).
- Email sending remains disabled.

**To obtain the installer:** push to GitHub and run the “Build Windows Installer” workflow. Download the artifact. Settings and local history are preserved on upgrade because they live in `%LocalAppData%\BankReceiptsManager`.

**Build**  
The Windows installer (`BankReceiptsManager-Setup.exe`) is produced by the GitHub Actions workflow
`.github/workflows/build-installer.yml` on a Windows runner (unit tests run first) and published as the `BankReceiptsManager-Setup` artifact.

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
