# Decisões

## ADR-001 · Migrar o site da Bruna de Next.js para .NET

**Data:** 2026-10-30 · **Status:** Aceita

**Contexto.** O site e o painel estão prontos em Next.js 16 + Supabase (repositório
`ProjetoSiteImoveisBrunaAlbuquerque`), mas a equipe que vai manter domina .NET.

**Decisão.** Reescrever em .NET 10, numa solução própria (`C:\dev\GFX\SiteBrunaAlbuquerque.Net`), no mesmo molde do
sistema interno: Modelo, Aplicacao, Dados, Api e Front separados. O site em Next continua no ar até a versão .NET ser
validada; a troca é no DNS.

**Consequências.**
- ✅ Código na tecnologia da equipe e com as mesmas regras de arquitetura do sistema interno.
- ➖ Durante a migração, ajustes pedidos pela Bruna valem para as duas versões (ou ficam congelados).

## ADR-002 · Front em Blazor separado, falando com a Api por HTTP

**Data:** 2026-10-30 · **Status:** Aceita

**Decisão.** `Front.Web` (Blazor, .NET 10) em solution própria, sem referência ao back (como o ADR-011 do sistema
interno). Diferente do sistema interno, o **site público usa renderização estática no servidor** (HTML pronto para o
Google, sem WebSocket); só o painel é interativo.

## ADR-003 · Schema por migrations do EF Core

**Data:** 2026-10-30 · **Status:** Aceita

**Contexto.** O banco de produção no Supabase ainda está vazio (nenhuma migração aplicada em 2026-10-28).

**Decisão.** O schema passa a mudar só por migrations do EF Core (`Dados/Migrations`), mantendo os nomes de tabela e
coluna em snake_case do site atual. Quem aplica é o dev; a Api só avisa no log se faltar migration.
Regras que estavam em triggers (código em maiúsculas, data de venda, visibilidade pública) passam para o Modelo.

## ADR-004 · Login próprio; fotos no Supabase Storage

**Data:** 2026-10-30 · **Status:** Aceita

**Decisão.** Login do painel como no sistema interno (hash do ASP.NET Core Identity + token), sem Supabase Auth. As
fotos dos imóveis continuam no Supabase Storage, gravadas pelo Dados com a chave de serviço; o site serve as URLs
públicas sem pesar na VPS. Processamento de imagem (WebP, sem EXIF/GPS) no Dados.

**Biblioteca de imagem (2026-10-30):** SkiaSharp (MIT), sem a questão de licença comercial do ImageSharp. Regrava
a imagem a partir dos pixels, então nenhum metadado (EXIF/GPS) sobrevive; a orientação do EXIF é aplicada antes.
Limitação: não lê AVIF nem HEIC (são recusados com mensagem; o WhatsApp já converte para JPEG).

## ADR-006 · Conteúdo editável tipado, com os textos padrão no Modelo

**Data:** 2026-10-30 · **Status:** Aceita

**Decisão.**
- Cada página editável é um `record` no Modelo (`ConteudoHome`, `ConteudoSobre`, `ConteudoMcmv`), com os textos padrão
  (`Padrao`) e os limites de tamanho. Os três foram **gerados a partir do `seed-data.ts` do site em Next**, então os
  textos são idênticos aos revisados pela Bruna; as mensagens de erro usam os rótulos do painel (`page-schema.ts`).
- `Configuracoes` e `Paginas` são linhas únicas que só entram no banco quando salvas; até lá o site usa os padrões
  (WhatsApp 49 99930-4262, CRECI/SC 60723, selo de vendido por 30 dias).
- Usuários do painel com login próprio (`usuarios`), senha só como hash (PBKDF2 do Identity, atrás de `IHasherSenha`),
  mesma regra de senha forte do sistema interno e a mesma mensagem para e-mail inexistente, senha errada e usuário
  desativado.
- Eventos sem dados pessoais nem IP; robôs e pré-visualizações de link são ignorados no Modelo (`Navegador`).

**Consequências.**
- ✅ Campo novo numa página vira erro de compilação onde precisa ser tratado, não um JSON solto.
- ➖ Mudar um texto padrão é mudança de código; a Bruna edita pelo painel e o que ela salva vale sobre o padrão.

