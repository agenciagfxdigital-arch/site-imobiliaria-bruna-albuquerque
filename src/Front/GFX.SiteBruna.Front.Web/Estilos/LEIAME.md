# CSS do Front.Web (Tailwind)

O site usa o mesmo Tailwind (v4) e o mesmo tema do site em Next (cores `navy-*`/`gold-*`, Poppins, `btn-*`, `card`...).
As classes ficam direto nos `.razor`, como nos componentes do Next.

- `Estilos/site.css` é a **entrada** (tema, fonte, utilitários próprios).
- `wwwroot/css/site.css` é o **CSS gerado**, que vai junto no repositório: o `dotnet build`/`publish` não precisa de Node.

## Quando regenerar

Sempre que uma tela ganhar uma classe que ainda não existia (ex.: `mt-7`, `bg-amber-50`). Sem regenerar, a classe
simplesmente não tem efeito.

```bash
cd src/Front/GFX.SiteBruna.Front.Web/Estilos
npm install
npm run css
```

Enquanto mexe nas telas, `npm run css:observar` refaz o CSS a cada arquivo salvo.

O Tailwind procura classes em `Components/`, `Apresentacao/` e `wwwroot/js/` (ver `@source` em `site.css`). Classe
montada por concatenação (`"bg-" + cor`) não é encontrada: escreva o nome inteiro.

## Fonte

A Poppins (licença SIL Open Font License 1.1) fica em `wwwroot/fontes/`, servida pelo próprio site: nenhuma requisição
ao Google antes do consentimento de cookies. São os mesmos arquivos (latin e latin-ext, pesos 300 a 700) que o
`next/font` gerava.
