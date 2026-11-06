# Configuração e rotas da Api

Segredos **nunca** em arquivo versionado: em desenvolvimento, `dotnet user-secrets` (fica no perfil do Windows);
em produção, variáveis de ambiente (`Secao__Chave`, com dois sublinhados).

## Chaves

| Chave | Segredo? | Para quê |
|---|---|---|
| `ConnectionStrings:SiteBruna` | sim | Postgres (Supabase: Session pooler com SSL). Nome próprio de propósito: uma variável `ConnectionStrings__DefaultConnection` de outro projeto não interfere |
| `JwtDoSite:Key` | sim | Assina o token do painel (mínimo 32 bytes, aleatório). Nome próprio pelo mesmo motivo: uma variável `Jwt__Key` de outro projeto não interfere |
| `JwtDoSite:Issuer`, `JwtDoSite:Audience` | não | `site-bruna`, `painel-bruna` (já no `appsettings.json`) |
| `JwtDoSite:MinutosDoToken` | não | Padrão 480 (8 h) |
| `Fotos:Pasta` | não | Onde as fotos são gravadas. Padrão `/dados/fotos` (volume na VPS); em desenvolvimento, `.fotos-locais` na raiz do repositório. Precisa ser a mesma no Front.Web |
| `WhatsApp:Habilitado` | não | `true` liga o webhook |
| `WhatsApp:Modo` | não | `meta` ou `simulado` (nada vai para a Meta; respostas no log) |
| `WhatsApp:Token`, `WhatsApp:PhoneNumberId` | sim / não | Cloud API da Meta |
| `WhatsApp:AppSecret`, `WhatsApp:VerifyToken` | sim | Conferência do webhook (16+ caracteres) |
| `WhatsApp:UrlDaSimulacao` | não | Só o comando `simular-whatsapp`. Padrão `http://localhost:5130/` |
| `Site:UrlPublica` | não | Links que o assistente manda para a Bruna |
| `Site:UrlInternaDoFront`, `Site:ChaveDoCache` | não / sim | Avisar o front para refazer o cache |

Exemplo (desenvolvimento, uma vez por máquina):

```bash
dotnet user-secrets set "JwtDoSite:Key" "<texto aleatório de 48+ caracteres>" --project src/Back/GFX.SiteBruna.Api
```
```bash
dotnet user-secrets set "ConnectionStrings:SiteBruna" "Host=localhost;Port=5432;Database=site_bruna_dev;Username=postgres;Password=<senha>" --project src/Back/GFX.SiteBruna.Api
```

### Front.Web

| Chave | Segredo? | Para quê |
|---|---|---|
| `Api:UrlBase` | não | Endereço da Api na rede interna (em desenvolvimento, `http://localhost:5130/`) |
| `Site:UrlPublica` | não | Endereço público (canônico, sitemap, compartilhamento) |
| `Site:ChaveDoCache` | sim | A mesma do `Site:ChaveDoCache` da Api |
| `Site:CacheEmMinutos` | não | Cache das páginas do site (padrão 5; 0 desliga) |
| `Fotos:Pasta` | não | A mesma da Api: o Front serve essa pasta em `/media` |
| `DataProtection:PastaDasChaves` | não | Chaves que criptografam a sessão do painel. Padrão `/dados/chaves` (volume na VPS: sem ele, cada deploy desconecta quem está no painel); vazio em desenvolvimento |

Em desenvolvimento o Front roda em `http://localhost:5000` (o mesmo `Site:UrlPublica` e `Site:UrlInternaDoFront` do
`appsettings.Development.json` da Api). Para testar a invalidação do cache, use a mesma chave nos dois:

```bash
dotnet user-secrets set "Site:ChaveDoCache" "<texto aleatório>" --project src/Back/GFX.SiteBruna.Api
```
```bash
dotnet user-secrets set "Site:ChaveDoCache" "<o mesmo texto>" --project src/Front/GFX.SiteBruna.Front.Web
```

Atrás do Caddy, ligue `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` também no Front: ele repassa o IP de quem navega para
a Api (`X-Forwarded-For`), nos eventos e no login.

