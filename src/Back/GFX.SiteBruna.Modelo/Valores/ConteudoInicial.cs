namespace GFX.SiteBruna.Modelo.Valores;

/// <summary>Conteúdo revisado pela Bruna para a primeira carga do site, depois editável pelo painel.</summary>
public static class ConteudoInicial
{
    public static readonly IReadOnlyList<string> Bairros =
    [
        "Centro",
        "Matriz",
        "Alvorada",
        "Universitário",
        "Santa Gema",
        "Dois Pinheiros",
        "Panazzolo",
        "Ipiranga",
        "Rio das Pedras",
        "Vila Verde",
    ];

    public static readonly IReadOnlyList<(string Pergunta, string Resposta)> Perguntas =
    [
        ("Sou autônomo. Consigo financiar?",
            "Na maioria dos casos, sim. A renda de autônomo pode ser comprovada com extratos bancários, declaração de imposto de renda ou outros documentos. Na simulação eu vejo com você qual é o melhor caminho para o seu caso."),
        ("Casal pode somar a renda?",
            "Pode, sim. Casados, em união estável ou até pessoas que vão comprar juntas podem somar a renda no financiamento. Isso costuma ajudar bastante no valor que cabe na parcela."),
        ("Posso usar o FGTS?",
            "Se você tem carteira assinada há pelo menos três anos (somando todos os empregos) e não tem outro imóvel na cidade, normalmente pode usar o FGTS na entrada ou para amortizar o financiamento. Eu confiro as regras certinhas com você."),
        ("Preciso ter dinheiro guardado para a entrada?",
            "Nem sempre de uma vez. Em muitos imóveis na planta a entrada pode ser parcelada com a construtora durante a obra, e o FGTS também pode compor esse valor."),
        ("Quanto tempo leva todo o processo?",
            "Depende do imóvel e da análise de crédito, mas eu acompanho cada etapa e te aviso do que vem a seguir. Você não fica sem saber em que pé está."),
        ("A simulação tem algum custo?",
            "Não. A conversa e a simulação são sem compromisso. É o primeiro passo para você entender o que cabe no seu bolso."),
        ("Qual a diferença entre SAC e PRICE?",
            "São dois jeitos de pagar a dívida. No SAC a parcela começa um pouco maior e vai diminuindo com o tempo. Na PRICE a parcela fica mais parecida do começo ao fim. Na simulação eu te mostro os dois para você escolher com tranquilidade."),
        ("Em quanto tempo posso pagar?",
            "O prazo pode chegar a até 35 anos, dependendo da modalidade e da sua idade. Um prazo maior deixa a parcela menor; um prazo menor faz você pagar menos juros no total. A gente encontra o equilíbrio na simulação."),
        ("O seguro do financiamento é obrigatório?",
            "Sim, é obrigatório por lei e já vem dentro da parcela. São dois: um protege a família em caso de morte ou invalidez permanente de quem compôs a renda, e o outro cobre danos físicos ao imóvel."),
        ("Dá para financiar as despesas de cartório?",
            "Na maioria das modalidades, sim: os custos de cartório podem entrar no financiamento, conforme as regras do banco na contratação. Eu te passo todos os custos antes de começar."),
        ("Depois de comprar, ainda posso usar o FGTS?",
            "Pode. O FGTS pode ser usado para amortizar ou quitar a dívida e também para pagar parte das parcelas, seguindo as regras do programa."),
        ("Posso levar meu financiamento para outro banco depois?",
            "Pode. Isso se chama portabilidade: a qualquer momento você pode transferir o financiamento para outro banco, se encontrar condições melhores, seguindo as regras do Banco Central."),
    ];

    /// <summary>Depoimentos reais de clientes: só o primeiro nome, sem foto.</summary>
    public static readonly IReadOnlyList<(string Nome, string Texto)> Depoimentos =
    [
        ("Carlos",
            "Muito obrigado por você não ter desistido de encontrar um imóvel que batesse com a renda que eu tenho. Muito obrigado mesmo, de coração. Deus abençoe!"),
        ("Marcos e Maieli",
            "A gente que agradece o seu empenho e a sua dedicação, que são perceptíveis. Excelente profissional! Teve uma paciência destacável conosco, sempre procurando a melhor oportunidade que se encaixasse nas nossas condições. Você foi muito importante nesse processo. Obrigada por não desistir de nós!"),
        ("Caroline e Luiz",
            "Muito obrigada, Bruna, pelo carinho! Estamos muito felizes com essa conquista: um sonho realizado, o início de uma nova jornada em nossas vidas. Que Deus abençoe sempre!"),
    ];
}
