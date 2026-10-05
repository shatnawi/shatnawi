<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Raw.aspx.cs" Inherits="_Raw" %>
<!DOCTYPE html>
<html>
<head runat="server">
  <meta charset="utf-8" />
  <title>Fingerprint Records Dashboard</title>
  <style>
    body { font-family: Segoe UI, Arial, sans-serif; margin: 20px; background: #f5f6f8; }
    .bar { background: #fff; padding: 12px; border-radius: 6px; margin-bottom: 12px; }
    .bar > * { margin-right: 10px; }
    .grid { background: #fff; border-collapse: collapse; width: 100%; }
    .grid th { background: #2b579a; color: #fff; padding: 6px 8px; text-align: left; }
    .grid td { padding: 5px 8px; border-bottom: 1px solid #e3e3e3; }
    .grid tr:nth-child(even) td { background: #fafafa; }
    .grid .pager td { text-align: center; }
    .msg { color: #b00020; } .info { color: #555; }
  </style>
</head>
<body>
<form id="form1" runat="server">
  <h2>Raw table browser</h2><p><a href="Default.aspx">&larr; Attendance dashboard</a></p>
  <div class="bar">
    Table: <asp:DropDownList ID="ddlTable" runat="server" AutoPostBack="true" OnSelectedIndexChanged="Reload" />
    Search: <asp:TextBox ID="txtSearch" runat="server" Width="140" />
    From: <asp:TextBox ID="txtFrom" runat="server" TextMode="Date" />
    To: <asp:TextBox ID="txtTo" runat="server" TextMode="Date" />
    <asp:Button ID="btnApply" runat="server" Text="Apply" OnClick="Reload" />
    <asp:Button ID="btnCsv" runat="server" Text="Export CSV" OnClick="btnCsv_Click" />
  </div>
  <asp:Label ID="lblMsg" runat="server" CssClass="msg" EnableViewState="false" />
  <asp:Label ID="lblInfo" runat="server" CssClass="info" EnableViewState="false" />
  <asp:GridView ID="gv" runat="server" CssClass="grid" AllowPaging="true" PageSize="25"
      AllowSorting="true" AutoGenerateColumns="true" EnableViewState="false"
      OnPageIndexChanging="gv_PageIndexChanging" OnSorting="gv_Sorting">
    <PagerStyle CssClass="pager" />
  </asp:GridView>
</form>
</body>
</html>
