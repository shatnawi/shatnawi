using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AWVA.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Targets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Host = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ResolvedIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Profile = table.Column<int>(type: "int", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Targets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GroundTruthItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OwaspCategory = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Cwe = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    WstgId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ExpectedSeverity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroundTruthItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroundTruthItems_Targets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Targets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Scans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    Profile = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StepBudget = table.Column<int>(type: "int", nullable: false),
                    StepsUsed = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scans_Targets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Targets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScanId = table.Column<int>(type: "int", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Actor = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogEntries_Scans_ScanId",
                        column: x => x.ScanId,
                        principalTable: "Scans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ToolCalls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScanId = table.Column<int>(type: "int", nullable: false),
                    ExternalCallId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ToolName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ToolKind = table.Column<int>(type: "int", nullable: false),
                    ArgumentsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RefusalReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawOutput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedOutput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolCalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolCalls_Scans_ScanId",
                        column: x => x.ScanId,
                        principalTable: "Scans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Findings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScanId = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Cwe = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OwaspCategory = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WstgId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    SeveritySource = table.Column<int>(type: "int", nullable: false),
                    SeverityJustification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Evidence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceToolCallId = table.Column<int>(type: "int", nullable: true),
                    AffectedUrlsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Remediation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Confidence = table.Column<int>(type: "int", nullable: false),
                    IsHallucination = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Findings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Findings_Scans_ScanId",
                        column: x => x.ScanId,
                        principalTable: "Scans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Findings_ToolCalls_SourceToolCallId",
                        column: x => x.SourceToolCallId,
                        principalTable: "ToolCalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_ScanId",
                table: "AuditLogEntries",
                column: "ScanId");

            migrationBuilder.CreateIndex(
                name: "IX_Findings_ScanId",
                table: "Findings",
                column: "ScanId");

            migrationBuilder.CreateIndex(
                name: "IX_Findings_SourceToolCallId",
                table: "Findings",
                column: "SourceToolCallId");

            migrationBuilder.CreateIndex(
                name: "IX_GroundTruthItems_TargetId",
                table: "GroundTruthItems",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_Scans_TargetId",
                table: "Scans",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_Targets_Host",
                table: "Targets",
                column: "Host");

            migrationBuilder.CreateIndex(
                name: "IX_ToolCalls_ScanId_ExternalCallId",
                table: "ToolCalls",
                columns: new[] { "ScanId", "ExternalCallId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogEntries");

            migrationBuilder.DropTable(
                name: "Findings");

            migrationBuilder.DropTable(
                name: "GroundTruthItems");

            migrationBuilder.DropTable(
                name: "ToolCalls");

            migrationBuilder.DropTable(
                name: "Scans");

            migrationBuilder.DropTable(
                name: "Targets");
        }
    }
}
