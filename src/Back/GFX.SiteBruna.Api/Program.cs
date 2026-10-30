using GFX.SiteBruna.Aplicacao;
using GFX.SiteBruna.Dados;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAplicacao();
builder.Services.AddDados(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Só avisa no log se faltar migration; quem aplica é o dev (docs/MIGRATIONS.md)
await app.Services.VerificarBancoAsync();
await app.RunAsync();

/// <summary>Exposto para os testes de arquitetura e de integração.</summary>
public partial class Program;
