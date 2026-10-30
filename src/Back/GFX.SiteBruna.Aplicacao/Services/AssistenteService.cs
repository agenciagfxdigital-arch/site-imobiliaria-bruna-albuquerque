using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.Extensions.Logging;

namespace GFX.SiteBruna.Aplicacao.Services;

/// <summary>
/// Assistente do painel pelo WhatsApp (fase 1, sem IA). O webhook só grava as mensagens na fila
/// (<see cref="Receber"/>); <see cref="ProcessarFila"/> trata uma por vez, por operador. O que aparece no site só muda
/// quando a Bruna toca no botão (ou responde "sim" a uma única confirmação aberta), nunca por um texto qualquer.
/// </summary>
public sealed class AssistenteService
{
    /// <summary>Tempo de cada reserva do operador; é renovada a cada mensagem tratada.</summary>
    private static readonly TimeSpan DuracaoDaReserva = TimeSpan.FromMinutes(2);

    private readonly IntegracaoService _integracao;
    private readonly ICanalWhatsapp _canal;
    private readonly IFilaDeMensagens _fila;
    private readonly IOperadorRepository _operadores;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRelogio _relogio;
    private readonly IEspera _espera;
    private readonly ILogger<AssistenteService> _logger;

    internal AssistenteService(
        IntegracaoService integracao, ICanalWhatsapp canal, IFilaDeMensagens fila, IOperadorRepository operadores,
        IUnitOfWork unitOfWork, IRelogio relogio, IEspera espera, ILogger<AssistenteService> logger)
    {
        _integracao = integracao;
        _canal = canal;
        _fila = fila;
        _operadores = operadores;
        _unitOfWork = unitOfWork;
        _relogio = relogio;
        _espera = espera;
        _logger = logger;
    }

    /// <summary>
    /// Grava as mensagens recebidas pelo webhook e devolve os telefones que têm algo para processar.
    /// Telefone não autorizado fica registrado como ignorado e não recebe resposta.
    /// </summary>
    public async Task<IReadOnlyList<string>> Receber(IReadOnlyList<MensagemRecebida> mensagens)
    {
        HashSet<string> telefones = [];
        foreach (var m in mensagens)
        {
            var op = await _integracao.OperadorAutorizado(m.Telefone);
            if (op is null)
            {
                await _fila.Inserir(MensagemDoWhatsapp.Ignorada(m, null, "operador não autorizado"));
                continue;
            }
            var recentes = await _fila.ContarDesde(op.Id, _relogio.Agora.AddHours(-1));
            if (recentes >= LimitesDaIntegracao.EscritasPorHora)
            {
                var nova = await _fila.Inserir(MensagemDoWhatsapp.Ignorada(m, op.Id, "limite por hora"));
                if (nova && recentes == LimitesDaIntegracao.EscritasPorHora) await _canal.EnviarTexto(m.Telefone, TextosDoAssistente.Limite);
                continue;
            }
            if (await _fila.Inserir(MensagemDoWhatsapp.ParaProcessar(m, op.Id))) telefones.Add(m.Telefone);
        }
        return [.. telefones];
    }

    /// <summary>
    /// Trata as pendentes do operador, em ordem. Se outro processamento já estiver com o operador, sai na hora (aquele
    /// pega as mensagens novas). Ao fim de um lote de fotos, espera <see cref="Assistente.EsperaFotos"/> sem mensagem
    /// nova antes de responder.
    /// </summary>
    public async Task ProcessarFila(string telefone)
    {
        var op = await _integracao.OperadorAutorizado(telefone);
        if (op is null) return;
        var operadorId = op.Id;
        var token = Guid.NewGuid().ToString("N");
        Task<bool> Reservar() => _operadores.Reservar(operadorId, token, _relogio.Agora + DuracaoDaReserva, _relogio.Agora);

        for (var rodada = 0; rodada < 3; rodada++)
        {
            if (!await Reservar()) return;
            try
            {
                HashSet<string> avisos = [];
                var fotosSemResposta = false;
                while (true)
                {
                    if (await _fila.ProximaPendente(operadorId) is { } m)
                    {
                        var entrou = await Tratar(m, avisos);
                        fotosSemResposta = m.EhFoto ? fotosSemResposta || entrou : false;
                        await Reservar();
                        continue;
                    }
                    if (!fotosSemResposta) break;
                    await _espera.Esperar(Assistente.EsperaFotos);
                    await Reservar();
                    if (await _fila.ProximaPendente(operadorId) is not null) continue;
                    await ResponderFotos(telefone);
                    break;
                }
            }
            finally
            {
                await _operadores.Liberar(operadorId, token);
            }
            // Uma mensagem pode ter chegado entre a última leitura e a liberação.
            if (await _fila.ProximaPendente(operadorId) is null) return;
        }
    }

    // ---------------------------------------------------------------------------------------------

