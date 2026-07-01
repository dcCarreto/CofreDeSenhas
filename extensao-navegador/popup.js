const conteudo = document.getElementById("conteudo");
const rodape = document.getElementById("rodape");
const elDominio = document.getElementById("dominio");
const btnAdicionar = document.getElementById("btnAdicionar");
const btnTrancar = document.getElementById("btnTrancar");

let dominioAtual = "";
let urlAtual = "";
let sessaoAtual = null;
let termoBusca = "";
let limpezaClipboard = null;
let avisoLista = "";
let classeAvisoLista = "sucesso";

const TEMPO_LIMPEZA_CLIPBOARD_MS = 30000;

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

async function obterDadosAba() {
  const [aba] = await chrome.tabs.query({ active: true, currentWindow: true });
  try {
    const url = new URL(aba?.url ?? "");
    if (url.protocol !== "http:" && url.protocol !== "https:") {
      return { dominio: "", url: "" };
    }
    return { dominio: url.hostname.toLowerCase(), url: url.origin };
  } catch {
    return { dominio: "", url: "" };
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

function erroDeCofre(erro) {
  return erro === "cofre_nao_encontrado" ||
    erro === "cofre_sem_permissao" ||
    erro === "cofre_formato_invalido";
}

function textoErroGeral(erro) {
  switch (erro) {
    case "bloqueado":
      return "O cofre está bloqueado. Desbloqueie novamente para continuar.";
    case "cofre_nao_encontrado":
      return "Cofre não encontrado.";
    case "cofre_sem_permissao":
      return "Não foi possível acessar os arquivos do cofre.";
    case "cofre_formato_invalido":
      return "O formato do cofre é incompatível.";
    case "permissao_negada":
      return "A operação foi bloqueada pela extensão.";
    case "credencial_indisponivel":
      return "Credencial indisponível.";
    case "nao_encontrado":
      return "Credencial não encontrada.";
    case "campo_nao_encontrado":
      return "Nenhum campo de senha visível foi encontrado na aba atual.";
    case "aba_indisponivel":
      return "A aba atual não está disponível para preenchimento.";
    case "sem_resposta":
      return "O host nativo não respondeu.";
    case "id_invalido":
      return "Identificador da credencial inválido.";
    case "dominio_ausente":
      return "Não foi possível identificar o domínio da aba.";
    case "conflito_escrita":
      return "O cofre foi alterado fora da extensão. Tranque e desbloqueie novamente.";
    case "integridade_invalida":
      return "A gravação falhou na validação de integridade.";
    case "erro_gravacao":
      return "Não foi possível gravar no cofre.";
    default:
      return "Não foi possível concluir a operação.";
  }
}

function textoCurtoErro(erro) {
  switch (erro) {
    case "bloqueado":
      return "Bloqueado";
    case "campo_nao_encontrado":
      return "Sem campo";
    case "nao_encontrado":
      return "Não achou";
    case "permissao_negada":
      return "Negado";
    default:
      return "Falhou";
  }
}

function renderCofreIndisponivel(cofre = {}, erro = "cofre_nao_encontrado") {
  rodape.hidden = true;
  btnAdicionar.disabled = true;
  limpar(conteudo);

  let titulo = "Cofre não encontrado.";
  let detalhe = "A extensão só funciona quando já existe um cofre criado em %APPDATA%\\GerenciadorSenhas\\.";

  if (erro === "cofre_sem_permissao" || cofre.estado === "sem_permissao") {
    titulo = "Não foi possível acessar o cofre.";
    detalhe = "Verifique se auth.dat e senhas.json.enc existem e podem ser lidos e alterados pelo usuário atual.";
  } else if (erro === "cofre_formato_invalido" || cofre.estado === "formato_invalido") {
    titulo = "Formato do cofre incompatível.";
    detalhe = "O arquivo senhas.json.enc foi encontrado, mas não parece estar no formato criptografado esperado.";
  } else if (cofre.estado === "incompleto") {
    detalhe = "A pasta do cofre foi encontrada, mas auth.dat e senhas.json.enc precisam existir juntos.";
  }

  conteudo.append(
    elemento("p", { className: "erro", textContent: titulo }),
    elemento("p", { className: "aviso", textContent: detalhe }),
  );

  if (cofre.erro) {
    conteudo.append(elemento("p", { className: "aviso detalhe", textContent: cofre.erro }));
  }
}

function renderBloqueado(mensagem = "O cofre está bloqueado.") {
  sessaoAtual = null;
  rodape.hidden = true;
  btnAdicionar.disabled = true;
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
      const status = await nativo({ tipo: "status" });
      if (status.ok) sessaoAtual = status.sessao ?? null;
      await renderLista();
    } else if (r.ok && r.status === "cancelled") {
      renderBloqueado("Desbloqueio cancelado.");
    } else if (erroDeCofre(r.erro)) {
      renderCofreIndisponivel(r.cofre, r.erro);
    } else {
      mostrarErroTransporte(r.erro ?? "desconhecido");
    }
  });

  conteudo.append(
    elemento("p", { className: mensagem === "O cofre está bloqueado." ? "aviso" : "erro", textContent: mensagem }),
    botao,
  );
}

