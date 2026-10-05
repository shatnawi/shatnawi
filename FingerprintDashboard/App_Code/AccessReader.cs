using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;

/// <summary>
/// Reads the remote Access file. The file is copied to a local cache first so the
/// fingerprint app's live file is never locked or touched, and the copy is opened read-only.
/// </summary>
public static class AccessReader
{
    private static readonly object CopyLock = new object();

    private static string Setting(string key)
    {
        return ConfigurationManager.AppSettings[key] ?? "";
    }

    private static string GetLocalCopy()
    {
        string src = Setting("AccessSourcePath");
        string folder = Setting("LocalCacheFolder");
        int seconds;
        if (!int.TryParse(Setting("CacheSeconds"), out seconds)) seconds = 60;

        Directory.CreateDirectory(folder);
        string local = Path.Combine(folder, Path.GetFileName(src));

        lock (CopyLock)
        {
            bool stale = !File.Exists(local) ||
                         (DateTime.UtcNow - File.GetLastWriteTimeUtc(local)).TotalSeconds > seconds;
            if (stale)
            {
                string tmp = local + ".tmp";
                File.Copy(src, tmp, true);               // reads over UNC share
                if (File.Exists(local)) File.Delete(local);
                File.Move(tmp, local);
                File.SetLastWriteTimeUtc(local, DateTime.UtcNow);
            }
        }
        return local;
    }

    private static OleDbConnection Open()
    {
        string path = GetLocalCopy();
        string provider = path.EndsWith(".mdb", StringComparison.OrdinalIgnoreCase)
            ? "Microsoft.ACE.OLEDB.12.0"   // also reads .mdb; use Microsoft.Jet.OLEDB.4.0 if app pool is 32-bit
            : "Microsoft.ACE.OLEDB.12.0";
        string cs = "Provider=" + provider + ";Data Source=" + path + ";Mode=Read;";
        string pwd = Setting("AccessPassword");
        if (pwd.Length > 0) cs += "Jet OLEDB:Database Password=" + pwd + ";";
        var con = new OleDbConnection(cs);
        con.Open();
        return con;
    }

    public static DateTime LastRefreshUtc()
    {
        string local = GetLocalCopy();
        return File.GetLastWriteTimeUtc(local);
    }

    public static List<string> GetTables()
    {
        using (var con = Open())
        {
            DataTable t = con.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
                new object[] { null, null, null, "TABLE" });
            return t.Rows.Cast<DataRow>()
                    .Select(r => (string)r["TABLE_NAME"])
                    .OrderBy(n => n).ToList();
        }
    }

    /// <summary>Returns up to MaxRows rows of the table, optionally filtered by a text search and date range.</summary>
    public static DataTable Preview(string table, string search, DateTime? from, DateTime? to)
    {
        int max;
        if (!int.TryParse(Setting("MaxRows"), out max)) max = 5000;

        // Table name is only ever used if it exists in the file's schema (prevents injection).
        if (!GetTables().Contains(table)) throw new ArgumentException("Unknown table.");

        using (var con = Open())
        {
            // Discover columns
            DataTable cols = con.GetOleDbSchemaTable(OleDbSchemaGuid.Columns,
                new object[] { null, null, table, null });
            var textCols = new List<string>();
            var dateCols = new List<string>();
            foreach (DataRow r in cols.Rows)
            {
                string name = (string)r["COLUMN_NAME"];
                int type = Convert.ToInt32(r["DATA_TYPE"]);
                if (type == 130 || type == 202 || type == 203) textCols.Add(name);       // WChar / VarWChar / LongVarWChar
                else if (type == 7 || type == 133 || type == 135) dateCols.Add(name);    // Date / DBDate / DBTimeStamp
            }

            var where = new List<string>();
            var cmd = con.CreateCommand();

            if (!string.IsNullOrWhiteSpace(search) && textCols.Count > 0)
            {
                var ors = new List<string>();
                foreach (string c in textCols)
                {
                    ors.Add("[" + c.Replace("]", "]]") + "] LIKE ?");
                    cmd.Parameters.AddWithValue("p", "%" + search.Trim() + "%");
                }
                where.Add("(" + string.Join(" OR ", ors) + ")");
            }
            if (dateCols.Count > 0 && (from.HasValue || to.HasValue))
            {
                string d = "[" + dateCols[0].Replace("]", "]]") + "]";   // first date column
                if (from.HasValue)
                {
                    where.Add(d + " >= ?");
                    cmd.Parameters.Add("f", OleDbType.Date).Value = from.Value.Date;
                }
                if (to.HasValue)
                {
                    where.Add(d + " < ?");
                    cmd.Parameters.Add("t", OleDbType.Date).Value = to.Value.Date.AddDays(1);
                }
            }

            string order = dateCols.Count > 0
                ? " ORDER BY [" + dateCols[0].Replace("]", "]]") + "] DESC" : "";
            cmd.CommandText = "SELECT TOP " + max + " * FROM [" + table.Replace("]", "]]") + "]" +
                              (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "") + order;

            var dt = new DataTable();
            using (var da = new OleDbDataAdapter(cmd)) da.Fill(dt);
            return dt;
        }
    }
}