## Primeiro usuário do painel

Depois de aplicar as migrations (`docs/MIGRATIONS.md`):

```bash
dotnet run --project src/Back/GFX.SiteBruna.Api -- criar-usuario email@dominio "Nome"
```

A senha é pedida no terminal (sem aparecer na tela). Os próximos usuários podem ser criados pelo painel.

## Conteúdo inicial

Depois das migrations, uma vez, para preencher bairros, perguntas frequentes e depoimentos. Lista que já tem cadastro fica
como está, então pode rodar de novo sem duplicar:

```bash
dotnet run --project src/Back/GFX.SiteBruna.Api -- carregar-conteudo-inicial
```

## Assistente do WhatsApp

Quem pode usar o assistente é autorizado no painel, em Configurações → Assistente do WhatsApp.

Para testar sem a Meta, ligue `WhatsApp:Habilitado=true` e grave `WhatsApp:AppSecret` e `WhatsApp:VerifyToken` nos user
secrets (16+ caracteres; o modo `simulado` já vem no `appsettings.Development.json`). Com a Api rodando, em outro terminal:

```bash
dotnet run --no-build --project src/Back/GFX.SiteBruna.Api -- simular-whatsapp 5549999999999 texto ficha
```

Outros formatos: `arquivo ficha.txt`, `fotos 3 [legenda]` e `botao <id>`. A rota de leitura da resposta só existe no modo
simulado e fora de produção.

## Rotas

Negação por padrão: toda rota exige o token do painel, exceto as marcadas como públicas.

| Rota | Acesso | O que faz |
|---|---|---|
| `POST /api/Auth/entrar` | pública, 10/min por IP | Login → token |
| `GET /api/Auth/eu`, `POST /api/Auth/trocar-senha` | painel | |
| `/api/Imovel` (listar, obter, salvar, fotos, status, destaque, duplicar, excluir) | painel | Inclui proprietário e endereço exato |
| `POST /api/Imagem` | painel | Upload (formato pelos bytes, WebP, sem EXIF/GPS) |
| `GET /api/Metricas?dias=7\|30\|90` ou `?de=&ate=` | painel | Números do início do painel |
| `GET/POST /api/Operador` | painel | Telefones autorizados no assistente do WhatsApp |
| `/api/Conteudo`, `/api/Faq`, `/api/Depoimento`, `/api/Bairro`, `/api/Usuario` | painel | |
| `GET /api/Site/...` (configurações, páginas, FAQ, depoimentos, bairros) | pública | Sem dado interno |
| `GET /api/Site/imoveis` (filtros na query), `/imoveis/destaques`, `/imoveis/aluguel`, `/imoveis/mcmv` | pública | Cartões, sem dado interno |
| `GET /api/Site/imovel/{codigo ou trecho da URL}`, `GET /api/Site/sitemap` | pública | Página do imóvel com semelhantes; 404 se não visível |
| `GET /api/Imovel/{id}/previa` | painel | A página como fica no site, em qualquer status |
| `POST /api/Site/eventos` | pública, 120/min por IP | Sempre 204 |
| `GET/POST /api/whatsapp/webhook` | pública, assinatura da Meta | Única rota da Api exposta na internet além do site |
| `GET /health` | pública | Banco no ar |
| `/swagger` (e `/` redireciona para lá) | só em Development | Documentação e testes das rotas; o token vai em "Authorize" |

Rotas do Front.Web além das páginas: `GET /sitemap.xml`, `GET /robots.txt`, `POST /eventos` (repassa à Api) e
`POST /_cache/invalidar` (só com a chave). As fotos ficam em `GET /media/...`. O painel fica em `/admin` (login em `/admin/entrar`).

Na VPS, só o Front.Web e o webhook ficam expostos pelo Caddy; as demais rotas da Api são chamadas pelo Front na rede
interna do Docker (como no sistema interno, ADR-027 de lá). Atrás do proxy, ligue
`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` para os limites por IP usarem o IP real.
