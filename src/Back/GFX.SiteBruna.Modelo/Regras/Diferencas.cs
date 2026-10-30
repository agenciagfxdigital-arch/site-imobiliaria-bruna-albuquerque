using System.Collections;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Modelo.Regras;

/// <summary>Diferença antes → depois entre dois estados do imóvel, campo a campo (com os dados internos).</summary>
public static class Diferencas
{
    /// <param name="Copiar">Devolve o destino com o valor deste campo vindo da origem.</param>
    private sealed record Campo(
        string Nome, string Rotulo, Func<DadosDoImovel, object?> Valor, Func<DadosDoImovel, DadosDoImovel, DadosDoImovel> Copiar);

    private static readonly Campo[] Campos =
    [
        new("tipo", "Tipo", d => d.Tipo, (a, o) => a with { Tipo = o.Tipo }),
        new("titulo", "Título", d => d.Titulo, (a, o) => a with { Titulo = o.Titulo }),
        new("descricao", "Descrição", d => d.Descricao, (a, o) => a with { Descricao = o.Descricao }),
        new("preco", "Preço", d => d.Preco, (a, o) => a with { Preco = o.Preco }),
        new("aluguel", "Aluguel", d => d.Aluguel, (a, o) => a with { Aluguel = o.Aluguel }),
        new("condominio", "Condomínio", d => d.Condominio, (a, o) => a with { Condominio = o.Condominio }),
        new("iptu", "IPTU", d => d.Iptu, (a, o) => a with { Iptu = o.Iptu }),
        new("entrada_a_partir", "Entrada a partir de", d => d.EntradaAPartir, (a, o) => a with { EntradaAPartir = o.EntradaAPartir }),
        new("bairro_id", "Bairro", d => d.BairroId, (a, o) => a with { BairroId = o.BairroId }),
        new("endereco", "Endereço (interno)", d => d.Endereco, (a, o) => a with { Endereco = o.Endereco ?? "" }),
        new("mostrar_mapa", "Mostrar mapa", d => d.MostrarMapa, (a, o) => a with { MostrarMapa = o.MostrarMapa }),
        new("quartos", "Quartos", d => d.Quartos, (a, o) => a with { Quartos = o.Quartos }),
        new("suites", "Suítes", d => d.Suites, (a, o) => a with { Suites = o.Suites }),
        new("banheiros", "Banheiros", d => d.Banheiros, (a, o) => a with { Banheiros = o.Banheiros }),
        new("vagas", "Vagas", d => d.Vagas, (a, o) => a with { Vagas = o.Vagas }),
        new("area", "Área", d => d.Area, (a, o) => a with { Area = o.Area }),
        new("andar", "Andar", d => d.Andar, (a, o) => a with { Andar = o.Andar }),
        new("diferenciais", "Diferenciais", d => d.Diferenciais, (a, o) => a with { Diferenciais = o.Diferenciais }),
        new("mcmv", "Minha Casa Minha Vida", d => d.Mcmv, (a, o) => a with { Mcmv = o.Mcmv }),
        new("entrada_parcelada", "Entrada parcelada", d => d.EntradaParcelada, (a, o) => a with { EntradaParcelada = o.EntradaParcelada }),
        new("aceita_fgts", "Aceita FGTS", d => d.AceitaFgts, (a, o) => a with { AceitaFgts = o.AceitaFgts }),
        new("situacao", "Situação", d => d.Situacao, (a, o) => a with { Situacao = o.Situacao }),
        new("previsao_entrega", "Previsão de entrega", d => d.PrevisaoEntrega, (a, o) => a with { PrevisaoEntrega = o.PrevisaoEntrega }),
        new("mobiliado", "Mobiliado", d => d.Mobiliado, (a, o) => a with { Mobiliado = o.Mobiliado }),
        new("aceita_pet", "Aceita pet", d => d.AceitaPet, (a, o) => a with { AceitaPet = o.AceitaPet }),
        new("garantias", "Garantias", d => d.Garantias, (a, o) => a with { Garantias = o.Garantias }),
        new("video_url", "Vídeo", d => d.VideoUrl, (a, o) => a with { VideoUrl = o.VideoUrl }),
        new("proprietario_nome", "Proprietário (interno)", d => d.ProprietarioNome,
            (a, o) => a with { ProprietarioNome = o.ProprietarioNome ?? "" }),
        new("status", "Status", d => d.Status, (a, o) => a with { Status = o.Status }),
        new("destaque", "Destaque na página inicial", d => d.Destaque, (a, o) => a with { Destaque = o.Destaque }),
    ];

    private static readonly Dictionary<string, Campo> PorNome = Campos.ToDictionary(c => c.Nome);

    /// <summary>Só os campos que mudaram. Texto vazio e nulo contam como iguais; listas comparam item a item.</summary>
    public static IReadOnlyList<MudancaCampo> Entre(DadosDoImovel antes, DadosDoImovel depois) =>
    [
        .. Campos
            .Select(c => (c.Nome, Antes: c.Valor(antes), Depois: c.Valor(depois)))
            .Where(x => !Iguais(x.Antes, x.Depois))
            .Select(x => new MudancaCampo(x.Nome, x.Antes, x.Depois)),
    ];

    /// <summary>O estado <paramref name="atual"/> com os <paramref name="campos"/> de volta como estavam em <paramref name="antes"/>.</summary>
    public static DadosDoImovel Restaurar(DadosDoImovel atual, DadosDoImovel antes, IEnumerable<string> campos) =>
        campos.Where(PorNome.ContainsKey).Aggregate(atual, (estado, campo) => PorNome[campo].Copiar(estado, antes));

    public static string RotuloDo(string campo) => PorNome.TryGetValue(campo, out var c) ? c.Rotulo : campo;

    private static bool Iguais(object? a, object? b)
    {
        if (a is IEnumerable lista1 and not string && b is IEnumerable lista2 and not string)
            return lista1.Cast<object?>().SequenceEqual(lista2.Cast<object?>());
        return Equals(Vazio(a), Vazio(b));
    }

    private static object? Vazio(object? v) => v is string { Length: 0 } ? null : v;
}