## ADR-005 · Schema `site`, estado completo em jsonb e uma capa só pelo Modelo

**Data:** 2026-10-30 · **Status:** Aceita

**Decisão.**
- Tabelas no schema `site` (inclusive o histórico de migrations), não no `public`: o Supabase expõe o `public` pela
  Data API para a chave anônima, e aqui só a Api acessa o banco. Nomes de tabela iguais aos do site em Next.
- Ação pendente e auditoria guardam o estado completo do imóvel (`DadosDoImovel`) em `jsonb`; enums gravados como
  texto em snake_case (`sala_comercial`), com `check` na coluna.
- Sem índice único de capa por imóvel: trocar a capa atualiza duas linhas no mesmo `SaveChanges`, e o Postgres
  confere índice único linha a linha. "Uma capa por imóvel" é regra do Modelo (`PlanejamentoDeFotos`), testada.
- Reservas (operador, confirmação) e a fila do WhatsApp gravam na hora com `ExecuteUpdate`/contexto próprio, fora do
  `IUnitOfWork`.

**Consequências.**
- ✅ Banco fechado para acesso direto; mesmo com a chave anônima vazada, não há tabela exposta.
- ➖ O conteúdo de `jsonb` acompanha o formato do `DadosDoImovel`: renomear uma propriedade exige cuidado com os
  registros antigos de auditoria (que só servem para desfazer em 30 minutos).

## ADR-007 · Site público estático com um script só; painel interativo

**Data:** 2026-11-03 · **Status:** Aceita

**Contexto.** No Next, o site tinha componentes "use client" (filtros, galeria, cookies, métricas). No Blazor, deixar
o site interativo exigiria um circuito (WebSocket) por visitante.

**Decisão.**
- Páginas do site em renderização estática no servidor, sem `blazor.web.js`. O comportamento no navegador fica em
  `wwwroot/js/site.js` (JavaScript puro, sem dependências). Filtros são formulários GET: sem JavaScript, a página recebe
  `faixa=` e campos vazios e redireciona para o endereço limpo; com JavaScript, o endereço já sai limpo.
- Mesmos endereços e parâmetros do Next (`/imovel/{codigo}-{slug}`, `?tipo=casa&preco_min=...`): links de anúncios
  continuam valendo. Slug antigo redireciona com 301 para o atual.
- Painel interativo (`InteractiveServer`, sem pré-renderização), como o sistema interno.
- Tailwind v4 com o mesmo tema; o CSS gerado vai no repositório para o build .NET não depender de Node.

**Consequências.**
- ✅ HTML pronto, leve e com cache; nenhuma conexão aberta por visitante.
- ➖ Classe Tailwind nova exige regenerar o CSS (`Estilos/LEIAME.md`).

## ADR-008 · Cache de saída no lugar do ISR, invalidado pela Api

**Data:** 2026-11-03 · **Status:** Aceita

**Decisão.** Output caching do ASP.NET Core nas páginas do site (5 minutos, por endereço e query, etiqueta `site`); a
Api chama `POST /_cache/invalidar` (cabeçalho `X-Chave-Cache`) a cada escrita, e o selo de vendido vence sozinho pelo
prazo do cache. O Blazor grava o cookie de antiforgery em toda página estática, o que impediria o cache: como o site
não tem formulário POST, esse cookie é retirado das respostas do site (`Cache/SemAntiforgeryNoSite`); o painel mantém.

**Consequências.**
- ✅ A edição no painel ou pelo WhatsApp aparece no site na hora (testado de ponta a ponta).
- ➖ Se um dia o site tiver formulário POST, a página dele precisa ficar fora dessa regra.

## ADR-009 · Visibilidade no site como expressão do Modelo

**Data:** 2026-11-03 · **Status:** Aceita

**Decisão.** `Imovel.VisivelNoSite(agora, dias)` devolve a regra como `Expression<Func<Imovel,bool>>`, que o Dados usa na
consulta; `Imovel.EstaVisivelNoSite` continua para o objeto em memória. Um teste do Modelo garante que as duas formas
concordam, e um teste de integração confere a tradução para SQL. Endereço da página (`Imovel.Caminho`) e escolha de
semelhantes (`ImoveisSemelhantes`) também ficam no Modelo; o Front recebe o caminho pronto.
