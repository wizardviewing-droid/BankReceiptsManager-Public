# Bank Receipts Manager – User Guide (Milestone 1)

## Installing the test version

1. Open the completed **Build Windows Installer** run on GitHub (Actions tab) and click the **BankReceiptsManager-Setup** artifact to download it (a .zip file).
2. Open the downloaded .zip and extract **BankReceiptsManager-Setup.exe** (right-click → Extract All).
3. Double-click **BankReceiptsManager-Setup.exe**. Because the installer is not code-signed, Windows SmartScreen may show "Windows protected your PC" – click **More info → Run anyway**.
4. Follow the wizard (no administrator rights needed). Tick "Launch Bank Receipts Manager" at the end, or start it from the Start menu.

## Overview
This is the first installable test version of the Bank Receipts Manager. It allows Accounts to import the daily Barclays BACS CHAPS RECEIPTS report, extract Proclaim references, and review allocations.

## Daily process (Milestone 1)

1. Download **BACS CHAPS RECEIPTS** from Barclays iPortal as the original `.xls` file.
2. Open **Bank Receipts Manager**.
3. Click **Import Bank Receipts**.
4. Browse to the downloaded file and click **Import**.
5. Confirm the number of receipts and total value.
6. Click **Review Allocations**.
7. Click **Run Matching**. References are extracted and matched against the live departmental reports configured in **Settings** (PI All cases.csv, HDR All cases - Balance review.csv, All cases Conv.csv and the Mailinfo workbook, by default in `Y:\accounts\All cases reports\`). There is no demonstration matching: if a report cannot be found or opened, matching stops and the error is shown in the status area.
8. Review any items marked “Requires Review” or “Unmatched”.
9. Manually correct extracted references in the grid where necessary.

## Important notes

- Only one person should process receipts at a time.
- The application does not send emails in this version.
- Unreferenced money, 14-day alerts, and company enquiries are not yet active.
- All data is currently stored locally for testing. Production storage on the Y: drive will be added in the next version.

## Troubleshooting

- “Could not locate the transaction header row” – ensure you are using the original Barclays iPortal export, not a modified copy.
- "Case report not found" / "Mailinfo file not found" – check the folder and file names in **Settings**, then click **Save Settings** and run matching again.
