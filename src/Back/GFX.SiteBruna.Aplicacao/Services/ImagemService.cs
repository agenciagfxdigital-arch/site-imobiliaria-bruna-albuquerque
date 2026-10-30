using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Aplicacao.DTOs.Imagens;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Aplicacao.Services;

/// <summary>Recebe imagens: valida (Modelo), processa sem metadados/GPS e grava no storage.</summary>
public sealed class ImagemService(
    IImovelRepository imoveis,
    IProcessadorDeImagens processador,
    IArmazenamentoDeArquivos arquivos)
{
    private readonly IImovelRepository _imoveis = imoveis;
    private readonly IProcessadorDeImagens _processador = processador;
    private readonly IArmazenamentoDeArquivos _arquivos = arquivos;

    /// <summary>
    /// Imagem enviada pelo painel. <paramref name="pasta"/> é "imoveis/&lt;id&gt;" (foto de imóvel, que entra na lista
    /// quando o imóvel é salvo) ou uma pasta do site ("site", "depoimentos", "entregas").
    /// </summary>
    public Task<ApiResponse<ImagemEnviadaResponse>> Enviar(string? pasta, byte[] conteudo) =>
        Resposta.De(() => Gravar(Imagens.InterpretarDestino(pasta), conteudo));

    /// <summary>Grava num destino já validado. Usado também pelos casos de uso de imóveis.</summary>
    internal async Task<ImagemEnviadaResponse> Gravar(DestinoImagem destino, byte[] conteudo)
    {
        Imagens.ValidarArquivo(conteudo);
        if (destino.ImovelId is { } imovelId && await _imoveis.GetById(imovelId) is null)
            throw new DomainException(CodigoErro.NaoEncontrado, "Imóvel não encontrado.");

        var imagem = await _processador.Processar(conteudo);
        var caminho = Imagens.CaminhoDoArquivo(destino, Guid.CreateVersion7().ToString("D"));
        var url = await _arquivos.Enviar(caminho, imagem.Conteudo, imagem.Tipo);
        return new ImagemEnviadaResponse(url, caminho, Imagens.AvisoDeTamanho(imagem.Largura, imagem.Altura));
    }
}
