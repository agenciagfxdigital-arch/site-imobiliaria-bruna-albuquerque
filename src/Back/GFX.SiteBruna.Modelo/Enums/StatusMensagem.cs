namespace GFX.SiteBruna.Modelo.Enums;

public enum StatusMensagem
{
    Pendente,
    Processada,
    Erro,
    /// <summary>Telefone não autorizado ou acima do limite: registrada, sem resposta.</summary>
    Ignorada,
}
