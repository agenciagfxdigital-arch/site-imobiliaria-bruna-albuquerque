# Arquitetura

> Documento vivo. Atualize sempre que uma camada, padrão ou dependência entre projetos mudar.
> Segue o molde do sistema interno da GFX (`C:\dev\GFXSistemaInterno\docs\ARQUITETURA.md`).

## 1. Visão geral

Backend em Clean Architecture (Onion), com quatro projetos em `src/Back/` (`GFX.SiteBruna.Back.sln`).
**As dependências apontam sempre para dentro.** O front fica em `src/Front/` (`GFX.SiteBruna.Front.sln`) e fala
com a Api só por HTTP.

```
Api ──▶ Aplicacao ──▶ Modelo
 └ ─ (só AddDados) ─▶ Dados ──▶ Aplicacao, Modelo
Front.Web ── HTTP ──▶ Api
```

| Projeto | Camada |
|---|---|
| `GFX.SiteBruna.Modelo` | Domain |
| `GFX.SiteBruna.Aplicacao` | Application |
| `GFX.SiteBruna.Dados` | Infrastructure |
| `GFX.SiteBruna.Api` | API (transporte HTTP, webhook do WhatsApp) |
| `GFX.SiteBruna.Front.Web` | Site público e painel (Blazor) |

**Regras inegociáveis**

1. **Regras de negócio ficam no Modelo.** Validação, valores padrão, transições de estado, visibilidade no site e
   permissões são métodos das entidades ou classes de `Regras/`. A Aplicacao só orquestra. Inclui o que no site em
   Next estava em triggers do banco (data de venda, imóvel visível ao público).
2. **O Modelo não referencia nada:** nem os outros projetos, nem pacote NuGet. C# puro.
3. **A Api não acessa dados.** Ela referencia o Dados só para chamar `AddDados()`; todas as classes do Dados são
   `internal`. Usar `AppDbContext` ou um repositório na Api é erro de compilação (CS0122).
4. **O Front não referencia o back.** Contratos copiados em `Front.Web/DTOs`, chamadas por `HttpClient`.

As regras 2 a 4 são conferidas por `Testes/Arquitetura/CamadasTests.cs`. Se um teste dali falhar, corrija a
dependência, não o teste.

## 2. Responsabilidade de cada camada

### `Modelo`
- `Entities/` — `private set`, construtor privado para o EF, construtor público e métodos de intenção que validam e
  lançam `DomainException`. Ex.: `new Imovel(...)`, `Imovel.Atualizar`, `Imovel.AlterarStatus`,
  `Imovel.EstaVisivelNoSite`, `Imovel.Copiar`, `DadosInternosImovel.Atualizar`.
- `Valores/` — entradas imutáveis que o domínio valida (`DadosDoImovel`).
- `Regras/` — regras sem estado (`Texto.Slug`, `Texto.Normalizar`; depois a ficha do WhatsApp).
- `Enums/`, `Exceptions/` (`DomainException` + `CodigoErro`, mensagens prontas em pt-BR).
- Hora sempre por parâmetro (`agora`): o domínio não lê o relógio.

### `Aplicacao`
- `Services/` — um serviço por caso de uso/módulo. Carrega, chama o domínio, `IUnitOfWork.Commit()`. Captura
  `DomainException` e devolve `ApiResponse<T>.De(erro)`.
- `Interfaces/` — repositórios, `IUnitOfWork`, `IRelogio`, armazenamento de fotos, envio do WhatsApp.
- `DTOs/` — `*Request`/`*Response` por agregado; `ApiResponse<T>` e `TipoFalha`.
- Sem `Microsoft.AspNetCore.*` nem EF Core.

### `Dados`
- `AppDbContext` (snake_case, mesmas tabelas do site em Next), `Configurations/`, `Repositories/`, `UnitOfWork`.
- Clientes externos: Supabase Storage (fotos), Cloud API do WhatsApp, processamento de imagem.
- Tudo `internal`, exceto `DependencyInjection` (`AddDados`) e `VerificacaoBanco`.

### `Api`
- Controllers finos (`ApiControllerBase` traduz `TipoFalha` em status HTTP), autenticação, webhook do WhatsApp
  (assinatura conferida no corpo bruto), limites de requisição, `GlobalExceptionHandler`.

### `Front.Web`
- **Site público** (`Components/Pages/Site`): renderização estática no servidor, HTML pronto para o Google, sem
  WebSocket. `[ExcludeFromInteractiveRouting]` no `_Imports` da pasta; o `App.razor` só carrega o `blazor.web.js` no
  painel. A interação do site (cookies, métricas, menu, filtros, galeria, mapa, compartilhar) fica em
  `wwwroot/js/site.js`, que só acrescenta comportamento: filtros são formulários GET e funcionam sem JavaScript.
- **Painel** (`Components/Pages/Painel`, rotas `/admin`): Blazor interativo no servidor, sem pré-renderização. A sessão
  (token da Api) fica no `localStorage` criptografada pelo Data Protection (`Autenticacao/SessaoDoPainel`).
- `Services/`: clientes da Api (`SiteApi` público; `ImovelApi`, `ConteudoApi`, `UsuarioApi` com token) que sempre
  devolvem `ApiResponse<T>`; `DadosDoSite` evita chamar a Api duas vezes na mesma requisição (layout + página).
- `Apresentacao/`: só formatação e leitura da URL (equivalente a `src/lib` do Next). Regras continuam na Api/Modelo.
- `Endpoints/RotasDoSite`: `sitemap.xml`, `robots.txt`, `POST /eventos` (repassa à Api, que fica na rede interna) e
  `POST /_cache/invalidar` (chamado pela Api a cada escrita).
- `Cache/`: cache de saída das páginas do site (5 min, separado por endereço e filtros), descartado na invalidação.
- CSS: Tailwind gerado a partir de `Estilos/site.css` para `wwwroot/css/site.css` (versionado; ver `Estilos/LEIAME.md`).

## 3. Convenções

- Nomes em português (domínio, DTOs, serviços); métodos de repositório em inglês (`GetById`, `Add`).
- `namespace` file-scoped, *primary constructors*, *collection expressions*, `Nullable` habilitado.
- **Um tipo por arquivo `.cs`**, com o nome do tipo.
- Chaves `Guid` v7 geradas na entidade.
- Banco em snake_case por convenção; o schema muda só por migrations do EF Core, aplicadas pelo dev.
