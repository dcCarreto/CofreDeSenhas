# Changelog

Todas as mudanças notáveis deste projeto são documentadas aqui.
O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/)
e o projeto adota [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [Não lançado]

### Alterado
- Licença migrada de MIT para PolyForm Noncommercial 1.0.0: o código-fonte
  continua aberto para estudar, modificar e redistribuir, agora com uso
  comercial vedado. O aplicativo é e continuará gratuito.

### Segurança
- O `SECURITY.md` passou a documentar como verificar a autenticidade dos
  binários de uma release sem depender do aviso do sistema operacional: hash
  SHA-256, assinatura RSA destacada do `CHECKSUMS.txt` conferida contra a chave
  pública agora versionada no repositório (`update-signing-public.pem`) e a
  prova de proveniência SLSA. Fica registrado também que os binários não têm
  certificado de assinatura de código pago e como prosseguir apesar do aviso do
  SmartScreen no Windows.
- O workflow de release passou a gerar uma assinatura GPG destacada do
  `CHECKSUMS.txt` (`CHECKSUMS.txt.asc`) quando há chave de assinatura
  configurada.
- Os metadados de autor e copyright dos executáveis e do instalador passaram a
  trazer o nome completo do responsável no lugar de um identificador curto.

## [2.2.3] - 2026-09-03

Patch de manutenção em cima da 2.2.2: uma varredura de segurança de todo o
código fechou nove pontos — a maioria dependente de pré-condições fortes
(atacante com escrita no banco compartilhado ou no diretório de dados, MITM
de TLS com certificado confiável), sem funcionalidade nova. Inclui também a
otimização de performance da lista, da rolagem e da partida.

### Melhorado
- Lista principal virtualizada e com caches: rolagem, filtro e abertura do
  cofre ficam mais rápidos em cofres grandes.

### Segurança
- O app instalado passa a ser instância única: abrir de novo enquanto já
  está rodando traz a janela existente para a frente (Windows) em vez de
  subir um segundo processo sobre o mesmo cofre — dois processos gravando
  `senhas.json.enc` ao mesmo tempo podiam perder alterações. Debug, testes
  e verify (`COFRE_BASE`) continuam podendo rodar cópias em paralelo.
- Ao abrir, o aplicativo avisa se o `senhas.json.enc` for uma cópia idêntica
  de um dos arquivos em `backups/` — sinal de que alguém restaurou um backup
  por cima do cofre por fora do app (rollback manual, que reverte senhas
  trocadas e itens removidos). Não é anti-rollback completo (um atacante com a
  própria cópia antiga do arquivo dribla), mas cobre o caso óbvio; o
  `THREAT_MODEL.md` explica o limite.
- O `biometria.dat` (envelope da chave do cofre para desbloqueio por Windows
  Hello) passa a ser protegido por DPAPI amarrado à conta do Windows, por cima
  da cifra que já existia. Copiar o arquivo para outra conta ou máquina não
  abre mais nada, nem antes de chegar na assinatura do Windows Hello. Um
  `biometria.dat` de uma versão anterior é migrado sozinho no próximo
  desbloqueio bem-sucedido.
- O log de erros (`logs/erros.log`) parou de gravar a mensagem de uma
  `JsonException`. Quando `senhas.json.enc`, um backup ou o `config.json`
  chegavam corrompidos, o erro de parsing acontecia depois da descriptografia
  e a mensagem podia ecoar um trecho do JSON já em texto puro (nome de
  serviço, usuário, URL, nota, host do banco). Agora registra só o caminho
  estrutural e a posição do erro, nunca o conteúdo.
- O bloqueio da tela de login depois de 5 senhas erradas seguidas passa a
  escalar: 5 s, depois 30 s, 2 min, 10 min, 30 min e, daí em diante, 1 h a
  cada nova rodada de 5 erros. Antes eram 5 s fixos e a contagem zerava a
  cada expiração, então dava para testar ~5 senhas a cada 5 s
  indefinidamente na própria tela de login. Um login correto zera a escala.
- No Windows, copiar uma senha, um TOTP, um usuário ou qualquer campo do cofre
  marca o conteúdo para não entrar no Histórico da Área de Transferência (Win+V)
  nem no Cloud Clipboard. Antes, a limpeza automática apagava só o conteúdo
  atual — o que já tinha ido para o histórico ou sincronizado para a nuvem
  continuava lá, dando uma falsa sensação de que a senha some depois dos
  segundos configurados.
- Conexão a um banco externo ganha a opção "Exigir assinatura de integridade
  nas linhas", ligada por padrão em conexões novas. Com ela ligada, uma linha
  do banco compartilhado sem HMAC não entra mais na mesclagem — fica de fora
  do cofre e aparece na tela de conflitos de sincronização. Antes, uma linha
  sem HMAC era tratada como confiável e mesclada mesmo assim (só registrava um
  aviso), então quem tivesse acesso de escrita ao banco podia forjar campos em
  texto puro — trocar a URL de uma credencial por um site de phishing, por
  exemplo — ou reverter uma senha para um valor antigo, bastando não gravar o
  HMAC. A opção fica desligada em "Restaurar de um banco" e pode ser desligada
  à mão para um banco compartilhado com dispositivos numa versão antiga do app.
- Os parâmetros de derivação de chave (memória, iterações e paralelismo do
  Argon2id; iterações do PBKDF2) lidos de fontes externas — arquivo de
  exportação `.gsenhas`, cabeçalho do `sincronizacao.dat` e a tabela de auth
  de um banco compartilhado — passam a ser validados contra tetos sãos antes
  de alimentar o KDF. Um arquivo ou linha de banco adulterado pedindo, por
  exemplo, alguns terabytes de memória travava ou derrubava o aplicativo por
  esgotamento de recursos ao importar, ao sincronizar ou ao restaurar de um
  banco; agora falha rápido com uma mensagem de arquivo inválido.
- O atualizador em um clique passa a exigir uma assinatura criptográfica destacada
  de `CHECKSUMS.txt`, conferida contra uma chave pública fixada no aplicativo, antes
  de executar o instalador, o portátil ou o AppImage baixado. Até agora a única
  barreira era o hash SHA256 de um `CHECKSUMS.txt` sem assinatura — um intermediário
  de TLS com certificado confiável, ou um comprometimento da release, trocaria o
  arquivo de hashes junto com o binário e o aplicativo instalaria código arbitrário
  sem aviso. Sem assinatura válida na release, a atualização automática é recusada e
  a página de lançamentos é aberta para download manual.

## [2.2.2] - 2026-08-27

Rodada de endurecimento em cima da 2.2.1: uma auditoria sistemática de todo o
inventário de funcionalidades, seguida de uma varredura de QA com o aplicativo
rodando de verdade. Dezenas de brechas de segurança, robustez e acessibilidade
fechadas, sem nenhuma funcionalidade nova — o foco é deixar o que já existe
mais difícil de quebrar.

### Adicionado
- "Atualizar agora" pede confirmação antes de baixar e instalar, mostrando a
  versão e as notas de mudança da release — antes o aplicativo se fechava
  sozinho para aplicar a atualização sem o usuário ver o que entraria.

### Segurança
- Usar o cofre no intervalo entre "trocar a senha mestra" e o reinício
  obrigatório podia corromper o cofre. A janela principal continuava vinculada
  à chave **antiga** até o restart, então qualquer gravação nesse meio-tempo
  (criar, editar, excluir, importar CSV, restaurar ou esvaziar a lixeira)
  regravava `senhas.json.enc` com a chave antiga por cima da versão já
  recifrada com a nova — deixando o cofre ilegível com a senha nova depois de
  reiniciar. A janela agora congela por completo assim que a troca é gravada
  em disco, até o processo reiniciar.
- O limite de tentativas da senha mestra (5, com bloqueio temporário) passa a
  persistir em disco e a sobreviver a reinício do aplicativo. O mesmo limite
  passa a valer no diálogo compartilhado de reautenticação (excluir cofre,
  limpar cofre, regerar QR code, ativar sincronização), que antes não tinha
  limite nenhum — dava para forçar a senha mestra à vontade numa sessão já
  desbloqueada.
- "Limpar cofre" passa a exigir reautenticação com a senha mestra, como
  "Excluir cofre" já exigia — antes bastava um clique de confirmação para
  mover o cofre inteiro para a lixeira.
- "Excluir cofre" passa a apagar também o histórico de pontuação de segurança,
  que sobrevivia em texto claro em `%APPDATA%` mesmo depois do cofre ter sido
  apagado por completo.
- O botão "Editar" de uma linha da lista ignorava o modo privacidade: um
  clique abria a janela de edição com usuário, URL, notas e campos extras em
  texto puro, driblando a máscara que a lista tinha acabado de aplicar.
- O arquivo temporário do instalador/AppImage baixado na atualização
  automática tinha nome previsível a partir só da tag da release — no Linux,
  onde `/tmp` é compartilhado entre usuários locais, isso abria uma corrida
  entre a checagem de hash e o uso do arquivo. Passa a baixar numa subpasta de
  nome aleatório.
- A verificação de vazamento (Have I Been Pwned) tratava uma resposta da API
  em formato inesperado como "senha não vazada" (fail-open) em vez de sinalizar
  que a verificação falhou.
- A união de códigos de recuperação na mesclagem de sincronização não
  reaplicava o teto de 100 (dois dispositivos gerando lotes independentes
  furavam o limite); histórico e etiquetas já tinham essa proteção.
- A leitura de `sincronizacao.dat` não distinguia falha real de I/O (pasta de
  nuvem ainda baixando, outro dispositivo escrevendo) de "chave errada" /
  "arquivo corrompido" — as duas viravam lista vazia, arriscando sobrescrever
  o remoto só com o que havia localmente.
- Conflitos de sincronização (em especial integridade violada — possível
  adulteração de um banco compartilhado) ficavam só numa lista em memória,
  perdida a cada reconexão ou reinício; agora vão também para o log de
  diagnóstico persistente.

### Corrigido
- A barra lateral de navegação inteira (os quatro modos e as cinco categorias)
  não tinha nome acessível: um leitor de tela lia "Avalonia.Controls.Grid" nos
  nove botões, sem forma de diferenciá-los.
- Trocar o idioma dentro da Lixeira trocava a lista pelo cofre inteiro,
  enquanto a barra de ferramentas e o menu continuavam na Lixeira.
- O botão de revelar senha do gerador nunca atualizava o próprio nome e a dica
  de acessibilidade: a senha nasce visível, mas o botão anunciava "Revelar
  senha" quando a ação seria ocultar.
- O nome acessível do botão "Excluir definitivamente" da lixeira usava o texto
  inteiro do diálogo de confirmação, com quebra de linha, em vez de um rótulo
  curto.
- O contador "N itens" do cabeçalho não acompanhava busca nem filtros —
  mostrava sempre o total do cofre.
- O anúncio de acessibilidade ao restaurar da lixeira dizia "copiado para a
  área de transferência".
- O campo de código de recuperação renderizava estreito demais na tela de
  edição, colado nos botões de ação.
- Cinco janelas de diálogo (confirmar senha mestra, alterar senha mestra,
  exportar/importar, conectar banco, editar credencial) nunca registravam um
  anunciador para leitor de tela — mensagens de erro inline eram descartadas
  em silêncio.
- Cinco mensagens de erro do atualizador automático estavam fixas em
  português, vazando para a interface de quem usa o aplicativo em outro idioma.
- `RemoverDefinitivamenteAsync` do banco de dados tinha voltado a virar tumba
  por engano, deixando o item preso na lixeira em vez de sumir de vez.
- Falsa detecção de duplicata na importação de CSV / `.gsenhas`: a chave de
  deduplicação concatenava nome do serviço + usuário numa única string, e duas
  credenciais diferentes podiam colidir nessa concatenação e uma ser
  descartada como duplicata sem nunca ter sido.
- O timer de sincronização automática só reaplicava um novo intervalo escolhido
  nas configurações quando o diálogo fechava.
- O indicador de força de senha não refletia a ausência de símbolo quando
  comprimento + maiúscula/minúscula + dígito já saturavam o nível — a mesma
  senha aparecia "Excelente" ali e "Fraca" no Relatório de Segurança.
- Categoria inválida vinda da interface podia gerar um índice fora do enum
  `Categoria` em vez de cair em "Outra".
- Campos extras (validade/CVV, host/porta etc.) eram perdidos ao trocar de
  tipo de credencial e voltar, se o tipo intermediário não os compartilhasse.
- O teto de 20 etiquetas por credencial estava sendo aplicado por engano ao
  total de etiquetas distintas do cofre inteiro, escondendo etiquetas do
  filtro sem aviso.
- Segredo TOTP sem limite de tamanho: texto colado por engano no lugar do
  segredo era normalizado e cifrado inteiro a cada tecla digitada.

### Desenvolvimento
- Rodar o projeto a partir do código-fonte (build de Debug) e a suíte de
  testes passam a usar uma pasta de dados isolada — `GerenciadorSenhas.dev`
  para o build de Debug, uma pasta temporária descartável para os testes — em
  vez de `%APPDATA%\GerenciadorSenhas`. Antes, isso podia sobrescrever o
  `config.json`, os logs e o histórico de pontuação do cofre de um aplicativo
  instalado na mesma máquina.
- O Windows Hello do cofre instalado deixa de ser afetado por qualquer
  execução que não seja o aplicativo instalado. A credencial do Windows Hello
  é global por conta do Windows, não por pasta, então o isolamento acima não a
  cobria sozinho; o serviço de biometria agora não toca na credencial do
  Windows a menos que seja o build de release instalado.

## [2.2.1] - 2026-08-20

Rodada extra de auditoria em cima da 2.2.0, com uma nova forma de restaurar o
cofre a partir de um banco de dados conectado e correções de segurança em
sincronização, banco de dados externo e troca de senha mestra.

### Adicionado
- Restaurar o cofre local a partir de um banco de dados conectado: ao abrir o
  app pela primeira vez num dispositivo novo, é possível digitar a senha
  mestra já em uso e trazer o cofre inteiro de um banco de dados externo já
  compartilhado, em vez de recriar as credenciais do zero.
- Executável standalone do gerador de senhas (`GeradorDeSenhas.exe`), com as
  mesmas regras de geração do cofre mas sem nenhuma delas — sem senha mestra,
  sem salvar nada. Disponível como executável portátil à parte na página de
  releases, para quem só quer gerar senhas.

### Segurança
- A tabela de autenticação publicada num banco de dados externo (necessária
  para viabilizar a restauração acima) carrega o mesmo salt e verificador da
  senha mestra que antes só existiam localmente — ver
  [THREAT_MODEL.md](THREAT_MODEL.md) para o que isso muda no modelo de
  ameaça.
- Trocar a senha mestra passa a recifrar e republicar o cofre inteiro no
  banco de dados conectado e na pasta de sincronização, não só localmente —
  antes disso, o próprio dispositivo que trocou a senha ficava sem
  conseguir sincronizar de volta com a chave antiga ainda esperada do outro
  lado, e "Restaurar de um banco de dados" num dispositivo novo ficava
  travado no salt/verificador antigos para sempre.
- Excluir definitivamente um item agora grava uma "tumba" também na
  sincronização por pasta (só existia para banco de dados) — sem isso, o
  próximo ciclo de sincronização podia trazer de volta um item que tinha
  acabado de ser apagado para sempre.
- Uma linha vinda do banco de dados com HMAC de integridade inválido
  (possível adulteração) deixa de ser sobrescrita automaticamente pela
  sincronização antes que o conflito apareça para alguém revisar.

### Corrigido
- Corrida entre conectar e desconectar de um banco de dados podia
  reconectar o cofre por cima de uma escolha mais recente do usuário.
- Uma falha temporária de rede na primeira sincronização com o banco
  travava o cofre inteiro (nem listar, nem editar funcionavam) até
  reiniciar o app.
- Sincronização automática em segundo plano podia sobrescrever
  silenciosamente uma edição em andamento no painel de detalhes; agora
  pede confirmação quando o item mudou por fora enquanto o painel estava
  aberto.
- Diversos pontos de reentrância por clique duplo/repetido (login,
  bloqueio do cofre, painel de detalhes, ativar/desativar sincronização)
  que podiam disparar a mesma ação mais de uma vez ou derrubar um ciclo de
  sincronização em andamento.
- Limpeza dos backups da troca de senha mestra deixou de depender de
  heurística e passou a usar um marcador de conclusão, evitando reverter
  por engano uma troca que já tinha dado certo.

## [2.2.0] - 2026-07-29

Leva adiante uma auditoria de código completa: bugs críticos, altos, médios
e baixos corrigidos, lacunas fechadas e um conjunto de ideias futuras do
roadmap implementadas — com peso forte em segurança, principalmente na
sincronização com banco de dados externo.

### Segurança
- Troca de senha mestra agora recifra o cofre inteiro: campos extras,
  códigos de recuperação e anexos, que antes ficavam cifrados com a chave
  antiga depois de trocar a senha mestra.
- Troca de senha mestra e salvamento local passam a gravar de forma
  atômica (arquivo temporário + substituição), eliminando o risco de
  arquivo corrompido por uma queda de energia no meio da escrita; se isso
  acontecer mesmo assim, o cofre se recupera sozinho no próximo login.
- Duas credenciais com o mesmo domínio e usuário não se sobrescrevem mais
  ao sincronizar com um banco de dados externo compartilhado por vários
  dispositivos.
- Modo privacidade (Ctrl+H) não permite mais reabrir o painel de detalhes
  com a senha em texto claro enquanto está ativo; também parou de vazar o
  serviço, o usuário e as etiquetas reais por leitor de tela e tooltip.
- Nova opção "Exigir certificado válido do servidor" na tela de conexão a
  banco de dados (desligada por padrão, para não quebrar bancos locais com
  certificado autoassinado); quando desligada, fica sinalizada no
  relatório de segurança do cofre.
- HMAC de integridade para os dados vindos de um banco de dados externo:
  uma linha adulterada por alguém com acesso de escrita direto ao banco
  (sem a chave mestra) é detectada e rejeitada na sincronização, em vez de
  aceita como "mais recente".
- Argon2id (o mesmo usado no cofre local) passou a ser usado também na
  exportação `.gsenhas` e na sincronização por pasta compartilhada, que
  antes ainda usavam PBKDF2-SHA256; arquivos já existentes continuam
  sendo lidos normalmente.
- Aviso ao salvar o QR code de backup da senha mestra dentro de uma pasta
  sincronizada com a nuvem (OneDrive, Dropbox, Google Drive, iCloud
  Drive) — o QR code contém a senha mestra em forma reconstruível.
- Cadeia de build: pacotes NuGet verificados contra vulnerabilidades
  conhecidas a cada push; SBOM (Software Bill of Materials) publicado em
  cada release; Dependabot mantendo as GitHub Actions fixadas por hash
  atualizadas automaticamente.

### Adicionado
- Ações em lote na lista de credenciais: selecionar várias (Ctrl+clique)
  para favoritar, adicionar uma etiqueta ou mover para a lixeira de uma
  vez só.
- Log de conflitos de sincronização: mostra o que foi atualizado por
  outro dispositivo ou rejeitado por falha de integridade na última
  sincronização com o banco.
- Tendência da pontuação de segurança do cofre ao longo do tempo, no
  relatório de segurança.
- Exportação seletiva: exportar só os itens filtrados na lista atual, em
  vez do cofre inteiro sempre.
- Etiquetas e histórico de senha passam a somar as mudanças dos dois
  lados ao sincronizar (com banco de dados ou por pasta), em vez de o
  lado mais recente substituir tudo.
- Atualização em um clique passou a funcionar no Linux também (via
  AppImage), não só no Windows.
- Importação de CSV/JSON não aborta mais o lote inteiro por causa de um
  campo inválido numa linha — conta só aquela linha como inválida e
  segue com o resto.
- Barra de progresso na exportação, como já existia na importação.
- Foco inicial em vários diálogos que abriam sem nada selecionado por
  teclado.

### Corrigido
- Mensagens de erro cruas de conexão a banco de dados e de sincronização
  por pasta agora aparecem traduzidas, sem detalhe técnico exposto.
- `GarantirColunasAsync` deixou de abrir uma conexão nova por coluna ao
  inicializar o schema do banco (18 conexões viravam 1), e passou a
  tolerar dois clientes inicializando o schema ao mesmo tempo.
- Remover um anexo que falha agora mostra um erro amigável em vez de
  travar a tela silenciosamente.
- `release.yml` só publica depois de build e testes passarem, com
  verificação de versão e de seção do `CHANGELOG.md` falhando rápido
  antes de gastar os builds do Windows e do Linux.

## [2.1.1] - 2026-07-23

Sem mudança de funcionalidade para quem usa o cofre — esta versão existe pra
que os próprios arquivos publicados (instalador, portátil, pacote Linux,
AppImage) já saiam da cadeia de build endurecida descrita abaixo, em vez de
só o código-fonte carregar a mudança.

### Segurança
- Cada arquivo publicado numa release (instalador, portátil, pacote Linux,
  AppImage e o próprio `CHECKSUMS.txt`) agora carrega uma *attestation* de
  proveniência (SLSA/Sigstore), verificável publicamente com
  `gh attestation verify` — prova assinada de que o arquivo saiu do workflow
  de release deste repositório a partir de um commit específico, mais forte
  que o hash SHA256 sozinho (que um comprometimento da release já publicada
  trocaria junto). README documenta como conferir.
- O pipeline de build passou a fixar por hash de commit (em vez de tag
  flutuante) todas as GitHub Actions de terceiro que usa, e o `appimagetool`
  baixado durante o build do Linux trocou uma tag "continuous" por uma
  versão fixa com hash conferido antes de rodar.

## [2.1.0] - 2026-07-22

Terceira geração do projeto: zona de risco pra apagar o cofre por completo,
mensagens de erro traduzidas em vez de texto técnico cru, nova identidade
visual "cofre, latão escovado" com tema escuro único e catálogo de ícones
unificado, e atualização em um clique — o cofre agora se atualiza sozinho a
partir de uma release publicada automaticamente a cada push na `prod`.

### Adicionado
- Busca de ícones reais dos serviços (favicons) agora é opcional e desligada por
  padrão. Ao ativá-la no menu de configurações, o aplicativo pede consentimento
  explícito e esclarece que apenas o domínio de cada serviço é enviado ao serviço
  de ícones do Google, sem senhas, usuários ou outros dados. Enquanto desativada,
  o cofre exibe somente as iniciais e não faz nenhuma requisição de rede.
- Limpeza automática da área de transferência: a senha copiada (pela lista,
  pelo painel de detalhes, pelo histórico de senhas anteriores ou pelo gerador)
  some da área de transferência depois de um tempo configurável no menu de
  configurações (15, 30 ou 60 segundos, ou desativado — 30 segundos por
  padrão), com aviso discreto de quando isso vai acontecer. Se outro conteúdo
  for copiado antes desse tempo, a limpeza automática não o sobrescreve.
- Backup automático local: tela própria de "Backup e restauração..." no menu
  de configurações, com frequência configurável (manual, diário ou semanal —
  semanal por padrão), quantidade máxima de backups mantidos (5, 10 ou 20),
  backup manual a qualquer momento e data do último backup exibida. Qualquer
  backup listado pode ser restaurado, sempre com aviso e confirmação antes de
  substituir o cofre atual; a restauração fica indisponível com um banco de
  dados conectado. Os backups permanecem sempre cifrados.
- Painel de detalhes: a categoria agora pode ser trocada diretamente por ali
  (antes só era possível pela edição completa), com suporte a categoria
  personalizada. Quando a credencial tem autenticação em duas etapas (2FA)
  configurada, o painel passa a mostrar o código atual com contagem
  regressiva e botão de copiar — antes isso ficava invisível fora da edição
  completa. Um link "Edição completa..." abre a tela completa (2FA e
  histórico de senhas) sem perder o lugar.
- Lixeira criptografada: excluir uma credencial agora move para uma lixeira
  interna (nova seção "Lixeira" na navegação) em vez de apagar na hora, com
  a data da exclusão exibida em cada item. Da lixeira dá para restaurar a
  credencial, excluí-la definitivamente ou esvaziar a lixeira inteira de uma
  vez, sempre com confirmação antes de qualquer exclusão definitiva. Os
  itens da lixeira continuam cifrados junto com o resto do cofre, tanto no
  cofre local quanto em um banco de dados conectado.
- Instalador para Windows (`CofreDeSenhas-Setup-X.Y.Z.exe`, via Inno Setup):
  não exige privilégios de administrador, cria atalho no menu iniciar
  (opcionalmente na área de trabalho) e registra a desinstalação em
  "Aplicativos e recursos". Ao desinstalar, o cofre em
  `%APPDATA%\GerenciadorSenhas` é preservado por padrão — apagá-lo exige
  confirmação explícita numa caixa de diálogo, com "manter" como opção
  padrão; desinstalações silenciosas nunca apagam o cofre. O executável
  autocontido sem instalador continua disponível como alternativa.
- Relatório de segurança do cofre: nova tela na barra de ferramentas com uma
  pontuação geral (0 a 100) do cofre, calculada a partir de senhas fracas,
  repetidas, antigas e comprometidas, além de contas sem verificação em duas
  etapas, sem URL ou sem categoria definida. Cada linha mostra quantos itens
  têm aquele problema e pode ser clicada para filtrar a lista principal por
  ele, com aviso do filtro ativo e opção de limpá-lo a qualquer momento. A
  verificação de vazamentos (Have I Been Pwned) é disparada pela própria
  tela e continua opcional; o restante do relatório é sempre calculado
  localmente.
- Códigos de recuperação por credencial: novo campo na tela de edição para
  colar um ou mais códigos de backup (um por linha). Cada código pode ser
  copiado individualmente, marcado como usado — sem apagar, só fica
  esmaecido na lista — ou removido, sempre com confirmação antes de
  remover. Ficam ocultos por padrão, como as senhas, com opção de revelar.
  Todos os códigos são cifrados individualmente e acompanham a exportação
  protegida e a importação/exportação com banco de dados conectado.
- Releases confiáveis e bem documentados: workflow de CI disparado por push
  na branch `prod`, que lê a versão direto de `App.csproj`, ignora pushes que
  não mudaram a versão (sem gerar release duplicada) e, para uma versão
  nova, publica o instalador e o executável portátil do Windows e o pacote
  do Linux, gera `CHECKSUMS.txt` com o hash SHA256 de cada arquivo e cria e
  publica a release no GitHub automaticamente — sem nenhum passo manual no
  GitHub — seguindo um modelo padronizado (destaques, changelog, capturas de
  tela, downloads por sistema operacional, instalação/atualização com aviso
  de backup e verificação dos hashes). README documenta como conferir os
  hashes; assinatura de código no Windows foi avaliada e fica registrada
  como item futuro do roadmap.
- Empacotamento para Linux: cada release agora inclui um AppImage
  (`CofreDeSenhas-X.Y.Z-x86_64.AppImage`), gerado por
  `App/distribuicao/gerar-appimage.sh` e publicado automaticamente pelo
  workflow de release. Roda em qualquer distribuição x86_64 sem exigir o
  SDK do .NET instalado. O script de instalação continua disponível para
  quem prefere compilar do código-fonte. Pacote `.deb` e Flatpak foram
  avaliados e adiados por exigirem manutenção de repositório próprio.
- Atalhos de teclado para as ações mais frequentes: `Ctrl+F` busca,
  `Ctrl+N` cria uma nova senha, `Ctrl+G` abre/fecha o gerador, `Ctrl+L`
  bloqueia o cofre na hora e `Ctrl+Shift+U`/`Ctrl+Shift+P` copiam o
  usuário/a senha da linha com foco de teclado (ou a primeira da lista,
  sem foco em nenhuma). O menu de configurações ganhou os itens
  "Bloquear agora" e, numa nova seção "Ajuda", "Atalhos de teclado...",
  que abre uma folha consultável com todas as combinações.
- Suíte de testes automatizados de interface (`App.Testes`, com
  Avalonia.Headless), cobrindo desbloqueio do cofre, criação e edição de
  credencial, cópia de senha/usuário com aviso de limpeza automática e o
  atalho `Ctrl+L` de bloqueio imediato. Roda junto com o restante da suíte
  no CI, em Windows e Linux.
- Modo privacidade: botão na barra de título e atalho `Ctrl+H` ocultam de
  uma vez o nome do serviço, o usuário, a categoria/etiquetas e o avatar de
  cada credencial na lista, substituindo tudo por marcadores neutros — para
  reduzir o que fica visível em ambientes compartilhados. Enquanto ativo,
  fecha o painel de detalhes, desabilita o botão de revelar senha da linha
  e bloqueia a renomeação inline do serviço. É um modo de sessão, sem
  preferência persistida: começa sempre desativado ao abrir o cofre.
- Histórico operacional da credencial: o painel de detalhes mostra data de
  criação, data da última edição e data da última cópia de senha, usuário
  e código TOTP (ou "nunca"), atualizadas na hora a cada cópia. Novo item
  "Registrar histórico de uso" no menu de configurações (ativado por
  padrão) desliga só o registro de novas datas de cópia, sem apagar as já
  salvas. Datas de criação e edição, que já existiam no cofre local,
  passam a ser migradas e persistidas também para bancos de dados
  conectados (SQLite, PostgreSQL, MySQL/MariaDB e SQL Server).
- Atualização em um clique: o item "Verificar atualizações" no menu de
  configurações vem ligado por padrão e consulta a release mais recente do
  GitHub a cada abertura do cofre. Havendo versão nova, a barra inferior
  mostra um botão "Atualizar agora" que baixa o instalador (ou o executável
  portátil, conforme como o cofre está rodando), confere o hash SHA256
  contra o `CHECKSUMS.txt` da própria release e, só se bater, aplica a
  atualização sozinho: instala em modo silencioso ou troca o executável
  portátil no lugar, fecha o cofre e reabre a versão nova automaticamente —
  sem instalador pra rodar na mão. Qualquer falha (sem internet, checksum
  não bate, arquivo não encontrado) ou fora do Windows cai de volta no
  comportamento antigo, abrindo a página de releases no navegador. A
  consulta e o download são leitura pública da API do GitHub, sem enviar
  nenhum dado além disso; dispensar o aviso lembra a versão dispensada para
  não repetir o mesmo aviso a cada abertura.
- Anexos criptografados: a tela de edição completa ganhou uma seção para
  prender pequenos arquivos a uma credencial (até 5 por credencial, 5 MB
  cada, 100 MB no total do cofre) — cada um cifrado com AES-256-GCM e
  gravado como arquivo próprio em `anexos/`, mantendo o cofre principal
  leve. Baixar descriptografa e salva onde o usuário escolher; excluir a
  credencial em definitivo limpa os anexos junto. Exportar/importar o
  cofre (`.gsenhas`) inclui os anexos. Ficam sempre só no dispositivo
  local, sem sincronizar para bancos de dados conectados.
- Organização avançada com etiquetas: etiquetas passam a valer para
  qualquer categoria (não só "Outro"), com múltiplas por credencial, via
  um novo campo "Etiquetas" sempre visível nas telas de criação, edição e
  detalhes. Novo filtro de etiquetas na barra de ferramentas combina com o
  filtro de categoria e com os já existentes (favoritos, auditoria).
  Cabeçalhos da lista (Serviço, Usuário, Categoria, Força) ficam clicáveis
  para ordenar por qualquer um deles, com seta indicando coluna e sentido
  ativos. Novo botão "Fixar no topo" em cada linha mantém credenciais
  importantes sempre nas primeiras posições, independente da ordenação.
- Templates de credenciais: novo campo "Tipo de credencial" na criação e na
  edição, com seis modelos (Login, Cartão, Chave de licença, Wi-Fi,
  Servidor, Banco de dados). Cada tipo renomeia os campos
  de usuário/senha para o vocabulário certo e, quando aplicável, mostra
  campos extras próprios (Cartão: validade, CVV, bandeira; Wi-Fi: segurança,
  banda; Servidor/Banco de dados: host, porta e mais alguns) — todos
  cifrados individualmente. O painel de detalhes usa os mesmos rótulos do
  tipo escolhido. Exportação e importação (`.gsenhas`) carregam tipo e
  campos extras. Local por dispositivo, como a categoria e os itens
  fixados, sem sincronizar para bancos de dados conectados.
- Sincronização criptografada de ponta a ponta: novo item
  "Sincronização..." no menu de configurações liga o cofre a outros
  dispositivos através de uma pasta compartilhada (Dropbox, OneDrive,
  Google Drive ou qualquer pasta sincronizada por outro meio) — o
  aplicativo só lê e escreve um arquivo cifrado ali dentro, sem falar
  diretamente com nenhum provedor. A chave de sincronização deriva da
  própria senha mestra (mesma senha em todos os dispositivos), e os dados
  saem cifrados com AES-256-GCM antes de qualquer gravação, então o
  provedor de nuvem nunca vê texto puro. Sincroniza ao desbloquear, em
  intervalo configurável (5 a 60 minutos) ou sob demanda; sem pasta
  acessível, o cofre continua funcionando 100% local. Conflitos usam a
  edição mais recente por credencial inteira, incluindo exclusão e
  restauração da lixeira. Anexos ainda não sincronizam nesta primeira
  versão.
- `SECURITY.md` com a política de divulgação responsável de
  vulnerabilidades: como reportar (aviso de segurança privado do GitHub,
  nunca issue pública), o que esperar de resposta e o escopo do projeto.
- `THREAT_MODEL.md` documentando o que o cofre protege, os perfis de
  atacante considerados e o que fica deliberadamente fora de escopo —
  incluindo, sem rodeios, que a chave mestra fica em memória comum do
  processo enquanto o cofre está aberto, sem proteção contra dump de
  memória ou swap/hibernação.
- Banco de dados externo ganhou colunas para URL, categoria, tipo de
  credencial e campos extras, histórico de senhas anteriores, favorito e
  fixado — campos que já sincronizavam pela pasta cifrada, mas ficavam de
  fora do banco. A conexão a banco de dados externo passa a ser apresentada
  como recurso self-hosted/compartilhado, não mais como sincronização
  pessoal.
- Banco de dados externo ganha identidade estável por credencial (coluna
  `guid_id`, preenchida automaticamente em tabelas antigas) e passa a usar
  o mesmo motor de mesclagem por "edição mais recente" já usado pela pasta
  de sincronização, no lugar da mesclagem "local sempre vence" de antes.
  Uma reconciliação de identidade legada, feita uma única vez por
  dispositivo, evita duplicar credenciais que já estavam pareadas pelo
  modelo antigo de nome de serviço + usuário.
- Zona de risco no menu de configurações: "Limpar cofre" move todas as
  credenciais para a lixeira (reversível) e "Excluir cofre" apaga em
  definitivo o cofre local, os anexos, a senha mestra e a credencial
  biométrica associada — sempre com reautenticação pela senha mestra antes
  de confirmar, e reinício automático do aplicativo ao final.
- Mensagens de erro amigáveis: os erros que apareciam com texto técnico fixo
  em português (senha, geração, exportação, importação CSV, troca de senha
  mestra, anexos, persistência local) agora são traduzidos para o idioma
  ativo antes de chegar à tela. O erro original continua registrado num log
  de diagnóstico rotativo em disco (limitado a 1 MB) para investigação, mas
  sem expor detalhes técnicos a quem usa o cofre. A falha ao descriptografar
  um campo extra agora avisa em vez de simplesmente desaparecer.
- Importação (JSON e CSV) passa a distinguir itens inválidos de duplicados,
  com contadores separados, e mostra uma barra de progresso visível em vez
  de travar a janela sem feedback. A importação de CSV também mostra uma
  lista prévia dos itens detectados antes de pedir confirmação.

### Corrigido
- Três bugs na escrita espelhada com banco de dados externo: a exclusão
  lógica de uma credencial era revertida para "não excluída" a cada nova
  gravação espelhada; as datas de atualização e de exclusão não eram
  gravadas no banco por esse caminho; e o `INSERT` desse caminho usava uma
  lista de colunas mais estreita do que os parâmetros realmente vinculados,
  então itens criados enquanto espelhados ficavam com data de criação e
  atualização nulas no banco.
- O painel de detalhes agora flutua sobre a lista de senhas em vez de
  espremê-la: antes, abrir os detalhes de uma credencial podia esconder por
  completo as colunas de força e de ações de todas as linhas, sem rolagem e
  sem aviso, em janelas no tamanho padrão.
- O botão "Salvar alterações" do painel de detalhes não corta mais o próprio
  texto.
- Contraste de cores revisado em pontos que ficavam abaixo do mínimo
  recomendado (WCAG AA): a estrela de favorito preenchida, a cor de força
  "média" e a cor de força "fraca" quando usada como texto pequeno
  (inclusive em mensagens de erro).
- O botão "Nova senha" abre a tela de cadastro de uma credencial, como
  esperado — antes abria por engano o gerador de senha avulso.
- O ícone de confirmação que aparece ao copiar uma senha (na lista e no
  gerador) agora realmente fica verde — a cor certa era calculada, mas
  nunca chegava a ser aplicada ao ícone.

### Segurança
- As iterações do PBKDF2-SHA256 usadas para derivar a chave a partir da senha
  mestra subiram de 100 mil para 600 mil (patamar recomendado pela OWASP).
  Cofres criados com a contagem antiga são migrados de forma transparente no
  próximo desbloqueio por senha mestra, com backup e rollback seguro, sem ação
  extra do usuário. Se o Windows Hello estiver habilitado, ele é desativado
  automaticamente nesse momento (o vínculo biométrico antigo perde a validade)
  e pode ser reativado logo em seguida.
- A derivação de chave da senha mestra migrou de PBKDF2-SHA256 para Argon2id
  (64 MiB de memória, 3 iterações, paralelismo 1), o padrão atual recomendado
  por resistir melhor a ataques por GPU/ASIC. Cofres ainda em PBKDF2-SHA256
  (de qualquer contagem de iterações) são migrados de forma transparente no
  próximo desbloqueio por senha mestra, com o mesmo mecanismo de backup e
  rollback seguro — inclusive a desativação e reoferta do Windows Hello,
  pelo mesmo motivo de antes.
- A chave mestra e sua cópia interna são apagadas da memória
  (`CryptographicOperations.ZeroMemory`) ao bloquear ou fechar o cofre.
- O painel de detalhes e as linhas reveladas da lista deixam de reter a senha
  em texto claro além do tempo necessário: fechar o painel ou bloquear o cofre
  limpa qualquer senha exibida.
- Bloquear o cofre — pelo atalho, pelo menu ou por inatividade — agora
  limpa a área de transferência na hora, em vez de depender apenas do
  temporizador de limpeza automática.

### Alterado
- Ícones baixados passam a ser guardados em cache no disco (por domínio), evitando
  novas consultas a cada sessão. Ao desativar a busca online, o cache é apagado.
- Identidade visual "cofre, latão escovado": o aplicativo deixou de ter tema
  claro (era selecionável, agora é escuro único) em favor de uma paleta de
  carvão quente com latão escovado como único destaque, tipografia serifada
  nos títulos, cantos mais quadrados e um pequeno rebite de latão decorativo
  no item de navegação ativo e no medidor de força.
- Catálogo de ícones unificado: todos os ícones da interface (barra lateral,
  barra de ferramentas, menu de configurações, campos, alternadores, rodapé,
  estado vazio e tela de desbloqueio) vêm de uma única biblioteca vetorial
  (traço, sem preenchimento salvo os estados ativos), com espessura de traço,
  tamanho e cor padronizados por seção em vez de valores soltos por tela.
- Telas repaginadas: tela de senha mestra unificada com o gerador embutido,
  diálogos com medidor de força, anel de progresso do TOTP e estados vazios
  ilustrados. Novo ícone do aplicativo em múltiplas resoluções.
- Contrastes de texto e distintivos de categoria revisados para atender ao
  nível AA, com realce de foco de teclado visível em todos os controles.
- Diálogos e caixas de mensagem agora escurecem a janela por trás ao abrir,
  reforçando que o restante do aplicativo está temporariamente bloqueado.
- O nome do serviço no painel de detalhes deixou de parecer um campo de
  formulário comum: agora é exibido como título, sem caixa nem borda visíveis
  em repouso, continuando editável com um clique.
- Botões de busca/filtro da barra de ferramentas agora ficam visualmente
  separados do botão "+ Nova senha" por um espaçamento maior e um divisor
  sutil, deixando clara a ação principal da tela.
- Menu de configurações reorganizado em seções rotuladas (Segurança,
  Aparência e Dados) em vez de uma lista única com mais de dez itens.
- Colunas da tabela do cofre redistribuem a largura proporcionalmente ao
  redimensionar a janela, em vez de manter larguras fixas.
- O zoom de acessibilidade (escala de fonte maior) trava o crescimento da
  janela no tamanho útil da tela, em vez de deixá-la crescer para fora dos
  limites visíveis.
- Diálogos ganharam botão de fechar com ícone, corpo rolável e largura maior.

## [2.0.0] - 2026-07-03

Segunda geração do projeto: o que era um gerador de senhas com cofre local
passa a reunir sincronização opcional com banco de dados, autenticação em duas
etapas, biometria, internacionalização, importação de outros gerenciadores,
acessibilidade e histórico por credencial — mantendo tudo local por padrão,
livre e auditável.

### Adicionado
- Histórico de alterações por credencial: a cada troca da senha de um item, a
  senha anterior é preservada de forma cifrada, junto da data da substituição.
  As últimas dez versões ficam disponíveis na tela de edição, com opções de
  revelar, copiar e reutilizar uma senha anterior. O histórico acompanha a
  exportação do cofre e é re-cifrado ao alterar a senha mestra.
- Recursos de acessibilidade: modos para daltonismo (protanopia, deuteranopia,
  tritanopia e monocromacia), alto contraste, escala de fonte, redução de
  animações e suporte aprimorado a leitores de tela, com anúncios de ações e
  rótulos de automação distribuídos pela interface.
- Importação a partir de arquivos CSV de outros gerenciadores, pelo menu de
  configurações. O delimitador (vírgula, ponto e vírgula ou tabulação) é
  detectado automaticamente e as colunas são reconhecidas pelo cabeçalho,
  cobrindo Bitwarden, LastPass, 1Password, Chrome/Edge, Firefox, KeePass,
  Dashlane, NordPass e CSVs genéricos. Segredos TOTP e favoritos são preservados
  quando presentes, entradas já existentes são ignoradas e o formato detectado é
  confirmado antes de importar.
- Bloqueio automático do cofre após período de inatividade: passado o tempo sem
  uso de mouse ou teclado, a janela é fechada, a chave é descartada da memória e
  o cofre volta à tela de senha mestra. O tempo é configurável no menu de
  configurações (desativado, 1, 5, 15 ou 30 minutos), com 5 minutos por padrão.
- Conexão a banco de dados externo pelo menu de configurações, com
  sincronização automática. Ao conectar, o cofre local e o banco são mesclados
  (em conflito de serviço+usuário, a senha do local prevalece) e passam a ser
  espelhados: toda criação, edição e exclusão é gravada nos dois.
- Suporte a SQLite, PostgreSQL, MySQL/MariaDB e SQL Server, com telas de seleção
  do motor e de dados de conexão, incluindo teste de conexão.
- Detecção da tabela `CofreDeSenhas` e criação sob confirmação, com as colunas
  id, usuario, senha, dominio, descricao, totp, etiquetas e excluido (exclusão
  lógica).
- Migração leve que adiciona as colunas `descricao`, `totp` e `etiquetas` a
  tabelas já existentes ao reconectar.
- Memória do último perfil de conexão para pré-preencher a tela de conexão.
- Geração de frases-senha (passphrases) a partir de lista de palavras, com
  controles de quantidade de palavras, separador, capitalização e número final.
- Gerador de senhas disponível na própria tela de senha mestra, utilizável sem
  autenticação: à esquerda o gerador e à direita o desbloqueio. Enquanto o cofre
  está trancado, o gerador apenas cria e copia senhas — a opção de salvar no
  cofre fica oculta e só aparece com o cofre aberto.
- Auditoria local do cofre, com detecção de senhas fracas, repetidas ou sem
  atualização há 365 dias ou mais e marcação visual das entradas afetadas.
- Banco visual de ícones por serviço, com associação por nome/alias e uso de
  favicons reais quando disponíveis, mantendo fallback local por iniciais.
- Edição inline do nome do serviço diretamente na lista do cofre.
- Cópia do usuário ao clicar na coluna correspondente, com confirmação visual
  temporária na própria linha.
- Colunas redimensionáveis na lista do cofre, incluindo serviço, usuário,
  categoria, data e ações.
- Testes de banco (criação da tabela, migração de colunas e CRUD com exclusão
  lógica) executados sobre SQLite.
- Categorias personalizadas a partir de `Outro`, com criação/edição, busca,
  filtro no mesmo seletor de categorias, exibição na lista e preservação em
  importação/exportação e banco de dados.
- Internacionalização da interface, com seletor de idioma persistido e suporte a
  português do Brasil, inglês, espanhol, francês, alemão e italiano.
- Desbloqueio por Windows Hello/biometria no Windows, ativável por dispositivo.
  A chave do cofre é cifrada (AES-256-GCM) com uma chave derivada de credencial
  do Windows Hello respaldada pelo TPM e a senha mestra permanece como fallback.

### Alterado
- Reorganização visual do cofre, com melhor distribuição entre gerador e lista,
  ações com ícones mais profissionais e largura padrão menor para a coluna de
  serviço, priorizando a exibição completa do usuário.
- Ajustes no gerador de senhas: melhor espaçamento, área de senha gerada
  expansível verticalmente, rolagem no painel esquerdo e senhas múltiplas
  exibidas no mesmo espaço da geração principal.
- Redução visual dos ícones de serviço e dos distintivos de categoria para
  melhorar a densidade e o alinhamento da tabela.
- Ícones de auditoria e verificação de vazamentos substituídos por símbolos mais
  legíveis e preenchidos.

### Corrigido
- Correção da interação dos sliders e seletores do gerador na tela de senha
  mestra, permitindo clicar, arrastar e abrir os controles normalmente sem estar
  logado no cofre.
- Ao alterar a senha mestra, o segredo TOTP de cada credencial passa a ser
  re-cifrado com a nova chave. Antes ele permanecia cifrado com a chave antiga e
  ficava inacessível após a troca. O histórico de senhas também é re-cifrado.

### Segurança
- A senha gravada no banco é sempre o texto cifrado (AES-256-GCM derivado da
  senha mestra), nunca a senha em claro. A senha do servidor de banco não é
  gravada em disco.
- QR code de backup passa a codificar a senha mestra como senha-frase, sem
  expor a senha original caractere a caractere em texto puro.
- Vocabulário do QR code de backup ampliado para reduzir repetições na
  representação por palavras.
- Atualização do binário nativo do SQLite para corrigir a vulnerabilidade
  GHSA-2m69-gcr7-jv3q.

## [1.0.0] - 2026-06-28

Primeira versão estável — transformação do gerador de senhas em um
gerenciador de senhas seguro e completo.

### Adicionado
- Gerador de senhas com comprimento ajustável (4 a 64), seleção de tipos de
  caractere, indicador de força e geração de múltiplas senhas simultâneas.
- Cofre criptografado local (AES-256-GCM) com cadastro, edição e remoção,
  categorias, favoritos, busca em tempo real e filtros.
- Senha mestra com chave derivada por PBKDF2-SHA256 (nunca armazenada); tela de
  criação e desbloqueio com limite de tentativas.
- Indicador de força por senha salva.
- Verificação de vazamentos via Have I Been Pwned (k-anonymity).
- Exportação e importação do cofre em arquivo portável `.gsenhas`, protegido por
  uma senha de exportação própria (AES-256-GCM e PBKDF2).
- Alteração da senha mestra pelo menu de configurações, com re-criptografia
  automática do cofre e backup com rollback.
- QR code de backup da senha mestra, oferecido na criação do cofre e a cada
  alteração da senha mestra.
- Tema claro e escuro com preferência persistida.
- Ícone próprio no executável, na janela e na bandeja do sistema, com opção de
  minimizar para a bandeja.
- Interface minimalista e responsiva (janela sem moldura, cantos arredondados,
  redimensionável).
- Suíte de testes (xUnit): unitários, integração de ponta a ponta, segurança
  (adulteração e chave incorreta) e desempenho (mais de 1000 senhas).

### Segurança
- AES-256-GCM para confidencialidade e integridade dos dados.
- PBKDF2-SHA256 (100k iterações) para a senha mestra; verificador one-way em `auth.dat`.
- Comparações em tempo constante; arquivos sensíveis isolados em `%APPDATA%`.

[Não lançado]: https://github.com/dcCarreto/CofreDeSenhas/compare/v2.2.1...HEAD
[2.2.1]: https://github.com/dcCarreto/CofreDeSenhas/compare/v2.2.0...v2.2.1
[2.2.0]: https://github.com/dcCarreto/CofreDeSenhas/compare/v2.1.1...v2.2.0
[2.1.1]: https://github.com/dcCarreto/CofreDeSenhas/compare/v2.1.0...v2.1.1
[2.1.0]: https://github.com/dcCarreto/CofreDeSenhas/compare/v2.0.0...v2.1.0
[2.0.0]: https://github.com/dcCarreto/CofreDeSenhas/compare/v1.0.0...v2.0.0
[1.0.0]: https://github.com/dcCarreto/CofreDeSenhas/releases/tag/v1.0.0
