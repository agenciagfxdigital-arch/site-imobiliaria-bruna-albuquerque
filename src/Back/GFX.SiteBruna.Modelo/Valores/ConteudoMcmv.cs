// Gerado a partir dos padrões do site em Next (src/lib/seed-data.ts). Os textos são editáveis pelo painel.
using GFX.SiteBruna.Modelo.Regras;

namespace GFX.SiteBruna.Modelo.Valores;

/// <summary>Página educativa do Minha Casa Minha Vida (sem números de faixas fixos: as regras mudam).</summary>
public sealed record ConteudoMcmv
{
    public string Titulo { get; init; } = "";
    public string TextoAbertura { get; init; } = "";
    public string Foto { get; init; } = "";
    public string ComoFunciona { get; init; } = "";
    public string EntradaParcelada { get; init; } = "";
    public string Fgts { get; init; } = "";
    public IReadOnlyList<Passo> Entenda { get; init; } = [];
    public IReadOnlyList<Passo> Etapas { get; init; } = [];
    public IReadOnlyList<string> Documentos { get; init; } = [];
    public IReadOnlyList<string> DocumentosFgts { get; init; } = [];
    public string DocumentosObs { get; init; } = "";
    public string DicaPlanejamento { get; init; } = "";
    public string Aviso { get; init; } = "";

    /// <summary>Textos padrão (em primeira pessoa da Bruna), usados até a página ser salva pelo painel.</summary>
    public static ConteudoMcmv Padrao { get; } = new()
    {
        Titulo = "Comprar o primeiro imóvel é mais possível do que parece",
        TextoAbertura = "O Minha Casa Minha Vida ajuda muitas famílias a sair do aluguel com parcelas que cabem no orçamento. Eu te explico como funciona, faço a simulação com você e acompanho cada passo.",
        Foto = "",
        ComoFunciona = "O Minha Casa Minha Vida é um programa do Governo Federal que facilita a compra do imóvel próprio. Dependendo da renda da família, ele pode oferecer juros menores e, em alguns casos, um subsídio — um desconto que ajuda a pagar parte do imóvel.\n\nAs regras e faixas de renda são atualizadas de tempos em tempos. Por isso, o melhor caminho é fazer uma simulação com os seus dados: assim você sabe exatamente quanto fica a parcela no seu caso.",
        EntradaParcelada = "Em imóveis na planta, muitas construtoras permitem parcelar a entrada durante o período da obra. Isso significa que você não precisa ter todo o valor da entrada guardado de uma vez: ele é dividido em parcelas menores até a entrega do imóvel.",
        Fgts = "O FGTS pode ser usado para dar a entrada ou para diminuir o valor financiado. Em geral, é preciso ter pelo menos três anos de carteira assinada (somando todos os empregos) e não ter outro imóvel na mesma cidade.\n\nE ele continua ajudando depois da compra: dá para usar o FGTS para amortizar a dívida ou pagar parte das parcelas, conforme as regras do programa.",
        Entenda =
        [
            new("O que forma a parcela", "A parcela (o banco também chama de encargo mensal) é a soma de quatro partes: a amortização, que é o pedaço que de fato abate a dívida; os juros; o seguro habitacional; e as taxas. O que ainda falta pagar se chama saldo devedor. O prazo pode chegar a até 35 anos, dependendo da modalidade."),
            new("SAC: parcela que diminui", "No SAC (Sistema de Amortização Constante), você abate sempre o mesmo valor da dívida e os juros vão caindo mês a mês. Por isso a parcela começa um pouco mais alta e tende a diminuir com o tempo."),
            new("PRICE: parcela mais estável", "Na tabela PRICE, a parcela tende a ficar parecida do começo ao fim: no início ela paga mais juros e, com o tempo, mais dívida. É boa para quem prefere previsibilidade. Na simulação eu te mostro as duas opções lado a lado."),
            new("Correção pela TR", "No Minha Casa Minha Vida, o financiamento é corrigido pela TR (Taxa Referencial), um índice que costuma variar pouco. Isso deixa a parcela mais previsível ao longo dos anos."),
            new("Seguros obrigatórios", "Todo financiamento habitacional tem dois seguros obrigatórios por lei: o MIP, que paga parte ou toda a dívida em caso de morte ou invalidez permanente de quem compôs a renda, e o DFI, que cobre danos físicos ao imóvel. É uma proteção para a sua família."),
            new("Despesas de cartório", "Os custos com o cartório também podem entrar no financiamento, conforme as regras do banco na hora da contratação. Eu te passo todos os custos antes, para não ter surpresa."),
        ],
        Etapas =
        [
            new("Simulação", "A gente simula juntos quanto dá para financiar, o prazo e a parcela que cabe no seu orçamento. É uma estimativa: os valores finais saem na análise do banco."),
            new("Documentos e análise de crédito", "Eu te ajudo a reunir a documentação e entregar tudo de uma vez. Nessa etapa o banco faz a análise de crédito e confere como usar o seu FGTS."),
            new("Avaliação do imóvel", "Um engenheiro do banco vistoria o imóvel para confirmar o valor e se ele pode ser a garantia do financiamento. Aqui é paga a primeira parte da tarifa de avaliação."),
            new("Assinatura do contrato", "Compradores e vendedores assinam o contrato no dia agendado pelo banco. Nesse dia são pagos a segunda parte da tarifa de avaliação e os primeiros seguros."),
            new("Registro e entrega da chave", "O contrato é registrado no cartório em até 30 dias após a assinatura. Com o registro, o banco libera o valor para o vendedor e a chave é sua."),
        ],
        Documentos =
        [
            "Documento oficial de identificação (RG ou CNH) de todos que vão compor a renda",
            "Comprovante de renda atualizado, emitido há no máximo 2 meses",
            "Última declaração do Imposto de Renda com o recibo de entrega (se você declara)",
            "Certidão de nascimento ou de casamento",
        ],
        DocumentosFgts =
        [
            "Carteira de Trabalho (física ou digital) ou extrato do FGTS",
            "Última declaração do Imposto de Renda com o recibo de entrega",
        ],
        DocumentosObs = "Os documentos do vendedor e a certidão de matrícula do imóvel eu providencio junto com o vendedor ou a construtora. Dependendo do caso, o banco pode pedir algum documento a mais. Se acontecer, eu te aviso e te ajudo a separar.",
        DicaPlanejamento = "Antes da simulação, vale anotar quanto entra por mês (salário, benefícios), quanto sai (incluindo gastos que aparecem só uma vez por ano, como IPVA e material escolar) e quanto você tem guardado. Assim a gente encontra uma parcela que cabe no orçamento com folga e ainda sobra para uma reserva de emergência.",
        Aviso = "As condições dependem de análise de crédito e das regras vigentes do programa.",
    };

