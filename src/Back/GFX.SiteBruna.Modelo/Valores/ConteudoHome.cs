// Gerado a partir dos padrões do site em Next (src/lib/seed-data.ts). Os textos são editáveis pelo painel.
using GFX.SiteBruna.Modelo.Regras;

namespace GFX.SiteBruna.Modelo.Valores;

/// <summary>Textos e fotos da página inicial.</summary>
public sealed record ConteudoHome
{
    public string HeroTitulo { get; init; } = "";
    public string HeroSubtitulo { get; init; } = "";
    public string HeroFoto { get; init; } = "";
    public IReadOnlyList<string> Servicos { get; init; } = [];
    public string PassosTitulo { get; init; } = "";
    public string PassosTexto { get; init; } = "";
    public IReadOnlyList<Passo> Passos { get; init; } = [];
    public string McmvTitulo { get; init; } = "";
    public string McmvTexto { get; init; } = "";
    public string BioTitulo { get; init; } = "";
    public string BioTexto { get; init; } = "";
    public string BioFoto { get; init; } = "";
    public string CtaTitulo { get; init; } = "";
    public string CtaTexto { get; init; } = "";

    /// <summary>Textos padrão (em primeira pessoa da Bruna), usados até a página ser salva pelo painel.</summary>
    public static ConteudoHome Padrao { get; } = new()
    {
        HeroTitulo = "Seu próximo imóvel começa com uma boa escolha.",
        HeroSubtitulo = "Do primeiro imóvel à próxima conquista, ajudo você a encontrar a melhor oportunidade para o seu momento, com orientação em cada etapa.",
        HeroFoto = "",
        Servicos =
        [
            "Compra",
            "Venda",
            "Locação",
            "Imóvel na planta",
            "Minha Casa Minha Vida",
        ],
        PassosTitulo = "Do primeiro contato à realização do seu objetivo.",
        PassosTexto = "Meu trabalho começa entendendo o que você procura e segue com orientação em cada etapa, da escolha do imóvel à entrega das chaves.",
        Passos =
        [
            new("Conversa inicial", "Você me conta o que procura, suas prioridades e o que é importante para você. Entendo seu momento para direcionar a busca de forma mais assertiva."),
            new("Planejamento da compra", "Avaliamos juntos as possibilidades de compra, considerando seu orçamento, suas necessidades e a forma de pagamento. Também fazemos simulações de financiamento."),
            new("Escolha do imóvel", "Seleciono opções alinhadas ao que você procura e às suas possibilidades. Acompanho as visitas e ajudo você a avaliar cada oportunidade."),
            new("Negociação e acompanhamento", "Acompanho a negociação, os contratos e as etapas necessárias para concluir o negócio. Nos imóveis na planta, também oriento você durante o processo de compra e acompanho as etapas da obra conforme o empreendimento."),
        ],
        McmvTitulo = "Seu primeiro imóvel pode estar mais perto do que você imagina.",
        McmvTexto = "Em muitos imóveis na planta é possível parcelar a entrada com a construtora e usar o FGTS para ajudar na compra.\n\nJuntos, vamos entender as possibilidades de financiamento e encontrar uma opção que combine com o seu momento. Eu te explico cada etapa, sem complicação.",
        BioTitulo = "Prazer, sou a Bruna Albuquerque.",
        BioTexto = "Há 7 anos, faço parte do mercado imobiliário. Nos últimos 3 anos, minha atuação como corretora tem um foco especial em ajudar pessoas a conquistar o primeiro imóvel, especialmente por meio do Minha Casa Minha Vida.\n\nAjudo quem quer sair do aluguel a entender como funciona a compra do primeiro imóvel e quais são as possibilidades para dar esse passo.\n\nTambém trabalho com imóveis prontos, terrenos, oportunidades para investidores e locações, sempre buscando entender o que cada cliente procura e orientar cada negociação com clareza e segurança.",
        BioFoto = "",
        CtaTitulo = "Quer saber o que cabe no seu bolso?",
        CtaTexto = "Me chama no WhatsApp. A gente conversa sem compromisso e eu faço a simulação com você.",
    };

    /// <summary>Confere os tamanhos antes de gravar (mensagens prontas para o painel).</summary>
    public void Validar()
    {
        ConteudoDePagina.Texto(HeroTitulo, "Título principal", 200);
        ConteudoDePagina.Texto(HeroSubtitulo, "Subtítulo", 500);
        ConteudoDePagina.Texto(HeroFoto, "Sua foto do topo", 1000);
        ConteudoDePagina.Lista(Servicos, "Serviços abaixo dos botões");
        ConteudoDePagina.Texto(PassosTitulo, "Título (Passo a passo)", 200);
        ConteudoDePagina.Texto(PassosTexto, "Texto (Passo a passo)", 5000);
        ConteudoDePagina.Passos(Passos, "Passos (Passo a passo)");
        ConteudoDePagina.Texto(McmvTitulo, "Título (Bloco Minha Casa Minha Vida)", 200);
        ConteudoDePagina.Texto(McmvTexto, "Texto (Bloco Minha Casa Minha Vida)", 5000);
        ConteudoDePagina.Texto(BioTitulo, "Título (Quem é a Bruna (resumo))", 200);
        ConteudoDePagina.Texto(BioTexto, "Mini bio", 5000);
        ConteudoDePagina.Texto(BioFoto, "Foto (Quem é a Bruna (resumo))", 1000);
        ConteudoDePagina.Texto(CtaTitulo, "Título (Chamada final)", 200);
        ConteudoDePagina.Texto(CtaTexto, "Texto (Chamada final)", 5000);
    }
}
