using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class _Default : Page
{
    private const string DateFmt = "yyyy-MM-dd";

    protected void Page_Load(object sender, EventArgs e)
    {
        if (IsPostBack) return;
        try
        {
            DataTable depts = AccessReader.Departments();
            ddlDept.Items.Add(new ListItem("All departments", ""));
            foreach (DataRow r in depts.Rows)
                ddlDept.Items.Add(new ListItem(Convert.ToString(r["DEPTNAME"]), Convert.ToString(r["DEPTID"])));

            DateTime last = AccessReader.LastPunchDate() ?? DateTime.Today;
            txtTo.Text = last.ToString(DateFmt);
            txtFrom.Text = last.AddDays(-6).ToString(DateFmt);   // last week of available data
            Bind();
        }
        catch (Exception ex) { lblMsg.Text = Server.HtmlEncode(ex.Message); }
    }

    private int? DeptId()
    {
        int d;
        return int.TryParse(ddlDept.SelectedValue, out d) ? d : (int?)null;
    }

    /// <summary>One row per employee per day: first punch, last punch, hours between them.</summary>
    private DataTable BuildSummary(out int punchCount, out DateTime from, out DateTime to)
    {
        DateTime f, t;
        if (!DateTime.TryParse(txtFrom.Text, out f)) f = DateTime.Today;
        if (!DateTime.TryParse(txtTo.Text, out t)) t = f;
        if (t < f) { var x = f; f = t; t = x; }
        from = f; to = t;

        DataTable p = AccessReader.Punches(f, t, DeptId());
        punchCount = p.Rows.Count;
        string q = (txtSearch.Text ?? "").Trim().ToLowerInvariant();

        var dt = new DataTable();
        dt.Columns.Add("Date", typeof(string));
        dt.Columns.Add("Badge", typeof(string));
        dt.Columns.Add("Name", typeof(string));
        dt.Columns.Add("Department", typeof(string));
        dt.Columns.Add("First punch", typeof(string));
        dt.Columns.Add("Last punch", typeof(string));
        dt.Columns.Add("Hours", typeof(double));
        dt.Columns.Add("Punches", typeof(int));

        var groups = p.Rows.Cast<DataRow>()
            .GroupBy(r => new { U = Convert.ToString(r["USERID"]), D = ((DateTime)r["CHECKTIME"]).Date });
        foreach (var g in groups.OrderBy(g => g.Key.D).ThenBy(g => g.Key.U))
        {
            DataRow first = g.First();
            string name = first["Name"] is DBNull ? "User #" + g.Key.U : Convert.ToString(first["Name"]);
            string badge = first["Badgenumber"] is DBNull ? g.Key.U : Convert.ToString(first["Badgenumber"]);
            if (q.Length > 0 && name.ToLowerInvariant().IndexOf(q) < 0 && badge.ToLowerInvariant().IndexOf(q) < 0)
                continue;
            DateTime min = g.Min(r => (DateTime)r["CHECKTIME"]);
            DateTime max = g.Max(r => (DateTime)r["CHECKTIME"]);
            int n = g.Count();
            DataRow row = dt.NewRow();
            row["Date"] = g.Key.D.ToString(DateFmt);
            row["Badge"] = badge;
            row["Name"] = name;
            row["Department"] = first["DEPTNAME"] is DBNull ? "" : Convert.ToString(first["DEPTNAME"]);
            row["First punch"] = min.ToString("HH:mm");
            row["Last punch"] = n > 1 ? max.ToString("HH:mm") : "";
            row["Hours"] = n > 1 ? Math.Round((max - min).TotalHours, 2) : 0.0;
            row["Punches"] = n;
            dt.Rows.Add(row);
        }
        return dt;
    }

    private void Bind()
    {
        try
        {
            int punches; DateTime from, to;
            DataTable dt = BuildSummary(out punches, out from, out to);

            // KPIs
            int emp = AccessReader.EmployeeCount(DeptId());
            int presentPeople = dt.AsEnumerable().Select(r => r.Field<string>("Badge")).Distinct().Count();
            var kp = new StringBuilder();
            Kpi(kp, emp.ToString(), "Employees");
            Kpi(kp, presentPeople.ToString(), "Employees with punches");
            if (from == to) Kpi(kp, Math.Max(0, emp - presentPeople).ToString(), "No punch that day");
            Kpi(kp, punches.ToString(), "Total punches");
            var withHours = dt.AsEnumerable().Where(r => r.Field<int>("Punches") > 1).ToList();
            Kpi(kp, withHours.Count == 0 ? "-" : withHours.Average(r => r.Field<double>("Hours")).ToString("0.0"),
                "Avg hours / day");
            litKpis.Text = "<div class='kpis'>" + kp + "</div>";

            // present per day
            var days = dt.AsEnumerable().GroupBy(r => r.Field<string>("Date"))
                         .Select(g => new { Day = g.Key, N = g.Select(r => r.Field<string>("Badge")).Distinct().Count() })
                         .ToList();
            int maxN = days.Count == 0 ? 1 : days.Max(d => d.N);
            var sb = new StringBuilder("<table class='grid'>");
            foreach (var d in days)
                sb.Append("<tr><td style='width:110px'>" + d.Day + "</td><td style='width:50px'>" + d.N +
                          "</td><td><span class='daybar' style='width:" + (d.N * 100 / maxN) + "%'></span></td></tr>");
            litDays.Text = days.Count == 0 ? "<p class='info'>No punches in this range.</p>" : sb + "</table>";

            string sort = (string)ViewState["sort"];
            if (!string.IsNullOrEmpty(sort)) dt.DefaultView.Sort = sort;
            gv.DataSource = dt.DefaultView;
            gv.DataBind();
            lblInfo.Text = dt.Rows.Count + " rows &middot; data as of " +
                           AccessReader.LastRefreshUtc().ToLocalTime().ToString("g");
        }
        catch (Exception ex) { lblMsg.Text = Server.HtmlEncode(ex.Message); }
    }

    private static void Kpi(StringBuilder sb, string value, string label)
    {
        sb.Append("<div class='kpi'><b>" + value + "</b><span>" + label + "</span></div>");
    }

    protected void Apply(object sender, EventArgs e)
    {
        gv.PageIndex = 0;
        ViewState["sort"] = null;
        Bind();
    }

    protected void gv_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        gv.PageIndex = e.NewPageIndex;
        Bind();
    }

    protected void gv_Sorting(object sender, GridViewSortEventArgs e)
    {
        string next = "[" + e.SortExpression + "] ASC";
        if ((string)ViewState["sort"] == next) next = "[" + e.SortExpression + "] DESC";
        ViewState["sort"] = next;
        Bind();
    }

    protected void btnCsv_Click(object sender, EventArgs e)
    {
        int punches; DateTime from, to;
        DataTable dt = BuildSummary(out punches, out from, out to);
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", dt.Columns.Cast<DataColumn>().Select(c => Csv(c.ColumnName))));
        foreach (DataRow r in dt.Rows)
            sb.AppendLine(string.Join(",", r.ItemArray.Select(v => Csv(Convert.ToString(v)))));
        Response.Clear();
        Response.ContentType = "text/csv";
        Response.AddHeader("Content-Disposition", "attachment; filename=attendance_" + from.ToString(DateFmt) + "_" + to.ToString(DateFmt) + ".csv");
        Response.BinaryWrite(Encoding.UTF8.GetPreamble());
        Response.BinaryWrite(Encoding.UTF8.GetBytes(sb.ToString()));
        Response.End();
    }

    private static string Csv(string s)
    {
        return "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
    }
}
