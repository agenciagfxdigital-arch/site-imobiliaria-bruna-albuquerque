using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GFX.SiteBruna.Front.Web.Services;

/// <summary>Mesmo formato da Api: camelCase e enums como texto ("Venda", "SalaComercial").</summary>
public static class Json
{
    public static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static HttpContent? Conteudo(object? corpo) => corpo is null ? null : JsonContent.Create(corpo, corpo.GetType(), options: Opcoes);
}
