const conteudo = document.getElementById("conteudo");
const rodape = document.getElementById("rodape");
const elDominio = document.getElementById("dominio");
const btnTrancar = document.getElementById("btnTrancar");

let dominioAtual = "";

async function nativo(payload) {
  try {
    const resposta = await chrome.runtime.sendMessage({ canal: "nativo", payload });
    return resposta ?? { ok: false, erro: "sem_resposta" };
  } catch (e) {
    return { ok: false, erro: e?.message ?? String(e) };
  }
}

function elemento(tag, props = {}, ...filhos) {
  const n = document.createElement(tag);
  Object.assign(n, props);
  for (const f of filhos) if (f != null) n.append(f);
  return n;
}

function limpar(no) {
  while (no.firstChild) no.removeChild(no.firstChild);
}

async function obterDominioAba() {
  const [aba] = await chrome.tabs.query({ active: true, currentWindow: true });
  try {
    return new URL(aba?.url ?? "").hostname.toLowerCase();
  } catch {
    return "";
  }
}

function mostrarAviso(texto, classe = "aviso") {
  limpar(conteudo);
  conteudo.append(elemento("p", { className: classe, textContent: texto }));
}

function mostrarErroTransporte(erro) {
  limpar(conteudo);
  conteudo.append(
    elemento("p", { className: "erro", textContent: "Não foi possível falar com o Cofre." }),
    elemento("p", {
      className: "aviso",
      textContent:
        "Verifique se o host nativo está registrado (tools/registrar-host-dev.ps1). Detalhe: " + erro,
    }),
  );
}

function renderBloqueado() {
  rodape.hidden = true;
  limpar(conteudo);

  const botao = elemento("button", {
    className: "botao",
    type: "button",
    textContent: "Desbloquear cofre",
  });
  botao.addEventListener("click", async () => {
    botao.disabled = true;
    botao.textContent = "Aguardando senha mestra…";
    const r = await nativo({ tipo: "unlock" });
    if (r.ok && r.status === "unlocked") {
      await renderLista();
    } else if (r.ok && r.status === "cancelled") {
      renderBloqueado();
    } else {
      mostrarErroTransporte(r.erro ?? "desconhecido");
    }
  });

  conteudo.append(
    elemento("p", { className: "aviso", textContent: "O cofre está bloqueado." }),
    botao,
  );
}

async function copiar(id, campo, botao) {
  const textoOriginal = botao.textContent;
  botao.disabled = true;
  const r = await nativo({ tipo: "getCredential", id });
  if (!r.ok) {
    botao.textContent = "Erro";
    setTimeout(() => { botao.textContent = textoOriginal; botao.disabled = false; }, 1500);
    return;
  }
  try {
    await navigator.clipboard.writeText(campo === "usuario" ? r.usuario : r.senha);
    botao.textContent = "Copiado!";
  } catch {
    botao.textContent = "Falhou";
  }
  setTimeout(() => { botao.textContent = textoOriginal; botao.disabled = false; }, 1500);
}

function cartaoCredencial(item) {
  const btnUsuario = elemento("button", { className: "acao", type: "button", textContent: "Copiar usuário" });
  btnUsuario.addEventListener("click", () => copiar(item.id, "usuario", btnUsuario));

  const btnSenha = elemento("button", { className: "acao", type: "button", textContent: "Copiar senha" });
  btnSenha.addEventListener("click", () => copiar(item.id, "senha", btnSenha));

  return elemento("li", { className: "cartao" },
    elemento("div", { className: "cartao-servico", textContent: item.servico }),
    elemento("div", { className: "cartao-usuario", textContent: item.usuario }),
    elemento("div", { className: "cartao-acoes" }, btnUsuario, btnSenha),
  );
}

async function renderLista() {
  rodape.hidden = false;
  mostrarAviso("Buscando credenciais…");

  const r = await nativo({ tipo: "query", dominio: dominioAtual });

  if (!r.ok) {
    if (r.erro === "bloqueado") { renderBloqueado(); return; }
    mostrarErroTransporte(r.erro ?? "desconhecido");
    return;
  }

  const itens = r.itens ?? [];
  limpar(conteudo);

  if (itens.length === 0) {
    conteudo.append(
      elemento("p", { className: "aviso", textContent: `Nenhuma credencial salva para ${dominioAtual || "este site"}.` }),
    );
    return;
  }

  conteudo.append(
    elemento("p", { className: "contagem", textContent: `${itens.length} credencial(is) para ${dominioAtual}` }),
    elemento("ul", { className: "lista" }, ...itens.map(cartaoCredencial)),
  );
}

btnTrancar.addEventListener("click", async () => {
  await nativo({ tipo: "lock" });
  renderBloqueado();
});

async function iniciar() {
  dominioAtual = await obterDominioAba();
  elDominio.textContent = dominioAtual || "site não suportado";

  const status = await nativo({ tipo: "status" });
  if (!status.ok) {
    mostrarErroTransporte(status.erro ?? "desconhecido");
    return;
  }

  if (status.destrancado) {
    await renderLista();
  } else {
    renderBloqueado();
  }
}

iniciar();
