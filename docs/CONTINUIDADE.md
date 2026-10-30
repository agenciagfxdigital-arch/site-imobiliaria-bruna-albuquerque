# Continuidade

> Ponto de partida para uma nova sessão de trabalho. Atualize ao fim de cada etapa.
> Última revisão: 2026-10-30.

## Onde estamos

Migração do site + painel da Bruna Albuquerque de Next.js (`C:\dev\GFX\ProjetoSiteImoveisBrunaAlbuquerque`) para
.NET 10, no molde do sistema interno (`C:\dev\GFXSistemaInterno`). Plano completo em `docs/PLANO-MIGRACAO.md`.

| Etapa | Situação |
|---|---|
| Semana 1 — Modelo e Aplicacao (imóvel, fotos, imagens, ficha do WhatsApp, integração, assistente) | ✅ |
| Semana 2 — Dados (EF Core, schema `site`, Storage, SkiaSharp, WhatsApp) | ✅ |
| Semana 2 — Conteúdo (configurações, páginas, FAQ, depoimentos, bairros), eventos, usuários | ✅ |
| Semana 2 — Api (login JWT, controllers, webhook, `criar-usuario`) | ✅ |
| Semana 3 — Imóveis no site público e telas do Front.Web (site + painel) | ⏳ próxima |
| Semana 4 — Docker/Caddy, comparação visual, teste com a Meta, troca de DNS | pendente |

Testes: 191 de unidade (`Testes`) e 24 de integração (`Testes.Integracao`, Postgres descartável no Docker).
Build das duas solutions sem avisos.

## O que não foi feito (de propósito ou por regra)

- **Migrations não aplicadas** em nenhum banco: `Inicial` e `ConteudoDoSite` (ver `docs/MIGRATIONS.md`). Quem aplica é o dev.
- **Commits:** só o inicial (`e65d16e`). Conteúdo, eventos, usuários, Api e a tradução das telas do Blazor estão sem
  commit (o dev faz).
- Fase 2 do agente de IA (buscar imóveis, organizar fotos etc.) não portada; a fase 1 não usa.
- Script de simulação do WhatsApp sem a Meta (o `whatsapp:simular` do Next): hoje o modo simulado só escreve no log.
- Métricas do início do painel (consultas sobre `events`).

## Próximo passo (semana 3)

1. **Imóveis no site público** (Aplicacao + Dados + Api, rotas em `SiteController`): lista com filtros (finalidade,
   tipo, bairro, faixa de preço, quartos), página do imóvel por `/imovel/{codigo}-{slug}` (mesmo formato do Next),
   destaques da home, aluguéis recentes, sitemap. Só imóveis com `Imovel.EstaVisivelNoSite` (ativo ou vendido/alugado
   dentro do prazo do selo); **nunca** proprietário nem endereço exato.
2. **Front.Web** (Blazor, renderização estática no servidor para o site; painel interativo): reproduzir as páginas do
   Next (`src/app/(site)` e `src/components`), com o mesmo visual (Tailwind: cores `navy-*`/`gold-*`, Poppins).
   Endpoint `POST /_cache/invalidar` (cabeçalho `X-Chave-Cache`) para o `CacheDoSiteViaHttp`.
3. Telas do painel (login, imóveis com fotos, páginas, FAQ, depoimentos, bairros, configurações, usuários).

## Regras que valem sempre

- Sem commit/push/PR e sem aplicar migrations; sugerir a mensagem de commit ao fim de cada etapa.
- Regras de negócio no Modelo, com teste; Api sem acesso a dados; Dados todo `internal` (ver `CamadasTests`).
- Um tipo por arquivo; nomes em português; migration nova gerada como `public` → trocar para `internal`.
- **Todo texto que o usuário vê em português**, inclusive o que vem dos modelos do Blazor; `lang="pt-BR"`.
- Textos do site em primeira pessoa da Bruna, sem urgência forçada; MCMV sem números de faixas fixos.
- Se o Dados mudar um comportamento, ajustar o `Testes/Aplicacao/Mundo.cs` junto.
- Antes de concluir: build das duas solutions e todos os testes passando.
