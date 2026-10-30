using System.Globalization;
using System.Text.RegularExpressions;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Modelo.Regras;

/// <summary>
/// A "ficha do imóvel" enviada pelo WhatsApp. A Bruna recebe um modelo de texto, preenche e manda de volta; aqui o texto
/// vira <see cref="CamposDoImovel"/>. As regras do imóvel continuam no <c>Imovel</c>: esta parte só traduz rótulos e
/// formatos brasileiros e junta TODOS os problemas encontrados, para ela corrigir de uma vez.
/// </summary>
public static partial class Ficha
{
    public const string Cabecalho = "FICHA DO IMÓVEL";

    private const string ModeloVenda = $"""
        {Cabecalho}
        Finalidade: venda
        Tipo: apartamento
        Título:
        Bairro:
        Preço:
        Entrada a partir de:
        Condomínio:
        IPTU:
        Quartos:
        Suítes:
        Banheiros:
        Vagas:
        Área:
        Andar:
        Situação: pronto
        Previsão de entrega:
        Minha Casa Minha Vida: não
        Aceita FGTS: não
        Entrada parcelada: não
        Diferenciais:
        Vídeo:
        Proprietário (não aparece no site):
        Endereço (não aparece no site):
        Descrição:

        """;

    private const string ModeloAluguel = $"""
        {Cabecalho}
        Finalidade: aluguel
        Tipo: apartamento
        Título:
        Bairro:
        Aluguel:
        Condomínio:
        IPTU:
        Quartos:
        Suítes:
        Banheiros:
        Vagas:
        Área:
        Andar:
        Mobiliado: não
        Aceita pet: não
        Garantias:
        Diferenciais:
        Vídeo:
        Proprietário (não aparece no site):
        Endereço (não aparece no site):
        Descrição:

        """;

    /// <summary>Modelo enviado para a Bruna preencher.</summary>
    public static string Modelo(Finalidade finalidade) => finalidade == Finalidade.Venda ? ModeloVenda : ModeloAluguel;

    private enum Conversor { Texto, Dinheiro, Inteiro, Decimal, SimNao, Lista, Finalidade, Tipo, Situacao }

    private sealed record Definicao(string Campo, string Rotulo, Conversor Conversor, string[] Apelidos);

    private static readonly Definicao[] Definicoes =
    [
        new("codigo", "Código", Conversor.Texto, ["codigo", "cod"]),
        new("finalidade", "Finalidade", Conversor.Finalidade, ["finalidade"]),
        new("tipo", "Tipo", Conversor.Tipo, ["tipo", "tipo do imovel"]),
        new("titulo", "Título", Conversor.Texto, ["titulo", "titulo do anuncio"]),
        new("bairro", "Bairro", Conversor.Texto, ["bairro"]),
        new("preco", "Preço", Conversor.Dinheiro, ["preco", "preco de venda", "valor", "valor de venda"]),
        new("aluguel", "Aluguel", Conversor.Dinheiro, ["aluguel", "valor do aluguel"]),
        new("entrada_a_partir", "Entrada a partir de", Conversor.Dinheiro, ["entrada a partir de", "entrada"]),
        new("condominio", "Condomínio", Conversor.Dinheiro, ["condominio"]),
        new("iptu", "IPTU", Conversor.Dinheiro, ["iptu"]),
        new("quartos", "Quartos", Conversor.Inteiro, ["quartos", "dormitorios"]),
        new("suites", "Suítes", Conversor.Inteiro, ["suites"]),
        new("banheiros", "Banheiros", Conversor.Inteiro, ["banheiros"]),
        new("vagas", "Vagas", Conversor.Inteiro, ["vagas", "vagas de garagem", "garagem"]),
        new("area", "Área", Conversor.Decimal, ["area", "area m2", "area (m2)"]),
        new("andar", "Andar", Conversor.Texto, ["andar"]),
        new("situacao", "Situação", Conversor.Situacao, ["situacao"]),
        new("previsao_entrega", "Previsão de entrega", Conversor.Texto, ["previsao de entrega", "entrega"]),
        new("mcmv", "Minha Casa Minha Vida", Conversor.SimNao, ["minha casa minha vida", "mcmv"]),
        new("aceita_fgts", "Aceita FGTS", Conversor.SimNao, ["aceita fgts", "fgts"]),
        new("entrada_parcelada", "Entrada parcelada", Conversor.SimNao, ["entrada parcelada"]),
        new("mobiliado", "Mobiliado", Conversor.SimNao, ["mobiliado"]),
        new("aceita_pet", "Aceita pet", Conversor.SimNao, ["aceita pet", "pet"]),
        new("mostrar_mapa", "Mostrar mapa", Conversor.SimNao, ["mostrar mapa", "mapa"]),
        new("garantias", "Garantias", Conversor.Lista, ["garantias", "garantia"]),
        new("diferenciais", "Diferenciais", Conversor.Lista, ["diferenciais"]),
        new("video_url", "Vídeo", Conversor.Texto, ["video", "link do video"]),
        new("proprietario_nome", "Proprietário", Conversor.Texto, ["proprietario", "nome do proprietario"]),
        new("endereco", "Endereço", Conversor.Texto, ["endereco"]),
        new("descricao", "Descrição", Conversor.Texto, ["descricao"]),
    ];

