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
