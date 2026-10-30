using GFX.SiteBruna.Aplicacao.DTOs.Imagens;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

/// <summary>
/// Converte a imagem para o padrão do site: rotação corrigida, lado maior limitado, WebP e SEM metadados
/// (o EXIF de fotos de celular traz a localização GPS, que revelaria o endereço do imóvel).
/// Lança <c>DomainException</c> de validação se a imagem não puder ser lida.
/// </summary>
public interface IProcessadorDeImagens
{
    Task<ImagemProcessada> Processar(byte[] conteudo);
}
