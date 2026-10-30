# Configuração e rotas da Api

Segredos **nunca** em arquivo versionado: em desenvolvimento, `dotnet user-secrets` (fica no perfil do Windows);
em produção, variáveis de ambiente (`Secao__Chave`, com dois sublinhados).

## Chaves

| Chave | Segredo? | Para quê |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | sim | Postgres (Supabase: Session pooler com SSL) |
| `Jwt:Key` | sim | Assina o token do painel (mínimo 32 bytes, aleatório) |
| `Jwt:Issuer`, `Jwt:Audience` | não | Ex.: `site-bruna`, `painel-bruna` |
| `Jwt:MinutosDoToken` | não | Padrão 480 (8 h) |
| `Supabase:Url` | não | URL do projeto (Storage das fotos) |
| `Supabase:ChaveServico` | sim | Chave service_role, só no servidor |
| `Supabase:Bucket` | não | Padrão `media` |
| `WhatsApp:Habilitado` | não | `true` liga o webhook |
| `WhatsApp:Modo` | não | `meta` ou `simulado` (nada vai para a Meta; respostas no log) |
| `WhatsApp:Token`, `WhatsApp:PhoneNumberId` | sim / não | Cloud API da Meta |
| `WhatsApp:AppSecret`, `WhatsApp:VerifyToken` | sim | Conferência do webhook (16+ caracteres) |
| `Site:UrlPublica` | não | Links que o assistente manda para a Bruna |
| `Site:UrlInternaDoFront`, `Site:ChaveDoCache` | não / sim | Avisar o front para refazer o cache |

Exemplo (desenvolvimento, uma vez por máquina):

```bash
dotnet user-secrets set "Jwt:Key" "<texto aleatório de 48+ caracteres>" --project src/Back/GFX.SiteBruna.Api
```
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=54322;Database=site_bruna_dev;Username=postgres;Password=<senha>" --project src/Back/GFX.SiteBruna.Api
```

## Primeiro usuário do painel

Depois de aplicar as migrations (`docs/MIGRATIONS.md`):

```bash
dotnet run --project src/Back/GFX.SiteBruna.Api -- criar-usuario email@dominio "Nome"
```

A senha é pedida no terminal (sem aparecer na tela). Os próximos usuários podem ser criados pelo painel.

## Rotas

Negação por padrão: toda rota exige o token do painel, exceto as marcadas como públicas.

| Rota | Acesso | O que faz |
|---|---|---|
| `POST /api/Auth/entrar` | pública, 10/min por IP | Login → token |
| `GET /api/Auth/eu`, `POST /api/Auth/trocar-senha` | painel | |
| `/api/Imovel` (listar, obter, salvar, fotos, status, destaque, duplicar, excluir) | painel | Inclui proprietário e endereço exato |
| `POST /api/Imagem` | painel | Upload (formato pelos bytes, WebP, sem EXIF/GPS) |
| `/api/Conteudo`, `/api/Faq`, `/api/Depoimento`, `/api/Bairro`, `/api/Usuario` | painel | |
| `GET /api/Site/...` (configurações, páginas, FAQ, depoimentos, bairros) | pública | Sem dado interno |
| `POST /api/Site/eventos` | pública, 120/min por IP | Sempre 204 |
| `GET/POST /api/whatsapp/webhook` | pública, assinatura da Meta | Única rota da Api exposta na internet além do site |
| `GET /health` | pública | Banco no ar |

Na VPS, só o Front.Web e o webhook ficam expostos pelo Caddy; as demais rotas da Api são chamadas pelo Front na rede
interna do Docker (como no sistema interno, ADR-027 de lá). Atrás do proxy, ligue
`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` para os limites por IP usarem o IP real.
