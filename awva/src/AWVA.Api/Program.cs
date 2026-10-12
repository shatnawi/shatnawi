using AWVA.Api.Services;
using AWVA.Core;
using AWVA.Core.Planning;
using AWVA.Data;
using AWVA.Tools;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Data layer (SQL Server via EF Core) ---------------------------------------------
builder.Services.AddDbContext<AwvaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AwvaDb")));

// --- Scope guard: the authority on scan scope, configured from appsettings/env, never
// from anything the LLM proposes (Section 6) ------------------------------------------
var scopeGuardOptions = new ScopeGuardOptions();
builder.Configuration.GetSection("ScopeGuard").Bind(scopeGuardOptions);
builder.Services.AddSingleton(scopeGuardOptions);
builder.Services.AddSingleton<IScopeGuard, ScopeGuard>();

// --- Tool wrappers (Section 3/4/5) + HttpClient for the ones that make HTTP calls ------
builder.Services.AddHttpClient();
builder.Services.AddAwvaTools();

// --- LLM planner: deterministic by default so the API runs without Ollama installed;
// flip "UseDeterministicPlanner" to false once Ollama is wired up via OllamaLlmPlanner
// (Section 9 baseline (a) uses the deterministic planner on purpose) -------------------
var ollamaOptions = new OllamaOptions();
builder.Configuration.GetSection("Ollama").Bind(ollamaOptions);
builder.Services.AddSingleton(ollamaOptions);
if (builder.Configuration.GetValue("UseDeterministicPlanner", true))
{
    builder.Services.AddSingleton<ILlmPlanner, DeterministicPlanner>();
}
else
{
    builder.Services.AddHttpClient<OllamaLlmPlanner>();
    builder.Services.AddSingleton<ILlmPlanner, OllamaLlmPlanner>();
}

builder.Services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();

// --- Background scan execution (ASP.NET analog of Section 3's "Job queue: asyncio tasks
// (Celery/RQ optional)") + per-scan kill switch (Section 6) ---------------------------
builder.Services.AddSingleton<IScanJobQueue, ScanJobQueue>();
builder.Services.AddSingleton<IScanCancellationRegistry, ScanCancellationRegistry>();
builder.Services.AddHostedService<ScanBackgroundWorker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
