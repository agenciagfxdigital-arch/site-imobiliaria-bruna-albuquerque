using GFX.SiteBruna.Aplicacao.Services;

namespace GFX.SiteBruna.Api.Comandos;

/// <summary>
/// <c>dotnet run --project src/Back/GFX.SiteBruna.Api -- carregar-conteudo-inicial</c>: preenche bairros, perguntas
/// frequentes e depoimentos, uma vez, depois de aplicar as migrations. O que já tiver cadastro fica como está.
/// </summary>
public static class CarregarConteudoInicial
{
    public const string Nome = "carregar-conteudo-inicial";

    public static async Task<int> Executar(IServiceProvider servicos)
    {
        await using var escopo = servicos.CreateAsyncScope();
        var resposta = await escopo.ServiceProvider.GetRequiredService<ConteudoInicialService>().Carregar();
        if (!resposta.Sucesso)
        {
            Console.Error.WriteLine(string.Join(" ", resposta.Mensagens));
            return 1;
        }
        var d = resposta.Dados!;
        Console.WriteLine($"Incluídos: {d.Bairros} bairro(s), {d.Perguntas} pergunta(s) e {d.Depoimentos} depoimento(s).");
        return 0;
    }
}
