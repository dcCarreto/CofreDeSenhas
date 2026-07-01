# Extensão Cofre de Senhas (Chromium)

Extensão MV3 offline que preenche/copiar credenciais do seu Cofre de Senhas local,
conversando com um host nativo (Bridge → Agent) via Native Messaging. Veja o design
em [../docs/extensao-arquitetura.md](../docs/extensao-arquitetura.md).

> Estado atual (Fase 10): o popup destranca o cofre, lista credenciais da aba
> atual, busca manualmente por serviço/usuário/URL, preenche a aba atual por
> clique, copia usuário/senha tentando limpar o clipboard após 30 segundos e
> adiciona/edita credenciais do cofre. O content script desenha o ícone do Cofre
> dentro dos campos de senha e também preenche por clique. Nada é preenchido sem
> ação do usuário, o formulário nunca é enviado automaticamente e a extensão não
> oferece remoção de credenciais. As gravações criam backup local, usam
> substituição atômica e recusam salvar quando o arquivo foi alterado fora da
> extensão desde o desbloqueio. O background valida os comandos permitidos por
> origem antes de chamar o host nativo. Os estados de cofre ausente, senha mestra
> incorreta, cofre bloqueado, conflito de escrita, falta de permissão e formato
> incompatível têm mensagens próprias no fluxo.

## Pré-requisitos

- Ter um cofre já criado em `%APPDATA%\GerenciadorSenhas\`, com a senha mestra
  configurada e ao menos uma credencial salva **com URL** (ex.:
  `https://github.com`).
- Windows com .NET 10 SDK (para compilar Bridge e Agent).

## Permissões

A extensão declara `nativeMessaging`, `activeTab`, `clipboardWrite` e
`clipboardRead`. O content script roda apenas em páginas `http://` e `https://`.
O JavaScript da extensão não lê arquivos locais; o acesso ao cofre é feito pelo
Bridge/Agent registrado como host nativo e limitado ao ID fixo da extensão por
`allowed_origins`.

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
   **Preencher**, **Editar**, **Copiar usuário** ou **Copiar senha**. Use
   **Adicionar senha** para salvar uma nova credencial; a URL da aba atual é
   sugerida no formulário. As cópias tentam limpar o clipboard após 30 segundos
   quando o navegador permite.

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

Para validar compatibilidade criptográfica entre a biblioteca base e a extensão:

```powershell
powershell -ExecutionPolicy Bypass -File tools/testar-compatibilidade.ps1
```
