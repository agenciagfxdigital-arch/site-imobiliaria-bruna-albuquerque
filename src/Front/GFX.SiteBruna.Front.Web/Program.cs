using GFX.SiteBruna.Front.Web.Autenticacao;
using GFX.SiteBruna.Front.Web.Cache;
using GFX.SiteBruna.Front.Web.Components;
using GFX.SiteBruna.Front.Web.Configuracao;
using GFX.SiteBruna.Front.Web.Endpoints;
using GFX.SiteBruna.Front.Web.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Site público: renderização estática no servidor (HTML pronto para o Google, sem WebSocket).
// Painel (/admin): interativo, sem pré-renderização (a sessão fica no navegador, criptografada).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<OpcoesDoSite>(builder.Configuration.GetSection("Site"));

// A Api fica na rede interna; quem fala com ela é este servidor (sem CORS)
var urlDaApi = builder.Configuration["Api:UrlBase"]
    ?? throw new InvalidOperationException("Configure Api:UrlBase com o endereço da Api (ex.: http://localhost:5130/).");
void Cliente(HttpClient http, TimeSpan tempo)
{
    http.BaseAddress = new Uri(urlDaApi);
    http.Timeout = tempo;
}
builder.Services.AddHttpClient<SiteApi>(h => Cliente(h, TimeSpan.FromSeconds(15)));
builder.Services.AddHttpClient<UsuarioApi>(h => Cliente(h, TimeSpan.FromSeconds(15)));
builder.Services.AddHttpClient<OperadorApi>(h => Cliente(h, TimeSpan.FromSeconds(15)));
builder.Services.AddHttpClient<MetricasApi>(h => Cliente(h, TimeSpan.FromSeconds(30)));
builder.Services.AddHttpClient<ConteudoApi>(h => Cliente(h, TimeSpan.FromSeconds(60)));
builder.Services.AddHttpClient<ImovelApi>(h => Cliente(h, TimeSpan.FromSeconds(60)));
builder.Services.AddScoped<DadosDoSite>();
builder.Services.AddScoped<SessaoDoPainel>();
builder.Services.AddScoped<OrigemDaConexao>();

builder.Services.AddOutputCache();
builder.Services.AddSingleton<CacheDoSite>();

// A sessão do painel é criptografada com estas chaves: na VPS ficam num volume, para um deploy não derrubar quem está logado
if (builder.Configuration["DataProtection:PastaDasChaves"] is { Length: > 0 } pastaDasChaves)
{
    builder.Services.AddDataProtection()
        .SetApplicationName("GFX.SiteBruna")
        .PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(pastaDasChaves, builder.Environment.ContentRootPath)));
}

var app = builder.Build();

app.UseCabecalhosDeSeguranca();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/erro", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/nao-encontrada", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseFotosNoDisco();
app.UseSemAntiforgeryNoSite();
app.UseOutputCache();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRotasDoSite();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .CacheOutput(app.Services.GetRequiredService<CacheDoSite>());

app.Run();
