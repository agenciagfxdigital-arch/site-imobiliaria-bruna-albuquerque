namespace GFX.SiteBruna.Aplicacao.Interfaces;

/// <summary>Hora atual. Os casos de uso passam o "agora" para o domínio; nos testes, o relógio é controlado.</summary>
public interface IRelogio
{
    DateTimeOffset Agora { get; }
}
