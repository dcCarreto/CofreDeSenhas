# Cofre de Senhas - extensao de navegador

Branch enxuta para desenvolvimento da extensao Chromium do Cofre de Senhas.

A extensao roda offline e conversa com o cofre local por Native Messaging. O
app desktop nao faz parte desta branch; ficam aqui apenas a extensao, os hosts
nativos e a biblioteca minima usada para abrir o cofre existente.

O JavaScript da extensao nao acessa o sistema de arquivos. A leitura e a escrita
do cofre passam pelo Bridge/Agent nativo, registrado no navegador com
`allowed_origins` preso ao ID da extensao. No navegador, o manifest usa apenas
`nativeMessaging`, `activeTab`, `clipboardWrite` e `clipboardRead`.

## Estrutura

```text
CofreDeSenhas.sln
|-- extensao-navegador/        Extensao MV3 para Chrome, Edge e Brave
|-- CofreDeSenhas.Bridge/      Host Native Messaging iniciado pelo navegador
|-- CofreDeSenhas.Agent/       Processo nativo com dialogo de desbloqueio
|-- CofreDeSenhas.Nucleo/      Protocolo, pipe, sessao e regras da extensao
|-- GerenciadorDeSenhas/       Biblioteca de cofre, criptografia e persistencia
|-- tools/                     Scripts de icones, instalacao e registro do host
`-- docs/                      Arquitetura da extensao
```

## Requisitos

- Windows 10 ou 11.
- .NET 10 SDK para compilar Bridge e Agent.
- Chrome, Edge, Brave ou outro navegador Chromium compativel com extensoes MV3.
- Um cofre ja criado em `%APPDATA%\GerenciadorSenhas\`.

## Desenvolvimento

Gerar icones da extensao:

```powershell
powershell -ExecutionPolicy Bypass -File tools/gerar-icones.ps1
```

Compilar os hosts nativos:

```powershell
dotnet build CofreDeSenhas.sln
```

Registrar o host nativo em modo desenvolvimento:

```powershell
powershell -ExecutionPolicy Bypass -File tools/registrar-host-dev.ps1
```

Validar compatibilidade criptografica entre a biblioteca base e a extensao:

```powershell
powershell -ExecutionPolicy Bypass -File tools/testar-compatibilidade.ps1
```

Carregue `extensao-navegador` em `chrome://extensions`, `edge://extensions` ou
`brave://extensions` usando "Carregar sem compactacao". Depois reinicie o
navegador para ele reler o registro do host nativo.

## Instalacao local

Para publicar Bridge e Agent em `%LOCALAPPDATA%\CofreDeSenhas\host` e registrar
o Native Messaging Host:

```powershell
powershell -ExecutionPolicy Bypass -File tools/instalar.ps1
```

Para publicar autocontido:

```powershell
powershell -ExecutionPolicy Bypass -File tools/instalar.ps1 -SelfContained
```

Para remover o host:

```powershell
powershell -ExecutionPolicy Bypass -File tools/desinstalar.ps1
```

## Arquitetura

Veja [docs/extensao-arquitetura.md](docs/extensao-arquitetura.md) e
[extensao-navegador/LEIA-ME.md](extensao-navegador/LEIA-ME.md).
