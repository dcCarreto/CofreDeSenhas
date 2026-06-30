const HOST_NATIVO = "com.dccarreto.cofredesenhas";

chrome.runtime.onMessage.addListener((mensagem, _remetente, responder) => {
  if (mensagem?.canal !== "nativo") {
    return false;
  }

  chrome.runtime.sendNativeMessage(HOST_NATIVO, mensagem.payload, (resposta) => {
    if (chrome.runtime.lastError) {
      responder({ ok: false, erro: chrome.runtime.lastError.message });
    } else {
      responder(resposta);
    }
  });

  return true;
});
