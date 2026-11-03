// Gera wwwroot/css/site.css a partir de Estilos/site.css, procurando as classes usadas nos .razor e .cs.
// Uso (na pasta Estilos): npm install  →  npm run css   (ou "npm run css:observar" enquanto edita as telas)
import { readFile, writeFile, mkdir } from "node:fs/promises";
import { watch } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import postcss from "postcss";
import tailwind from "@tailwindcss/postcss";

const pasta = dirname(fileURLToPath(import.meta.url));
const entrada = resolve(pasta, "site.css");
const saida = resolve(pasta, "../wwwroot/css/site.css");

async function gerar() {
  const css = await readFile(entrada, "utf8");
  const resultado = await postcss([tailwind({ base: pasta, optimize: { minify: true } })]).process(css, { from: entrada, to: saida });
  await mkdir(dirname(saida), { recursive: true });
  await writeFile(saida, resultado.css);
  console.log(`[css] ${new Date().toLocaleTimeString("pt-BR")} wwwroot/css/site.css (${(resultado.css.length / 1024).toFixed(1)} KB)`);
}

await gerar();

if (process.argv.includes("--observar")) {
  let agendado = null;
  const refazer = () => {
    clearTimeout(agendado);
    agendado = setTimeout(() => gerar().catch((e) => console.error(e.message)), 150);
  };
  for (const alvo of ["../Components", "../Apresentacao", "../wwwroot/js", "."])
    watch(resolve(pasta, alvo), { recursive: true }, (_, arquivo) => {
      if (arquivo && /\.(razor|cs|js|css)$/.test(arquivo) && !arquivo.endsWith("gerar-css.mjs")) refazer();
    });
  console.log("[css] observando as telas (Ctrl+C para sair)");
}