    /// <summary>Trata uma mensagem; erros de negócio viram resposta amigável e não param a fila. Devolve se uma foto entrou.</summary>
    private async Task<bool> Tratar(MensagemDoWhatsapp m, ISet<string> avisos)
    {
        var op = await _integracao.OperadorAutorizado(m.Telefone); // relido a cada mensagem (o imóvel atual muda)
        if (op is null)
        {
            m.Ignorar("operador não autorizado");
            await _fila.Gravar(m);
            return false;
        }
        try
        {
            var fotoEntrou = await Executar(op, m.Telefone, Assistente.Interpretar(m.Conteudo), avisos);
            m.MarcarProcessada();
            await _fila.Gravar(m);
            return fotoEntrou;
        }
        catch (DomainException erro)
        {
            _unitOfWork.Descartar();
            await EnviarSemFalhar(m.Telefone, TextoDoErro(erro));
            m.MarcarProcessada(erro.Message);
            await _fila.Gravar(m);
            return false;
        }
        catch (Exception erro)
        {
            _logger.LogError(erro, "Assistente: erro ao tratar a mensagem {Wamid}", m.Wamid);
            _unitOfWork.Descartar();
            await EnviarSemFalhar(m.Telefone, TextosDoAssistente.ErroInesperado);
            m.MarcarErro(erro.Message);
            await _fila.Gravar(m);
            return false;
        }
    }

    private async Task<bool> Executar(Operador op, string telefone, Comando comando, ISet<string> avisos)
    {
        switch (comando)
        {
            case Comando.Foto foto:
                return await TratarFoto(op, telefone, foto, avisos);
            case Comando.FichaEnviada ficha:
                await TratarFicha(op, telefone, ficha.Texto);
                break;
            case Comando.PedirFicha:
                await _canal.EnviarTexto(telefone, TextosDoAssistente.InstrucaoFicha);
                await _canal.EnviarTexto(telefone, Ficha.Modelo(Finalidade.Venda));
                await _canal.EnviarTexto(telefone, Ficha.Modelo(Finalidade.Aluguel));
                break;
            case Comando.Selecionar selecionar:
                var imovel = await _integracao.SelecionarImovel(op, selecionar.Codigo);
                await _canal.EnviarTexto(telefone, TextosDoAssistente.ImovelSelecionado(imovel.Codigo, imovel.Titulo, imovel.Status));
                break;
            case Comando.Publicar publicar:
                var codigo = publicar.Codigo ?? (await _integracao.Sessao(op)).ImovelAtual?.Codigo
                    ?? throw DomainException.Validacao("Qual imóvel? Mande, por exemplo, *publicar BA107*.");
                await ProporPublicacao(op, telefone, codigo, prefixo: null);
                break;
            case Comando.Confirmar confirmar:
                await Confirmar(op, telefone, confirmar.AcaoId);
                break;
            case Comando.Cancelar cancelar:
                await Cancelar(op, telefone, cancelar.AcaoId);
                break;
            case Comando.Sim:
                await ResponderPendente(op, telefone, aceitar: true);
                break;
            case Comando.Nao:
                await ResponderPendente(op, telefone, aceitar: false);
                break;
            case Comando.NaoSuportado:
                await _canal.EnviarTexto(telefone, TextosDoAssistente.NaoSuportado);
                break;
            default:
                await _canal.EnviarTexto(telefone, TextosDoAssistente.Ajuda);
                break;
        }
        return false;
    }

    private async Task TratarFicha(Operador op, string telefone, string texto)
    {
        var ficha = Ficha.Ler(texto);
        List<string> problemas = [.. ficha.Problemas];
        var campos = ficha.Campos;

        if (ficha.Bairro is { } escrito)
        {
            var bairros = await _integracao.Bairros();
            if (bairros.FirstOrDefault(b => b.TemONome(escrito)) is { } achado)
            {
                campos = campos with { BairroId = achado.Id };
            }
            else
            {
                var normalizado = Texto.Normalizar(escrito);
                var parecidos = bairros
                    .Where(b => Texto.Normalizar(b.Nome).Contains(normalizado) || normalizado.Contains(Texto.Normalizar(b.Nome)))
                    .Take(3).Select(b => b.Nome).ToList();
                problemas.Add(parecidos.Count > 0
                    ? $"Bairro: não encontrei \"{escrito}\". Seria {string.Join(" ou ", parecidos)}?"
                    : $"Bairro: não encontrei \"{escrito}\" no cadastro. Confira o nome ou cadastre o bairro no painel.");
            }
        }
        if (problemas.Count > 0)
        {
            await _canal.EnviarTexto(telefone, TextosDoAssistente.ProblemasDaFicha(problemas));
            return;
        }

        if (ficha.Codigo is null)
        {
            var criado = await _integracao.CriarRascunho(op, campos);
            await _canal.EnviarTexto(telefone, TextosDoAssistente.RascunhoCriado(criado.Codigo, criado.Avisos, criado.LinkPrevia));
            return;
        }

        var imovel = await _integracao.ExigirImovelPorCodigo(ficha.Codigo);
        if (campos.Finalidade is { } finalidade && finalidade != imovel.Finalidade)
        {
            await _canal.EnviarTexto(telefone, TextosDoAssistente.ProblemasDaFicha(
            [
                $"Finalidade: o {imovel.Codigo} é de {Formatos.Rotulo(imovel.Finalidade).ToLowerInvariant()}. Para mudar a finalidade, use o painel.",
            ]));
            return;
        }
        campos = campos with { Finalidade = null };

        if (imovel.Status == StatusImovel.Rascunho)
        {
            var atualizado = await _integracao.AtualizarRascunho(op, imovel, campos);
            await _canal.EnviarTexto(telefone,
                TextosDoAssistente.RascunhoAtualizado(atualizado.Codigo, atualizado.CamposAlterados, atualizado.Avisos));
            return;
        }
        var proposta = await _integracao.ProporAlteracao(op, imovel, campos);
        await _canal.EnviarBotoes(telefone, proposta.Resumo, Assistente.BotoesDeConfirmacao(proposta.AcaoId));
    }

