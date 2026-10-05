<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>
<!DOCTYPE html>
<html>
<head runat="server">
  <meta charset="utf-8" />
  <title>Attendance Dashboard</title>
  <style>
    body { font-family: Segoe UI, Arial, sans-serif; margin: 20px; background: #f5f6f8; color: #222; }
    .bar { background: #fff; padding: 12px; border-radius: 6px; margin-bottom: 12px; }
    .bar > * { margin-right: 10px; }
    .kpis { display: flex; gap: 12px; margin-bottom: 12px; flex-wrap: wrap; }
    .kpi { background: #fff; border-radius: 6px; padding: 12px 18px; min-width: 130px; }
    .kpi b { display: block; font-size: 26px; color: #2b579a; } .kpi span { color: #666; font-size: 12px; }
    .grid { background: #fff; border-collapse: collapse; width: 100%; }
    .grid th { background: #2b579a; color: #fff; padding: 6px 8px; text-align: left; }
    .grid th a { color: #fff; } .grid td { padding: 5px 8px; border-bottom: 1px solid #e3e3e3; }
    .grid tr:nth-child(even) td { background: #fafafa; } .grid .pager td { text-align: center; }
    .daybar { display: inline-block; height: 10px; background: #2b579a; border-radius: 2px; vertical-align: middle; }
    .msg { color: #b00020; } .info { color: #555; }
    h3 { margin: 18px 0 6px; }
  </style>
</head>
<body>
<form id="form1" runat="server">
  <h2>Attendance Dashboard</h2>
  <div class="bar">
    From: <asp:TextBox ID="txtFrom" runat="server" TextMode="Date" />
    To: <asp:TextBox ID="txtTo" runat="server" TextMode="Date" />
    Department: <asp:DropDownList ID="ddlDept" runat="server" />
    Name/Badge: <asp:TextBox ID="txtSearch" runat="server" Width="120" />
    <asp:Button ID="btnApply" runat="server" Text="Apply" OnClick="Apply" />
    <asp:Button ID="btnCsv" runat="server" Text="Export CSV" OnClick="btnCsv_Click" />
    <a href="Raw.aspx">Raw tables</a>
  </div>
  <asp:Label ID="lblMsg" runat="server" CssClass="msg" EnableViewState="false" />
  <asp:Literal ID="litKpis" runat="server" EnableViewState="false" />
  <h3>Present employees per day</h3>
  <asp:Literal ID="litDays" runat="server" EnableViewState="false" />
  <h3>Daily attendance</h3>
  <asp:Label ID="lblInfo" runat="server" CssClass="info" EnableViewState="false" />
  <asp:GridView ID="gv" runat="server" CssClass="grid" AllowPaging="true" PageSize="30"
      AllowSorting="true" AutoGenerateColumns="true" EnableViewState="false"
      OnPageIndexChanging="gv_PageIndexChanging" OnSorting="gv_Sorting">
    <PagerStyle CssClass="pager" />
  </asp:GridView>
</form>
</body>
</html>
