using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Modelo.Entities;

/// <summary>Toda escrita feita pelo WhatsApp fica registrada aqui (quem, o quê, antes e depois).</summary>
public sealed class RegistroDeAuditoria
{
    public const string CriarRascunho = "criar_rascunho";
    public const string AtualizarRascunho = "atualizar_rascunho";
    public const string AdicionarFoto = "adicionar_foto";
    public const string ConfirmarAcao = "confirmar_acao";

    public Guid Id { get; private set; }
    public Guid? OperadorId { get; private set; }
    public string Operacao { get; private set; } = "";
    public Guid? ImovelId { get; private set; }
    /// <summary>Detalhe livre para suporte (ex.: id da ação confirmada, caminho da foto).</summary>
    public string Detalhe { get; private set; } = "";
    /// <summary>Estado completo antes e depois (com dados internos): permite desfazer sem adivinhar tipos.</summary>
    public DadosDoImovel? Antes { get; private set; }
    public DadosDoImovel? Depois { get; private set; }
    public List<string> CamposAlterados { get; private set; } = [];
    public bool Sucesso { get; private set; }
    public string? Erro { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    private RegistroDeAuditoria() { } // EF

    private RegistroDeAuditoria(Guid operadorId, string operacao, Guid? imovelId, string detalhe, DateTimeOffset agora)
    {
        Id = Guid.CreateVersion7();
        OperadorId = operadorId;
        Operacao = operacao;
        ImovelId = imovelId;
        Detalhe = detalhe;
        Sucesso = true;
        CriadoEm = agora;
    }

    public static RegistroDeAuditoria Sucedido(
        Guid operadorId, string operacao, Guid? imovelId, DateTimeOffset agora,
        DadosDoImovel? antes = null, DadosDoImovel? depois = null, string detalhe = "")
    {
        var registro = new RegistroDeAuditoria(operadorId, operacao, imovelId, detalhe, agora) { Antes = antes, Depois = depois };
        if (antes is not null && depois is not null)
            registro.CamposAlterados = [.. Diferencas.Entre(antes, depois).Select(m => m.Campo)];
        return registro;
    }

    public static RegistroDeAuditoria Falhou(Guid operadorId, string operacao, Guid? imovelId, string erro, DateTimeOffset agora, string detalhe = "") =>
        new(operadorId, operacao, imovelId, detalhe, agora) { Sucesso = false, Erro = erro };

    /// <summary>Só dá para desfazer a própria ação confirmada, dentro da janela de tempo.</summary>
    public static RegistroDeAuditoria VerificarDesfazer(RegistroDeAuditoria? registro, Guid operadorId, DateTimeOffset agora)
    {
        if (registro is null || registro.OperadorId != operadorId || !registro.Sucesso || registro.Operacao != ConfirmarAcao)
            throw new DomainException(CodigoErro.NaoEncontrado, "Não encontrei uma alteração sua para desfazer.");
        if (agora - registro.CriadoEm > TimeSpan.FromMinutes(LimitesDaIntegracao.JanelaDesfazerMinutos))
            throw new DomainException(CodigoErro.Expirada,
                $"Só dá para desfazer até {LimitesDaIntegracao.JanelaDesfazerMinutos} minutos depois. Use o painel.");
        if (registro.ImovelId is null || registro.Antes is null || registro.CamposAlterados.Count == 0)
            throw new DomainException(CodigoErro.NaoEncontrado, "Não há o que desfazer nessa alteração.");
        return registro;
    }

    /// <summary>Estado proposto para desfazer: o atual com os campos que esta alteração mudou de volta como estavam.</summary>
    public DadosDoImovel EstadoParaDesfazer(DadosDoImovel atual) => Diferencas.Restaurar(atual, Antes!, CamposAlterados);
}
