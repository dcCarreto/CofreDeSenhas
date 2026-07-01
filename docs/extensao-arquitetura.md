# Extensão de navegador — arquitetura e protocolo

Documento de design da extensão offline para navegadores Chromium (Chrome, Edge,
Brave e derivados). A extensão pede a senha mestra **uma vez** e preenche
usuário/senha do site quando o usuário clica.

## Por que Native Messaging

Uma extensão Chromium roda numa *sandbox*: não consegue ler o arquivo do cofre
(`%AppData%\GerenciadorSenhas\senhas.json.enc`) nem deve reimplementar o
AES-256-GCM/PBKDF2 em JavaScript (duplicaria código sensível e exporia a chave na
memória do navegador). A forma estabelecida — usada por KeePassXC, 1Password e
Bitwarden — é **Native Messaging**: a extensão troca mensagens JSON via
stdin/stdout com um executável local registrado no navegador.

O host nativo é um projeto .NET que **reaproveita a biblioteca
`GerenciadorDeSenhas`** sem reescrever criptografia. A cadeia de reuso para
recuperar uma senha em claro:

```
AutenticacaoMestra.Autenticar(senhaMestra)   -> chave (PBKDF2-SHA256, 32 bytes)
new ServicoCriptografia(chave)
new PersistenciaLocal(cripto)                 -> carrega senhas.json.enc
new RepositorioSenha(persistencia, chave)     -> List<Senha>
cripto.Descriptografar(senha.SenhaHash)       -> senha em claro
```

> Nota: o campo `Senha.SenhaHash` tem nome enganoso — é AES-256-GCM reversível
> (`ServicoCriptografia.Criptografar`), não um hash de via única. Por isso o
> autofill é possível.

## Especificação criptográfica

| Item | Valor |
| ---- | ----- |
| Derivação da chave | PBKDF2-SHA256 |
| Iterações | 100000 |
| Salt | 16 bytes |
| Chave | 32 bytes |
| Verificador de senha mestra | SHA-256 da chave derivada |
| Criptografia do cofre e das senhas | AES-256-GCM |
| IV | 12 bytes |
| Tag | 16 bytes |
| Payload criptografado | Base64 de `IV + ciphertext + tag` |

Esses valores ficam centralizados em `EspecificacaoCriptografica` e são usados
por autenticação, criptografia, descoberta do cofre e validação de formato.

## Componentes

```
┌─────────────────────┐   Native Messaging   ┌──────────────────────────┐
│  Extensão (MV3)     │  (JSON via stdio,    │  Bridge (.NET console)   │
│  ├ background.js    │   prefixo 4 bytes)   │  ponte stdio <-> pipe    │
│  ├ content.js       │ ◄──────────────────► │  (sem UI, sem estado)    │
│  └ popup.html/js    │                      └──────────┬───────────────┘
└─────────────────────┘                                 │ named pipe
        ▲ autofill no clique                            │ (JSON por linha)
   página do site                          ┌────────────▼───────────────┐
                                           │  Agent (.NET WinForms)      │
                                           │  - diálogo de unlock nativo │
                                           │  - chave em memória         │
                                           │  - auto-lock por inatividade│
                                           │  - pipe server              │
                                           └─────────────────────────────┘
```

Dois processos do lado nativo porque, no MV3, o *service worker* da extensão é
efêmero: se ele morre, a conexão de Native Messaging cai e o processo do host
encerra. Mantendo a sessão destrancada no **Agent** (processo de vida longa),
a senha mestra é pedida uma única vez e sobrevive à reciclagem do service worker.
O **Bridge** é descartável: o navegador o inicia a cada conexão e ele apenas
repassa mensagens entre o stdio e o named pipe do Agent (iniciando o Agent se
ainda não estiver no ar).

## Protocolo de mensagens

Mensagens JSON. Native Messaging (extensão ↔ Bridge) usa o enquadramento padrão
do Chromium: cada mensagem é precedida de 4 bytes (UInt32, ordem nativa) com o
tamanho do JSON UTF-8. Bridge ↔ Agent usa JSON por linha sobre o named pipe.

Campo comum: toda requisição tem `tipo`; toda resposta tem `ok` (bool) e, em erro,
`erro` (string).

