# Continuidade

> Ponto de partida para uma nova sessão de trabalho. Atualize ao fim de cada etapa.
> Última revisão: 2026-11-03.

## Onde estamos

Migração do site + painel da Bruna Albuquerque de Next.js (`C:\dev\GFX\ProjetoSiteImoveisBrunaAlbuquerque`) para
.NET 10, no molde do sistema interno (`C:\dev\GFXSistemaInterno`). Plano completo em `docs/PLANO-MIGRACAO.md`.

| Etapa | Situação |
|---|---|
| Semana 1 — Modelo e Aplicacao (imóvel, fotos, imagens, ficha do WhatsApp, integração, assistente) | ✅ |
| Semana 2 — Dados (EF Core, schema `site`, Storage, SkiaSharp, WhatsApp) | ✅ |
| Semana 2 — Conteúdo (configurações, páginas, FAQ, depoimentos, bairros), eventos, usuários | ✅ |
| Semana 2 — Api (login JWT, controllers, webhook, `criar-usuario`) | ✅ |
| Semana 3 — Imóveis no site público e telas do Front.Web (site + painel) | ✅ |
| Semana 4 — Docker/Caddy e guia de deploy | ✅ testados localmente |
| Semana 4 — VPS, comparação visual, teste com a Meta, troca de DNS | ⏳ próxima |

Testes: 258 de unidade (`Testes`) e 45 de integração (`Testes.Integracao`, Postgres descartável no Docker).
Build das duas solutions sem avisos.

## O que não foi feito (de propósito ou por regra)

- **Migrations não aplicadas** em nenhum banco: `Inicial` e `ConteudoDoSite` (ver `docs/MIGRATIONS.md`). Quem aplica é o dev.
- Fase 2 do agente de IA (buscar imóveis, organizar fotos etc.) não portada; a fase 1 não usa.

## Como rodar tudo localmente

1. Banco com as migrations aplicadas (pelo dev, `docs/MIGRATIONS.md`) e o primeiro usuário (`docs/CONFIGURACAO.md`).
2. `dotnet run --project src/Back/GFX.SiteBruna.Api` (porta 5130) e `dotnet run --project src/Front/GFX.SiteBruna.Front.Web`
   (porta 5000). Site em `http://localhost:5000`, painel em `http://localhost:5000/admin`.
3. Mudou classe Tailwind numa tela? Regenerar o CSS (`src/Front/GFX.SiteBruna.Front.Web/Estilos/LEIAME.md`).

## Próximo passo (semana 4)

1. Primeira subida na VPS, num subdomínio, e backup das fotos com destino definido (`docs/DEPLOY.md`).
2. Comparação visual página a página com o site em Next (as telas foram conferidas com dados de exemplo; falta com as
   fotos reais).
3. Teste do WhatsApp com a Meta, revisão de segurança, aprovação da Bruna (incluindo o trecho novo da privacidade).
4. Evolução depois da entrega: fase 2 do agente de IA.

## Regras que valem sempre

- Sem commit/push/PR e sem aplicar migrations; sugerir a mensagem de commit ao fim de cada etapa.
- Regras de negócio no Modelo, com teste; Api sem acesso a dados; Dados todo `internal` (ver `CamadasTests`).
- Um tipo por arquivo; nomes em português; migration nova gerada como `public` → trocar para `internal`.
- **Todo texto que o usuário vê em português**, inclusive o que vem dos modelos do Blazor; `lang="pt-BR"`.
- Textos do site em primeira pessoa da Bruna, sem urgência forçada; MCMV sem números de faixas fixos.
- Se o Dados mudar um comportamento, ajustar o `Testes/Aplicacao/Mundo.cs` junto.
- Antes de concluir: build das duas solutions e todos os testes passando.
