using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GFX.SiteBruna.Dados.Data;

/// <summary>Colunas jsonb: estado do imóvel nas ações e na auditoria, diferenças e conteúdo das mensagens.</summary>
internal static class Json
{
    public static readonly JsonSerializerOptions Opcoes = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { ConteudoComTipo } },
        // O jsonb do Postgres reordena as chaves: o "tipo" pode não vir primeiro
        AllowOutOfOrderMetadataProperties = true,
    };

    public static ValueConverter<T, string> Conversor<T>() =>
        new(v => JsonSerializer.Serialize(v, Opcoes), v => JsonSerializer.Deserialize<T>(v, Opcoes)!);

    /// <summary>O conteúdo da mensagem leva o tipo junto: {"tipo":"imagem","midia_id":…}.</summary>
    private static void ConteudoComTipo(JsonTypeInfo info)
    {
        if (info.Type != typeof(ConteudoMensagem)) return;
        info.PolymorphismOptions = new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = "tipo",
            DerivedTypes =
            {
                new JsonDerivedType(typeof(ConteudoMensagem.Texto), "texto"),
                new JsonDerivedType(typeof(ConteudoMensagem.Imagem), "imagem"),
                new JsonDerivedType(typeof(ConteudoMensagem.Botao), "botao"),
                new JsonDerivedType(typeof(ConteudoMensagem.Outro), "outro"),
            },
        };
    }
}