| `tipo`           | Requisição                | Resposta (`ok: true`)                       |
| ---------------- | ------------------------- | ------------------------------------------- |
| `status`         | —                         | `{ destrancado: bool, total: int, sessao: { destrancado, total, bloqueioAutomaticoSegundos, expiraEmSegundos }, cofre: { encontrado, pronto, estado, formato, erro } }` |
| `unlock`         | —                         | `{ status: "unlocked" \| "cancelled" }`     |
| `query`          | `{ dominio: string }`     | `{ itens: [{ id, servico, usuario, url }] }` |
| `search`         | `{ termo: string }`       | `{ itens: [{ id, servico, usuario, url }] }` |
| `getCredential`  | `{ id: string }`          | `{ usuario: string, senha: string }`        |
| `getItem`        | `{ id: string }`          | `{ item: { id, servico, usuario, url, categoria, notas, favorito } }` |
| `addCredential`  | `{ servico, usuario, senha, url }` | `{ item: { id, servico, usuario, url } }` |
| `updateCredential` | `{ id, servico, usuario, senha, url, categoria, notas, favorito }` | `{ item: { id, servico, usuario, url, categoria, notas, favorito } }` |
| `lock`           | —                         | `{ }`                                        |

Regras de segurança do protocolo:

- `query` e `search` devolvem **apenas metadados** (id, serviço, usuário e URL)
  — nunca a senha — para montar a lista de escolha no popup/página.
- A senha em claro só sai em `getCredential`, que é disparado por um **clique
  explícito** do usuário. Não há preenchimento automático nem submissão de
  formulário.
- O botão **Preencher** do popup envia a credencial escolhida para o content
  script da aba atual; o content script localiza um campo de senha visível e só
  então chama `getCredential`.
- Os botões de cópia usam o Clipboard API e tentam limpar o clipboard após 30
  segundos quando conseguem confirmar que o conteúdo ainda é o mesmo.
- `addCredential` valida campos obrigatórios, normaliza URL sem esquema para
  `https://`, recusa duplicatas óbvias por serviço/usuário/domínio e grava a
  senha com a mesma criptografia do cofre.
- `getItem` devolve apenas campos editáveis não sensíveis. A senha atual não é
  retornada; em `updateCredential`, senha vazia preserva a senha existente e
  senha preenchida substitui o valor criptografado.
- `updateCredential` preserva o identificador, data de criação e campos JSON
  desconhecidos, atualiza `DataAtualizacao` e não expõe nenhuma ação de remoção.
- Gravações de `addCredential` e `updateCredential` usam arquivo temporário na
  pasta do cofre, backup local do arquivo anterior, substituição atômica,
  validação de descriptografia/leitura após salvar e retornam `conflito_escrita`
  quando o arquivo muda fora da extensão desde o desbloqueio.
- `unlock` faz o Agent exibir um diálogo nativo; a senha mestra é digitada ali e
  **nunca transita pelo JavaScript da extensão**.
- O manifesto do host nativo declara `allowed_origins` com o ID da extensão —
  apenas ela pode conversar com o Bridge.
- O Agent mantém a chave derivada apenas em memória, pede a senha mestra uma vez
  por sessão e re-tranca após 15 minutos de inatividade ou quando recebe `lock`.
- Antes do desbloqueio, o Agent valida a presença de `auth.dat` e
  `senhas.json.enc` nos caminhos suportados e retorna `cofre_nao_encontrado`,
  `cofre_sem_permissao` ou `cofre_formato_invalido` quando a extensão não pode
  abrir o cofre local.

## Correspondência de domínio

O `query` recebe o host da aba atual (ex.: `login.exemplo.com`) e compara com o
host de `Senha.Url` de cada credencial, normalizando para o domínio registrável
(eTLD+1) e ignorando `www.`. Credenciais sem `Url` não aparecem na detecção
automática da página, apenas na busca manual do popup.

## Modelo de permissões

A extensão mantém o `manifest.json` sem `host_permissions` separados e declara
apenas permissões de API necessárias para o fluxo atual:

