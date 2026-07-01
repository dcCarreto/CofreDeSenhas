const HOST_NATIVO = "com.dccarreto.cofredesenhas";
const COMANDOS_POPUP = new Set([
  "status",
  "unlock",
  "lock",
  "query",
  "search",
  "getCredential",
  "getItem",
  "addCredential",
  "updateCredential",
]);
const COMANDOS_PAGINA = new Set([
  "unlock",
  "query",
  "getCredential",
]);

function remetentePagina(remetente) {
  return urlPaginaRemetente(remetente) !== null;
}

function urlPaginaRemetente(remetente) {
  if (typeof remetente?.tab?.id !== "number" || !remetente.url) return null;
  try {
    const url = new URL(remetente.url);
    return url.protocol === "http:" || url.protocol === "https:" ? url : null;
  } catch {
    return null;
  }
}

function comandoPermitido(payload, remetente) {
  if (!payload || typeof payload.tipo !== "string") return false;
  if (remetente?.tab) {
    return remetentePagina(remetente) && COMANDOS_PAGINA.has(payload.tipo);
  }
  return COMANDOS_POPUP.has(payload.tipo);
}

function payloadComOrigem(payload, remetente) {
  const url = urlPaginaRemetente(remetente);
  if (url && payload.tipo === "query") {
    return { ...payload, dominio: url.hostname.toLowerCase() };
  }
  return payload;
}

chrome.runtime.onMessage.addListener((mensagem, remetente, responder) => {
  if (mensagem?.canal !== "nativo") {
    return false;
  }

  if (!comandoPermitido(mensagem.payload, remetente)) {
    responder({ ok: false, erro: "permissao_negada" });
    return false;
  }

  const payload = payloadComOrigem(mensagem.payload, remetente);

  chrome.runtime.sendNativeMessage(HOST_NATIVO, payload, (resposta) => {
    if (chrome.runtime.lastError) {
      responder({ ok: false, erro: chrome.runtime.lastError.message });
    } else {
      responder(resposta);
    }
  });

  return true;
});