function textoAutoLock() {
  const segundos = sessaoAtual?.expiraEmSegundos;
  if (typeof segundos !== "number") return null;
  const minutos = Math.max(1, Math.ceil(segundos / 60));
  return `Sessão destrancada. Bloqueio automático em até ${minutos} min.`;
}

function formularioBusca() {
  const form = elemento("form", { className: "busca" });
  const input = elemento("input", {
    className: "campo-busca",
    type: "search",
    value: termoBusca,
    placeholder: "Buscar serviço, usuário ou URL",
    autocomplete: "off",
  });
  const botao = elemento("button", { className: "botao-busca", type: "submit", textContent: "Buscar" });

  form.append(input, botao);

  if (termoBusca) {
    const limparBusca = elemento("button", { className: "limpar-busca", type: "button", textContent: "Site atual" });
    limparBusca.addEventListener("click", async () => {
      termoBusca = "";
      await renderLista();
    });
    form.append(limparBusca);
  }

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    termoBusca = input.value.trim();
    if (termoBusca) await renderBuscaManual();
    else await renderLista();
  });

  return form;
}

function servicoSugerido() {
  return dominioAtual.replace(/^www\./i, "");
}

function textoErroGravacao(erro) {
  switch (erro) {
    case "servico_obrigatorio":
      return "Informe o serviço.";
    case "usuario_obrigatorio":
      return "Informe o usuário.";
    case "senha_obrigatoria":
      return "Informe a senha.";
    case "url_invalida":
      return "Informe uma URL válida.";
    case "duplicado":
      return "Já existe uma credencial parecida no cofre.";
    case "categoria_invalida":
      return "Selecione uma categoria válida.";
    case "nao_encontrado":
      return "Credencial não encontrada.";
    case "bloqueado":
      return "O cofre foi bloqueado antes de salvar. Desbloqueie novamente.";
    case "cofre_sem_permissao":
      return "Não foi possível gravar porque os arquivos do cofre não estão acessíveis.";
    case "cofre_formato_invalido":
      return "Não foi possível gravar porque o formato do cofre é incompatível.";
    case "permissao_negada":
      return "A extensão bloqueou essa operação.";
    case "conflito_escrita":
      return "O cofre foi alterado fora da extensão. Tranque e desbloqueie novamente antes de salvar.";
    case "integridade_invalida":
      return "A gravação falhou na validação de integridade.";
    case "erro_gravacao":
      return "Não foi possível gravar no cofre.";
    default:
      return "Não foi possível salvar a credencial.";
  }
}

function campoFormulario(nome, texto, input) {
  const label = elemento("label", { className: "grupo-form" });
  label.append(
    elemento("span", { className: "rotulo-form", textContent: texto }),
    input,
  );
  input.name = nome;
  input.className = "campo-form";
  return label;
}