    /// <summary>Confere os tamanhos antes de gravar (mensagens prontas para o painel).</summary>
    public void Validar()
    {
        ConteudoDePagina.Texto(Titulo, "Título (Abertura)", 200);
        ConteudoDePagina.Texto(TextoAbertura, "Texto (Abertura)", 5000);
        ConteudoDePagina.Texto(Foto, "Foto (Abertura)", 1000);
        ConteudoDePagina.Texto(ComoFunciona, "Como funciona o programa", 5000);
        ConteudoDePagina.Texto(EntradaParcelada, "Entrada parcelada", 5000);
        ConteudoDePagina.Texto(Fgts, "FGTS", 5000);
        ConteudoDePagina.Passos(Entenda, "Cartões (Entenda a parcela)");
        ConteudoDePagina.Passos(Etapas, "Etapas (Etapas do financiamento)");
        ConteudoDePagina.Lista(Documentos, "Documentos de quem vai comprar");
        ConteudoDePagina.Lista(DocumentosFgts, "Para usar o FGTS");
        ConteudoDePagina.Texto(DocumentosObs, "Observação (vendedor e imóvel)", 5000);
        ConteudoDePagina.Texto(DicaPlanejamento, "Dica de planejamento antes da simulação", 5000);
        ConteudoDePagina.Texto(Aviso, "Aviso no fim da página", 500);
    }
}