    private async Task<bool> TratarFoto(Operador op, string telefone, Comando.Foto foto, ISet<string> avisos)
    {
        var codigo = foto.Codigo ?? (await _integracao.Sessao(op)).ImovelAtual?.Codigo;
        if (codigo is null)
        {
            if (avisos.Add("sem_imovel")) await _canal.EnviarTexto(telefone, TextosDoAssistente.SemImovelParaFotos);
            return false;
        }
        var conteudo = await _canal.BaixarMidia(foto.MidiaId);
        try
        {
            await _integracao.AdicionarFoto(op, codigo, conteudo);
            return true;
        }
        catch (DomainException erro) when (erro.Codigo == CodigoErro.Conflito)
        {
            _unitOfWork.Descartar();
            if (avisos.Add($"publicado:{codigo}")) await _canal.EnviarTexto(telefone, TextosDoAssistente.FotoDeImovelPublicado(codigo));
            return false;
        }
    }

    private async Task ProporPublicacao(Operador op, string telefone, string codigo, Func<string, string>? prefixo)
    {
        var proposta = await _integracao.ProporPublicacao(op, codigo);
        var texto = prefixo is null ? proposta.Resumo : prefixo(proposta.Resumo);
        await _canal.EnviarBotoes(telefone, texto, Assistente.BotoesDePublicacao(proposta.AcaoId));
    }

    private async Task Confirmar(Operador op, string telefone, Guid acaoId)
    {
        var concluida = await _integracao.Confirmar(op, acaoId);
        await _canal.EnviarTexto(telefone, TextosDoAssistente.ComLink(concluida.Mensagem, concluida.LinkPublico));
    }

    private async Task Cancelar(Operador op, string telefone, Guid acaoId)
    {
        var cancelada = await _integracao.Cancelar(op, acaoId);
        await _canal.EnviarTexto(telefone, cancelada.Tipo == TipoAcao.Publicar && cancelada.Codigo.Length > 0
            ? TextosDoAssistente.PublicacaoAdiada(cancelada.Codigo)
            : cancelada.Mensagem);
    }

    /// <summary>"sim" / "não" digitados: só valem se houver exatamente uma confirmação aberta.</summary>
    private async Task ResponderPendente(Operador op, string telefone, bool aceitar)
    {
        var pendentes = (await _integracao.Sessao(op)).Pendentes;
        if (pendentes.Count == 0) await _canal.EnviarTexto(telefone, TextosDoAssistente.NadaPendente);
        else if (pendentes.Count > 1) await _canal.EnviarTexto(telefone, TextosDoAssistente.VariasPendentes);
        else if (aceitar) await Confirmar(op, telefone, pendentes[0].Id);
        else await Cancelar(op, telefone, pendentes[0].Id);
    }

    /// <summary>Ao fim de um lote de fotos: resumo do anúncio + botões [Publicar] [Agora não].</summary>
    private async Task ResponderFotos(string telefone)
    {
        var op = await _integracao.OperadorAutorizado(telefone);
        if (op is null) return;
        var sessao = await _integracao.Sessao(op);
        if (sessao.ImovelAtual is not { } imovel) return;
        try
        {
            await ProporPublicacao(op, telefone, imovel.Codigo, TextosDoAssistente.FotosRecebidas);
        }
        catch (DomainException)
        {
            _unitOfWork.Descartar();
            await _canal.EnviarTexto(telefone, $"Fotos recebidas ✅ O {imovel.Codigo} está com {sessao.TotalDeFotos} foto(s).");
        }
    }

    private static string TextoDoErro(DomainException erro) =>
        erro.Sugestoes.Count > 0 ? $"{erro.Message}\nVeja se é um destes: {string.Join(", ", erro.Sugestoes)}." : erro.Message;

    private async Task EnviarSemFalhar(string telefone, string texto)
    {
        try
        {
            await _canal.EnviarTexto(telefone, texto);
        }
        catch (Exception erro)
        {
            _logger.LogError(erro, "Assistente: não consegui responder no WhatsApp");
        }
    }
}