| Permissão | Uso |
| --------- | --- |
| `nativeMessaging` | Comunicação com o Bridge, que chama o Agent local e acessa o cofre fora da sandbox do navegador. |
| `activeTab` | Leitura da URL da aba atual no popup e envio do comando de preenchimento para essa aba após ação do usuário. |
| `clipboardWrite` | Cópia de usuário ou senha quando o usuário clica no botão correspondente. |
| `clipboardRead` | Conferência posterior do clipboard para limpar o valor copiado após 30 segundos quando ele ainda for o mesmo. |

O acesso a páginas fica restrito aos `matches` do content script. Ele é carregado
somente em páginas `http://` e `https://`, sem acesso a `file://`, páginas
internas do navegador, páginas da loja de extensões ou outras origens não
suportadas. Esse script apenas desenha o botão no campo de senha e solicita
consulta/preenchimento quando há clique do usuário.

O arquivo local do cofre nunca é lido diretamente pelo JavaScript da extensão. O
acesso ao disco fica restrito ao host nativo registrado no Windows. Esse host é
independente do app desktop: o navegador inicia o Bridge sob demanda, o Bridge
inicia ou reaproveita o Agent, e o Agent lê `auth.dat` e `senhas.json.enc` nos
caminhos suportados.

O manifesto do host nativo usa `allowed_origins` com o ID fixo da extensão. O
`background.js` também aplica uma lista de comandos permitidos antes de repassar
qualquer mensagem ao host:

| Origem | Comandos permitidos |
| ------ | ------------------- |
| Popup da extensão | `status`, `unlock`, `lock`, `query`, `search`, `getCredential`, `getItem`, `addCredential`, `updateCredential` |
| Content script da página | `unlock`, `query`, `getCredential` |

Mensagens vindas de abas com origem diferente de `http://` ou `https://` são
negadas. Quando o content script pede `query`, o `background.js` ignora o domínio
informado no payload e usa o host real da aba remetente. Com isso, páginas só
conseguem iniciar desbloqueio/consulta/preenchimento através do content script
instalado pela própria extensão. Operações de escrita
(`addCredential` e `updateCredential`) ficam restritas ao popup, e não existe
comando de remoção no manifesto, no JavaScript ou no protocolo nativo.

## Estados de erro e UX

| Estado | Superfície | Tratamento |
| ------ | ---------- | ---------- |
| Cofre ausente | Popup e content script | Mostra que a extensão só funciona com um cofre já criado. |
| Senha mestra incorreta | Diálogo nativo | Mantém o diálogo aberto, limpa o campo e informa que a senha está incorreta. |
| Cofre bloqueado ou sessão expirada | Popup e content script | Solicita novo desbloqueio antes de listar, buscar, copiar, salvar ou preencher. |
| Conflito de escrita | Popup | Informa que o cofre mudou fora da extensão e pede trancar/desbloquear antes de salvar. |
| Sem permissão no arquivo | Popup e content script | Informa que `auth.dat` e `senhas.json.enc` não estão acessíveis pelo usuário atual. |
| Formato incompatível | Popup e content script | Informa que o arquivo encontrado não está no formato criptografado esperado. |
| Campo de senha ausente | Popup | Informa que não há campo visível na aba atual para preenchimento. |
| Clipboard bloqueado | Popup | Informa que o navegador bloqueou a cópia. |

Os formulários de adição e edição validam campos obrigatórios antes de chamar o
host, desabilitam os botões durante a gravação e retornam mensagens específicas
para duplicidade, URL inválida, categoria inválida, conflito, erro de integridade
e falha de gravação. O content script mostra tooltips curtos no próprio campo de
senha para falhas de desbloqueio, consulta ou preenchimento. Nenhuma dessas
superfícies oferece ou sugere remoção de credenciais.

## Registro no navegador

O Bridge é registrado via um manifesto JSON apontado por uma chave de registro
por usuário:

- Chrome/Brave: `HKCU\Software\Google\Chrome\NativeMessagingHosts\com.dccarreto.cofredesenhas`
- Edge: `HKCU\Software\Microsoft\Edge\NativeMessagingHosts\com.dccarreto.cofredesenhas`

O manifesto contém o caminho do `CofreDeSenhas.Bridge.exe` e o `allowed_origins`
com o ID da extensão. O registro será feito por script de instalação (fase 5).
