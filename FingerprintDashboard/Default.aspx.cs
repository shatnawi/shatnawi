using System;
using System.Data;
using System.Text;
using System.Web.UI;

public partial class _Default : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (IsPostBack) return;
        try
        {
            ddlTable.DataSource = AccessReader.GetTables();
            ddlTable.DataBind();
            Bind();
        }
        catch (Exception ex) { lblMsg.Text = Server.HtmlEncode(ex.Message); }
    }

    private DataTable Load()
    {
        DateTime f, t;
        DateTime? from = DateTime.TryParse(txtFrom.Text, out f) ? f : (DateTime?)null;
        DateTime? to = DateTime.TryParse(txtTo.Text, out t) ? t : (DateTime?)null;
        return AccessReader.Preview(ddlTable.SelectedValue, txtSearch.Text, from, to);
    }

    private void Bind()
    {
        try
        {
            DataTable dt = Load();
            string sort = (string)ViewState["sort"];
            if (!string.IsNullOrEmpty(sort)) dt.DefaultView.Sort = sort;
            gv.DataSource = dt.DefaultView;
            gv.DataBind();
            lblInfo.Text = dt.Rows.Count + " rows &middot; data as of " +
                           AccessReader.LastRefreshUtc().ToLocalTime().ToString("g");
        }
        catch (Exception ex) { lblMsg.Text = Server.HtmlEncode(ex.Message); }
    }

    protected void Reload(object sender, EventArgs e)
    {
        gv.PageIndex = 0;
        ViewState["sort"] = null;
        Bind();
    }

    protected void gv_PageIndexChanging(object sender, System.Web.UI.WebControls.GridViewPageEventArgs e)
    {
        gv.PageIndex = e.NewPageIndex;
        Bind();
    }

    protected void gv_Sorting(object sender, System.Web.UI.WebControls.GridViewSortEventArgs e)
    {
        string cur = (string)ViewState["sort"];
        string next = e.SortExpression + " ASC";
        if (cur == next) next = e.SortExpression + " DESC";
        ViewState["sort"] = next;
        Bind();
    }

    protected void btnCsv_Click(object sender, EventArgs e)
    {
        DataTable dt = Load();
        var sb = new StringBuilder();
        for (int i = 0; i < dt.Columns.Count; i++)
            sb.Append(i > 0 ? "," : "").Append(Csv(dt.Columns[i].ColumnName));
        sb.AppendLine();
        foreach (DataRow r in dt.Rows)
        {
            for (int i = 0; i < dt.Columns.Count; i++)
                sb.Append(i > 0 ? "," : "").Append(Csv(Convert.ToString(r[i])));
            sb.AppendLine();
        }
        Response.Clear();
        Response.ContentType = "text/csv";
        Response.AddHeader("Content-Disposition", "attachment; filename=" + ddlTable.SelectedValue + ".csv");
        Response.BinaryWrite(Encoding.UTF8.GetPreamble());
        Response.BinaryWrite(Encoding.UTF8.GetBytes(sb.ToString()));
        Response.End();
    }

    private static string Csv(string s)
    {
        s = s ?? "";
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
