# CLAUDE.md — Site Bruna Albuquerque (.NET)

Migração para .NET 10 do site + painel da corretora Bruna Albuquerque (Videira/SC). O original em Next.js fica em
`C:\dev\GFX\ProjetoSiteImoveisBrunaAlbuquerque` e é a referência de regras, textos e testes.
Leia `docs/ARQUITETURA.md`, `docs/DECISOES.md`, `docs/PLANO-MIGRACAO.md` e `docs/CONFIGURACAO.md` (chaves, rotas, primeiro usuário).

## Regras do desenvolvedor (obrigatórias)

- **Nunca** fazer commit, pull, push ou pull request. O desenvolvedor faz. Ao terminar uma etapa, sugira a mensagem de commit.
- **Nunca** aplicar migrations (`dotnet ef database update`) nem mexer no banco de produção. Crie a migration e explique como aplicar.
- Consultas somente leitura ao banco local para diagnóstico são permitidas.
- Não colocar segredos em arquivos versionados nem repetir valores de chaves/senhas nas respostas.

## Arquitetura (resumo)

- **Todas as regras de negócio no Modelo**, com teste em `Testes/Modelo`. O Modelo não referencia nenhum projeto nem pacote.
- **A Api não acessa dados**: o Dados é todo `internal`, exceto `DependencyInjection`/`VerificacaoBanco`.
- O Front não referencia o back. `Testes/Arquitetura/CamadasTests.cs` confere essas regras.
- Um tipo por arquivo `.cs`; nomes em português; `DomainException` com mensagem pronta para o usuário.

## Comandos

- `dotnet test GFX.SiteBruna.Back.sln` (inclui `Testes.Integracao`, que sobe um Postgres descartável no Docker via
  Testcontainers; só os de unidade: `dotnet test src/Back/GFX.SiteBruna.Testes`) · `dotnet build GFX.SiteBruna.Front.sln`
- Migrations: `docs/MIGRATIONS.md` (`dotnet tool restore`; criar com `dotnet ef migrations add`; **aplicar é com o dev**).
- Antes de concluir qualquer mudança: build das duas solutions e todos os testes passando.
- O `Testes/Aplicacao/Mundo.cs` imita o EF Core e os UPDATEs atômicos do Dados: se o Dados mudar um comportamento,
  ajuste o `Mundo` junto.

## Conteúdo e tom

- Textos do site em primeira pessoa da Bruna, acolhedor, calmo e claro; sem urgência forçada.
- **Todo texto que o usuário vê é em português**, inclusive o que vem dos modelos do .NET/Blazor (aviso de reconexão,
  barra de erro, 404, página de erro, `lang="pt-BR"`). Nada de detalhe técnico para quem visita o site.
- Minha Casa Minha Vida: sem números de faixas fixos; tudo editável pelo painel.
- Objetivo principal: lead no WhatsApp. GA4/Meta Pixel só após consentimento de cookies.
- Imagens sem EXIF/GPS; o site nunca mostra o endereço exato.
