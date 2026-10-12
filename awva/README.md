# FBSU-AWVA — ASP.NET Core / SQL Server implementation

This is an ASP.NET Core (.NET 8) + SQL Server port of the architecture described in
`Agentic_Web_Vulnerability_Auditor_AWVA___Project_Proposal.docx`. It swaps the proposal's
Python/FastAPI/Streamlit/SQLite stack for ASP.NET Core Web API, EF Core, and SQL Server,
while keeping every safety control, data shape, and workflow step from the proposal.

**Scope of this codebase:** "full breadth, stubbed" — every module and tool named in the
proposal exists as a real class wired into the agent loop and the scope guard, with a
working SQL Server schema and a deterministic (no-LLM) planner you can run end to end
today. A subset of tools (security headers, cookies, TLS handshake, fingerprinting,
crawler, open redirect, access-control/BOLA replay, bounded auth testing, error probing,
JWT `alg:none` detection) has real working logic. The rest (Nuclei, ZAP, sqlmap, SSLyze
CLI, jwt_tool secret-cracking, Schemathesis, graphql-cop, TruffleHog/gitleaks, Trivy,
retire.js, the real Ollama planner) are documented stubs: the method signature, OWASP
category tags, scope-guard wiring, and a `TODO` comment naming exactly what external tool
to shell out to are in place; the external process/API call itself is not. This mirrors
the 5-person, 14-week team split in Section 7 of the proposal — each stub is one person's
remaining task, not a redesign.

## Project layout

```
awva/
  src/
    AWVA.Domain/   Enums + EF Core entities (Target, Scan, ToolCall, Finding, AuditLogEntry, GroundTruthItem)
    AWVA.Data/     AwvaDbContext, EF Core migrations, design-time factory
    AWVA.Core/     ScopeGuard, ISecurityTool/IToolRegistry contracts, ILlmPlanner + DeterministicPlanner,
                   AgentOrchestrator (the agent loop), FindingValidator (hallucination check), SeverityRuleTable
    AWVA.Tools/    One class per tool named in Sections 3-5 of the proposal
    AWVA.Api/      Controllers, Program.cs DI wiring, background scan worker, Markdown report builder
  tests/
    AWVA.Tests/    xunit tests for the scope guard and the hallucination check
  db/
    schema.sql     Idempotent SQL Server DDL generated from the EF Core migrations
  docker-compose.yml   Local SQL Server for dev (NOT the security lab — see below)
```

## How the proposal's sections map to this code

| Proposal section | Where it lives here |
|---|---|
| §2 System architecture ("the LLM never talks to the target directly") | `AWVA.Core.AgentOrchestrator` calls `IScopeGuard.ValidateToolCall` before every tool execution; tools never run unguarded. |
| §3 Technology stack | ASP.NET Core Web API (`AWVA.Api`) + EF Core/SQL Server (`AWVA.Data`) replace FastAPI/Streamlit/SQLite; `ScanJobQueue` + `ScanBackgroundWorker` (`System.Threading.Channels` + `BackgroundService`) replace the asyncio job queue; each row of the tool table has a matching class in `AWVA.Tools`. |
| §4 Execution workflow steps 1-6 | 1: `TargetsController.Submit` + `ScopeGuard.ValidateTargetAsync`. 2: `ILlmPlanner.PlanNextStepAsync` (stubbed with `DeterministicPlanner`, a real `OllamaLlmPlanner` TODO). 3: `AgentOrchestrator.RunAsync`'s loop, `ScopeGuard.ValidateToolCall`, `Scan.StepBudget`. 4: `ToolCall.RawOutput`/`NormalizedOutput`. 5: `ILlmPlanner.TriageAsync` + `FindingValidator`. 6: `ReportsController` + `MarkdownReportBuilder`. |
| §5 OWASP Top 10:2025 coverage / §5.1 extra modules | Every `ISecurityTool.OwaspCategories` tags the categories it contributes to; see `AWVA.Tools.Passive`/`AWVA.Tools.Active`. |
| §6 Safety, ethics, security of the agent | `AWVA.Core.ScopeGuard` (allowlist, profile enforcement, Nuclei tag allowlist, crawler bounds, step budget), `AuditLogEntry` (audit log), `ScansController.Abort` + `IScanCancellationRegistry` (kill switch). |
| §9 Evaluation methodology | `GroundTruthItem` entity; `Finding.IsHallucination` is exactly the "hallucination rate" metric; `DeterministicPlanner` is baseline (a) ("tools run in a fixed script with no LLM"). |

## Running it locally

```bash
# 1. Start SQL Server for dev
docker compose up -d sqlserver

# 2. Apply the EF Core migration (or run db/schema.sql directly against SQL Server)
dotnet tool install --global dotnet-ef
cd src/AWVA.Data
dotnet ef database update --connection "Server=localhost,1433;Database=AWVA;User Id=sa;Password=ChangeMe_InDevOnly!;TrustServerCertificate=True;"

# 3. Run the API (appsettings.json already points at the same connection string)
cd ../AWVA.Api
dotnet run
# Swagger UI: https://localhost:<port>/swagger

# 4. Run the tests
cd ../..
dotnet test
```

With `UseDeterministicPlanner: true` (the default), the API runs the full agent loop and
scope guard without needing Ollama installed — this is also evaluation baseline (a) from
Section 9. Set it to `false` and fill in `AWVA.Core.Planning.OllamaLlmPlanner` once Ollama
is running to get the real LLM-driven planning/triage described in Section 3.

## What is intentionally not here

- **The security lab** (Docker Compose for Juice Shop/DVWA/crAPI or VAmPI, the ZAP
  container, interactsh, Wazuh/ELK) is a separate, isolated environment per Section 6 —
  `docker-compose.yml` in this repo is only the application's own SQL Server.
- **The external tool integrations** listed as stubs above — wiring each one up is the
  remaining work for the "Active testing and scope guard" / "Passive tools and API
  module" / "Lab, safety, and evaluation" roles in Section 7.
- **Authentication/authorization on the API itself** — out of scope for this pass; add
  ASP.NET Core Identity or an auth middleware before exposing this beyond localhost.

## Ethics and authorization

Per Section 6 of the proposal: this tool is to be run only against the team's own lab
containers or systems with written authorization from the owner. Scanning third-party
systems without permission is illegal under Saudi Arabia's Anti-Cyber Crime Law and
comparable laws elsewhere.
