# Cofre de Senhas

🇧🇷 Português | [🇺🇸 English](README.en.md)

Gerenciador de senhas multiplataforma (Windows e Linux), com gerador de senhas
integrado e cofre local criptografado. A aplicação reúne, em uma única
interface, a criação de senhas fortes e o armazenamento seguro de credenciais,
protegidos por uma senha mestra e por criptografia AES-256-GCM. Foi desenvolvido
em C# com .NET 10 e Avalonia, com uma única base de código para todas as
plataformas.

[![CI](https://github.com/dcCarreto/CofreDeSenhas/actions/workflows/ci.yml/badge.svg)](https://github.com/dcCarreto/CofreDeSenhas/actions/workflows/ci.yml)
![Licença](https://img.shields.io/badge/licen%C3%A7a-PolyForm%20Noncommercial-blue)
![Plataforma](https://img.shields.io/badge/plataforma-Windows%2010%2F11%20%7C%20Linux-0078D6)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)

Este é um projeto de código aberto, gratuito e sem fins comerciais, distribuído
sob a licença PolyForm Noncommercial 1.0.0. Você pode usá-lo, estudá-lo,
modificá-lo e compartilhá-lo livremente para qualquer finalidade não comercial.

## Sumário

- [Sobre o projeto e o código aberto](#sobre-o-projeto-e-o-código-aberto)
- [Visão geral](#visão-geral)
- [Capturas de tela](#capturas-de-tela)
- [Funcionalidades](#funcionalidades)
- [Modelo de segurança](#modelo-de-segurança)
- [Política de segurança e divulgação de vulnerabilidades](SECURITY.md)
- [Política de assinatura de código](#política-de-assinatura-de-código-code-signing-policy)
- [Download e instalação](#download-e-instalação)
- [Estrutura do projeto](#estrutura-do-projeto)
- [Arquitetura](#arquitetura)
- [Tecnologias](#tecnologias)
- [Requisitos](#requisitos)
- [Compilação e execução](#compilação-e-execução)
- [Testes](#testes)
- [Armazenamento de dados](#armazenamento-de-dados)
- [Como contribuir](#como-contribuir)
- [Licença](#licença)

## Sobre o projeto e o código aberto

Sempre acreditei que boas ferramentas deveriam estar ao alcance de qualquer
pessoa. O Cofre de Senhas nasce dessa convicção: um projeto aberto e gratuito
para usar, examinar, modificar e distribuir sem fins comerciais, sob a licença
PolyForm Noncommercial 1.0.0 — sem versões pagas, sem recursos escondidos atrás
de assinatura e sem coleta de dados. A motivação completa está em
[docs/SOBRE.md](docs/SOBRE.md).

## Visão geral

O programa funciona, por padrão, inteiramente de forma local: nenhum dado é
enviado a servidores externos, salvo a verificação opcional de vazamento de
senhas, que utiliza um protocolo de anonimato descrito na seção de segurança. As
credenciais ficam em um arquivo criptografado dentro do perfil do usuário, e a
chave de criptografia é derivada da senha mestra em tempo de execução, nunca
sendo gravada em disco. Opcionalmente, é possível conectar o cofre a um banco de
dados de sua escolha (SQLite, PostgreSQL, MySQL/MariaDB ou SQL Server); nesse
caso, o cofre local e o banco são sincronizados e mantidos espelhados, sempre
com a senha armazenada de forma cifrada.

A janela principal tem uma barra lateral de navegação (cofre, favoritas,
recentes e categorias) e, ao centro, a lista de credenciais com busca, filtro
por categoria, auditoria e ações por item. O gerador de senhas abre em um painel
sob demanda e também aparece lado a lado com o desbloqueio na tela de senha
mestra, permitindo criar e copiar senhas antes mesmo de abrir o cofre.

## Capturas de tela

A tela principal reúne a barra lateral de navegação, a lista de credenciais e o
status do cofre no rodapé, na identidade visual padrão "cofre, latão escovado":
tema escuro de carvão quente, com latão escovado como destaque e tipografia
serifada nos títulos — a aba Aparência das Configurações oferece tema claro,
outras cores de destaque e densidade compacta. As imagens abaixo usam dados
fictícios apenas para demonstração.

![Cofre de Senhas: barra lateral, lista de credenciais e rodapé](docs/captura-cofre.png)

Na tela de senha mestra, o gerador fica à esquerda e o desbloqueio à direita, com
seletor de idioma e, no Windows, a opção de desbloqueio por Windows Hello:

![Tela de senha mestra com gerador e desbloqueio](docs/tela-login.png)

## Funcionalidades

Lista completa, com todos os detalhes e capturas de tela adicionais, em
[docs/FUNCIONALIDADES.md](docs/FUNCIONALIDADES.md). Resumo por área:

- **Gerador de senhas** — senhas e frases-senha configuráveis, indicador de
  força em tempo real, geração em lote, disponível também antes do desbloqueio.
- **Cofre de senhas** — tipos de credencial (cartão, Wi-Fi, servidor, banco de
  dados...), TOTP, códigos de recuperação, categorias e etiquetas, busca e
  filtros, lixeira, relatório de segurança, histórico de senhas, anexos
  criptografados, digitação automática (Windows) e atalhos de teclado.
- **Segurança e autenticação** — limite de tentativas, bloqueio automático,
  Windows Hello, troca de senha mestra com re-criptografia, zona de risco para
  limpar ou excluir o cofre.
- **Backup e recuperação** — backup automático local, exportação/importação
  (`.gsenhas` e CSV de outros gerenciadores), QR code de backup, chave de
  recuperação opcional.
- **Sincronização entre dispositivos** — pasta compartilhada (Dropbox,
  OneDrive etc.), cifrada de ponta a ponta, sem depender de banco de dados.
- **Banco de dados externo** — SQLite, PostgreSQL, MySQL/MariaDB ou SQL
  Server, para uso self-hosted ou compartilhado, com HMAC de integridade por
  linha.
- **Interface** — janela sem moldura, temas e cores, manual embutido em seis
  idiomas, modo privacidade, acessibilidade de nível especialista (daltonismo,
  contraste, leitor de tela com voz neural embutida), mesma experiência no
  Windows e no Linux.
- **Atualização automática** — verifica a release mais recente e se atualiza
  sozinho (instalador/portátil/AppImage), conferindo o hash antes de aplicar.

## Modelo de segurança

Tabela completa de algoritmos e parâmetros em
[docs/SEGURANCA.md](docs/SEGURANCA.md). Resumo:

- Cofre cifrado com AES-256-GCM; chave derivada da senha mestra por Argon2id
  (64 MiB, 3 iterações) — a senha mestra em si nunca é armazenada.
- Comparações sensíveis em tempo constante; verificação de vazamento de senha
  (Have I Been Pwned) por k-anonymity, opcional e desligada por padrão.
- Sincronização por pasta e banco de dados externo sempre cifrados
  (AES-256-GCM / TLS), com HMAC de integridade por credencial no banco externo.
- Desbloqueio por Windows Hello (opcional) protegido por TPM; higiene de
  memória ao bloquear ou fechar o cofre.
- Dados ficam só na pasta do usuário (`%APPDATA%\GerenciadorSenhas\` no
  Windows, `~/.config/GerenciadorSenhas/` no Linux), fora do repositório.

Para como reportar uma vulnerabilidade encontrada no cofre, veja a
[política de segurança](SECURITY.md). Para o que o desenho de segurança do cofre
cobre e o que fica deliberadamente fora dele, veja o [modelo de ameaça](THREAT_MODEL.md).

## Política de assinatura de código (Code signing policy)

O projeto solicitou uma assinatura de código gratuita à [SignPath
Foundation](https://signpath.org) para os binários do Windows, hoje sem
certificado Authenticode (ver [Verificando a integridade dos
arquivos](docs/INSTALACAO.md#verificando-a-integridade-dos-arquivos)). Assim que a
inscrição for aprovada, as releases do Windows passam a exibir:

> Free code signing provided by [SignPath.io](https://about.signpath.io),
> certificate by [SignPath Foundation](https://signpath.org)

**Papéis do projeto** — mantido por uma única pessoa, que acumula todos os
papéis abaixo:

- Committers e revisores de código: [dcCarreto](https://github.com/dcCarreto)
- Aprovador de cada release para assinatura: [dcCarreto](https://github.com/dcCarreto)

**Política de privacidade** — este programa não transfere nenhuma informação a
outros sistemas em rede, a não ser quando explicitamente solicitado por quem
instala ou opera o programa ("*this program will not transfer any information
to other networked systems unless specifically requested by the user or the
person installing or operating it*"). Os dois únicos casos, ambos opcionais e
desligados por padrão, estão descritos no [modelo de
segurança](#modelo-de-segurança): a verificação de vazamento de senha (Have I
Been Pwned, por k-anonymity) e a busca de ícones de serviço por domínio.

## Download e instalação

A forma mais simples é baixar o executável pronto nas
[releases](../../releases) — autocontido, sem precisar instalar o .NET. Veja a
[Política de assinatura de código](#política-de-assinatura-de-código-code-signing-policy)
sobre o estado da assinatura Authenticode no Windows.

- **Windows**: baixe e rode `CofreDeSenhas-Setup-*.exe` (instalador, não exige
  administrador) ou `CofreDeSenhas.exe` (portátil, sem instalador) na
  [última release](../../releases/latest).
- **Linux**: baixe `CofreDeSenhas-*-x86_64.AppImage`, rode `chmod +x` nele e
  execute — sem precisar instalar o .NET.
- Só quer gerar senhas sem abrir o cofre? `GeradorDeSenhas-*-win-x64-portatil.exe`,
  também na página de releases.
- Prefere terminal? Cada release traz `cofre`, uma linha de comando
  somente-leitura para automação — veja `cofre --help`.

No primeiro uso, o programa pede a criação de uma senha mestra: guarde-a com
cuidado, é a única forma de abrir o cofre. Passo a passo completo — instalador
do Windows, script de instalação e desinstalação no Linux, uso do `cofre`,
verificação de integridade dos arquivos por hash e attestation — em
[docs/INSTALACAO.md](docs/INSTALACAO.md).

## Estrutura do projeto

```text
CofreDeSenhas.sln
├─ App/                          Interface (Avalonia), multiplataforma
│  ├─ Janelas/                   Telas e diálogos da aplicação
│  ├─ Controles/                 Controles customizados de UI
│  ├─ Infraestrutura/            Tema, preferências, recursos e utilitários
│  ├─ Ativos/                    Ícone do aplicativo
│  └─ distribuicao/              Scripts e atalhos de instalação (Linux e Windows)
├─ Gerador/                      Gerador de senhas standalone (Avalonia)
├─ Cli/                          Executável de linha de comando `cofre`
│                                (somente-leitura; referencia só o domínio)
├─ GerenciadorDeSenhas/          Biblioteca de domínio
│  ├─ Modelos/                   Entidades (Senha, Categoria, SenhaExportada,
│  │                             TipoBanco, ConexaoBanco)
│  ├─ Repositorios/              Acesso às credenciais (arquivo local, banco e
│  │                             espelho que sincroniza os dois)
│  └─ Servicos/                  Criptografia, persistência, autenticação,
│                                exportação, verificação de vazamento, conexão
│                                a banco de dados e regras
├─ GerenciadorDeSenhas.Testes/   Testes de domínio (xUnit)
├─ App.Testes/                   Testes da interface (xUnit + Avalonia headless)
└─ Cli.Testes/                   Testes do CLI (xUnit)
```

A solução separa a interface (projeto `App`) da lógica de domínio (projeto
`GerenciadorDeSenhas`). Isso mantém as regras de negócio e a criptografia
independentes da camada gráfica e permite testá-las de forma isolada — o CLI
(`Cli`) consome o mesmo domínio sem nenhuma dependência de UI.

## Arquitetura

A aplicação é organizada em camadas, sem dependências obrigatórias de banco de
dados ou de serviços externos:

- Apresentação (projeto `App`): janelas e controles em Avalonia, com o layout
  em XAML e os controles customizados desenhados por código. Não contém regra de
  negócio nem operação criptográfica.
- Domínio e serviços (projeto `GerenciadorDeSenhas`): autenticação da senha
  mestra, criptografia, persistência, exportação, auditoria e verificação de
  vazamento.
- Persistência: por padrão, os dados são serializados em JSON e gravados de
  forma criptografada em um arquivo no perfil do usuário. Opcionalmente, o cofre
  pode ser sincronizado e mantido espelhado com um banco de dados externo
  (SQLite, PostgreSQL, MySQL/MariaDB ou SQL Server), acessado por ADO.NET,
  guardando a senha sempre cifrada.

O fluxo completo de derivação e re-criptografia da senha mestra (Argon2id,
migração de cofres antigos em PBKDF2, backup e rollback) está em
[docs/SEGURANCA.md](docs/SEGURANCA.md#fluxo-da-senha-mestra).

## Tecnologias

- C# e .NET 10.
- Avalonia e XAML para a interface, multiplataforma (Windows e Linux).
- Criptografia da biblioteca padrão (`System.Security.Cryptography`):
  AES-256-GCM e PBKDF2-SHA256 (exportação e migração de cofres antigos), mais
  Argon2id (`Konscious.Security.Cryptography.Argon2`) para a chave do cofre.
- Serialização com `System.Text.Json`.
- Acesso a banco de dados por ADO.NET: Microsoft.Data.Sqlite, Npgsql,
  MySqlConnector e Microsoft.Data.SqlClient.
- Geração de QR code com a biblioteca QRCoder.
- Testes com xUnit.

## Requisitos

- Windows 10 ou 11, ou uma distribuição Linux com X11 ou Wayland.
- Para compilar a partir do código-fonte: .NET 10 SDK.
- Para executar o binário publicado: nada além do próprio executável, que é
  autocontido.

## Compilação e execução

A partir da raiz do repositório, em qualquer plataforma:

```sh
dotnet run --project App/App.csproj
```

Para gerar o executável publicável ou o instalador do Windows, veja
[docs/BUILD.md](docs/BUILD.md).

## Testes

```sh
dotnet test
```

A suíte cobre testes unitários, testes de integração de ponta a ponta (criação,
persistência e recarga do cofre), testes de segurança (rejeição de dados
adulterados e de chave incorreta) e testes de desempenho com grande volume de
senhas.

## Armazenamento de dados

Os arquivos da aplicação ficam em `%APPDATA%\GerenciadorSenhas\` no Windows e
em `~/.config/GerenciadorSenhas/` no Linux:

- `auth.dat`: salt e verificador da senha mestra.
- `senhas.json.enc`: cofre criptografado com as credenciais.
- `biometria.dat`: chave do cofre cifrada e vinculada a uma credencial do
  Windows Hello, para desbloqueio biométrico quando ativado neste dispositivo.
- `config.json`: preferências da interface (como o idioma e a acessibilidade) e
  o último perfil de conexão a banco, sem a senha do servidor.
- `backups/`: cópias de segurança do cofre.

Esses arquivos não fazem parte do repositório e contêm dados sensíveis.

Ao rodar o projeto a partir do código-fonte (`dotnet run`, ou o executável de
uma compilação de Debug), os dados vão para `GerenciadorSenhas.dev/` ao lado
da pasta acima — nunca para a pasta do aplicativo instalado. A suíte de testes
usa uma pasta temporária descartável, apagada ao final. Só o binário publicado
para release usa `GerenciadorSenhas/`.

## Como contribuir

Contribuições são bem-vindas. Sinta-se à vontade para abrir uma issue relatando
um problema ou sugerindo uma melhoria, ou para enviar um pull request. Algumas
orientações simples:

- Mantenha o estilo de código existente.
- Sempre que possível, acompanhe novas funcionalidades com testes.
- Descreva com clareza o que sua mudança faz e por quê.

## Licença

Distribuído sob a licença [PolyForm Noncommercial 1.0.0](https://polyformproject.org/licenses/noncommercial/1.0.0).
Consulte o arquivo [LICENSE](LICENSE) para os termos completos. Em resumo: você
pode usar, copiar, estudar, modificar e redistribuir este software livremente
para qualquer finalidade **não comercial**, mantendo o aviso de copyright e estes
termos. Uso comercial — vender o programa, cobrar por ele ou embuti-lo em um
produto ou serviço pago — não é permitido. O aplicativo é gratuito e assim
permanecerá.