function seletorCategoria(valor) {
  const select = elemento("select", { value: valor || "Other" });
  const categorias = [
    ["Work", "Trabalho"],
    ["Personal", "Pessoal"],
    ["Finance", "Finanças"],
    ["Social", "Social"],
    ["Other", "Outros"],
  ];
  for (const [value, label] of categorias) {
    select.append(elemento("option", { value, textContent: label }));
  }
  select.value = valor || "Other";
  return select;
}

async function voltarResultados() {
  if (termoBusca) await renderBuscaManual();
  else await renderLista();
}

function renderFormularioAdicionar() {
  rodape.hidden = false;
  btnAdicionar.disabled = true;
  limpar(conteudo);

  const erro = elemento("p", { className: "erro detalhe", textContent: "" });
  erro.hidden = true;

  const servico = elemento("input", {
    type: "text",
    value: servicoSugerido(),
    autocomplete: "off",
    required: true,
  });
  const usuario = elemento("input", {
    type: "text",
    autocomplete: "username",
    required: true,
  });
  const senha = elemento("input", {
    type: "password",
    autocomplete: "new-password",
    required: true,
  });
  const url = elemento("input", {
    type: "text",
    value: urlAtual,
    autocomplete: "url",
  });

  const cancelar = elemento("button", { className: "botao-secundario", type: "button", textContent: "Cancelar" });
  const salvar = elemento("button", { className: "botao", type: "submit", textContent: "Salvar" });
  const form = elemento(
    "form",
    { className: "formulario" },
    elemento("p", { className: "contagem", textContent: "Nova credencial" }),
    campoFormulario("servico", "Serviço", servico),
    campoFormulario("usuario", "Usuário", usuario),
    campoFormulario("senha", "Senha", senha),
    campoFormulario("url", "URL", url),
    erro,
    elemento("div", { className: "form-acoes" }, cancelar, salvar),
  );

  cancelar.addEventListener("click", () => {
    btnAdicionar.disabled = false;
    voltarResultados();
  });

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    erro.hidden = true;

    const payload = {
      tipo: "addCredential",
      servico: servico.value.trim(),
      usuario: usuario.value.trim(),
      senha: senha.value,
      url: url.value.trim(),
    };

    if (!payload.servico || !payload.usuario || !payload.senha) {
      erro.textContent = textoErroGravacao(!payload.servico
        ? "servico_obrigatorio"
        : !payload.usuario
          ? "usuario_obrigatorio"
          : "senha_obrigatoria");
      erro.hidden = false;
      return;
    }

    salvar.disabled = true;
    cancelar.disabled = true;
    salvar.textContent = "Salvando…";

    const r = await nativo(payload);
    if (r.ok) {
      senha.value = "";
      btnAdicionar.disabled = false;
      avisoLista = "Credencial adicionada.";
      classeAvisoLista = "sucesso";
      if (dominioAtual) {
        await renderLista();
      } else {
        termoBusca = payload.servico;
        await renderBuscaManual();
      }
      return;
    }

    salvar.disabled = false;
    cancelar.disabled = false;
    salvar.textContent = "Salvar";

    if (r.erro === "bloqueado") {
      renderBloqueado("O cofre foi bloqueado antes de salvar. Desbloqueie novamente.");
      return;
    }
    if (erroDeCofre(r.erro)) {
      renderCofreIndisponivel(r.cofre, r.erro);
      return;
    }

    erro.textContent = textoErroGravacao(r.erro);
    erro.hidden = false;
  });

  conteudo.append(form);
  servico.focus();
  servico.select();
}

async function abrirFormularioEditar(id, botao) {
  const textoOriginal = botao.textContent;
  botao.disabled = true;
  botao.textContent = "Abrindo…";

  const r = await nativo({ tipo: "getItem", id });
  if (r.ok) {
    renderFormularioEditar(r.item);
    return;
  }

  botao.disabled = false;
  botao.textContent = textoOriginal;

  if (r.erro === "bloqueado") {
    renderBloqueado("O cofre foi bloqueado antes de abrir a edição. Desbloqueie novamente.");
    return;
  }
  if (erroDeCofre(r.erro)) {
    renderCofreIndisponivel(r.cofre, r.erro);
    return;
  }

  avisoLista = textoErroGravacao(r.erro);
  classeAvisoLista = "erro detalhe";
  await voltarResultados();
}

