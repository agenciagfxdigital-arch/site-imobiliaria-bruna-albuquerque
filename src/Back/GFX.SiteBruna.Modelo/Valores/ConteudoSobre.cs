// Gerado a partir dos padrões do site em Next (src/lib/seed-data.ts). Os textos são editáveis pelo painel.
using GFX.SiteBruna.Modelo.Regras;

namespace GFX.SiteBruna.Modelo.Valores;

/// <summary>Página "Quem é a Bruna".</summary>
public sealed record ConteudoSobre
{
    public string Foto { get; init; } = "";
    public string Titulo { get; init; } = "";
    public string Subtitulo { get; init; } = "";
    public string Historia { get; init; } = "";
    public IReadOnlyList<Passo> Passos { get; init; } = [];
    public string AnosExperiencia { get; init; } = "";
    public string FamiliasAtendidas { get; init; } = "";
    public IReadOnlyList<string> Entregas { get; init; } = [];
    public string VideoUrl { get; init; } = "";
    public string CtaTitulo { get; init; } = "";

    /// <summary>Textos padrão (em primeira pessoa da Bruna), usados até a página ser salva pelo painel.</summary>
    public static ConteudoSobre Padrao { get; } = new()
    {
        Foto = "",
        Titulo = "Bruna Albuquerque",
        Subtitulo = "Corretora de imóveis em Videira, há 7 anos no mercado imobiliário",
        Historia = "Faço parte do mercado imobiliário há 7 anos e, nos últimos 3, como corretora, percebi uma coisa: para muita gente, comprar o primeiro imóvel parece um sonho distante, cheio de termos difíceis e burocracia.\n\nFoi por isso que dei um foco especial ao primeiro imóvel e ao Minha Casa Minha Vida. Eu gosto de sentar com cada pessoa, explicar as etapas com calma e mostrar quais são as possibilidades para sair do aluguel.\n\nTambém trabalho com imóveis prontos, terrenos, oportunidades para investidores e locações. Em todos os casos, começo entendendo o que você procura e acompanho a negociação com clareza e segurança.\n\nO que mais me realiza é ver a alegria de quem pega a chave. Cada entrega é uma história que eu tenho orgulho de ter acompanhado.",
        Passos =
        [
            new("Escuta", "Entendo o momento da sua família, o que vocês procuram e o que é prioridade."),
            new("Simulação", "Faço a simulação com você e explico renda, entrada, FGTS e parcelas em linguagem simples."),
            new("Escolha", "Selecionamos juntos os imóveis que fazem sentido e eu acompanho as visitas."),
            new("Acompanhamento até a chave", "Cuido da documentação com você e fico do seu lado até a entrega da chave."),
        ],
        AnosExperiencia = "7",
        FamiliasAtendidas = "",
        Entregas = [],
        VideoUrl = "",
        CtaTitulo = "Vamos conversar sobre o seu imóvel?",
    };

    /// <summary>Confere os tamanhos antes de gravar (mensagens prontas para o painel).</summary>
    public void Validar()
    {
        ConteudoDePagina.Texto(Foto, "Foto grande", 1000);
        ConteudoDePagina.Texto(Titulo, "Nome", 200);
        ConteudoDePagina.Texto(Subtitulo, "Frase abaixo do nome", 200);
        ConteudoDePagina.Texto(Historia, "Texto (Minha história)", 5000);
        ConteudoDePagina.Passos(Passos, "Passos (Como eu trabalho)");
        ConteudoDePagina.Texto(AnosExperiencia, "Anos de experiência", 200);
        ConteudoDePagina.Texto(FamiliasAtendidas, "Famílias atendidas", 200);
        ConteudoDePagina.Lista(Entregas, "Fotos");
        ConteudoDePagina.Texto(VideoUrl, "Link do Reels ou YouTube “Quem sou eu”", 1000);
        ConteudoDePagina.Texto(CtaTitulo, "Título da chamada final", 200);
    }
}
