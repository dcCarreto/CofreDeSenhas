# Modelo de ameaça

Este documento descreve o que o Cofre de Senhas protege, contra que tipo de atacante, e o
que fica deliberadamente fora do que o aplicativo se propõe a resolver. O objetivo é dar
transparência real a quem usa ou contribui com o projeto — nenhum software é seguro contra
tudo, e fingir o contrário é pior do que documentar os limites.

Para os mecanismos criptográficos em si (algoritmos, parâmetros), veja a tabela em
[Modelo de segurança](README.md#modelo-de-segurança) no README. Este documento é sobre
adversários e cenários, não sobre implementação.

## O que é protegido

- O conteúdo do cofre: nome de serviço, usuário, senha, notas, campos extras por tipo de
  credencial, segredo TOTP, histórico de senhas anteriores e códigos de recuperação.
- A senha mestra em si — nunca é gravada em disco, nem cifrada, nem em texto puro.
- A senha usada para conexão a um banco de dados externo, quando aplicável.
- A senha de exportação usada no arquivo `.gsenhas`.

## Perfis de atacante considerados

### Alguém com acesso ao arquivo do cofre, mas sem a senha mestra

Cenário: um pen drive, backup, ou o computador roubado/perdido enquanto o cofre está
fechado. `senhas.json.enc` é cifrado por inteiro com AES-256-GCM; `auth.dat` guarda só o
salt e um verificador (hash da chave derivada), nunca a chave em si nem a senha mestra.
Sem a senha mestra, o conteúdo não é recuperável — não existe backdoor nem chave mestra de
recuperação (ver "Fora de escopo" no `ROADMAP.md`). Adulterar o arquivo cifrado é detectado
e rejeitado alto e claro (`InvalidOperationException`), não faz o cofre carregar dado
corrompido silenciosamente.

O que o AEAD **não** cobre é *rollback*: quem tem escrita no diretório de dados pode
sobrescrever `senhas.json.enc` com uma cópia antiga, ainda válida, e reverter alterações
recentes (uma senha trocada, um item removido). Anti-rollback de verdade exige um contador
monotônico em armazenamento confiável fora do alcance de quem tampera (TPM, servidor) — fora
do modelo de um app de desktop sem privilégio elevado. O que o aplicativo faz é detectar o
caso óbvio: se o `senhas.json.enc` for uma cópia byte a byte de um dos arquivos em
`backups/`, ele avisa na abertura que o cofre parece ter sido restaurado por fora do app.
Um atacante que guarde a própria cópia antiga do arquivo dribla essa checagem.

### Alguém com acesso à memória do processo enquanto o cofre está aberto

**Este é o ponto mais fraco do modelo hoje, e vale ser dito sem rodeio.** Enquanto o cofre
está desbloqueado, a chave mestra vive como um `byte[]` comum na memória gerenciada do
processo — sem `SecureString`, sem travar a página contra swap (`mlock`/`VirtualLock`), sem
proteção contra hibernação. Ela só é zerada (`CryptographicOperations.ZeroMemory`) ao
bloquear ou fechar o cofre. Um atacante capaz de anexar um depurador, tirar um dump do
processo, ou vasculhar um arquivo de swap/hibernação **enquanto o cofre está aberto**
consegue, em tese, extrair a chave ou dados decifrados momentaneamente em memória. Isso não
é uma falha específica do cofre — é uma limitação inerente a rodar em um processo de
usuário comum, sem elevar privilégios nem depender de hardware dedicado (TPM/enclave) para
guardar segredos em uso — mas é real, e por isso listada aqui em vez de deixada implícita.

### Atacante de rede

O cofre não fala com nenhum serviço externo por padrão, fora duas exceções opcionais e bem
delimitadas: a verificação de senha comprometida (Have I Been Pwned, por k-anonymity — só
os 5 primeiros caracteres do hash SHA-1 da senha saem da máquina) e a busca de ícones reais
por favicon (desligada por padrão, exige consentimento explícito, envia só o domínio do
serviço). A checagem de nova versão é uma leitura pública da API do GitHub, sem enviar nada
além da própria consulta. Não há telemetria de nenhum tipo. Um atacante observando o
tráfego de rede não vê senhas, usuários ou qualquer conteúdo do cofre em trânsito.

### Provedor da pasta de sincronização (Dropbox, OneDrive etc.) ou alguém com acesso a ela

A sincronização por pasta compartilhada cifra tudo (AES-256-GCM) com uma chave derivada da
senha mestra antes de qualquer gravação em disco — o provedor de nuvem, ou qualquer pessoa
com acesso de leitura à pasta, só vê texto cifrado. Adulterar o arquivo de sincronização é
rejeitado pela autenticação do AEAD, mas de um jeito assimétrico em relação ao cofre local:
`ServicoSincronizacao` absorve qualquer erro de leitura (arquivo corrompido, chave errada,
formato inesperado) e volta uma lista vazia em vez de lançar exceção. Na prática, isso
significa que uma pasta de sincronização adulterada não expõe nem forja dado nenhum — mas
falha silenciosamente, sem avisar quem usa o cofre que a sincronização parou de funcionar
daquele ponto em diante. É uma lacuna de disponibilidade/aviso, não de confidencialidade.

### Administrador de um banco de dados externo conectado (self-hosted/compartilhado)

Ao conectar o cofre a um banco externo (SQLite, PostgreSQL, MySQL/MariaDB ou SQL Server),
nem todo campo é cifrado individualmente antes de virar uma coluna. Senha, segredo TOTP,
histórico de senhas anteriores e códigos de recuperação são — cada um cifrado com a mesma
chave da senha mestra antes de sair do aplicativo. Nome do serviço, usuário, notas e
etiquetas **não são**: ficam em texto puro nas colunas correspondentes, legíveis por
qualquer pessoa com acesso direto de leitura às linhas da tabela (um administrador do
banco, por exemplo), mesmo sem a senha mestra do cofre. Isso é diferente do cofre local e
da pasta de sincronização, onde o arquivo inteiro é um único blob cifrado — não existe essa
distinção campo a campo. Por isso o banco de dados externo é pensado como um recurso
self-hosted/compartilhado, sob controle de quem já confia no ambiente onde o banco roda, e
não como mais um dispositivo pessoal simétrico como a pasta de sincronização.

**O que mudou:** cada credencial agora carrega um HMAC-SHA256, calculado sobre todos os
campos relevantes (inclusive os que ficam em texto puro) e chaveado por uma subchave
derivada da senha mestra via HKDF — independente da chave usada pela cifra AES-GCM, para
não reaproveitar a mesma chave em dois primitivos diferentes. Isso cobre a lacuna de
**integridade** que existia antes: alguém com acesso de escrita ao banco, mas sem a senha
mestra, não conseguia forjar a senha em si (cifrada), mas conseguia alterar nome de
serviço, usuário, notas, etiquetas ou a própria data de atualização sem ser percebido — e,
pior, uma data de atualização forjada mais recente fazia esse dado adulterado "vencer" o
merge e se propagar para todos os outros dispositivos espelhados. Hoje, qualquer linha cujo
HMAC não bate com o conteúdo é rejeitada na sincronização (fica de fora do merge, o
dispositivo local continua com sua própria versão) em vez de aceita. Uma violação detectada
fica registrada e visível na tela de log de conflitos de sincronização do aplicativo.

**O que não mudou:** o HMAC garante integridade, não confidencialidade. Um administrador do
banco (ou qualquer pessoa com acesso de leitura direto às linhas) continua conseguindo
**ler** nome de serviço, usuário, notas e etiquetas em texto puro — só não consegue mais
**alterar** esses campos sem ser detectado. O tratamento de uma linha **sem HMAC nenhum**
na coluna depende da opção "Exigir assinatura de integridade nas linhas" da conexão, ligada
por padrão em conexões novas: ligada, a linha fica de fora da mesclagem (não entra no cofre)
e é registrada como conflito, fechando a brecha de forjar uma linha nova simplesmente não
gravando o HMAC; desligada — necessário só enquanto o banco é compartilhado com dispositivos
numa versão do app anterior a esse recurso —, a linha sem HMAC volta a ser tratada como
legado confiável e mesclada, com um aviso. Em qualquer dos modos, a próxima gravação naquela
linha por um dispositivo atualizado passa a assiná-la.

Também nesta versão: a conexão ao banco pode exigir certificado de servidor validado por
uma autoridade confiável (opção "Exigir certificado válido do servidor", desligada por
padrão) em vez de aceitar qualquer certificado autoassinado — reduz a superfície para um
ataque de intermediário (man-in-the-middle) contra quem conecta a um banco fora da rede
local. Desligada (o padrão), a conexão continua sempre cifrada em trânsito (TLS), só sem
validar a identidade do servidor — o relatório de segurança do cofre sinaliza quando essa
opção está desligada, como lembrete, não bloqueio.

**Também nesta versão:** conectar o cofre a um banco externo publica automaticamente, numa
tabela separada (`CofreDeSenhasAuth`), o mesmo salt e verificador da senha mestra que antes
só existiam em `auth.dat` local — feito para viabilizar a função "Restaurar de um banco de
dados" ao configurar um dispositivo novo a partir de um banco já em uso. Isso amplia a
superfície do administrador do banco: além dos campos em texto puro já descritos acima, ele
passa a ter acesso ao material necessário para tentar quebrar a senha mestra offline (ataque
de força bruta/dicionário contra o verificador, sem precisar de mais nada do dispositivo
local) — algo que antes exigia acesso ao dispositivo dono do cofre. O verificador nunca
permite decifrar o conteúdo do cofre diretamente, só confirmar se uma tentativa de senha
está correta; o risco real depende da força da senha mestra escolhida e dos parâmetros de
custo (Argon2id/PBKDF2) usados na derivação, os mesmos que já protegiam `auth.dat` local.
Esses parâmetros, lidos da tabela de auth do banco (assim como de um arquivo de exportação
ou do cabeçalho da pasta de sincronização), passam por um teto de sanidade antes de
alimentar o KDF: um valor absurdo — memória na casa dos terabytes, por exemplo — é
rejeitado como entrada inválida em vez de esgotar a memória do dispositivo que tenta usar.

### Desbloqueio biométrico (Windows Hello)

É opcional, por dispositivo — cada máquina tem seu próprio vínculo biométrico
(`biometria.dat`), nada disso sincroniza. A chave do cofre fica cifrada com uma chave
derivada da assinatura de uma credencial do Windows Hello, cuja chave privada mora no TPM;
o envelope só abre depois de uma autenticação biométrica bem-sucedida do sistema
operacional. Por cima disso, o `biometria.dat` é protegido por DPAPI amarrado à conta do
Windows — copiá-lo para outra conta ou máquina não abre nada, nem antes de chegar na
assinatura do Windows Hello. A senha mestra continua sempre disponível como alternativa — biometria nunca
substitui, só complementa. Se a chave do cofre muda (troca de senha mestra ou migração do
algoritmo de derivação), o vínculo antigo se autodesabilita em vez de continuar concedendo
acesso com uma chave desatualizada.

## Fora de escopo, deliberadamente

- **Sistema operacional comprometido.** Um keylogger, malware com acesso de tela, ou
  qualquer coisa capturando a senha mestra no momento em que é digitada está fora do que
  este aplicativo pode se proteger — nenhum gerenciador de senhas resolve isso sozinho.
- **Malware ativo com o cofre já desbloqueado.** Se algo malicioso já está rodando com os
  mesmos privilégios do usuário enquanto o cofre está aberto, ele pode, em tese, interagir
  com a interface ou ler memória do processo (ver seção acima). Isolamento de processo,
  sandboxing e antivírus são responsabilidade do sistema operacional, não deste aplicativo.
- **Perda da senha mestra.** Não existe recuperação — nem por servidor externo, nem por
  backdoor, nem por qualquer mecanismo que dispense a senha mestra (ou uma chave
  equivalente, como o vínculo biométrico). Esquecer a senha mestra sem ter um backup
  significa perder o acesso ao cofre.
- **Engenharia social e phishing.** Convencer a pessoa usuária a revelar a própria senha
  mestra por fora do aplicativo está fora de qualquer controle técnico que o cofre ofereça.
- **Ataques físicos avançados a hardware** (side-channel, cold boot attack, chip-off em
  TPM) — fora do modelo de ameaça de um gerenciador de senhas para uso pessoal em desktop.
- **Compromissos de cadeia de suprimento em dependências de terceiros** (pacotes NuGet,
  o próprio .NET) antes de chegarem a este repositório — isso continua fora de alcance.
  O que o pipeline de release cobre: cada artefato publicado (instalador, portátil,
  pacote Linux, AppImage) tem hash SHA256 em `CHECKSUMS.txt` e uma *attestation* de
  proveniência (SLSA/Sigstore, via `actions/attest-build-provenance`) verificável
  publicamente contra o log de transparência do Sigstore/Rekor — não é só um arquivo de
  hash ao lado do binário na mesma release (que um comprometimento da release trocaria
  junto), é uma prova assinada de que aquele arquivo exato saiu daquele workflow exato,
  a partir daquele commit exato. As dependências de terceiros usadas *pelo próprio
  workflow* (GitHub Actions de checkout, build, upload/download e publicação) são fixadas
  por hash de commit, não por tag flutuante, e o `appimagetool` baixado durante o build do
  Linux tem versão fixa com hash conferido antes de rodar. O atualizador em um clique
  embutido no aplicativo, além de conferir o SHA256, exige uma assinatura destacada de
  `CHECKSUMS.txt` verificada contra uma chave pública fixada no binário antes de executar
  o instalador/portátil/AppImage baixado — se a assinatura faltar ou não conferir, a
  atualização automática é recusada e a página de releases é aberta para download manual.
  Essa mesma chave pública RSA está versionada no repositório (`update-signing-public.pem`)
  para conferência manual da assinatura, e a release ainda carrega uma assinatura GPG
  destacada do `CHECKSUMS.txt` quando há chave configurada; o `SECURITY.md` descreve os
  passos. Assinatura de código (Authenticode) no instalador Windows segue como item futuro
  do roadmap, hoje sem certificado disponível — é o que eliminaria o aviso de "editor
  desconhecido" do SmartScreen.

## Como isso evolui

Este documento reflete o desenho atual, não uma promessa estática. À medida que o roadmap
avança — chave de hardware (FIDO2/YubiKey), por exemplo — as seções acima devem ser
revisadas para refletir o que muda. Encontrou algo que deveria estar aqui e não está? Veja
como reportar em [SECURITY.md](SECURITY.md).
