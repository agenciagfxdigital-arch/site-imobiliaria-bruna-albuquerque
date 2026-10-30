namespace GFX.SiteBruna.Modelo.Valores;

/// <param name="Id">Volta no webhook quando a Bruna toca ("ok:&lt;id da ação&gt;" ou "no:&lt;id da ação&gt;").</param>
/// <param name="Titulo">Até 20 caracteres (limite do WhatsApp).</param>
public sealed record BotaoDoWhatsapp(string Id, string Titulo)
{
    public const int TamanhoMaximoTitulo = 20;
}