    private static readonly Dictionary<string, Definicao> PorApelido =
        Definicoes.SelectMany(d => d.Apelidos.Select(a => (a, d))).ToDictionary(x => x.a, x => x.d);

    // ---------------------------------------------------------------------------------------------
    // Valores
    // ---------------------------------------------------------------------------------------------

    /// <summary>"365 mil", "1,6 mil", "1,2 milhão", "R$ 365.000,00", "365000", "1.650" → número; <c>null</c> se não entender.</summary>
    public static decimal? LerDinheiro(string texto)
    {
        var t = EspacosRepetidos().Replace(Texto.Normalizar(texto).Replace("r$", ""), " ").Trim();
        var multiplicador = 1m;
        var sufixo = SufixoDeValor().Match(t);
        if (sufixo.Success)
        {
            multiplicador = sufixo.Groups[1].Value is "mil" or "k" ? 1_000m : 1_000_000m;
            t = t[..sufixo.Index].Trim();
        }
        if (!SoNumero().IsMatch(t)) return null;

        string normalizado;
        if (t.Contains(',')) normalizado = t.Replace(".", "").Replace(",", "."); // vírgula = decimal; pontos = milhar
        else if (MilharComPonto().IsMatch(t)) normalizado = t.Replace(".", ""); // 365.000
        else normalizado = t; // 365000 ou 1.5 (com "mil")

        if (!decimal.TryParse(normalizado, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var n)) return null;
        return Math.Round(n * multiplicador, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>É uma ficha? Cabeçalho + algum campo, ou pelo menos 3 rótulos conhecidos. "Ficha" sozinho é pedido do modelo.</summary>
    public static bool EhFicha(string texto)
    {
        var linhas = Linhas(texto).Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
        var rotulos = linhas.Count(l => LerLinha(l) is not null);
        var comCabecalho = linhas.Length > 0 && EhCabecalho(linhas[0]);
        return (comCabecalho && rotulos >= 1) || rotulos >= 3;
    }

    public static FichaLida Ler(string texto)
    {
        var campos = new CamposDoImovel();
        List<string> problemas = [];
        List<string> descricao = [];
        string? bairro = null;
        string? codigo = null;
        var emDescricao = false;

        var linhas = Linhas(texto);
        var primeiraPreenchida = Array.FindIndex(linhas, l => l.Trim().Length > 0);

        for (var indice = 0; indice < linhas.Length; indice++)
        {
            var original = linhas[indice];
            var linha = original.Trim();
            if (emDescricao)
            {
                descricao.Add(original.TrimEnd());
                continue;
            }
            if (linha.Length == 0) continue;
            if (indice == primeiraPreenchida && EhCabecalho(linha)) continue;

            if (LerLinha(linha) is not { } lida)
            {
                var doisPontos = linha.IndexOf(':');
                problemas.Add(doisPontos >= 0
                    ? $"Não reconheci o campo \"{linha[..doisPontos].Trim()}\"."
                    : $"Não entendi a linha \"{(linha.Length > 40 ? linha[..40] : linha)}\". Use o formato Campo: valor.");
                continue;
            }
            var (def, valor) = lida;

            if (def.Campo == "descricao")
            {
                emDescricao = true;
                if (valor.Trim().Length > 0) descricao.Add(valor.Trim());
                continue;
            }
            valor = valor.Trim();
            if (valor.Length == 0) continue; // campo deixado em branco

            switch (def.Campo)
            {
                case "codigo":
                    var c = valor.ToUpperInvariant();
                    if (CodigoDoImovel().IsMatch(c)) codigo = c;
                    else problemas.Add("Código: use o código do imóvel (ex.: BA103).");
                    continue;
                case "bairro":
                    bairro = valor;
                    continue;
            }

            if (Converter(def, valor, out var problema) is not { } convertido)
            {
                problemas.Add(problema!);
                continue;
            }

            if (def.Campo == "garantias")
            {
                var lista = Opcoes.Canonicos((IReadOnlyList<string>)convertido, Opcoes.Garantias);
                if (lista.Any(g => !Opcoes.Garantias.Contains(g)))
                    problemas.Add($"Garantias: use {string.Join(", ", Opcoes.Garantias)}.");
                else campos = campos with { Garantias = lista };
                continue;
            }
            campos = Preencher(campos, def.Campo, convertido);
        }

        var textoDescricao = string.Join("\n", descricao).Trim();
        if (textoDescricao.Length > 0) campos = campos with { Descricao = textoDescricao };

        // Para criar um imóvel novo, estes três são obrigatórios (no painel também são).
        if (codigo is null)
        {
            List<string> faltando = [];
            if (campos.Finalidade is null) faltando.Add("Finalidade");
            if (campos.Tipo is null) faltando.Add("Tipo");
            if (string.IsNullOrEmpty(campos.Titulo)) faltando.Add("Título");
            if (faltando.Count > 0) problemas.Add($"Faltou preencher: {string.Join(", ", faltando)}.");
        }

        return new FichaLida(codigo, campos, bairro, problemas);
    }

    // ---------------------------------------------------------------------------------------------
    // Auxiliares
    // ---------------------------------------------------------------------------------------------

    private static string[] Linhas(string texto) => texto.Replace("\r\n", "\n").Split('\n');

    private static bool EhCabecalho(string linha) => Texto.Normalizar(linha).Replace("*", "").Replace("_", "").StartsWith("ficha");

    /// <summary>"Proprietário (não aparece no site)" → "proprietario".</summary>
    private static string ChaveDoRotulo(string rotulo) =>
        Texto.Normalizar(Parenteses().Replace(rotulo, "").Replace("*", "").Replace("_", ""));

    /// <summary>Uma linha "Rótulo: valor" com rótulo conhecido (ou <c>null</c>).</summary>
    private static (Definicao Def, string Valor)? LerLinha(string linha)
    {
        var i = linha.IndexOf(':');
        if (i <= 0) return null;
        return PorApelido.TryGetValue(ChaveDoRotulo(linha[..i]), out var def) ? (def, linha[(i + 1)..]) : null;
    }

    private static object? Converter(Definicao def, string v, out string? problema)
    {
        problema = null;
        var n = Texto.Normalizar(v);
        switch (def.Conversor)
        {
            case Conversor.Texto:
                return v;
            case Conversor.Dinheiro:
                if (LerDinheiro(v) is { } valor and >= 0m) return valor;
                problema = $"{def.Rotulo}: não entendi \"{v}\". Use, por exemplo, 365 mil ou 1.650,00.";
                return null;
            case Conversor.Inteiro:
                var inteiro = InteiroNoInicio().Match(n);
                if (inteiro.Success) return int.Parse(inteiro.Groups[1].Value, CultureInfo.InvariantCulture);
                problema = $"{def.Rotulo}: use só o número (ex.: 2).";
                return null;
            case Conversor.Decimal:
                if (LerDinheiro(UnidadeDeArea().Replace(n, "").Trim()) is { } area) return area;
                problema = $"{def.Rotulo}: use só o número em m² (ex.: 74 ou 74,5).";
                return null;
            case Conversor.SimNao:
                if (n is "sim" or "s" or "x" or "aceita" or "tem") return true;
                if (n is "nao" or "n" or "-" or "nao aceita" or "nao tem") return false;
                problema = $"{def.Rotulo}: responda sim ou não.";
                return null;
            case Conversor.Finalidade:
                if (n is "venda" or "vender" or "a venda") return Enums.Finalidade.Venda;
                if (n is "aluguel" or "alugar" or "locacao" or "para alugar") return Enums.Finalidade.Aluguel;
                problema = "Finalidade: use venda ou aluguel.";
                return null;
            case Conversor.Tipo:
                if (n is "apartamento" or "apto" or "ape" or "ap") return TipoImovel.Apartamento;
                if (n is "casa" or "sobrado") return TipoImovel.Casa;
                if (n is "terreno" or "lote") return TipoImovel.Terreno;
                if (n is "sala comercial" or "sala" or "comercial" or "loja") return TipoImovel.SalaComercial;
                problema = "Tipo: use apartamento, casa, terreno ou sala comercial.";
                return null;
            case Conversor.Situacao:
                if (n.StartsWith("pronto")) return Enums.Situacao.Pronto;
                if (n.Contains("planta") || n.Contains("construcao")) return Enums.Situacao.NaPlanta;
                problema = "Situação: use pronto ou na planta.";
                return null;
            case Conversor.Lista:
                return (IReadOnlyList<string>)[.. v.Split([',', ';']).Select(x => x.Trim()).Where(x => x.Length > 0)];
            default:
                throw new ArgumentOutOfRangeException(nameof(def));
        }
    }

    private static CamposDoImovel Preencher(CamposDoImovel c, string campo, object v) => campo switch
    {
        "finalidade" => c with { Finalidade = (Finalidade)v },
        "tipo" => c with { Tipo = (TipoImovel)v },
        "titulo" => c with { Titulo = (string)v },
        "preco" => c with { Preco = (decimal)v },
        "aluguel" => c with { Aluguel = (decimal)v },
        "entrada_a_partir" => c with { EntradaAPartir = (decimal)v },
        "condominio" => c with { Condominio = (decimal)v },
        "iptu" => c with { Iptu = (decimal)v },
        "quartos" => c with { Quartos = (int)v },
        "suites" => c with { Suites = (int)v },
        "banheiros" => c with { Banheiros = (int)v },
        "vagas" => c with { Vagas = (int)v },
        "area" => c with { Area = (decimal)v },
        "andar" => c with { Andar = (string)v },
        "situacao" => c with { Situacao = (Situacao)v },
        "previsao_entrega" => c with { PrevisaoEntrega = (string)v },
        "mcmv" => c with { Mcmv = (bool)v },
        "aceita_fgts" => c with { AceitaFgts = (bool)v },
        "entrada_parcelada" => c with { EntradaParcelada = (bool)v },
        "mobiliado" => c with { Mobiliado = (bool)v },
        "aceita_pet" => c with { AceitaPet = (bool)v },
        "mostrar_mapa" => c with { MostrarMapa = (bool)v },
        "diferenciais" => c with { Diferenciais = Opcoes.Canonicos((IReadOnlyList<string>)v, Opcoes.Diferenciais) },
        "video_url" => c with { VideoUrl = (string)v },
        "proprietario_nome" => c with { ProprietarioNome = (string)v },
        "endereco" => c with { Endereco = (string)v },
        _ => throw new ArgumentOutOfRangeException(nameof(campo), campo, null),
    };

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspacosRepetidos();

    [GeneratedRegex(@"\s*(mil|k|milhao|milhoes|mi)$")]
    private static partial Regex SufixoDeValor();

    [GeneratedRegex(@"^[\d.,]+$")]
    private static partial Regex SoNumero();

    [GeneratedRegex(@"^\d{1,3}(\.\d{3})+$")]
    private static partial Regex MilharComPonto();

    [GeneratedRegex(@"^(\d{1,2})\b")]
    private static partial Regex InteiroNoInicio();

    [GeneratedRegex("m2|m²|metros?")]
    private static partial Regex UnidadeDeArea();

    [GeneratedRegex(@"\(.*?\)")]
    private static partial Regex Parenteses();

    [GeneratedRegex("^[A-Z0-9]{2,20}$")]
    private static partial Regex CodigoDoImovel();
}