function renderFormularioEditar(item) {
  rodape.hidden = false;
  btnAdicionar.disabled = true;
  limpar(conteudo);

  const erro = elemento("p", { className: "erro detalhe", textContent: "" });
  erro.hidden = true;

  const servico = elemento("input", {
    type: "text",
    value: item.servico ?? "",
    autocomplete: "off",
    required: true,
  });
  const usuario = elemento("input", {
    type: "text",
    value: item.usuario ?? "",
    autocomplete: "username",
    required: true,
  });
  const senha = elemento("input", {
    type: "password",
    autocomplete: "new-password",
  });
  const url = elemento("input", {
    type: "text",
    value: item.url ?? "",
    autocomplete: "url",
  });
  const categoria = seletorCategoria(item.categoria);
  const notas = elemento("textarea", {
    rows: 3,
    value: item.notas ?? "",
  });
  const favorito = elemento("input", {
    type: "checkbox",
    checked: Boolean(item.favorito),
  });

  const cancelar = elemento("button", { className: "botao-secundario", type: "button", textContent: "Cancelar" });
  const salvar = elemento("button", { className: "botao", type: "submit", textContent: "Salvar" });
  const form = elemento(
    "form",
    { className: "formulario" },
    elemento("p", { className: "contagem", textContent: "Editar credencial" }),
    campoFormulario("servico", "Serviço", servico),
    campoFormulario("usuario", "Usuário", usuario),
    campoFormulario("senha", "Nova senha", senha),
    campoFormulario("url", "URL", url),
    campoFormulario("categoria", "Categoria", categoria),
    campoFormulario("notas", "Notas", notas),
    elemento("label", { className: "check-form" }, favorito, elemento("span", { textContent: "Favorito" })),
    erro,
    elemento("div", { className: "form-acoes" }, cancelar, salvar),
  );

  cancelar.addEventListener("click", () => {
    btnAdicionar.disabled = false;
    voltarResultados();
  });

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    erro.hidden = true;

    const payload = {
      tipo: "updateCredential",
      id: item.id,
      servico: servico.value.trim(),
      usuario: usuario.value.trim(),
      senha: senha.value,
      url: url.value.trim(),
      categoria: categoria.value,
      notas: notas.value.trim(),
      favorito: favorito.checked,
    };

    if (!payload.servico || !payload.usuario) {
      erro.textContent = textoErroGravacao(!payload.servico
        ? "servico_obrigatorio"
        : "usuario_obrigatorio");
      erro.hidden = false;
      return;
    }

    salvar.disabled = true;
    cancelar.disabled = true;
    salvar.textContent = "Salvando…";

    const r = await nativo(payload);
    if (r.ok) {
      senha.value = "";
      btnAdicionar.disabled = false;
      avisoLista = "Credencial atualizada.";
      classeAvisoLista = "sucesso";
      await voltarResultados();
      return;
    }

    salvar.disabled = false;
    cancelar.disabled = false;
    salvar.textContent = "Salvar";

    if (r.erro === "bloqueado") {
      renderBloqueado("O cofre foi bloqueado antes de salvar. Desbloqueie novamente.");
      return;
    }
    if (erroDeCofre(r.erro)) {
      renderCofreIndisponivel(r.cofre, r.erro);
      return;
    }

    erro.textContent = textoErroGravacao(r.erro);
    erro.hidden = false;
  });

  conteudo.append(form);
  servico.focus();
  servico.select();
}

function agendarLimpezaClipboard(texto) {
  if (limpezaClipboard) clearTimeout(limpezaClipboard);
  limpezaClipboard = setTimeout(async () => {
    const timer = limpezaClipboard;
    try {
      if (typeof navigator.clipboard?.readText !== "function") return;
      const atual = await navigator.clipboard.readText();
      if (atual === texto) await navigator.clipboard.writeText("");
    } catch {
    } finally {
      if (limpezaClipboard === timer) limpezaClipboard = null;
    }
  }, TEMPO_LIMPEZA_CLIPBOARD_MS);
}

