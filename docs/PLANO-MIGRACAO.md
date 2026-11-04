# Plano da migração (Next.js → .NET)

Origem: `C:\dev\GFX\ProjetoSiteImoveisBrunaAlbuquerque` (camadas em `src/server/`, telas em `src/app` e
`src/components`). Os testes de lá (Vitest) são a especificação: cada regra portada vem com o teste correspondente
em xUnit.

## Semana 1 — Modelo, Aplicacao e assistente do WhatsApp

- [x] Solução, projetos e referências entre camadas
- [x] Testes de arquitetura (`Testes/Arquitetura/CamadasTests.cs`)
- [x] `Imovel` e `DadosInternosImovel` (de `modelo/imovel.ts` + triggers do banco)
- [x] Fotos e imagens (`modelo/fotos.ts`, `modelo/imagem.ts`) → `FotoImovel`, `PlanejamentoDeFotos`, `Imagens`
- [x] Ficha do WhatsApp (`modelo/ficha.ts`, `modelo/extenso.ts`) → `Ficha`, `Extenso`, `CamposDoImovel`
- [x] Integração (`modelo/integracao.ts`) → `Operador`, `AcaoPendente`, `Diferencas`, `ResumosDeAcao`, `Sanidade`, `Telefone`
- [x] Assistente: comandos e textos (`modelo/assistente.ts`) → `Assistente`, `TextosDoAssistente`, `Comando`
- [x] Casos de uso + mundo em memória para testes (`aplicacao/*.ts`, `__testes__/mundo.ts`) →
  `ImovelService`, `ImagemService`, `IntegracaoService` (internal), `AssistenteService`, `Testes/Aplicacao/Mundo.cs`
- [ ] Fase 2 (agente de IA), não portado: `buscarImoveis`, `obterImovel` completo, `listarOpcoes`, `organizarFotos`,
  `verificarOperador`. A fase 1 (assistente sem IA) não usa; entram junto com o agente.

### Diferenças em relação ao original (intencionais)

- Regras que estavam em triggers e funções do banco agora são do Modelo: código em maiúsculas, data de venda
  (`Imovel.AlterarStatus`), visibilidade no site (`Imovel.EstaVisivelNoSite`) e reserva do operador
  (`Operador.Reservar`; o Dados repete a condição num UPDATE atômico).
- A ação pendente guarda o **estado completo proposto** (`DadosDoImovel`) em vez de um dicionário de campos: desfazer
  consegue voltar um campo para vazio, e a confirmação só chama `Imovel.Atualizar`.
- "Campo não permitido" (status, destaque, código pelo WhatsApp) é garantido pelo tipo `CamposDoImovel`, que não tem
  esses campos; não existe mais como erro em tempo de execução.
- Código informado precisa ter de 2 a 20 caracteres (antes o formulário aceitava 0 a 20 e o banco recusava 1).
- A auditoria guarda o **estado completo antes e depois** (`DadosDoImovel`) e a lista de campos alterados, em vez de
  mapas de valores soltos: desfazer não depende de converter JSON de volta para o tipo de cada campo.
- `IntegracaoService` é `internal` e o `AssistenteService` tem construtor `internal` (registrado por fábrica no
  `AddAplicacao`): a Api recebe o assistente pronto, mas não tem como chamar a confirmação de uma ação por fora.
- Confirmação que falha no meio: `IUnitOfWork.Descartar()` antes de registrar a falha, para nada da execução pela
  metade ir para o banco junto com a auditoria.

### Contratos que o Dados precisa cumprir (semana 2)

- `IOperadorRepository.Reservar/Liberar`, `IAcaoPendenteRepository.ReservarConfirmacao/DevolverParaPendente` e
  `IFilaDeMensagens.Inserir/Gravar` gravam **na hora** (UPDATE/INSERT atômico), fora do `IUnitOfWork`.
- `IFilaDeMensagens.Inserir` com `wamid` único (`ON CONFLICT DO NOTHING`).
- Exclusão de imóvel em cascata (fotos e dados internos); `IUnitOfWork.Commit` traduz índice único de `codigo`
  em `DomainException(CodigoDuplicado, "Já existe um imóvel com esse código.")`.
- O `Mundo` dos testes imita esses comportamentos; se o Dados mudar algum, ajuste o `Mundo` junto.

## Semana 2 — Dados, Api e painel

- [x] `AppDbContext`, configurações, migration `Inicial` (schema `site`; ver `docs/MIGRATIONS.md`) — **não aplicada**
- [x] Repositórios, `UnitOfWork`, `VerificacaoBanco`
- [x] Fotos em disco (ADR-010), imagens (SkiaSharp), Cloud API do WhatsApp e modo simulado, cache do front via HTTP
- [x] Testes de integração com Postgres descartável (`Testes.Integracao`, precisa do Docker), incluindo o assistente de
  ponta a ponta
- [x] Módulos de conteúdo (configurações, páginas, FAQ, depoimentos, bairros), eventos e usuários do painel:
  Modelo, Aplicacao, Dados, migration `ConteudoDoSite` (não aplicada) e testes
- [ ] Métricas do início do painel (cliques no WhatsApp, visualizações por imóvel): consultas sobre `events`
- [x] Api: login com JWT (desativado perde o acesso na hora), negação por padrão, controllers do painel e do site,
  webhook do WhatsApp (assinatura no corpo bruto, processamento em segundo plano), limites por IP, comando
  `criar-usuario` (ver `docs/CONFIGURACAO.md`)
- [x] Imóveis no site público (lista com filtros, página do imóvel, destaques, aluguel, sitemap)
- [ ] Simulação do WhatsApp sem a Meta por script (`whatsapp:simular` do site em Next) — hoje o modo simulado só
  escreve as respostas no log
- [x] Painel: login, imóveis (lista, edição, fotos, prévia), páginas, FAQ, depoimentos, bairros, configurações,
  usuários e troca de senha. O início mostra o resumo dos imóveis; as métricas continuam pendentes (acima)

## Semana 3 — Site público

- [x] Home, imóveis com filtros, página do imóvel (galeria, mapa), aluguel, Sobre, MCMV, contato
- [x] Privacidade e termos, cookies, GA4/Meta Pixel após consentimento, eventos de WhatsApp
- [x] SEO: metadados, sitemap, robots, imagem de compartilhamento, dados estruturados
- [x] Cache de saída no lugar do ISR, invalidado nas escritas (ADR-008)

### Diferenças em relação ao original (intencionais)

- Fotos no painel: cada mudança (enviar, ordem, capa, remover) é gravada na hora, e a ordem muda por setas em vez de
  arrastar. Imóvel novo precisa ser salvo (rascunho) antes de receber fotos. As fotos não são comprimidas no navegador:
  a Api recebe até 15 MB e regrava em WebP.
- Semelhantes: imóvel sem bairro não conta "bairro vazio" como bairro igual.
- Privacidade: "Vercel e Supabase" virou "o servidor contratado para manter o site no ar e o Supabase" (e a data,
  novembro de 2026). **A Bruna precisa revisar esse trecho.**
- "Esqueci minha senha" do login (Supabase Auth) não existe: o login é próprio; outro usuário do painel cria uma senha
  nova (Configurações → Quem acessa o painel).

## Semana 4 — Entrega

- [ ] Dockerfile da Api e do Front, `compose.yaml` com Caddy (VPS), volume `/dados/fotos` nos dois contêineres
- [ ] Backup automático das fotos para fora da VPS (ADR-010)
- [ ] Comparação visual página a página com o site em Next
- [ ] Teste do WhatsApp com a Meta, revisão de segurança, aprovação da Bruna
- [ ] Troca de DNS
