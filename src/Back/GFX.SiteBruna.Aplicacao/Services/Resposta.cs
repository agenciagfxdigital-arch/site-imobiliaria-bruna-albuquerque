using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Modelo.Exceptions;

namespace GFX.SiteBruna.Aplicacao.Services;

/// <summary>Executa um caso de uso e transforma erro de negócio em <see cref="ApiResponse{T}"/>.</summary>
internal static class Resposta
{
    public static async Task<ApiResponse<T>> De<T>(Func<Task<T>> casoDeUso)
    {
        try
        {
            return ApiResponse<T>.Ok(await casoDeUso());
        }
        catch (DomainException erro)
        {
            return ApiResponse<T>.De(erro);
        }
    }

    public static Task<ApiResponse<bool>> De(Func<Task> casoDeUso) => De(async () =>
    {
        await casoDeUso();
        return true;
    });
}
