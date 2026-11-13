# Política de privacidade

[English version](PRIVACY.en.md)

**Vigente desde:** 12 de novembro de 2026
**Aplica-se a:** Cofre de Senhas — aplicativo desktop para Windows e Linux, a
linha de comando `cofre` e os instaladores distribuídos pelas
[releases do GitHub](https://github.com/dcCarreto/CofreDeSenhas/releases), pelo
Windows Package Manager (winget, pacote `dcCarreto.CofreDeSenhas`) e pela
Microsoft Store.
**Responsável:** Denis Cristino Cantagallo Carreto, autor e mantenedor do projeto.

## Resumo

- O Cofre de Senhas é um aplicativo **local**. Tudo o que você guarda nele fica
  no seu computador, cifrado com uma chave derivada da sua senha mestra.
- O autor **não opera nenhum servidor** para o aplicativo e **não recebe, coleta
  nem armazena** nenhum dado seu. Não há conta, cadastro, telemetria, análise de
  uso, relatório de falhas automático nem publicidade.
- Por padrão, o aplicativo só acessa a internet para **procurar e baixar as
  próprias atualizações** nas releases do GitHub, sem enviar nenhum dado seu
  (seção 3.3); isso pode ser desligado. Os demais recursos que usam a rede são
  opcionais, ligados por você, e estão descritos abaixo com exatamente o que
  enviam e para quem.

## 1. Dados que o aplicativo guarda no seu computador

O aplicativo armazena, no seu próprio computador, os dados que você digita ou
importa nele:

- credenciais: nome do serviço, usuário, senha, endereço do site, notas,
  etiquetas e campos extras de cada tipo de item (por exemplo dados de cartão,
  quando você os informa);
- segredos de autenticação em dois fatores (TOTP) e códigos de recuperação;
- histórico de senhas anteriores de cada item;
- anexos (arquivos que você associa a um item);
- datas de criação, alteração e da última cópia de senha, usuário ou código
  TOTP (o registro da última cópia pode ser desligado nas configurações).

Esses dados ficam na pasta de dados do aplicativo:

- Windows: `%APPDATA%\GerenciadorSenhas\`
- Linux: `~/.config/GerenciadorSenhas/`

O cofre (`senhas.json.enc`), os backups e cada anexo são cifrados com
AES-256-GCM, usando uma chave derivada da senha mestra (Argon2id). A senha mestra
nunca é gravada em disco: guarda-se apenas um salt e um verificador
(`auth.dat`). Sem a senha mestra — ou sem a chave de recuperação, se você a
ativou — o conteúdo não pode ser lido, nem pelo autor. Os detalhes técnicos estão
em [docs/SEGURANCA.md](docs/SEGURANCA.md) e no
[modelo de ameaça](THREAT_MODEL.md).

Além do cofre, a mesma pasta pode conter:

| Arquivo ou pasta | Conteúdo | Cifrado? |
|---|---|---|
| `backups/` | Cópias automáticas do cofre. Por padrão, até 10 cópias; as mais antigas são apagadas automaticamente. Frequência e quantidade são configuráveis. | Sim |
| `anexos/` | Um arquivo por anexo, com nome aleatório. | Sim |
| `recuperacao.dat` | Só existe se você ativar a chave de recuperação. | Sim |
| `biometria.dat` | Só existe se você ativar o Windows Hello. A biometria em si é tratada pelo Windows; o aplicativo nunca acessa impressão digital nem imagem do rosto. | Sim (Windows Hello + DPAPI) |
| `config.json` | Preferências da interface e, se você conectar um banco de dados externo, o perfil da conexão (tipo, servidor, porta, nome do banco e usuário). A senha do servidor, se houver, é guardada cifrada com a chave do cofre. | Parcial |
| `pontuacao-historico.json` | Datas e notas da pontuação de segurança do cofre, para o gráfico de evolução. Não contém nomes de serviço nem senhas. | Não |
| `logs/erros.log` | Log de diagnóstico (ver seção 2). | Não |
| `icones/` | Cache de ícones de sites, só com os ícones online ligados. O nome de cada arquivo é um hash do domínio. | Não |
| `vozes/` | Modelos de voz para leitura em voz alta, só se você baixá-los. | Não |

Fora dessa pasta, o aplicativo grava apenas os **downloads de atualização**, numa
pasta `CofreDeSenhas-update-*` dentro da pasta temporária do sistema. São os
mesmos arquivos públicos das releases do GitHub, sem nenhum dado seu, e são
apagados na abertura seguinte do aplicativo. A versão da Microsoft Store usa a
mesma pasta de dados acima (não uma pasta privada do pacote), por isso
desinstalá-la não apaga o cofre.

## 2. Logs de diagnóstico

O aplicativo grava erros inesperados em `logs/erros.log`, na pasta de dados, para
ajudar você (ou o mantenedor, se você decidir compartilhar) a entender um
problema. Cada linha tem a data e hora (UTC), o tipo do erro e a mensagem dele.

- O log **nunca é enviado** a lugar nenhum pelo aplicativo. Ele só sai do seu
  computador se você mesmo anexá-lo a um relato.
- Senhas, notas, segredos TOTP e o conteúdo decifrado do cofre não são gravados
  no log. O caminho da pasta do seu perfil do Windows, que contém seu nome de
  usuário do sistema, é substituído por `%USERPROFILE%`.
- **Exceção:** quando a sincronização com um banco de dados externo detecta um
  conflito (por exemplo, uma linha adulterada), o log registra o tipo do
  conflito, o **nome do serviço** do item afetado e o identificador interno dele,
  para que você possa revisar depois. Revise o arquivo antes de compartilhá-lo.
- O arquivo é apagado e recomeçado quando passa de cerca de 1 MB.

## 3. Recursos que usam a rede

Nenhum dos recursos abaixo funciona sem uma ação ou um consentimento seu. Em todos
eles, o serviço de destino recebe, como em qualquer conexão de internet, o seu
endereço IP e um identificador do aplicativo (User-Agent). O que cada serviço faz
com esses dados é regido pela política de privacidade dele, não por esta.

### 3.1 Verificação de senhas vazadas (Have I Been Pwned)

- **Quando:** só quando você gera ou atualiza o relatório de segurança do cofre.
- **O que é enviado:** para cada senha, apenas os **5 primeiros caracteres do
  hash SHA-1** dela (técnica de *k-anonymity*) para `api.pwnedpasswords.com`. A
  senha, o hash completo, o usuário e o nome do serviço nunca saem do computador.
  A comparação com a lista de hashes devolvida é feita localmente.
- **Destino:** serviço Pwned Passwords, do Have I Been Pwned —
  <https://haveibeenpwned.com/Privacy>.

### 3.2 Ícones online dos serviços

- **Quando:** só se você ligar "Ícones online" nas configurações. Desligado por
  padrão; o aplicativo pede consentimento antes de ligar.
- **O que é enviado:** apenas o **domínio** de cada item (por exemplo
  `github.com`) para o serviço de favicons do Google
  (`www.google.com/s2/favicons`). Nenhuma senha, usuário ou nota é enviada.
- **Retenção:** os ícones baixados ficam em cache em `icones/`. Ao desligar o
  recurso, o cache é apagado.
- **Destino:** Google — <https://policies.google.com/privacy>.

### 3.3 Verificação e instalação de atualizações

- **Quando:** com "Atualizar automaticamente" ligado nas configurações (ligado
  por padrão), o aplicativo consulta a última release ao abrir o cofre e depois a
  cada 6 horas enquanto ele fica aberto.
- **O que é enviado:** uma consulta pública à API do GitHub
  (`api.github.com/repos/dcCarreto/CofreDeSenhas/releases/latest`), sem nenhum
  dado do cofre nem identificador seu ou do seu dispositivo.
- Havendo uma versão nova, o instalador (ou o executável portátil/AppImage) e os
  arquivos de verificação (`CHECKSUMS.txt` e assinatura) são baixados das releases
  do GitHub em segundo plano. A atualização só é instalada se a assinatura e o
  SHA256 conferirem, e a instalação acontece quando o cofre é bloqueado.
- Até a versão de 6 de novembro de 2026 esta verificação vinha desligada e nada
  era instalado sem confirmação. Quem já usava o aplicativo passa a ter a
  atualização automática ligada e vê um aviso na primeira abertura depois da
  mudança. Ela pode ser desligada a qualquer momento em Configurações >
  Comportamento.
- **Versão da Microsoft Store:** não consulta o GitHub. Quem atualiza é a
  própria Loja, conforme a configuração de atualização de aplicativos dela, sob a
  [política de privacidade da Microsoft](https://privacy.microsoft.com/privacystatement).
- **Destino:** GitHub — <https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement>.

### 3.4 Download de vozes para leitura em voz alta

- **Quando:** só quando você pede, nas configurações de acessibilidade, para
  baixar a voz neural de um idioma.
- **O que é enviado:** apenas a requisição do arquivo do modelo de voz, nas
  releases públicas do projeto sherpa-onnx no GitHub. A leitura em voz alta em
  si é feita localmente; nenhum texto do cofre é enviado.

### 3.5 Sincronização por pasta compartilhada

- **Quando:** só se você configurar uma pasta de sincronização (por exemplo uma
  pasta do OneDrive, Dropbox ou de rede).
- **O que é gravado:** o aplicativo escreve na pasta que **você** escolheu um
  arquivo `sincronizacao.dat`, cifrado com AES-256-GCM por uma chave derivada da
  senha mestra, e, se você ligar "Sincronizar anexos", um arquivo cifrado por
  anexo em `anexos-sincronizados/`. O autor não tem acesso a essa pasta.
- **O que um terceiro com acesso à pasta vê:** só texto cifrado. Com anexos
  sincronizados, também a quantidade de anexos, o tamanho aproximado de cada um
  e quando foram criados ou removidos.
- O transporte e o armazenamento ficam a cargo do provedor que você escolheu e
  seguem a política de privacidade dele.

### 3.6 Banco de dados externo

- **Quando:** só se você conectar o cofre a um banco de dados (SQLite,
  PostgreSQL, MySQL/MariaDB ou SQL Server) que **você** opera ou em que confia.
- **Visibilidade dos campos:** senha, segredo TOTP, histórico de senhas e
  códigos de recuperação são cifrados antes de sair do aplicativo. **Nome do
  serviço, usuário, notas e etiquetas ficam em texto puro** nas colunas do banco,
  legíveis por quem tiver acesso de leitura às tabelas (por exemplo, o
  administrador do banco). O banco também recebe o salt e o verificador da senha
  mestra (tabela `CofreDeSenhasAuth`), o que permite a quem tem acesso ao banco
  tentar adivinhar a senha mestra por força bruta offline. Anexos nunca vão para
  o banco.
- A conexão é sempre cifrada em trânsito (TLS). Os detalhes estão no
  [modelo de ameaça](THREAT_MODEL.md).

## 4. O que o aplicativo não faz

- Não envia telemetria, estatísticas de uso, relatórios de falha nem
  identificadores de dispositivo.
- Não exibe anúncios e não vende, aluga nem compartilha dados com ninguém — o
  autor não tem acesso a dados para compartilhar.
- Não exige conta nem cadastro.
- Não lê seus dados de outros aplicativos. Importações (CSV, JSON, KeePass
  `.kdbx` e outros) só leem o arquivo que você escolher.

## 5. Área de transferência, digitação automática e exportação

- Senhas e códigos copiados são marcados para ficar fora do Histórico da Área de
  Transferência e da Área de Transferência na Nuvem do Windows, e a área de
  transferência é limpa automaticamente após 30 segundos (configurável).
- A digitação automática (Windows) envia usuário e senha apenas para a janela
  que você confirmou.
- A exportação no formato `.gsenhas` é cifrada com uma senha que você define.
  As exportações em CSV ou JSON (formato Bitwarden) **saem em texto puro**, só
  depois de você confirmar a senha mestra e um aviso; o arquivo fica onde você
  salvá-lo e passa a ser responsabilidade sua.

## 6. Retenção e exclusão

Como os dados ficam no seu computador, você controla quanto tempo eles existem:

- **Itens excluídos** vão para a lixeira (dentro do cofre cifrado) e ficam lá
  até você restaurá-los, excluí-los em definitivo ou esvaziar a lixeira; não há
  expiração automática. Depois da exclusão definitiva, o item ainda existe nos
  backups cifrados até esses backups serem substituídos pela rotação automática.
- **"Excluir cofre"** (nas configurações, com confirmação da senha mestra) apaga
  o cofre, os backups, os anexos, o arquivo de autenticação, o vínculo do Windows
  Hello, o histórico de pontuação e os perfis de banco de dados e de
  sincronização salvos.
- **Para apagar tudo**, inclusive logs, cache de ícones, vozes e preferências,
  apague a pasta de dados inteira (seção 1). No Windows, o desinstalador oferece
  essa opção ao final da desinstalação.
- **Downloads de atualização** ficam na pasta temporária do sistema só até
  serem instalados e são apagados na abertura seguinte do aplicativo.
- Desinstalar o aplicativo pela **Microsoft Store** não apaga a pasta de dados;
  para apagar tudo, remova-a manualmente.
- Dados gravados **fora** do computador por sua escolha — a pasta de
  sincronização, um banco de dados externo ou arquivos exportados — não são
  apagados pelo aplicativo; remova-os diretamente onde estão.

## 7. Seus controles

| Recurso | Padrão | Onde mudar |
|---|---|---|
| Ícones online | Desligado | Configurações |
| Atualizar automaticamente | Ligado | Configurações |
| Verificação de vazamentos | Só sob demanda | Relatório de segurança |
| Registro da última cópia | Ligado (fica dentro do cofre cifrado) | Configurações |
| Sincronização por pasta | Desligada | Configurações |
| Banco de dados externo | Desconectado | Configurações |
| Windows Hello | Desligado | Configurações |
| Chave de recuperação | Desligada | Configurações |
| Backups automáticos | Semanais, até 10 cópias | Configurações |

## 8. Crianças

O aplicativo não é direcionado a crianças e não coleta dados de ninguém,
de nenhuma idade.

## 9. Leis de proteção de dados (LGPD, GDPR)

O autor não coleta nem trata dados pessoais dos usuários: os dados do cofre ficam
sob o seu controle, no seu dispositivo ou na infraestrutura que você escolher. Os
serviços de terceiros listados na seção 3 tratam os dados que recebem conforme as
próprias políticas.

## 10. Alterações nesta política

Esta política é mantida no repositório do projeto, e todas as versões anteriores
ficam no
[histórico do arquivo](https://github.com/dcCarreto/CofreDeSenhas/commits/prod/PRIVACY.md).
Mudanças relevantes — por exemplo, um novo recurso que use a rede — são
registradas aqui antes ou junto da release que as introduz, com a data de
vigência atualizada.

## 11. Contato

Dúvidas sobre esta política:
[abra uma issue](https://github.com/dcCarreto/CofreDeSenhas/issues). Para
relatar uma vulnerabilidade, use o canal privado descrito em
[SECURITY.md](SECURITY.md).
