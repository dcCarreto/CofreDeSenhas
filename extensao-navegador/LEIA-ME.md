# Extensão Cofre de Senhas (Chromium)

Extensão MV3 offline que preenche/copiar credenciais do seu Cofre de Senhas local,
conversando com um host nativo (Bridge → Agent) via Native Messaging. Veja o design
em [../docs/extensao-arquitetura.md](../docs/extensao-arquitetura.md).

> Estado atual (Fase 4): além do popup (que destranca e lista credenciais, com
> botões de copiar), há um **content script** que desenha o ícone do Cofre dentro
> dos campos de senha. Ao clicar no ícone, usuário e senha são preenchidos — com um
> menu de escolha quando há mais de uma credencial para o site. Nada é preenchido
> sem o clique do usuário e o formulário nunca é enviado automaticamente.

## Pré-requisitos

- Ter o app desktop **Cofre de Senhas** com a senha mestra já criada e ao menos uma
  credencial salva **com URL** (ex.: `https://github.com`).
- Windows com .NET 10 SDK (para compilar Bridge e Agent).

## Como testar em desenvolvimento

1. **Gerar os ícones** (uma vez):

   ```powershell
   powershell -ExecutionPolicy Bypass -File tools/gerar-icones.ps1
   ```

2. **Carregar a extensão**: abra `chrome://extensions` (ou `edge://extensions`,
   `brave://extensions`), ative o **Modo do desenvolvedor** e clique em
   **Carregar sem compactação**, apontando para a pasta `extensao-navegador`. Como o
   `manifest.json` fixa o ID via campo `key`, ele será sempre
   `glkkihapjkoncfobclhbcmndmpenmkhb`.

3. **Registrar o host nativo** (aponta para os binários de Debug):

   ```powershell
   powershell -ExecutionPolicy Bypass -File tools/registrar-host-dev.ps1
   ```

   Isso compila Bridge e Agent, grava o manifesto do host e cria as chaves de
   registro para Chrome, Edge e Brave usando o ID fixo.

4. **Reinicie o navegador** (para reler o registro de hosts nativos).

5. **Usar pelo popup**: navegue até um site para o qual você tem credencial salva
   (ex.: `github.com`), clique no ícone da extensão → **Desbloquear cofre** → digite
   a senha mestra no diálogo nativo → as credenciais do site aparecem. Use
   **Copiar senha** / **Copiar usuário**.

6. **Usar o preenchimento na página**: numa tela de login do mesmo site, o ícone do
   Cofre aparece dentro do campo de senha. Clique nele → (desbloqueie, se pedido) →
   usuário e senha são preenchidos. Havendo mais de uma credencial, escolha no menu.
   Após mudanças, recarregue a extensão em `chrome://extensions` e a página.

## Instalação para uso (produção)

Em vez do registro de desenvolvimento, instale o host de forma definitiva. Os
binários vão para `%LOCALAPPDATA%\CofreDeSenhas\host`:

```powershell
powershell -ExecutionPolicy Bypass -File tools/instalar.ps1
# ou, sem depender do runtime .NET instalado:
powershell -ExecutionPolicy Bypass -File tools/instalar.ps1 -SelfContained
```

Para remover:

```powershell
powershell -ExecutionPolicy Bypass -File tools/desinstalar.ps1
```

## Solução de problemas

- **"Não foi possível falar com o Cofre"**: o host não está registrado, ou o
  navegador não foi reiniciado. Rode `registrar-host-dev.ps1` (ou `instalar.ps1`) e
  reinicie o navegador.
- **Diálogo da senha mestra não aparece**: verifique se o `CofreDeSenhas.Agent.exe`
  iniciou (Gerenciador de Tarefas). O Bridge tenta iniciá-lo automaticamente.
- **Lista vazia num site com credencial**: confirme que a credencial tem o campo
  **URL** preenchido e que o domínio registrável bate (ex.: `github.com`).

## Teste sem navegador

Para validar o protocolo direto no Agent (sem a extensão):

```powershell
powershell -ExecutionPolicy Bypass -File tools/testar-protocolo.ps1 -Dominio github.com
```
