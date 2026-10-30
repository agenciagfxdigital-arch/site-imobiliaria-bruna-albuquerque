using System.Reflection;
using System.Xml.Linq;
using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Testes.Arquitetura;

/// <summary>
/// Regras de dependência entre as camadas. Se um destes testes falhar, a mudança quebrou a arquitetura:
/// corrija a dependência, não o teste.
/// </summary>
public class CamadasTests
{
    private static readonly Assembly Modelo = typeof(Imovel).Assembly;
    private static readonly Assembly Aplicacao = typeof(global::GFX.SiteBruna.Aplicacao.DependencyInjection).Assembly;
    private static readonly Assembly Dados = typeof(global::GFX.SiteBruna.Dados.DependencyInjection).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    private static string[] Referencias(Assembly assembly) =>
        [.. assembly.GetReferencedAssemblies().Select(a => a.Name!)];

    [Fact]
    public void Modelo_nao_referencia_outras_camadas_nem_pacotes()
    {
        // Só o runtime do .NET: nenhum projeto da solução e nenhum pacote NuGet.
        var proibidas = Referencias(Modelo).Where(n => !n.StartsWith("System") && n != "netstandard").ToArray();
        Assert.Empty(proibidas);
    }

    [Fact]
    public void Aplicacao_so_conhece_o_Modelo_e_nao_conhece_web_nem_banco()
    {
        var referencias = Referencias(Aplicacao);
        Assert.Contains(Modelo.GetName().Name, referencias);
        Assert.DoesNotContain(referencias, n =>
            n == Dados.GetName().Name || n == Api.GetName().Name
            || n.StartsWith("Microsoft.AspNetCore") || n.StartsWith("Microsoft.EntityFrameworkCore") || n.StartsWith("Npgsql"));
    }

    [Fact]
    public void Dados_expoe_so_o_registro_da_camada()
    {
        // Todo o resto é internal: a Api não consegue usar DbContext, repositório nem cliente externo (CS0122).
        string[] permitidos = ["DependencyInjection", "VerificacaoBanco"];
        var publicos = Dados.GetExportedTypes().Select(t => t.Name).Where(n => !permitidos.Contains(n)).ToArray();
        Assert.Empty(publicos);
    }

    [Fact]
    public void Api_nao_acessa_banco_diretamente()
    {
        Assert.DoesNotContain(Referencias(Api), n =>
            n.StartsWith("Microsoft.EntityFrameworkCore") || n.StartsWith("Npgsql"));
    }

    [Fact]
    public void Entidades_do_Modelo_nao_tem_setter_publico()
    {
        var comSetterPublico = Modelo.GetTypes()
            .Where(t => t.Namespace?.EndsWith(".Entities") == true)
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod?.IsPublic == true)
                .Select(p => $"{t.Name}.{p.Name}"))
            .ToArray();
        Assert.Empty(comSetterPublico);
    }

    [Fact]
    public void Front_nao_referencia_projetos_do_back()
    {
        var csproj = Path.Combine(RaizDaSolucao(), "src", "Front", "GFX.SiteBruna.Front.Web", "GFX.SiteBruna.Front.Web.csproj");
        var referencias = XDocument.Load(csproj).Descendants("ProjectReference")
            .Select(r => (string?)r.Attribute("Include") ?? "");
        Assert.DoesNotContain(referencias, r => r.Contains(Path.Combine("src", "Back")) || r.Contains(@"..\..\Back"));
    }

    private static string RaizDaSolucao()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "GFX.SiteBruna.Back.sln")))
            pasta = pasta.Parent;
        return pasta?.FullName ?? throw new InvalidOperationException("Raiz da solução não encontrada.");
    }
}
