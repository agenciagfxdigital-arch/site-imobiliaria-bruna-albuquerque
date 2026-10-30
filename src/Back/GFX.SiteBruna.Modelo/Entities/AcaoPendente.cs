using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Modelo.Entities;

/// <summary>
/// Proposta feita pelo WhatsApp que só muda o site depois da confirmação da Bruna (botão ou "sim").
/// A execução é feita por código, a partir do que ficou gravado aqui, nunca do que a conversa disser depois.
/// </summary>
public sealed class AcaoPendente
{
    public Guid Id { get; private set; }
    public Guid OperadorId { get; private set; }
    public Guid ImovelId { get; private set; }
    public TipoAcao Tipo { get; private set; }
    /// <summary>Estado completo proposto (alterar e desfazer).</summary>
    public DadosDoImovel? DadosPropostos { get; private set; }
    public StatusImovel? NovoStatus { get; private set; }
    public bool? NovoDestaque { get; private set; }
    public List<MudancaCampo> Diferencas { get; private set; } = [];
    /// <summary>Texto mostrado à Bruna no momento da proposta.</summary>
    public string Resumo { get; private set; } = "";
    /// <summary>Última alteração do imóvel quando a proposta foi feita (detecta edição no meio do caminho).</summary>
    public DateTimeOffset BaseAtualizadoEm { get; private set; }
    public StatusAcao Status { get; private set; }
    public DateTimeOffset ExpiraEm { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    private AcaoPendente() { } // EF

    private AcaoPendente(Guid operadorId, Imovel imovel, TipoAcao tipo, string resumo, DateTimeOffset agora)
    {
        Id = Guid.CreateVersion7();
        OperadorId = operadorId;
        ImovelId = imovel.Id;
        Tipo = tipo;
        Resumo = resumo;
        BaseAtualizadoEm = imovel.AtualizadoEm;
        Status = StatusAcao.Pendente;
        CriadoEm = agora;
        ExpiraEm = agora.AddMinutes(LimitesDaIntegracao.ValidadeAcaoMinutos);
    }

    public static AcaoPendente Publicar(Guid operadorId, Imovel imovel, string resumo, DateTimeOffset agora)
    {
        imovel.GarantirQuePodeSerPublicado();
        return new AcaoPendente(operadorId, imovel, TipoAcao.Publicar, resumo, agora);
    }

    /// <param name="tipo"><see cref="TipoAcao.Alterar"/> ou <see cref="TipoAcao.Desfazer"/>.</param>
    public static AcaoPendente AlterarCampos(
        Guid operadorId, Imovel imovel, TipoAcao tipo, DadosDoImovel propostos, IReadOnlyList<MudancaCampo> diferencas,
        string resumo, DateTimeOffset agora)
    {
        if (tipo is not (TipoAcao.Alterar or TipoAcao.Desfazer))
            throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Use Alterar ou Desfazer.");
        if (diferencas.Count == 0) throw DomainException.Validacao("Esses valores já estão salvos. Nada para alterar.");
        return new AcaoPendente(operadorId, imovel, tipo, resumo, agora) { DadosPropostos = propostos, Diferencas = [.. diferencas] };
    }

    public static AcaoPendente MudarStatus(Guid operadorId, Imovel imovel, StatusImovel novo, string resumo, DateTimeOffset agora)
    {
        imovel.GarantirNovoStatusValido(novo);
        return new AcaoPendente(operadorId, imovel, TipoAcao.Status, resumo, agora) { NovoStatus = novo };
    }

    public static AcaoPendente MudarDestaque(Guid operadorId, Imovel imovel, bool destaque, string resumo, DateTimeOffset agora)
    {
        if (imovel.Destaque == destaque)
            throw new DomainException(CodigoErro.Conflito,
                $"O {imovel.Codigo} {(destaque ? "já está" : "já não está")} nos destaques.");
        return new AcaoPendente(operadorId, imovel, TipoAcao.Destaque, resumo, agora)
        {
            NovoDestaque = destaque,
            Diferencas = [new MudancaCampo("destaque", imovel.Destaque, destaque)],
        };
    }

    /// <summary>Regras para executar (ou cancelar) uma ação: existe, é do operador, está pendente e no prazo.</summary>
    public static AcaoPendente Verificar(AcaoPendente? acao, Guid operadorId, DateTimeOffset agora)
    {
        if (acao is null || acao.OperadorId != operadorId)
            throw new DomainException(CodigoErro.NaoEncontrado, "Essa confirmação não existe mais.");
        if (acao.Status == StatusAcao.Confirmada) throw new DomainException(CodigoErro.Conflito, "Essa ação já foi confirmada.");
        if (acao.Status == StatusAcao.Cancelada)
            throw new DomainException(CodigoErro.Conflito, "Essa ação foi cancelada. Peça de novo, se ainda quiser.");
        if (acao.Status == StatusAcao.Expirada || acao.ExpiraEm <= agora)
            throw new DomainException(CodigoErro.Expirada,
                $"A confirmação expirou (vale {LimitesDaIntegracao.ValidadeAcaoMinutos} minutos). Peça de novo.");
        return acao;
    }

    /// <summary>O imóvel não pode ter mudado entre a proposta e a confirmação (ex.: editado no painel).</summary>
    public void GarantirImovelInalterado(Imovel imovel)
    {
        if (imovel.Id != ImovelId || imovel.AtualizadoEm != BaseAtualizadoEm)
            throw new DomainException(CodigoErro.Conflito,
                $"O {imovel.Codigo} foi alterado depois desta proposta. Peça a alteração de novo para conferir os valores atuais.");
    }

    /// <summary>
    /// Executa a ação no imóvel (e nos dados internos, quando a proposta os inclui). Quem chama verificou antes
    /// com <see cref="Verificar"/>.
    /// </summary>
    public void Executar(Imovel imovel, DadosInternosImovel internos, string? nomeDoBairro, string autor, DateTimeOffset agora)
    {
        GarantirImovelInalterado(imovel);
        switch (Tipo)
        {
            case TipoAcao.Publicar:
                imovel.GarantirQuePodeSerPublicado();
                imovel.AlterarStatus(StatusImovel.Ativo, agora);
                break;
            case TipoAcao.Status:
                imovel.GarantirNovoStatusValido(NovoStatus!.Value);
                imovel.AlterarStatus(NovoStatus.Value, agora);
                break;
            case TipoAcao.Destaque:
                imovel.DefinirDestaque(NovoDestaque!.Value, agora);
                break;
            default:
                var dados = DadosPropostos!;
                imovel.Atualizar(dados, nomeDoBairro, agora);
                internos.Atualizar(dados.ProprietarioNome, dados.Endereco, autor, agora);
                break;
        }
        Status = StatusAcao.Confirmada;
    }

    public void Cancelar() => Status = StatusAcao.Cancelada;
}
