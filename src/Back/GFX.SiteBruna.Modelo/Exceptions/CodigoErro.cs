namespace GFX.SiteBruna.Modelo.Exceptions;

/// <summary>Tipo do erro de negócio; a Api traduz em status HTTP.</summary>
public enum CodigoErro
{
    Validacao,
    NaoEncontrado,
    CodigoDuplicado,
    /// <summary>Operador sem permissão (telefone não autorizado ou desativado).</summary>
    NaoAutorizado,
    /// <summary>O estado mudou desde a proposta (imóvel editado no painel, ação já confirmada).</summary>
    Conflito,
    /// <summary>Prazo da ação pendente acabou.</summary>
    Expirada,
    /// <summary>Limite de uso atingido.</summary>
    Limite,
}
