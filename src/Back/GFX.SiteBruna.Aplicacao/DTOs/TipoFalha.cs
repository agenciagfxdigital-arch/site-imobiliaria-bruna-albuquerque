namespace GFX.SiteBruna.Aplicacao.DTOs;

/// <summary>Motivo da falha de um caso de uso; a Api traduz em status HTTP.</summary>
public enum TipoFalha
{
    Validacao,
    NaoEncontrado,
    Conflito,
    NaoAutorizado,
    Limite,
}
