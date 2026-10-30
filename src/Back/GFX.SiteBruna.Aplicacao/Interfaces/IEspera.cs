namespace GFX.SiteBruna.Aplicacao.Interfaces;

/// <summary>Pausa (a espera do fim de um lote de fotos). Nos testes, não espera de verdade.</summary>
public interface IEspera
{
    Task Esperar(TimeSpan tempo);
}
