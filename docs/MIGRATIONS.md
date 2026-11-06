# Migrations

O schema do banco muda **só por migrations do EF Core** (`src/Back/GFX.SiteBruna.Dados/Data/Migrations`), no schema
`site` (não no `public`, que o Supabase expõe pela Data API). **Quem aplica é o dev**; a Api só avisa no log, na
subida, se faltar alguma.

O `dotnet-ef` é ferramenta local (`dotnet-tools.json`): rode `dotnet tool restore` uma vez.

## Criar uma migration

```bash
dotnet ef migrations add NomeDaMudanca --project src/Back/GFX.SiteBruna.Dados --startup-project src/Back/GFX.SiteBruna.Dados --output-dir Data/Migrations
```

Não conecta em banco nenhum (a fábrica de design usa uma conexão fixa, sem senha). O EF gera a classe como `public`:
**troque para `internal`** (o Dados só expõe o registro da camada; o teste de arquitetura falha se esquecer).
Confira o SQL antes de aplicar:

```bash
dotnet ef migrations script --idempotent --project src/Back/GFX.SiteBruna.Dados --startup-project src/Back/GFX.SiteBruna.Dados -o migration.sql
```

## Aplicar

Sempre com `--connection` explícito (sem ele o comando aponta para a conexão fixa de design e falha):

```bash
dotnet ef database update --project src/Back/GFX.SiteBruna.Dados --startup-project src/Back/GFX.SiteBruna.Dados --connection "Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require"
```

- **Local:** um banco próprio (ex.: `site_bruna_dev` no Postgres do Supabase local, porta 54322), para não misturar
  com o schema do site em Next.
- **Produção (Supabase):** use o *Session pooler* com SSL. Alternativa sem CLI: gere o script idempotente acima e
  cole no SQL Editor.

## Migrations existentes

| Migration | O que faz |
|---|---|
| `Inicial` | Schema `site`: imóveis (`properties`, sequência `property_code_seq` a partir de 101), fotos, dados internos, bairros, operadores do WhatsApp, ações pendentes, auditoria e fila de mensagens. Sem índice único de capa (regra do Modelo). |

| `ConteudoDoSite` | `settings` e `pages` (linha única cada; páginas em uma coluna `jsonb` por página), `faqs`, `testimonials`, `events` (uma visualização por visitante por imóvel) e `usuarios` (login do painel, senha só como hash). Só cria tabelas. |

Depois de aplicar em produção: crie o primeiro usuário do painel (`criar-usuario`) e carregue bairros, depoimentos e FAQ
com `carregar-conteudo-inicial` (`docs/CONFIGURACAO.md`). Configurações e páginas começam com os valores padrão (WhatsApp, CRECI e
textos revisados pela Bruna) e só viram linha no banco quando forem salvas.