function restaurarBotao(botao, textoOriginal, tempo = 1500) {
  setTimeout(() => {
    botao.textContent = textoOriginal;
    botao.disabled = false;
  }, tempo);
}

async function gravarClipboard(texto, botao, textoOriginal) {
  try {
    await navigator.clipboard.writeText(texto);
    agendarLimpezaClipboard(texto);
    botao.textContent = "Copiado 30s";
    botao.title = "";
  } catch {
    botao.textContent = "Não copiou";
    botao.title = "O navegador bloqueou o acesso ao clipboard.";
  }
  restaurarBotao(botao, textoOriginal);
}

async function copiarTexto(texto, botao) {
  const textoOriginal = botao.textContent;
  botao.disabled = true;
  await gravarClipboard(texto, botao, textoOriginal);
}

async function copiarSenha(id, botao) {
  const textoOriginal = botao.textContent;
  botao.disabled = true;
  botao.textContent = "Carregando…";
  const r = await nativo({ tipo: "getCredential", id });
  if (!r.ok) {
    if (r.erro === "bloqueado") {
      renderBloqueado("Sessão expirada. Desbloqueie o cofre para copiar a senha.");
      return;
    }
    if (erroDeCofre(r.erro)) {
      renderCofreIndisponivel(r.cofre, r.erro);
      return;
    }
    botao.textContent = textoCurtoErro(r.erro);
    botao.title = textoErroGeral(r.erro);
    restaurarBotao(botao, textoOriginal);
    return;
  }
  await gravarClipboard(r.senha, botao, textoOriginal);
}

async function enviarParaAba(payload) {
  const [aba] = await chrome.tabs.query({ active: true, currentWindow: true });
  if (!aba?.id) return { ok: false, erro: "aba_indisponivel" };

  return new Promise((resolve) => {
    chrome.tabs.sendMessage(aba.id, payload, (resposta) => {
      if (chrome.runtime.lastError) {
        resolve({ ok: false, erro: chrome.runtime.lastError.message });
      } else {
        resolve(resposta ?? { ok: false, erro: "sem_resposta" });
      }
    });
  });
}

async function preencherAba(id, botao) {
  const textoOriginal = botao.textContent;
  botao.disabled = true;
  botao.textContent = "Preenchendo…";

  const r = await enviarParaAba({ canal: "cofre", tipo: "preencher", id });
  if (r.ok) {
    botao.textContent = "Preenchido";
    restaurarBotao(botao, textoOriginal);
  } else if (r.erro === "bloqueado") {
    renderBloqueado("O cofre foi bloqueado antes de preencher. Desbloqueie novamente.");
  } else if (r.erro === "campo_nao_encontrado") {
    botao.textContent = "Sem campo";
    botao.title = textoErroGeral(r.erro);
    restaurarBotao(botao, textoOriginal);
  } else {
    botao.textContent = textoCurtoErro(r.erro);
    botao.title = textoErroGeral(r.erro);
    restaurarBotao(botao, textoOriginal);
  }
}

function cartaoCredencial(item) {
  const btnEditar = elemento("button", { className: "acao", type: "button", textContent: "Editar" });
  btnEditar.addEventListener("click", () => abrirFormularioEditar(item.id, btnEditar));

  const btnUsuario = elemento("button", { className: "acao", type: "button", textContent: "Copiar usuário" });
  btnUsuario.addEventListener("click", () => copiarTexto(item.usuario, btnUsuario));

  const btnSenha = elemento("button", { className: "acao", type: "button", textContent: "Copiar senha" });
  btnSenha.addEventListener("click", () => copiarSenha(item.id, btnSenha));

  const acoes = [btnEditar, btnUsuario, btnSenha];
  if (dominioAtual) {
    const btnPreencher = elemento("button", { className: "acao acao-principal", type: "button", textContent: "Preencher" });
    btnPreencher.addEventListener("click", () => preencherAba(item.id, btnPreencher));
    acoes.unshift(btnPreencher);
  }

  const filhos = [
    elemento("div", { className: "cartao-servico", textContent: item.servico }),
    elemento("div", { className: "cartao-usuario", textContent: item.usuario }),
  ];
  if (item.url) filhos.push(elemento("div", { className: "cartao-url", textContent: item.url }));
  filhos.push(elemento("div", { className: "cartao-acoes" }, ...acoes));

  return elemento("li", { className: "cartao" }, ...filhos);
}

