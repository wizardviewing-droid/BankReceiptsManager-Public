# Bank Receipts Manager – User Guide (Milestone 1)

## Overview
This is the first installable test version of the Bank Receipts Manager. It allows Accounts to import the daily Barclays BACS CHAPS RECEIPTS report, extract Proclaim references, and review allocations.

## Daily process (Milestone 1)

1. Download **BACS CHAPS RECEIPTS** from Barclays iPortal as the original `.xls` file.
2. Open **Bank Receipts Manager**.
3. Click **Import Bank Receipts**.
4. Browse to the downloaded file and click **Import**.
5. Confirm the number of receipts and total value.
6. Click **Review Allocations**.
7. Click **Run Matching** to extract references and apply matching (currently in demo mode – live departmental reports will be used once configured).
8. Review any items marked “Requires Review” or “Unmatched”.
9. Manually correct extracted references in the grid where necessary.

## Important notes

- Only one person should process receipts at a time.
- The application does not send emails in this version.
- Unreferenced money, 14-day alerts, and company enquiries are not yet active.
- All data is currently stored locally for testing. Production storage on the Y: drive will be added in the next version.

## Troubleshooting

- “Could not locate the transaction header row” – ensure you are using the original Barclays iPortal export, not a modified copy.
- Matching shows demo results – live PI / HDR / Conveyancing / Mailinfo files have not yet been connected.
