(() => {
  "use strict";

  if (window.__cofreDeSenhasCarregado) return;
  window.__cofreDeSenhasCarregado = true;

  const MARCA = "cofreProcessado";
  const registros = [];
  let menuAberto = null;

  async function nativo(payload) {
    try {
      const r = await chrome.runtime.sendMessage({ canal: "nativo", payload });
      return r ?? { ok: false, erro: "sem_resposta" };
    } catch (e) {
      return { ok: false, erro: e?.message ?? String(e) };
    }
  }

  function visivel(el) {
    if (!(el instanceof HTMLElement)) return false;
    const r = el.getBoundingClientRect();
    if (r.width < 8 || r.height < 8) return false;
    const s = getComputedStyle(el);
    if (s.visibility === "hidden" || s.display === "none" || s.opacity === "0") return false;
    if (el.offsetParent === null && s.position !== "fixed") return false;
    return true;
  }

  function acharCampoUsuario(campoSenha) {
    const escopo = campoSenha.form ?? document;
    const tipos = ["text", "email", "tel", ""];
    const candidatos = Array.from(escopo.querySelectorAll("input")).filter(
      (i) => i !== campoSenha && tipos.includes(i.type) && visivel(i),
    );
    const antes = candidatos.filter(
      (i) => campoSenha.compareDocumentPosition(i) & Node.DOCUMENT_POSITION_PRECEDING,
    );
    return antes.at(-1) ?? candidatos[0] ?? null;
  }

  function definirValor(input, valor) {
    const proto =
      input instanceof HTMLTextAreaElement ? HTMLTextAreaElement.prototype : HTMLInputElement.prototype;
    const setter = Object.getOwnPropertyDescriptor(proto, "value")?.set;
    input.focus();
    if (setter) setter.call(input, valor);
    else input.value = valor;
    input.dispatchEvent(new Event("input", { bubbles: true }));
    input.dispatchEvent(new Event("change", { bubbles: true }));
  }

  function textoErro(erro) {
    switch (erro) {
      case "bloqueado":
        return "Cofre bloqueado.";
      case "cofre_nao_encontrado":
        return "Cofre não encontrado.";
      case "cofre_sem_permissao":
        return "Sem acesso aos arquivos do cofre.";
      case "cofre_formato_invalido":
        return "Formato do cofre incompatível.";
      case "permissao_negada":
        return "Operação bloqueada pela extensão.";
      case "credencial_indisponivel":
      case "nao_encontrado":
        return "Credencial indisponível.";
      case "sem_resposta":
        return "Host nativo sem resposta.";
      default:
        return "Não foi possível preencher.";
    }
  }

  async function preencher(campo, id) {
    const c = await nativo({ tipo: "getCredential", id });
    if (!c.ok) return { ok: false, erro: c.erro ?? "credencial_indisponivel" };
    if (campo.usuario && c.usuario) definirValor(campo.usuario, c.usuario);
    definirValor(campo.senha, c.senha);
    return { ok: true };
  }

  function campoParaPreenchimento() {
    const ativo = document.activeElement;
    if (ativo instanceof HTMLInputElement) {
      const porSenha = registros.find((r) => r.campo.senha === ativo && visivel(r.campo.senha));
      if (porSenha) return porSenha.campo;

      const porUsuario = registros.find((r) => r.campo.usuario === ativo && visivel(r.campo.senha));
      if (porUsuario) return porUsuario.campo;
    }

    return registros.find((r) => document.contains(r.campo.senha) && visivel(r.campo.senha))?.campo ?? null;
  }

  async function aoClicarIcone(campo, botao) {
    const dominio = location.hostname;
    let r = await nativo({ tipo: "query", dominio });

    if (!r.ok && r.erro === "bloqueado") {
      const u = await nativo({ tipo: "unlock" });
      if (u.ok && u.status === "cancelled") {
        mostrarTooltip(botao, "Desbloqueio cancelado.");
        return;
      }
      if (!(u.ok && u.status === "unlocked")) {
        mostrarTooltip(botao, textoErro(u.erro));
        return;
      }
      r = await nativo({ tipo: "query", dominio });
    }

    if (!r.ok) {
      mostrarTooltip(botao, textoErro(r.erro));
      return;
    }

    const itens = r.itens ?? [];
    if (itens.length === 0) {
      mostrarTooltip(botao, "Nenhuma credencial para este site.");
      return;
    }
    if (itens.length === 1) {
      const preenchido = await preencher(campo, itens[0].id);
      if (!preenchido.ok) mostrarTooltip(botao, textoErro(preenchido.erro));
      return;
    }
    mostrarMenu(campo, botao, itens);
  }

  function criarIcone(campo) {
    const botao = document.createElement("button");
    botao.type = "button";
    botao.className = "cofre-icone";
    botao.title = "Preencher com o Cofre de Senhas";
    botao.style.backgroundImage = `url("${chrome.runtime.getURL("icones/icone16.png")}")`;
    botao.addEventListener("mousedown", (e) => e.preventDefault());
    botao.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      aoClicarIcone(campo, botao);
    });
    document.body.appendChild(botao);
    return botao;
  }

  function posicionar(botao, alvo) {
    const r = alvo.getBoundingClientRect();
    const tam = 20;
    botao.style.top = `${r.top + (r.height - tam) / 2}px`;
    botao.style.left = `${r.right - tam - 6}px`;
  }

  function fecharMenu() {
    if (!menuAberto) return;
    menuAberto.remove();
    menuAberto = null;
    document.removeEventListener("mousedown", aoClicarFora, true);
  }

  function aoClicarFora(e) {
    if (menuAberto && !menuAberto.contains(e.target)) fecharMenu();
  }

  function mostrarMenu(campo, botao, itens) {
    fecharMenu();
    const menu = document.createElement("div");
    menu.className = "cofre-menu";

    for (const item of itens) {
      const linha = document.createElement("div");
      linha.className = "cofre-menu-item";

      const servico = document.createElement("div");
      servico.className = "cofre-menu-servico";
      servico.textContent = item.servico;

      const usuario = document.createElement("div");
      usuario.className = "cofre-menu-usuario";
      usuario.textContent = item.usuario;

      linha.append(servico, usuario);
      if (item.url) {
        const url = document.createElement("div");
        url.className = "cofre-menu-url";
        url.textContent = item.url;
        linha.append(url);
      }

      linha.addEventListener("mousedown", (e) => e.preventDefault());
      linha.addEventListener("click", async () => {
        fecharMenu();
        const preenchido = await preencher(campo, item.id);
        if (!preenchido.ok) mostrarTooltip(botao, textoErro(preenchido.erro));
      });
      menu.append(linha);
    }

    document.body.append(menu);
    const r = botao.getBoundingClientRect();
    menu.style.top = `${r.bottom + 4}px`;
    menu.style.left = `${Math.max(4, Math.min(r.left, window.innerWidth - menu.offsetWidth - 8))}px`;
    menuAberto = menu;
    setTimeout(() => document.addEventListener("mousedown", aoClicarFora, true), 0);
  }

  function mostrarTooltip(botao, texto) {
    const t = document.createElement("div");
    t.className = "cofre-tooltip";
    t.textContent = texto;
    document.body.append(t);
    const r = botao.getBoundingClientRect();
    t.style.top = `${r.bottom + 4}px`;
    t.style.left = `${Math.max(4, Math.min(r.left, window.innerWidth - t.offsetWidth - 8))}px`;
    setTimeout(() => t.remove(), 2200);
  }

  function escanear() {
    const campos = document.querySelectorAll('input[type="password"]');
    for (const senha of campos) {
      if (senha.dataset[MARCA] || !visivel(senha)) continue;
      senha.dataset[MARCA] = "1";

      const campo = { senha, usuario: acharCampoUsuario(senha) };
      const botao = criarIcone(campo);
      const reposicionar = () => posicionar(botao, senha);
      reposicionar();
      registros.push({ campo, botao, reposicionar });
    }
  }

  function reposicionarTodos() {
    for (const reg of registros) {
      const ativo = document.contains(reg.campo.senha) && visivel(reg.campo.senha);
      reg.botao.style.display = ativo ? "" : "none";
      if (ativo) reg.reposicionar();
    }
  }

  let agendado = false;
  function agendar() {
    if (agendado) return;
    agendado = true;
    setTimeout(() => {
      agendado = false;
      escanear();
      reposicionarTodos();
    }, 150);
  }

  window.addEventListener("scroll", reposicionarTodos, true);
  window.addEventListener("resize", reposicionarTodos);

  new MutationObserver(agendar).observe(document.documentElement, {
    childList: true,
    subtree: true,
  });

  chrome.runtime.onMessage.addListener((mensagem, _remetente, responder) => {
    if (mensagem?.canal !== "cofre" || mensagem.tipo !== "preencher") {
      return false;
    }

    escanear();
    reposicionarTodos();

    const campo = campoParaPreenchimento();
    if (!campo) {
      responder({ ok: false, erro: "campo_nao_encontrado" });
      return false;
    }

    preencher(campo, mensagem.id)
      .then(responder)
      .catch((e) => responder({ ok: false, erro: e?.message ?? String(e) }));
    return true;
  });

  escanear();
})();
