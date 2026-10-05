# Fingerprint Records Dashboard (ASP.NET Web Forms, C#)

Read-only preview of the Access (.mdb/.accdb) file written by the fingerprint app, from a separate web server.
Open as a **Web Site** in Visual Studio (File > Open > Web Site) or deploy the folder to IIS.

## How it works
1. Copies the Access file over a UNC share to a local cache (refreshed at most every `CacheSeconds`).
   The live file is never opened by the dashboard, so the fingerprint app is not locked or corrupted.
2. Opens the cached copy read-only with OleDb, lists tables, and previews with search, date filter, sorting, paging, and CSV export.
   Table/column names are discovered automatically, so no schema knowledge is required.

## Setup
**On the fingerprint server:** share the folder holding the .mdb **read-only** to a dedicated account (e.g. `FP-SERVER\dashreader`).

**On the dashboard server:**
- Install *Microsoft Access Database Engine 2016 Redistributable* with the **same bitness as the IIS app pool**
  (64-bit by default; for 32-bit pools enable "Enable 32-bit applications"; for old `.mdb` you may use `Microsoft.Jet.OLEDB.4.0` in 32-bit only).
- Run the app pool under the account that has read access to the share (same-name/same-password local account on both machines, or a domain account),
  and give it modify rights to `LocalCacheFolder`.
- Edit `Web.config` appSettings (`AccessSourcePath`, `LocalCacheFolder`, ...).
- Windows authentication is enabled and anonymous denied; restrict further with `<authorization>` roles and use HTTPS, since this is employee data.

## Next steps
Once you know the real table/column names (e.g. `CheckInOut`: `UserID`, `CheckTime`), add summary cards
(late arrivals, hours per employee) as dedicated queries in `AccessReader`.
