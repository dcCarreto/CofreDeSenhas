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
| `status`         | —                         | `{ destrancado: bool, total: int }`         |
| `unlock`         | —                         | `{ status: "unlocked" \| "cancelled" }`     |
| `query`          | `{ dominio: string }`     | `{ itens: [{ id, servico, usuario }] }`     |
| `getCredential`  | `{ id: string }`          | `{ usuario: string, senha: string }`        |
| `lock`           | —                         | `{ }`                                        |

Regras de segurança do protocolo:

- `query` devolve **apenas metadados** (id, serviço, usuário) — nunca a senha —
  para montar a lista de escolha no popup/página.
- A senha em claro só sai em `getCredential`, que é disparado por um **clique
  explícito** do usuário. Não há preenchimento automático nem submissão de
  formulário.
- `unlock` faz o Agent exibir um diálogo nativo; a senha mestra é digitada ali e
  **nunca transita pelo JavaScript da extensão**.
- O manifesto do host nativo declara `allowed_origins` com o ID da extensão —
  apenas ela pode conversar com o Bridge.
- O Agent re-tranca (descarta a chave da memória) após período de inatividade.

## Correspondência de domínio

O `query` recebe o host da aba atual (ex.: `login.exemplo.com`) e compara com o
host de `Senha.Url` de cada credencial, normalizando para o domínio registrável
(eTLD+1) e ignorando `www.`. Credenciais sem `Url` não aparecem na detecção
automática da página, apenas na busca manual do popup.

## Registro no navegador

O Bridge é registrado via um manifesto JSON apontado por uma chave de registro
por usuário:

- Chrome/Brave: `HKCU\Software\Google\Chrome\NativeMessagingHosts\com.dccarreto.cofredesenhas`
- Edge: `HKCU\Software\Microsoft\Edge\NativeMessagingHosts\com.dccarreto.cofredesenhas`

O manifesto contém o caminho do `CofreDeSenhas.Bridge.exe` e o `allowed_origins`
com o ID da extensão. O registro será feito por script de instalação (fase 5).
