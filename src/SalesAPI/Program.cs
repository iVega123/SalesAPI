using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Serilog;
using SalesAPI.Application.Interfaces;
using SalesAPI.Application.Services;
using SalesAPI.Infrastructure.Data;
using Microsoft.OpenApi;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/salesapi-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, config) => config
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console()
    .WriteTo.File("logs/salesapi-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "SalesAPI", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT: Bearer {seu-token}"
    });
});

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=salesdb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(connectionString));
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// JWT
var jwtKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey não configurada.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(opt => opt.UseNpgsqlConnection(connectionString)));

builder.Services.AddHangfireServer(opt =>
{
    opt.WorkerCount = 2;
    opt.Queues = ["default"];
});

// Services — Fase 1-3
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEstoqueService, EstoqueService>();
builder.Services.AddScoped<ICompraService, CompraService>();
builder.Services.AddScoped<IVendaService, VendaService>();
builder.Services.AddScoped<IPdvService, PdvService>();
builder.Services.AddScoped<IFinanceiroService, FinanceiroService>();

// Services — Fase 4
builder.Services.AddScoped<IRelatorioService, RelatorioService>();
builder.Services.AddScoped<IFiscalService, FiscalService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Jobs — Fase 4
builder.Services.AddScoped<AlertaEstoqueJob>();
builder.Services.AddScoped<AtualizarContasVencidasJob>();
builder.Services.AddScoped<RelatorioSemanalJob>();
builder.Services.AddScoped<BackupDadosJob>();

// QuestPDF — licença Community (gratuita para projetos individuais/pequenas empresas)
QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (apenas em desenvolvimento)
if (app.Environment.IsDevelopment())
    app.UseHangfireDashboard("/hangfire");

app.MapControllers();

// Registrar jobs recorrentes após a aplicação inicializar
if (!app.Environment.IsEnvironment("Testing"))
    HangfireJobsRegistration.RegistrarJobs();

await app.RunAsync();

public partial class Program
{
    protected Program() { }
}
