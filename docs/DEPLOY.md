# Deploy na VPS

Produção: **VPS Linux com Docker** (Caddy, Front.Web e Api) e **banco no Supabase**. Arquivos em `deploy/`, imagens em
`src/Back/GFX.SiteBruna.Api/Dockerfile` e `src/Front/GFX.SiteBruna.Front.Web/Dockerfile`.

Regra do `CLAUDE.md` que vale aqui: commit, push e aplicação de migration são do dev. Os comandos deste documento são para
o dev executar. Segredos só no `deploy/.env` da VPS, que fica fora do git.

## Como fica

| Endereço | Quem atende | Acesso |
|---|---|---|
| `https://<DOMINIO>/` | Caddy → Front.Web | Público |
| `https://<DOMINIO_ADMIN>/admin` | Caddy → Front.Web (WebSocket do Blazor) | Login do painel, fora dos buscadores |
| `https://<DOMINIO_ADMIN>/` | Caddy | Redireciona para `/admin` |
| `https://<DOMINIO_ADMIN>/imovel/...` | Caddy | Redireciona para o `DOMINIO` (links "ver no site") |
| `https://<DOMINIO>/admin` | Caddy | Redireciona para o `DOMINIO_ADMIN` |
| `https://<DOMINIO>/media/...` | Caddy → Front.Web (volume `fotos`) | Público, cache de 1 ano |
| `https://<DOMINIO>/api/whatsapp/webhook` | Caddy → Api | Assinatura da Meta |
| `https://<DOMINIO_SEM_WWW>/...` | Caddy | Redireciona para o `DOMINIO` |
| `/_cache/*` | Caddy responde 404 | Rota interna (Api → Front) |
| Demais rotas da Api, `/health`, `/swagger` | Sem caminho pela internet | Só na rede do Docker |

| Volume | Conteúdo | Se perder |
|---|---|---|
| `bruna_fotos` | Fotos dos imóveis (Api grava, Front só lê) | **Perde as fotos.** Restaure do backup |
| `bruna_chaves-front` | Chaves da sessão do painel | Quem está logado entra de novo |
| `bruna_caddy-dados` | Certificados HTTPS | O Caddy pede novos sozinho |

Recém-subidos num teste local, os três contêineres usaram cerca de 250 MB de memória. Com tráfego real ainda não há medição.

## O que precisa estar pronto

- [ ] VPS Linux com Docker e o plugin Compose, portas 80 e 443 abertas e acesso por SSH.
- [ ] Banco do Supabase com as migrations aplicadas (`docs/MIGRATIONS.md`) e a Data API desligada.
- [ ] Destino do backup das fotos fora da VPS (Google Drive, S3 ou outro aceito pelo `rclone`).
- [ ] Domínio com acesso ao DNS.

## Primeira subida

Todos os comandos na VPS, na pasta do repositório clonado.

**1. Preencha os segredos.** Gere os valores com `openssl rand -base64 48 | tr -d '/+=\n'` e guarde uma cópia do `.env` no
gerenciador de senhas.

```bash
cp deploy/.env.example deploy/.env
```

```bash
nano deploy/.env
```

**2. Teste antes num subdomínio.** Crie um registro DNS `A` de `novo.brunaalbuquerque.com.br` para o IP da VPS e use, no
`.env`, `DOMINIO=novo.brunaalbuquerque.com.br`. Em `DOMINIO_SEM_WWW` e `DOMINIO_ADMIN`, use outros subdomínios que apontem para
a VPS. O Caddy só consegue certificado de endereços que já apontam para ela.

**3. Suba.**

```bash
cd deploy
```

```bash
docker compose up -d --build
```

```bash
docker compose ps
```

**4. Crie o primeiro usuário e carregue o conteúdo.** A senha é pedida no terminal.

```bash
docker compose run --rm api criar-usuario email@dominio "Nome"
```

```bash
docker compose run --rm api carregar-conteudo-inicial
```

**5. Confira:** site, painel em `https://<DOMINIO_ADMIN>`, envio de uma foto e a foto aparecendo no site.

## Backup das fotos

Sem cópia fora da VPS, perder o servidor é perder as fotos (ADR-010). Configure o destino uma vez:

```bash
docker run --rm -it -v "$HOME/.config/rclone:/config/rclone" rclone/rclone:1.71 config
```

Preencha `BACKUP_DESTINO` no `deploy/.env` (ex.: `gdrive:backup-fotos-bruna`), rode uma vez e confira os arquivos no destino:

```bash
sh deploy/backup-fotos.sh
```

Agende todo dia às 3h, com `crontab -e`, ajustando o caminho:

```bash
0 3 * * * sh /caminho/do/repositorio/deploy/backup-fotos.sh >> /var/log/backup-fotos.log 2>&1
```

Para restaurar, com os contêineres de pé:

```bash
docker run --rm -v bruna_fotos:/fotos -v "$HOME/.config/rclone:/config/rclone:ro" rclone/rclone:1.71 copy <remoto>:<pasta> /fotos
```

O backup do banco é o do Supabase: confira o que o plano contratado oferece.

## WhatsApp com a Meta

1. No `.env`: `WHATSAPP_HABILITADO=true`, `WHATSAPP_TOKEN`, `WHATSAPP_PHONE_NUMBER_ID`, `WHATSAPP_APP_SECRET` e
   `WHATSAPP_VERIFY_TOKEN` (16+ caracteres). Depois, `docker compose up -d`.
2. Na Meta, aponte o webhook para `https://<DOMINIO>/api/whatsapp/webhook`, com o mesmo Verify Token.
3. Autorize os telefones no painel, em Configurações → Assistente do WhatsApp.

## Troca do DNS

1. Com o subdomínio aprovado, ajuste `DOMINIO`, `DOMINIO_SEM_WWW` e `DOMINIO_ADMIN` no `.env`.
2. Aponte `www`, `admin` e o domínio raiz para o IP da VPS.
3. Rode `docker compose up -d`, confira o HTTPS e envie `https://<DOMINIO>/sitemap.xml` ao Google Search Console.
4. Se os imóveis foram cadastrados pelo subdomínio, nada muda: as fotos usam endereço relativo.

## Atualizar

```bash
git pull
```

```bash
cd deploy && docker compose up -d --build
```

Uma migration nova deve ser aplicada **antes** de subir a versão que depende dela (`docs/MIGRATIONS.md`). A Api avisa no
log, na subida, se faltar alguma. Durante a recriação dos contêineres o site fica fora do ar por alguns segundos.

## Problemas comuns

```bash
docker compose logs --tail 100 api front caddy
```

- **Sem certificado:** o DNS ainda não aponta para a VPS, ou as portas 80 e 443 estão fechadas (`docker compose logs caddy`).
- **Api para na subida:** falta um valor no `.env`; a mensagem do log diz qual.
- **"Failed to determine the https port" no log do Front:** é esperado. Quem cuida do HTTPS é o Caddy.
- **Aviso sobre chaves "unencrypted" do Data Protection:** é esperado, as chaves ficam em volume só deste servidor.
