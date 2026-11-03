namespace GFX.SiteBruna.Front.Web.Services;

/// <summary>A Api não entregou algo de que a página precisa. Vai para o log; o visitante vê só a página de erro.</summary>
public sealed class SiteIndisponivelException(string detalhe) : Exception($"A Api não respondeu como esperado: {detalhe}");
