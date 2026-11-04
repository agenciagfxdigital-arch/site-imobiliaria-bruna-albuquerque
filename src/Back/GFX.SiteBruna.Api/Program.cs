using System.Text.Json.Serialization;
using GFX.SiteBruna.Api.Comandos;
using GFX.SiteBruna.Api.ExceptionHandling;
using GFX.SiteBruna.Api.Extensions;
using GFX.SiteBruna.Api.RateLimiting;
using GFX.SiteBruna.Api.Whatsapp;
using GFX.SiteBruna.Aplicacao;
using GFX.SiteBruna.Dados;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe apenas o token (sem o prefixo 'Bearer '). Pegue um em POST /api/Auth/entrar.",
    });
    options.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", documento)] = [],
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddAutenticacaoDoPainel(builder.Configuration, builder.Environment);
builder.Services.AddLimites();
builder.Services.AddAplicacao();
// A Api só registra a camada de dados: DbContext, repositórios e clientes externos são internal no projeto Dados
builder.Services.AddDados(builder.Configuration);

builder.Services.Configure<OpcoesDoWebhook>(builder.Configuration.GetSection("WhatsApp"));
builder.Services.AddSingleton<FilaDeProcessamento>();
builder.Services.AddHostedService<ProcessadorDoWhatsapp>();

var app = builder.Build();

if (args.FirstOrDefault() == CriarUsuario.Nome) return await CriarUsuario.Executar(app.Services, args);

app.UseExceptionHandler();
// Swagger só em desenvolvimento e antes da autenticação: a negação por padrão bloquearia a própria tela com 401
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.EnablePersistAuthorization());
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription().AllowAnonymous();
}
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

// Só avisa no log se faltar migration; quem aplica é o dev (docs/MIGRATIONS.md)
await app.Services.VerificarBancoAsync();
await app.RunAsync();
return 0;

/// <summary>Exposto para os testes de arquitetura e de integração.</summary>
public partial class Program;