async function atualizarSessao() {
  const status = await nativo({ tipo: "status" });
  if (status.ok) sessaoAtual = status.sessao ?? sessaoAtual;
}

function renderResultados(titulo, itens, vazio) {
  limpar(conteudo);

  const blocos = [formularioBusca()];
  const resumoSessao = textoAutoLock();
  if (resumoSessao) blocos.push(elemento("p", { className: "sessao", textContent: resumoSessao }));
  if (avisoLista) {
    blocos.push(elemento("p", { className: classeAvisoLista, textContent: avisoLista }));
    avisoLista = "";
    classeAvisoLista = "sucesso";
  }

  if (itens.length === 0) {
    blocos.push(elemento("p", { className: "aviso", textContent: vazio }));
  } else {
    blocos.push(
      elemento("p", { className: "contagem", textContent: titulo }),
      elemento("ul", { className: "lista" }, ...itens.map(cartaoCredencial)),
    );
  }

  conteudo.append(...blocos);
}

async function renderLista() {
  termoBusca = "";
  rodape.hidden = false;
  btnAdicionar.disabled = false;

  if (!dominioAtual) {
    await atualizarSessao();
    renderResultados(
      "Credenciais",
      [],
      "Use a busca manual para encontrar credenciais.",
    );
    return;
  }

  mostrarAviso("Buscando credenciais…");

  const r = await nativo({ tipo: "query", dominio: dominioAtual });

  if (!r.ok) {
    if (r.erro === "bloqueado") { renderBloqueado("Sessão expirada. Desbloqueie o cofre para listar as credenciais."); return; }
    if (erroDeCofre(r.erro)) { renderCofreIndisponivel(r.cofre, r.erro); return; }
    mostrarErroTransporte(r.erro ?? "desconhecido");
    return;
  }

  const itens = r.itens ?? [];
  await atualizarSessao();
  renderResultados(
    `${itens.length} credencial(is) para ${dominioAtual}`,
    itens,
    `Nenhuma credencial salva para ${dominioAtual || "este site"}.`,
  );
}

async function renderBuscaManual() {
  rodape.hidden = false;
  btnAdicionar.disabled = false;
  mostrarAviso("Buscando credenciais…");

  const r = await nativo({ tipo: "search", termo: termoBusca });

  if (!r.ok) {
    if (r.erro === "bloqueado") { renderBloqueado("Sessão expirada. Desbloqueie o cofre para buscar credenciais."); return; }
    if (erroDeCofre(r.erro)) { renderCofreIndisponivel(r.cofre, r.erro); return; }
    mostrarErroTransporte(r.erro ?? "desconhecido");
    return;
  }

  const itens = r.itens ?? [];
  await atualizarSessao();
  renderResultados(
    `${itens.length} resultado(s) para "${termoBusca}"`,
    itens,
    `Nenhuma credencial encontrada para "${termoBusca}".`,
  );
}

btnTrancar.addEventListener("click", async () => {
  await nativo({ tipo: "lock" });
  renderBloqueado();
});

btnAdicionar.addEventListener("click", renderFormularioAdicionar);

async function iniciar() {
  const aba = await obterDadosAba();
  dominioAtual = aba.dominio;
  urlAtual = aba.url;
  elDominio.textContent = dominioAtual || "site não suportado";

  const status = await nativo({ tipo: "status" });
  if (!status.ok) {
    mostrarErroTransporte(status.erro ?? "desconhecido");
    return;
  }

  if (!status.cofre?.pronto) {
    renderCofreIndisponivel(status.cofre, "cofre_nao_encontrado");
    return;
  }

  if (status.destrancado) {
    sessaoAtual = status.sessao ?? null;
    await renderLista();
  } else {
    renderBloqueado();
  }
}

iniciar();
