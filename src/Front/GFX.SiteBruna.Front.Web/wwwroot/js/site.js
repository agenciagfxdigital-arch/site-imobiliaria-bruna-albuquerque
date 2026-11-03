// Comportamentos do site público (as páginas chegam prontas do servidor; isto só acrescenta interação).
// Equivale aos componentes "use client" do site em Next: cookies, métricas, menu, filtros, galeria, mapa e compartilhar.
(() => {
  "use strict";

  const COOKIE_CONSENTIMENTO = "ba_consent";
  const COOKIE_VISITANTE = "ba_vid";
  const CHAVE_ORIGEM = "ba_attr";
  const config = lerConfig();

  function lerConfig() {
    try {
      return JSON.parse(document.getElementById("config-do-site")?.textContent || "{}");
    } catch {
      return {};
    }
  }

  // --- Cookies e consentimento -----------------------------------------------------------------
  function lerCookie(nome) {
    const m = document.cookie.match(new RegExp(`(?:^|; )${nome}=([^;]*)`));
    return m ? decodeURIComponent(m[1]) : null;
  }

  function gravarCookie(nome, valor, dias) {
    const seguro = location.protocol === "https:" ? "; Secure" : "";
    document.cookie = `${nome}=${encodeURIComponent(valor)}; Max-Age=${dias * 86400}; Path=/; SameSite=Lax${seguro}`;
  }

  function consentimento() {
    const v = lerCookie(COOKIE_CONSENTIMENTO);
    return v === "granted" || v === "denied" ? v : null;
  }

  function idDoVisitante() {
    let id = lerCookie(COOKIE_VISITANTE);
    if (!id || !/^[A-Za-z0-9-]{8,64}$/.test(id)) {
      id = crypto.randomUUID?.() ?? `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 12)}`;
    }
    gravarCookie(COOKIE_VISITANTE, id, 365);
    return id;
  }

  /** Origem da visita: UTMs da URL de entrada e o site de onde a pessoa veio. Vale para a sessão toda. */
  function origemDaVisita() {
    let guardada = null;
    try {
      guardada = JSON.parse(sessionStorage.getItem(CHAVE_ORIGEM) ?? "null");
    } catch {
      guardada = null;
    }
    const p = new URLSearchParams(location.search);
    const temUtm = ["utm_source", "utm_medium", "utm_campaign"].some((k) => p.get(k));
    if (guardada && !temUtm) return guardada;

    let referrer;
    try {
      if (document.referrer) {
        const host = new URL(document.referrer).hostname;
        if (host !== location.hostname) referrer = host;
      }
    } catch {
      referrer = undefined;
    }
    const origem = {
      utmSource: p.get("utm_source")?.slice(0, 120) || (p.get("fbclid") ? "facebook" : p.get("gclid") ? "google" : undefined),
      utmMedium: p.get("utm_medium")?.slice(0, 120) || (p.get("gclid") ? "cpc" : undefined),
      utmCampaign: p.get("utm_campaign")?.slice(0, 200) || undefined,
      referrer: referrer ?? guardada?.referrer,
    };
    try {
      sessionStorage.setItem(CHAVE_ORIGEM, JSON.stringify(origem));
    } catch {
      /* navegação privada */
    }
    return origem;
  }

  // --- Métricas ------------------------------------------------------------------------------------
  const NOMES_GA = {
    PageView: "page_view",
    PropertyView: "property_view",
    WhatsappClick: "whatsapp_click",
    FilterUsed: "filter_used",
    ShareClick: "share_click",
  };

  /** Registra no banco (sempre) e no GA4/Meta Pixel (só com consentimento). Nunca quebra o site. */
  function registrar(tipo, { imovelId, imovelCodigo, detalhes } = {}) {
    const limpos = {};
    for (const [k, v] of Object.entries(detalhes ?? {})) if (v !== undefined && v !== null && v !== "") limpos[k] = String(v).slice(0, 200);
    try {
      const corpo = JSON.stringify({
        tipo,
        imovelId: imovelId || null,
        pagina: (location.pathname + location.search).slice(0, 500),
        visitanteId: idDoVisitante(),
        ...origemDaVisita(),
        detalhes: Object.keys(limpos).length ? limpos : null,
      });
      const enviado = navigator.sendBeacon?.("/eventos", new Blob([corpo], { type: "application/json" }));
      if (!enviado) fetch("/eventos", { method: "POST", body: corpo, keepalive: true, headers: { "Content-Type": "application/json" } }).catch(() => {});
    } catch {
      /* métricas nunca devem quebrar o site */
    }

    if (consentimento() !== "granted") return;
    const params = { property_id: imovelCodigo, ...limpos };
    if (tipo !== "PageView") window.gtag?.("event", NOMES_GA[tipo], params);
    if (!window.fbq) return;
    if (tipo === "WhatsappClick") window.fbq("track", "Contact", params);
    else if (tipo === "PropertyView") window.fbq("track", "ViewContent", { content_ids: [imovelCodigo], content_type: "home_listing" });
    else if (tipo === "FilterUsed") window.fbq("track", "Search", params);
    else if (tipo === "ShareClick") window.fbq("trackCustom", "Share", params);
  }

  function carregarScript(src) {
    const s = document.createElement("script");
    s.async = true;
    s.src = src;
    document.head.appendChild(s);
  }

  let terceirosCarregados = false;
  /** GA4 e Meta Pixel SOMENTE depois que o visitante aceita os cookies. */
  function carregarTerceiros() {
    if (terceirosCarregados || consentimento() !== "granted") return;
    terceirosCarregados = true;
    const ga4 = config.ga4 || "";
    const pixel = config.pixel || "";
    if (/^G-[A-Z0-9]+$/i.test(ga4)) {
      window.dataLayer = window.dataLayer || [];
      window.gtag = function gtag() {
        window.dataLayer.push(arguments);
      };
      window.gtag("js", new Date());
      window.gtag("config", ga4, { anonymize_ip: true });
      carregarScript(`https://www.googletagmanager.com/gtag/js?id=${encodeURIComponent(ga4)}`);
    }
    if (/^\d{6,20}$/.test(pixel)) {
      // Código oficial do Meta Pixel, adaptado
      const fbq = function (...args) {
        if (fbq.callMethod) fbq.callMethod(...args);
        else fbq.queue.push(args);
      };
      fbq.queue = [];
      fbq.push = fbq;
      fbq.loaded = true;
      fbq.version = "2.0";
      window.fbq = fbq;
      if (!window._fbq) window._fbq = fbq;
      carregarScript("https://connect.facebook.net/en_US/fbevents.js");
      window.fbq("init", pixel);
      window.fbq("track", "PageView");
    }
  }

  // --- Aviso de cookies ----------------------------------------------------------------------------
  function avisoDeCookies() {
    const aviso = document.getElementById("aviso-de-cookies");
    if (!aviso) return;
    const escolher = (valor) => {
      gravarCookie(COOKIE_CONSENTIMENTO, valor, 180);
      aviso.hidden = true;
      carregarTerceiros();
    };
    aviso.querySelector("[data-cookies='recusar']")?.addEventListener("click", () => escolher("denied"));
    aviso.querySelector("[data-cookies='aceitar']")?.addEventListener("click", () => escolher("granted"));
    document.querySelectorAll("[data-abrir-cookies]").forEach((b) => b.addEventListener("click", () => (aviso.hidden = false)));
    if (consentimento() === null) aviso.hidden = false;
  }

  // --- Cliques de WhatsApp e compartilhamento --------------------------------------------------------
  function cliques() {
    document.addEventListener("click", (e) => {
      const alvo = e.target instanceof Element ? e.target.closest("[data-whatsapp], [data-compartilhar]") : null;
      if (!alvo) return;
      const imovel = { imovelId: alvo.dataset.imovelId, imovelCodigo: alvo.dataset.imovelCodigo };

      if (alvo.hasAttribute("data-whatsapp")) {
        e.stopPropagation();
        registrar("WhatsappClick", { ...imovel, detalhes: { local: alvo.dataset.whatsapp, codigo: imovel.imovelCodigo } });
        return;
      }

      const canal = alvo.dataset.compartilhar;
      const url = alvo.dataset.url || location.href;
      registrar("ShareClick", { ...imovel, detalhes: { canal, codigo: imovel.imovelCodigo } });
      if (canal === "whatsapp") {
        window.open(`https://wa.me/?text=${encodeURIComponent(`${alvo.dataset.titulo ?? document.title}\n${url}`)}`, "_blank", "noopener");
      } else if (canal === "copiar_link") {
        const pronto = () => {
          const texto = alvo.querySelector("[data-texto]");
          const antes = texto?.textContent;
          if (texto) texto.textContent = "Link copiado";
          setTimeout(() => texto && (texto.textContent = antes), 2500);
        };
        navigator.clipboard?.writeText(url).then(pronto, () => window.prompt("Copie o link:", url));
      }
    });
  }

  /** Na página do imóvel, os botões gerais (cabeçalho, flutuante) passam a falar do imóvel. */
  function whatsappDaPagina() {
    const alvo = document.querySelector("[data-whatsapp-da-pagina]");
    if (!alvo || !config.whatsapp) return;
    const { mensagem, imovelId, imovelCodigo } = alvo.dataset;
    document.querySelectorAll("a[data-whatsapp-geral]").forEach((a) => {
      a.href = `https://wa.me/${config.whatsapp}?text=${encodeURIComponent(mensagem)}`;
      a.dataset.imovelId = imovelId;
      a.dataset.imovelCodigo = imovelCodigo;
    });
  }

  // --- Menu do celular -----------------------------------------------------------------------------
  function menu() {
    const botao = document.querySelector("[data-menu-botao]");
    const painel = document.getElementById("menu-celular");
    if (!botao || !painel) return;
    botao.addEventListener("click", () => {
      const abrir = painel.hidden;
      painel.hidden = !abrir;
      botao.setAttribute("aria-expanded", String(abrir));
      botao.setAttribute("aria-label", abrir ? "Fechar menu" : "Abrir menu");
      botao.querySelector("[data-icone='abrir']").hidden = abrir;
      botao.querySelector("[data-icone='fechar']").hidden = !abrir;
      document.body.style.overflow = abrir ? "hidden" : "";
    });
  }

  // --- Filtros da lista e busca rápida ------------------------------------------------------------
  /** Monta o endereço limpo (mesmo formato do site em Next) a partir do formulário de filtros. */
  function urlDosFiltros(form) {
    const dados = new FormData(form);
    const faixas = JSON.parse(form.dataset.faixas || "[]");
    const p = new URLSearchParams();
    const pegar = (nome) => (dados.get(nome) ?? "").toString().trim();
    for (const nome of ["tipo", "bairro"]) if (pegar(nome)) p.set(nome, pegar(nome));
    const faixa = faixas[Number(pegar("faixa"))];
    if (faixa?.min) p.set("preco_min", faixa.min);
    if (faixa?.max) p.set("preco_max", faixa.max);
    for (const nome of ["quartos", "vagas", "situacao"]) if (pegar(nome)) p.set(nome, pegar(nome));
    for (const nome of ["mcmv", "entrada", "fgts", "mobiliado", "pet"]) if (pegar(nome) === "1") p.set(nome, "1");
    if (pegar("ordem") && pegar("ordem") !== "recentes") p.set("ordem", pegar("ordem"));
    const texto = p.toString();
    return { url: `${form.getAttribute("action")}${texto ? `?${texto}` : ""}`, faixa };
  }

  function filtros() {
    document.querySelectorAll("form[data-filtros]").forEach((form) => {
      // "Pronto" e "Na planta" são opções da mesma escolha, mas dá para desmarcar as duas
      form.querySelectorAll("input[name='situacao']").forEach((caixa) =>
        caixa.addEventListener("change", () => {
          if (caixa.checked) form.querySelectorAll("input[name='situacao']").forEach((outra) => outra !== caixa && (outra.checked = false));
        }),
      );
      form.addEventListener("submit", (e) => {
        e.preventDefault();
        const { url, faixa } = urlDosFiltros(form);
        const dados = new FormData(form);
        const nomeDe = (campo) => form.querySelector(`[name='${campo}'] option:checked`)?.textContent?.trim();
        registrar("FilterUsed", {
          detalhes: {
            origem: form.dataset.filtros === "home" ? "busca_home" : undefined,
            finalidade: form.dataset.finalidade,
            tipo: dados.get("tipo") ? nomeDe("tipo") : undefined,
            bairro_nome: dados.get("bairro") ? nomeDe("bairro") : undefined,
            faixa: faixa?.label,
            quartos: dados.get("quartos") || undefined,
            ordem: dados.get("ordem") || undefined,
          },
        });
        location.assign(url);
      });
    });

    // Busca rápida da home: "Comprar"/"Alugar" trocam o destino e as faixas de preço
    const busca = document.querySelector("form[data-filtros='home']");
    if (busca) {
      const faixas = JSON.parse(busca.dataset.faixasPorFinalidade || "{}");
      const seletorFaixa = busca.querySelector("select[name='faixa']");
      busca.querySelectorAll("[data-finalidade-aba]").forEach((aba) =>
        aba.addEventListener("click", () => {
          const finalidade = aba.dataset.finalidadeAba;
          busca.dataset.finalidade = finalidade;
          busca.setAttribute("action", finalidade === "venda" ? "/imoveis" : "/aluguel");
          busca.dataset.faixas = JSON.stringify(faixas[finalidade]);
          busca.querySelectorAll("[data-finalidade-aba]").forEach((outra) => {
            const ativa = outra === aba;
            outra.setAttribute("aria-selected", String(ativa));
            outra.classList.toggle("bg-navy-800", ativa);
            outra.classList.toggle("text-white", ativa);
            outra.classList.toggle("shadow", ativa);
            outra.classList.toggle("text-navy-700", !ativa);
          });
          seletorFaixa.replaceChildren(
            new Option(finalidade === "venda" ? "Qualquer preço" : "Qualquer valor", ""),
            ...faixas[finalidade].map((f, i) => new Option(f.label, String(i))),
          );
        }),
      );
    }

    // Celular: painel de filtros em tela cheia
    document.querySelectorAll("[data-abrir-filtros]").forEach((botao) =>
      botao.addEventListener("click", () => {
        const painel = document.getElementById(botao.dataset.abrirFiltros);
        painel?.setAttribute("data-aberto", "");
        document.body.style.overflow = "hidden";
      }),
    );
    document.querySelectorAll("[data-fechar-filtros]").forEach((botao) =>
      botao.addEventListener("click", () => {
        document.getElementById(botao.dataset.fecharFiltros)?.removeAttribute("data-aberto");
        document.body.style.overflow = "";
      }),
    );
  }

  // --- Galeria de fotos ----------------------------------------------------------------------------
  function irPara(faixa, i, suave = true) {
    faixa.scrollTo({ left: i * faixa.clientWidth, behavior: suave ? "smooth" : "instant" });
  }

  function galeria() {
    const raiz = document.querySelector("[data-galeria]");
    if (!raiz) return;
    const faixa = raiz.querySelector("[data-galeria-faixa]");
    const total = faixa.children.length;
    const contador = raiz.querySelector("[data-galeria-contador]");
    const miniaturas = [...raiz.querySelectorAll("[data-galeria-miniatura]")];
    let atual = 0;

    const marcar = () => {
      atual = Math.round(faixa.scrollLeft / faixa.clientWidth);
      if (contador) contador.textContent = `${atual + 1} / ${total}`;
      miniaturas.forEach((m, i) => {
        m.classList.toggle("ring-gold-400", i === atual);
        m.classList.toggle("ring-transparent", i !== atual);
        m.classList.toggle("opacity-70", i !== atual);
      });
    };
    faixa.addEventListener("scroll", marcar, { passive: true });
    raiz.querySelector("[data-galeria-anterior]")?.addEventListener("click", () => irPara(faixa, Math.max(0, atual - 1)));
    raiz.querySelector("[data-galeria-proxima]")?.addEventListener("click", () => irPara(faixa, Math.min(total - 1, atual + 1)));
    miniaturas.forEach((m, i) => m.addEventListener("click", () => irPara(faixa, i)));

    // Tela cheia
    const caixa = document.getElementById("galeria-tela-cheia");
    if (!caixa) return;
    const faixaCheia = caixa.querySelector("[data-galeria-faixa]");
    const contadorCheio = caixa.querySelector("[data-galeria-contador]");
    let atualCheia = 0;
    const marcarCheia = () => {
      atualCheia = Math.round(faixaCheia.scrollLeft / faixaCheia.clientWidth);
      contadorCheio.textContent = `${atualCheia + 1} / ${total}`;
    };
    const fechar = () => {
      caixa.hidden = true;
      document.body.style.overflow = "";
      irPara(faixa, atualCheia, false);
    };
    const teclas = (e) => {
      if (caixa.hidden) return;
      if (e.key === "Escape") fechar();
      if (e.key === "ArrowRight") irPara(faixaCheia, Math.min(total - 1, atualCheia + 1));
      if (e.key === "ArrowLeft") irPara(faixaCheia, Math.max(0, atualCheia - 1));
    };
    faixaCheia.addEventListener("scroll", marcarCheia, { passive: true });
    faixa.querySelectorAll("[data-galeria-ampliar]").forEach((botao, i) =>
      botao.addEventListener("click", () => {
        caixa.hidden = false;
        document.body.style.overflow = "hidden";
        faixaCheia.scrollLeft = i * faixaCheia.clientWidth;
        marcarCheia();
        caixa.querySelector("[data-galeria-fechar]")?.focus();
      }),
    );
    caixa.querySelector("[data-galeria-fechar]")?.addEventListener("click", fechar);
    caixa.querySelector("[data-galeria-anterior]")?.addEventListener("click", () => irPara(faixaCheia, Math.max(0, atualCheia - 1)));
    caixa.querySelector("[data-galeria-proxima]")?.addEventListener("click", () => irPara(faixaCheia, Math.min(total - 1, atualCheia + 1)));
    window.addEventListener("keydown", teclas);
  }

  // --- Mapa (só carrega o Google Maps quando a pessoa pede) ----------------------------------------
  function mapa() {
    document.querySelectorAll("[data-mapa]").forEach((botao) =>
      botao.addEventListener("click", () => {
        const iframe = document.createElement("iframe");
        iframe.title = `Mapa da região: ${botao.dataset.mapa}`;
        iframe.src = `https://maps.google.com/maps?q=${encodeURIComponent(botao.dataset.mapa)}&z=14&output=embed`;
        iframe.className = "aspect-[16/9] w-full rounded-2xl border-0";
        iframe.loading = "lazy";
        iframe.referrerPolicy = "no-referrer-when-downgrade";
        botao.replaceWith(iframe);
      }),
    );
  }

  // --- Visualizações -------------------------------------------------------------------------------
  function visualizacoes() {
    if (document.body.dataset.semMetricas !== undefined) return;
    registrar("PageView");
    const imovel = document.querySelector("[data-imovel-visto]");
    if (!imovel) return;
    const { imovelId, imovelCodigo } = imovel.dataset;
    // O banco guarda uma visualização por visitante por imóvel; aqui só evita reenvios na mesma sessão
    const chave = `ba_pv_${imovelId}`;
    try {
      if (sessionStorage.getItem(chave)) return;
      sessionStorage.setItem(chave, "1");
    } catch {
      /* navegação privada */
    }
    registrar("PropertyView", { imovelId, imovelCodigo, detalhes: { codigo: imovelCodigo } });
  }

  function iniciar() {
    carregarTerceiros();
    avisoDeCookies();
    cliques();
    whatsappDaPagina();
    menu();
    filtros();
    galeria();
    mapa();
    visualizacoes();
  }

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", iniciar);
  else iniciar();
})();
