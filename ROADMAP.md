# Roadmap

Este documento registra o que já foi concluído no Cofre de Senhas e o que está
planejado para versões futuras. Ele não é um compromisso de datas, e sim uma
direção. Sugestões são bem-vindas pelas issues do projeto.

## Concluído

A versão 1.0.0 entrega o conjunto completo de funcionalidades essenciais de um
gerenciador de senhas seguro:

- Gerador de senhas configurável (comprimento, tipos de caractere, quantidade),
  baseado em gerador de números aleatórios criptográfico.
- Indicador de força de senha em tempo real.
- Cofre local criptografado com AES-256-GCM.
- Senha mestra com derivação de chave por PBKDF2-SHA256 e verificador one-way; a
  chave nunca é gravada em disco.
- Limite de tentativas de desbloqueio com bloqueio temporário.
- Cadastro, edição e remoção de credenciais, com categorias, favoritos, notas e
  URL.
- Busca em tempo real e filtro por categoria.
- Verificação de senhas comprometidas via Have I Been Pwned (k-anonymity).
- Exportação e importação do cofre em arquivo portável `.gsenhas`, protegido por
  senha de exportação independente.
- Alteração da senha mestra com re-criptografia automática do cofre e rollback em
  caso de falha.
- QR code de backup da senha mestra, na criação e a cada alteração.
- Tema claro e escuro, com preferência persistida.
- Ícone próprio no executável, na janela e na bandeja do sistema, com opção de
  minimizar para a bandeja.
- Suíte de testes automatizados (unitários, integração, segurança e desempenho).
- Interface única multiplataforma em Avalonia, rodando em Windows e Linux a
  partir da mesma base de código.
- Distribuição como executável único e autocontido para Windows, e script de
  instalação para Linux (atalho no menu de aplicativos e ícone, por usuário).

### Versão 2.0.0

- Conexão opcional a banco de dados externo (SQLite, PostgreSQL, MySQL/MariaDB e
  SQL Server) com sincronização automática: ao conectar, o cofre local e o banco
  são mesclados (o local prevalece em conflito) e passam a ser espelhados — cada
  criação, edição e exclusão vai para os dois. Inclui detecção e criação da
  tabela sob confirmação, migração leve de colunas e a senha sempre armazenada
  de forma cifrada.
- Bloqueio automático do cofre após período de inatividade: passado o tempo
  configurado sem uso, o cofre é fechado e volta à tela de senha mestra. O tempo
  (desativado, 1, 5, 15 ou 30 minutos) é escolhido no menu de configurações e
  fica em 5 minutos por padrão.
- Geração de frases-senha (passphrases) a partir de listas de palavras, com
  quantidade de palavras, separador, capitalização e número final configuráveis.
- Gerador de senhas disponível também na tela de senha mestra, antes do
  desbloqueio: à esquerda o gerador e à direita o login. Sem autenticação, o
  gerador apenas cria e copia senhas; a opção de salvar no cofre só aparece com o
  cofre aberto.
- Auditoria do cofre: detecção local de senhas fracas, repetidas ou sem
  atualização há 365 dias ou mais, com marcação visual dos itens afetados.
- Refinamento do gerador: correção da interação de sliders e seletores na tela
  de senha mestra, melhor espaçamento, área de senha gerada expansível e rolagem
  no painel esquerdo.
- Melhorias na lista do cofre: colunas redimensionáveis, largura inicial
  otimizada para exibir melhor o usuário, edição inline do serviço e cópia do
  usuário por clique com feedback visual.
- Banco visual de ícones por serviço, usando favicons reais quando disponíveis e
  fallback local quando necessário.
- QR code de backup representando a senha mestra como senha-frase, com
  vocabulário ampliado para reduzir repetição de palavras.
- Polimento visual dos ícones de auditoria, verificação de vazamentos, ações da
  lista e distintivos de categoria.
- Suporte a códigos TOTP (autenticação em duas etapas) por entrada: cada
  credencial pode guardar uma chave 2FA, colada como segredo Base32 ou link
  `otpauth://`. O código de seis dígitos é calculado localmente (RFC 6238), com
  prévia ao vivo e contagem regressiva na criação e na edição, e cópia rápida
  pela lista. O segredo é cifrado como a senha e acompanha a exportação e o
  banco de dados.
- Categorias personalizadas além das categorias fixas: ao selecionar `Outro`, a
  criação/edição libera um campo para nomear a categoria, que passa a aparecer no
  mesmo seletor de categorias e é preservada na importação/exportação e no banco
  de dados.
- Importação a partir de outros gerenciadores e de arquivos CSV: pelo menu de
  configurações, um arquivo CSV é lido com detecção automática de delimitador
  (vírgula, ponto e vírgula ou tabulação) e mapeamento das colunas pelo
  cabeçalho, reconhecendo os formatos de Bitwarden, LastPass, 1Password,
  Chrome/Edge, Firefox, KeePass, Dashlane e NordPass, além de CSVs genéricos.
  Preserva segredos TOTP e favoritos, ignora entradas já existentes e confirma o
  formato detectado antes de importar.
- Internacionalização da interface com seleção persistida de idioma e suporte a
  português do Brasil, inglês, espanhol, francês, alemão e italiano.
- Desbloqueio por Windows Hello/biometria no Windows, com registro por
  dispositivo respaldado por credencial do Windows Hello (TPM) e fallback pela
  senha mestra.
- Recursos de acessibilidade: modos para daltonismo, alto contraste, escala de
  fonte, redução de animações e suporte aprimorado a leitores de tela.
- Histórico de alterações por credencial: a senha anterior de cada item é
  guardada de forma cifrada a cada troca, com data da substituição. As últimas
  versões podem ser reveladas, copiadas e reutilizadas na tela de edição,
  acompanham a exportação e são re-cifradas ao alterar a senha mestra.

### Identidade visual

Repaginação completa da interface, preservando todas as funcionalidades:

- Paleta e tokens de cor revisados nos temas claro e escuro, com contrastes de
  texto e distintivos ajustados para o nível AA.
- Tipografia Plus Jakarta Sans (com Inter de reserva), escala tipográfica e
  raios/sombras padronizados.
- Catálogo unificado de ícones de traço e componentes (botões, campos,
  seletores, alternadores, controle deslizante e dicas) com estados consistentes.
- Telas repaginadas: tela de senha mestra unificada com o gerador embutido,
  diálogos com medidor de força, anel de progresso do TOTP e estados vazios
  ilustrados.
- Novo ícone do aplicativo em múltiplas resoluções e realce de foco de teclado
  visível em todos os controles.
- Refinamento posterior: micro-transições em botões, campos e painéis
  (respeitando a preferência de reduzir animações), iconografia 100% vetorial no
  lugar de glifos de fonte, correção dos ícones do painel de detalhes e
  tradução das últimas cadeias fixas da janela principal nos seis idiomas.

### Privacidade

- Privacidade dos ícones de serviço: a busca de favicons reais passou a ser
  opcional e desligada por padrão. Ao ativá-la no menu de configurações, o
  aplicativo pede consentimento explícito e esclarece que apenas o domínio de
  cada serviço é enviado ao serviço de ícones do Google. Enquanto desativada, o
  cofre exibe somente as iniciais e não faz nenhuma requisição de rede. Os ícones
  baixados ficam em cache no disco por domínio, e o cache é apagado ao desativar
  o recurso. O fallback local por iniciais permanece como padrão.

### Segurança

- Fortalecimento da derivação de chave e higiene de memória: as iterações do
  PBKDF2-SHA256 subiram de 100 mil para 600 mil (patamar recomendado pela
  OWASP). Cofres criados com a contagem antiga são migrados de forma
  transparente no próximo desbloqueio por senha mestra — sem ação do usuário
  além de digitar a senha — reaproveitando a re-criptografia com backup e
  rollback já usada na troca de senha mestra. Se o Windows Hello estiver
  habilitado, ele é desativado nesse momento (o vínculo biométrico guarda a
  chave antiga) e pode ser reativado em seguida. A chave mestra e sua cópia
  interna são zeradas da memória (`CryptographicOperations.ZeroMemory`) ao
  bloquear ou fechar o cofre, e o painel de detalhes e as linhas reveladas da
  lista deixam de reter a senha em texto claro além do necessário.
- Migração da derivação de chave de PBKDF2-SHA256 para Argon2id (64 MiB de
  memória, 3 iterações, paralelismo 1), o padrão atual recomendado por
  resistir melhor a ataques por GPU/ASIC — o PBKDF2, por mais que se suba a
  contagem de iterações, continua sendo barato demais de paralelizar em
  hardware dedicado. Cofres ainda em PBKDF2-SHA256 (de qualquer contagem de
  iterações) são migrados de forma transparente no próximo desbloqueio por
  senha mestra, reaproveitando o mesmo mecanismo de re-criptografia com
  backup e rollback já usado na troca de senha mestra e na migração anterior
  — incluindo a desativação e reoferta do Windows Hello, pelo mesmo motivo
  de o vínculo biométrico guardar a chave antiga.

### Versão 2.1.0

Terceira geração do projeto: zona de risco pra apagar o cofre por completo,
mensagens de erro amigáveis em vez de texto técnico cru, nova identidade
visual "cofre, latão escovado" com tema escuro único, relatório de segurança
com pontuação geral do cofre e atualização em um clique.

#### Identidade visual "cofre, latão escovado"

- Tema único escuro: a alternância entre tema claro e escuro (entregue na
  repaginação anterior, ver "Identidade visual" acima) dá lugar a um tema
  único, escuro por opção de design — mais consistente com a metáfora de
  "cofre" que o projeto persegue, e uma superfície visual só pra manter.
- Nova paleta "carvão quente" com latão escovado como único destaque de cor,
  tipografia serifada nos títulos (fonte de sistema, sem custo de
  empacotamento), cantos mais quadrados e um pequeno rebite de latão
  decorativo no item de navegação ativo e no medidor de força.
- Catálogo de ícones consolidado numa única biblioteca vetorial de traço
  (barra lateral, barra de ferramentas, menu de configurações, campos,
  alternadores, rodapé, estado vazio e tela de desbloqueio), com espessura,
  tamanho e cor padronizados por seção em vez de valores soltos por tela.
- Diálogos e caixas de mensagem passam a escurecer a janela por trás ao
  abrir, ganham botão de fechar com ícone, corpo rolável e largura maior; o
  zoom de acessibilidade passa a travar o crescimento da janela no tamanho
  útil da tela, em vez de deixá-la crescer para fora dos limites visíveis.
- Menu de configurações reorganizado em seções rotuladas (Segurança,
  Aparência e Dados) em vez de uma lista única com mais de dez itens; nome
  do serviço no painel de detalhes deixa de parecer campo de formulário
  comum; colunas da tabela do cofre redistribuem a largura proporcionalmente
  ao redimensionar a janela.

#### Backup e recuperação de dados

- Backup automático local: pela tela "Backup e restauração..." no menu de
  configurações, é possível escolher a frequência do backup automático
  (manual apenas, diário ou semanal — semanal por padrão) e a quantidade
  máxima de backups mantidos (5, 10 ou 20). O backup é verificado a cada
  desbloqueio do cofre e, se estiver na hora, roda sozinho; também pode ser
  disparado manualmente a qualquer momento. A tela mostra a data do último
  backup e a lista dos backups disponíveis, com a opção de restaurar
  qualquer um deles — sempre com aviso e confirmação antes de sobrescrever o
  cofre atual. Restauração fica indisponível enquanto o cofre estiver
  conectado a um banco de dados externo, para não gerar conflito com a
  sincronização. Os backups continuam sempre cifrados, do mesmo jeito que o
  cofre local.

- Lixeira criptografada: excluir uma credencial agora move para uma lixeira
  interna em vez de apagar na hora, com a data da exclusão exibida. Da
  lixeira dá para restaurar a credencial, excluí-la definitivamente ou
  esvaziar a lixeira inteira de uma vez — sempre com confirmação antes de
  qualquer exclusão definitiva. Os itens da lixeira continuam cifrados junto
  com o resto do cofre, e o mesmo comportamento vale tanto para o cofre local
  quanto para um banco de dados conectado.

- Códigos de recuperação por credencial: campo próprio na tela de edição
  para colar um ou mais códigos de backup (um por linha), guardados
  individualmente cifrados. Cada código pode ser copiado, marcado como
  usado (fica esmaecido na lista, sem ser apagado) ou removido, com
  confirmação antes da remoção. Ficam ocultos por padrão, com opção de
  revelar. Acompanham a exportação protegida e a importação/exportação do
  banco de dados, sempre cifrados.

#### Segurança e privacidade

- Limpeza automática da área de transferência: ao copiar a senha de uma
  credencial (pela lista, pelo painel de detalhes, pelo histórico de senhas
  anteriores ou pelo gerador), a área de transferência é apagada
  automaticamente depois de um tempo configurável (15, 30 ou 60 segundos, ou
  desativado — 30 segundos por padrão) no menu de configurações. Um aviso
  discreto (dica do botão, leitor de tela e, no gerador, a própria mensagem de
  sucesso) informa quando a senha copiada será removida. Se o usuário copiar
  outro conteúdo antes do tempo configurado, a limpeza automática não
  sobrescreve o que foi copiado por último.

- Relatório de segurança do cofre: nova tela, acessível pela barra de
  ferramentas do cofre, com uma pontuação geral (0 a 100) calculada a partir
  de senhas fracas, repetidas, antigas e comprometidas, além de contas sem
  verificação em duas etapas, sem URL ou sem categoria definida. Cada linha
  mostra a quantidade de itens afetados e pode ser clicada para filtrar a
  lista principal por aquele problema, com um aviso indicando o filtro ativo
  e a opção de limpá-lo. A verificação de vazamentos (Have I Been Pwned)
  continua opcional e é disparada pela própria tela; todo o resto do
  relatório é calculado localmente, sem enviar dados para fora.

- Modo privacidade: novo botão na barra de título (ao lado do de tema) e
  atalho `Ctrl+H` que, com um clique, ocultam nome do serviço, usuário,
  categoria/etiquetas e o avatar de cada credencial na lista, trocando tudo
  por marcadores neutros — pensado para reduzir o que aparece na tela em
  ambientes compartilhados. Enquanto ativo, o painel de detalhes é fechado
  automaticamente, o botão de revelar senha de cada linha fica desabilitado
  e renomear o serviço direto na lista fica bloqueado, para não vazar dado
  nenhum por um desses caminhos. É um modo de sessão: começa sempre
  desativado a cada abertura do cofre, sem gravar preferência em disco. Ao
  bloquear o cofre — pelo atalho, pelo menu ou por inatividade — a área de
  transferência agora é sempre limpa no mesmo instante, não só depois do
  temporizador de limpeza automática.

- Zona de risco no menu de configurações: "Limpar cofre" move todas as
  credenciais para a lixeira (reversível) e "Excluir cofre" apaga em
  definitivo o cofre local, os anexos, a senha mestra e a credencial
  biométrica associada — sempre com reautenticação pela senha mestra antes
  de confirmar, e reinício automático do aplicativo ao final.

- Mensagens de erro amigáveis: os erros que apareciam com texto técnico fixo
  (senha, geração, exportação, importação CSV, troca de senha mestra,
  anexos, persistência local) passam a ser traduzidos para o idioma ativo
  antes de chegar à tela. O erro original continua registrado num log de
  diagnóstico rotativo em disco (limitado a 1 MB) para investigação, sem
  expor detalhes técnicos a quem usa o cofre.

- `SECURITY.md` com a política de divulgação responsável de
  vulnerabilidades: como reportar (aviso de segurança privado do GitHub,
  nunca issue pública), o que esperar de resposta e o escopo do projeto.

- `THREAT_MODEL.md` documentando o modelo de ameaça do cofre: o que é
  protegido, os perfis de atacante considerados (acesso ao arquivo sem a
  senha mestra, acesso à memória do processo com o cofre aberto, atacante
  de rede, pasta de sincronização ou banco de dados comprometidos) e o que
  fica deliberadamente fora de escopo (SO comprometido, perda da senha
  mestra sem recuperação possível, engenharia social).

#### Organização do cofre

- Histórico operacional da credencial: o painel de detalhes passa a mostrar
  quando cada credencial foi criada e editada pela última vez, e quando a
  senha, o usuário e o código TOTP foram copiados pela última vez (ou
  "nunca", se ainda não aconteceu) — atualizado na hora a cada cópia, tanto
  pela lista quanto pelo próprio painel. Novo item "Registrar histórico de
  uso" no menu de configurações (ativado por padrão) permite desligar só o
  registro das datas de cópia; desligado, as datas já salvas continuam
  visíveis, só param de avançar. As datas de criação e edição sempre foram
  gravadas no cofre local mas nunca chegavam a um banco de dados conectado
  — lacuna fechada nesta rodada, com as cinco datas migradas
  automaticamente para os quatro motores suportados. Já a mirror-sync
  específica de "gravar por chave" (usada ao reconciliar local com banco
  logo após conectar) ficou de fora: sincroniza a credencial em si, mas as
  datas de cópia permanecem uma informação só local, específica do
  dispositivo, sem tentar reconciliar entre local e banco a cada cópia.

- Anexos criptografados: a tela de edição completa ganhou uma seção
  "Anexos" para prender pequenos arquivos a uma credencial (até 5 por
  credencial, 5 MB cada, 100 MB no total do cofre) — pensada para QR code
  de 2FA, chave de recuperação, PDF de backup ou qualquer documento/imagem
  relacionado. Cada anexo é cifrado com AES-256-GCM (a mesma chave mestra)
  e gravado como um arquivo próprio em `anexos/` dentro da pasta do cofre
  — só a lista de nome/tamanho fica junto com a credencial, então o
  arquivo principal do cofre continua leve mesmo com anexos grandes. Baixar
  descriptografa e salva onde o usuário escolher; remover apaga o arquivo
  cifrado do disco, e excluir a credencial em definitivo (ou esvaziar a
  lixeira) limpa os anexos dela junto. Exportar/importar o cofre
  (`.gsenhas`) inclui os anexos, cifrados dentro do mesmo envelope
  protegido pela senha de exportação. Ficam sempre só no dispositivo local
  que os criou — não sincronizam para bancos de dados conectados (mesma
  decisão já tomada para as datas de última cópia). Limitação conhecida:
  no caso raro de conexão direta a um banco sem espelho local, os
  metadados de anexo (nome/tamanho) podem não sobreviver a um recarregamento,
  já que não existe hoje uma coluna dedicada no banco para essa lista —
  os arquivos cifrados em si não são apagados, só a referência pode ficar
  órfã nesse cenário específico.

- Organização avançada com etiquetas: etiquetas deixam de ser exclusivas da
  categoria "Outro" — qualquer credencial, em qualquer categoria, pode
  levar uma ou mais etiquetas, digitadas separadas por vírgula no campo
  "Etiquetas" que agora aparece sempre nas telas de criação, edição e no
  painel de detalhes. A barra de ferramentas ganhou um segundo filtro,
  dedicado a etiquetas, ao lado do filtro de categoria — os dois combinam
  entre si e com os filtros de favoritos e de auditoria já existentes
  (ex.: categoria "Trabalho" + etiqueta "urgente" ao mesmo tempo). Os
  cabeçalhos da lista (Serviço, Usuário, Categoria, Força) ficaram
  clicáveis para ordenar por qualquer um deles, com uma seta indicando a
  coluna e o sentido atual; um novo botão "Fixar no topo" em cada linha
  mantém credenciais importantes sempre nas primeiras posições,
  independente da ordenação escolhida. Em "Outro", o comportamento
  existente foi preservado: a primeira etiqueta digitada continua servindo
  como nome da categoria personalizada exibido na lista. Fixação é local
  por dispositivo, como os favoritos, sem sincronizar para bancos de dados
  conectados.

- Templates de credenciais: a criação e a edição ganharam um campo "Tipo de
  credencial" com seis modelos — Login, Cartão, Chave de licença, Wi-Fi,
  Servidor e Banco de dados. Escolher um tipo diferente de Login renomeia os
  campos principais para o vocabulário certo (em Cartão, "Usuário"/"Senha"
  viram "Titular do cartão"/"Número do cartão"; em Wi-Fi, "Nome da rede
  (SSID)"/"Senha da rede"; em Chave de licença, "Produto/Software"/"Chave de
  licença") e mostra campos extras específicos do tipo (Cartão: validade,
  CVV, bandeira; Wi-Fi: segurança, banda; Servidor: host, porta, protocolo;
  Banco de dados: host, porta, nome do banco, motor) — sem extras,
  Login/Chave de licença usam só os campos renomeados. Cada campo extra é
  cifrado individualmente com a mesma chave
  mestra, como o TOTP. O painel de detalhes também usa os rótulos do tipo
  escolhido; a edição completa dos campos extras continua exclusiva das
  telas de criação/edição. Exportação e importação (`.gsenhas`) carregam
  tipo e campos extras normalmente. Assim como a categoria (que nunca teve
  coluna própria no banco, desde a v2.0.0) e os itens fixados, tipo e
  campos extras são hoje só locais: não sincronizam para bancos de dados
  conectados.

- Importação mais transparente: a importação (JSON e CSV) passa a
  distinguir itens inválidos de duplicados, com contadores separados, e
  mostra uma barra de progresso visível em vez de travar a janela sem
  feedback. A importação de CSV também mostra uma lista prévia dos itens
  detectados antes de pedir confirmação.

#### Sincronização e banco de dados

- Sincronização criptografada de ponta a ponta: novo item "Sincronização..."
  no menu de configurações permite ligar o cofre a outros dispositivos
  através de uma pasta compartilhada — normalmente a pasta local de um
  cliente de nuvem já instalado (Dropbox, OneDrive, Google Drive, iCloud
  Drive) ou qualquer pasta sincronizada por outro meio. O aplicativo nunca
  fala diretamente com nenhum provedor: só lê e escreve um arquivo cifrado
  dentro da pasta escolhida, deixando o transporte real por conta do
  cliente de sincronização que o usuário já usa — por isso qualquer
  provedor funciona, sem integração dedicada. A chave de criptografia é
  derivada da própria senha mestra do cofre (a mesma senha mestra precisa
  ser usada em todos os dispositivos que compartilham a pasta); os dados
  saem cifrados com AES-256-GCM antes de qualquer gravação em disco, então
  o provedor de nuvem nunca vê texto puro. A sincronização roda uma vez ao
  desbloquear o cofre, em intervalo configurável (5, 15, 30 ou 60 minutos)
  e sob demanda pelo botão "Sincronizar agora"; falhas (pasta inacessível,
  sem rede) são silenciosas e o cofre continua funcionando 100% local,
  como se a sincronização estivesse desligada. Conflitos são resolvidos
  por credencial inteira, não campo a campo: quando o mesmo item muda em
  dois dispositivos, vale a edição mais recente — diferente da mesclagem
  "local sempre vence" já usada na conexão a bancos de dados, que faz
  sentido para um banco compartilhado com um dispositivo principal, mas
  não para dispositivos simétricos sem prioridade entre si. Exclusão e
  restauração da lixeira também sincronizam. Fora desta primeira versão:
  anexos (ficam só no dispositivo que os criou, mesma decisão já tomada
  para a lista de anexos em si) e sincronização em tempo real (é sempre
  por verificação periódica ou manual, nunca imediata).

- Fechada a lacuna de campos entre cofre local, pasta de sincronização e
  banco de dados externo: o banco ganhou colunas para URL, categoria, tipo
  de credencial e campos extras, histórico de senhas anteriores, favorito e
  fixado — os mesmos campos que já sincronizavam pela pasta cifrada, mas
  ficavam de fora do banco. De quebra, corrigidos três bugs existentes na
  escrita espelhada: exclusão lógica sendo revertida para "não excluído" a
  cada gravação espelhada, e as datas de atualização/exclusão não sendo
  gravadas no banco por esse mesmo caminho. A conexão a banco de dados
  externo passa a ser apresentada como recurso self-hosted/compartilhado,
  não mais como sincronização pessoal — esse papel é da sincronização por
  pasta cifrada.

- Consolidação da sincronização: o banco de dados externo ganha identidade
  estável por credencial (coluna `guid_id`, preenchida automaticamente em
  tabelas antigas que não a tinham) e passa a usar o mesmo motor de
  mesclagem por "edição mais recente" já usado pela pasta cifrada — em vez
  da mesclagem "local sempre vence" de antes, que fazia sentido só para um
  banco com um dispositivo principal, não para dispositivos simétricos. Uma
  reconciliação de identidade legada, feita uma única vez por dispositivo
  na primeira sincronização após esta atualização, evita duplicar
  credenciais que já estavam pareadas pelo modelo antigo (nome de serviço +
  usuário).

#### Distribuição e atualizações

- Instalador para Windows: instalador `CofreDeSenhas-Setup-X.Y.Z.exe` (via
  Inno Setup), sem exigir privilégios de administrador, com atalho no menu
  iniciar, ícone correto e entrada em "Aplicativos e recursos" para
  desinstalar. Ao desinstalar, o cofre em `%APPDATA%\GerenciadorSenhas` é
  preservado por padrão; apagá-lo exige confirmação explícita, com "manter"
  como opção padrão.

- Releases confiáveis e bem documentados: workflow de CI
  (`.github/workflows/release.yml`) disparado por push na branch `prod`. Ele lê
  a versão direto de `App/App.csproj`, confere se aquela versão já foi lançada
  (procura a tag `vX.Y.Z` correspondente) e, se for realmente nova, gera o
  instalador e o executável portátil do Windows e o pacote do Linux, calcula
  o hash SHA256 de cada arquivo (`CHECKSUMS.txt`) e publica a release no
  GitHub automaticamente (a tag é criada pelo próprio workflow), com um
  modelo padronizado (`.github/RELEASE_TEMPLATE.md`) cobrindo destaques,
  changelog, capturas de tela, downloads por sistema operacional, instruções
  de instalação e atualização (com aviso de backup antes de atualizar) e
  verificação dos hashes. Não sobrou passo manual nenhum: subir a versão em
  `App.csproj` e dar push/merge na `prod` já é suficiente pra ficar
  disponível para todo mundo, inclusive para quem usa o botão "Atualizar
  agora" (abaixo); push na `prod` sem mudar a versão não gera release
  duplicada. O README ganhou instruções de verificação de integridade.
  Assinatura de código no Windows foi avaliada e documentada como item
  futuro, condicionada à obtenção de um certificado.
- Portão de qualidade antes de lançar: o workflow de release ganhou um job
  "testar" (compila a solução inteira e roda toda a suíte de testes) que
  precisa passar antes de qualquer artefato do Windows ou Linux ser gerado
  — e, por consequência, antes de qualquer release ser publicada. Como o
  workflow só dispara com um push real na `prod`, isso significa que uma
  versão só vira release se o código já estiver na `prod` e a suíte de
  testes daquele exato commit passar; nenhuma release sai de um build ou
  teste quebrado.

- Empacotamento para Linux: novo `App/distribuicao/gerar-appimage.sh` gera um
  AppImage autocontido (`CofreDeSenhas-X.Y.Z-x86_64.AppImage`), que roda em
  qualquer distribuição x86_64 sem exigir o SDK do .NET nem instalação —
  passou a ser publicado a cada release junto com o pacote `.tar.gz` e o
  instalador do Windows, com checksum no `CHECKSUMS.txt`. O script de
  instalação (`instalar.sh`/`desinstalar.sh`) foi mantido como alternativa
  para quem prefere compilar do código-fonte; ambos preservam o cofre em
  `~/.config/GerenciadorSenhas` na remoção e funcionam em X11 e Wayland.
  Pacote `.deb` e Flatpak foram avaliados e adiados: exigiriam manter um
  repositório próprio ou publicação no Flathub, esforço que não se justifica
  agora com o AppImage já cobrindo o uso sem gerenciador de pacotes.

- Atualização em um clique: o item "Verificar atualizações" no menu de
  configurações vem ligado por padrão e consulta a release mais recente do
  GitHub a cada abertura do cofre. Havendo versão mais nova, a barra inferior
  mostra um aviso com o botão "Atualizar agora" (além do dispensar, que
  lembra a versão dispensada nas preferências para não repetir o aviso). No
  Windows, o botão baixa o instalador (ou o executável portátil, conforme
  como o cofre está rodando) da release, confere o hash contra o
  `CHECKSUMS.txt` da própria release e, só se bater, aplica a atualização
  sozinho: roda o instalador em modo silencioso (sem elevar privilégio,
  já que o instalador nunca exigiu admin) ou troca o executável portátil no
  lugar, fecha o cofre e reabre a versão nova automaticamente — nenhum
  clique extra, nenhum instalador pra rodar na mão. Em qualquer falha
  (checksum não bate, arquivo não encontrado, sem internet) ou fora do
  Windows, cai de volta no comportamento antigo: abre a página de releases
  no navegador para baixar manualmente. A consulta e o download são leitura
  pública da API do GitHub, sem enviar nenhum dado além disso.

#### Produtividade e qualidade

- Atalhos de teclado: `Ctrl+F` foca a busca, `Ctrl+N` abre nova senha,
  `Ctrl+G` abre/fecha o gerador, `Ctrl+L` bloqueia o cofre na hora (novo item
  "Bloquear agora" no menu de configurações) e `Ctrl+Shift+U`/`Ctrl+Shift+P`
  copiam o usuário/a senha da linha selecionada — a com foco de teclado, ou a
  primeira da lista se nenhuma estiver focada. Uma folha de atalhos
  consultável (menu de configurações → "Atalhos de teclado...") lista todas
  as combinações. Nenhum atalho usa Insert/Caps Lock (modificadores padrão de
  leitores de tela como NVDA e JAWS) nem combinações reservadas pelo sistema.

- Testes automatizados de interface: novo projeto `App.Testes`, com
  Avalonia.Headless, cobrindo os fluxos críticos da UI sem precisar de tela —
  desbloqueio do cofre com senha certa e errada, criação e edição de
  credencial, cópia de senha e de usuário pela lista (com aviso de limpeza
  automática) e o atalho `Ctrl+L` de bloqueio imediato. Passou a rodar junto
  com o `dotnet test` da solução, então já está coberto pelo CI existente em
  Windows e Linux a cada push, sem passo extra no workflow. Bloqueio
  automático por tempo (`MonitorInatividade`) ficou fora desta rodada por
  depender do relógio real, sem um relógio injetável para simular a espera
  em teste; troca de idioma foi coberta diretamente pelo evento global de
  `Idioma`, sem passar pelo menu (evita gravar preferência de idioma no
  perfil real durante o teste).

### Versão 2.1.1

Sem mudança de funcionalidade para quem usa o cofre — existe só pra que os
próprios arquivos publicados numa release já saiam de uma cadeia de build
mais dura de comprometer, e pra que a nota de cada release publicada
reflita de verdade o que mudou.

- Endurecimento da cadeia de build: todas as GitHub Actions de terceiro
  usadas em `release.yml`/`ci.yml` (checkout, setup-dotnet, upload/download-
  artifact, action-gh-release) passaram de tag flutuante (`@v4`) para hash de
  commit fixo, cada uma com um comentário indicando a versão — uma tag pode
  ser remarcada pra outro commit sem aviso, um hash não. Cada artefato
  publicado (instalador, portátil, pacote Linux, AppImage, o próprio
  `CHECKSUMS.txt`) ganha também uma *attestation* de proveniência
  (`actions/attest-build-provenance`, SLSA/Sigstore) verificável publicamente
  contra o log de transparência do Sigstore — prova assinada de que aquele
  arquivo saiu daquele workflow, daquele commit, e não foi montado ou
  alterado por fora dele; README documenta como conferir com
  `gh attestation verify`. O `appimagetool` baixado durante o build do Linux
  passou de uma tag "continuous" (que muda sem aviso) pra uma versão fixa
  (1.9.1) com hash SHA256 conferido antes de rodar. Assinatura de código
  (Authenticode) no instalador Windows segue como o item de maior impacto
  ainda pendente, dependente de comprar um certificado — ver
  [`THREAT_MODEL.md`](THREAT_MODEL.md).
- Corrigido o gerador de notas de release: o workflow publicava o modelo
  (`.github/RELEASE_TEMPLATE.md`) direto como nota de cada release, sem
  trocar o placeholder de versão (`X.Y.Z`) nem preencher "Destaques desta
  versão" e "Mudanças" — toda release saía com texto cru e seções vazias.
  Um novo script (`.github/scripts/montar_notas_release.py`), chamado por
  um passo do workflow antes de publicar, monta a nota de verdade a partir
  do `CHANGELOG.md`; as notas já publicadas das releases 2.1.0 e 2.1.1
  foram corrigidas via API com o mesmo conteúdo que o script gera agora.

### Versão 2.2.0

Auditoria de código completa em cima da 2.1.1: bugs críticos, altos, médios e
baixos corrigidos, lacunas fechadas entre cofre local/pasta de
sincronização/banco de dados, e um conjunto de ideias futuras do roadmap
implementadas — com peso forte em segurança, principalmente na sincronização
com banco de dados externo.

#### Segurança

- Troca de senha mestra corrigida para recifrar o cofre inteiro: campos
  extras, códigos de recuperação e anexos ficavam cifrados com a chave
  antiga depois de trocar a senha mestra, um bug crítico que só afetava
  quem já tinha esses dados preenchidos antes da troca.
- Escrita atômica (arquivo temporário + substituição) na troca de senha
  mestra e no salvamento local, eliminando o risco de arquivo corrompido
  por queda de energia no meio da escrita; se acontecer mesmo assim, o
  cofre se recupera sozinho a partir do backup órfão no login seguinte.
- Corrigido bug em que duas credenciais com o mesmo domínio e usuário se
  sobrescreviam ao sincronizar com um banco de dados externo compartilhado
  por vários dispositivos, e outro em que uma exclusão era revertida para
  "não excluída" quando dois dispositivos espelhados no mesmo banco
  sincronizavam em sequência.
- Modo privacidade (`Ctrl+H`) corrigido para não permitir mais reabrir o
  painel de detalhes com a senha em texto claro enquanto ativo, e para não
  vazar mais o serviço, o usuário e as etiquetas reais por leitor de tela e
  tooltip da lista.
- Nova opção "Exigir certificado válido do servidor" na tela de conexão a
  banco de dados: desligada por padrão (a conexão continua sempre cifrada,
  mas aceita certificado autoassinado, comum em banco local/LAN); ligada,
  exige um certificado validado por autoridade confiável, recomendado para
  banco fora da rede local. Quando desligada, fica sinalizada no relatório
  de segurança do cofre como um lembrete, não um bloqueio.
- HMAC de integridade (chaveado por uma subchave derivada da própria senha
  mestra via HKDF, independente da chave de cifra AES-GCM) para os dados
  vindos de um banco de dados externo: uma linha adulterada por alguém com
  acesso de escrita direto ao banco — sem a chave mestra — é detectada e
  rejeitada na sincronização, em vez de aceita como "mais recente" só por
  ter uma data de atualização mais nova. Ver [modelo de ameaça](THREAT_MODEL.md)
  para o que isso muda (e o que continua igual) no cenário de banco
  externo compartilhado.
- Argon2id (o mesmo já usado no cofre local desde a repaginação de
  segurança anterior) passou a ser usado também na exportação `.gsenhas` e
  na sincronização por pasta compartilhada, que ainda usavam PBKDF2-SHA256;
  arquivos já existentes continuam sendo lidos normalmente no KDF com que
  foram criados, sem migração forçada (diferente do cofre local, esses
  arquivos podem ser lidos por outro dispositivo/instalação que ainda não
  atualizou).
- Aviso ao salvar o QR code de backup da senha mestra dentro de uma pasta
  sincronizada com a nuvem (OneDrive, Dropbox, Google Drive, iCloud Drive)
  — o QR code contém a senha mestra em forma reconstruível, e uma pasta de
  nuvem é exatamente o tipo de lugar onde ele não deveria parar.
- Mensagens de erro cruas de conexão a banco de dados e de sincronização
  por pasta passaram a aparecer traduzidas, sem detalhe técnico exposto.
- Cadeia de build: pacotes NuGet verificados contra vulnerabilidades
  conhecidas a cada push (o job falha se achar alguma); SBOM (Software
  Bill of Materials, formato CycloneDX) publicado em cada release; Dependabot
  mantendo as GitHub Actions fixadas por hash (desde a 2.1.1) atualizadas
  automaticamente, já que hash fixo não recebe patch sozinho como uma tag
  flutuante receberia.

#### Sincronização e banco de dados

- Etiquetas e histórico de senha passam a somar as mudanças dos dois lados
  ao sincronizar (com banco de dados ou por pasta compartilhada), em vez de
  o lado com a edição mais recente substituir a credencial inteira — os
  demais campos continuam resolvidos por "edição mais recente vence", só
  essas duas listas aditivas passaram a se unir.
- Log de conflitos de sincronização: um novo indicador ao lado do status de
  conexão (só aparece quando há algo a mostrar) abre uma tela simples
  listando o que foi atualizado por outro dispositivo na última
  sincronização com o banco, e também qualquer linha rejeitada por falha
  na verificação de integridade (HMAC).
- `GarantirColunasAsync` deixou de abrir uma conexão nova por coluna ao
  inicializar ou migrar o schema do banco (18 conexões viravam 1), e
  passou a tolerar dois clientes inicializando o schema ao mesmo tempo em
  vez de um deles falhar.
- Corrigida a leitura do id de uma linha recém-inserida no SQL Server:
  trocado `SELECT SCOPE_IDENTITY()` numa consulta separada (que podia
  voltar nulo sob concorrência real, achado só depois de rodar contra um
  SQL Server de verdade pela primeira vez) por `OUTPUT INSERTED.id` direto
  no próprio `INSERT`, o mesmo padrão atômico que o PostgreSQL já usava
  com `RETURNING id`.

#### Organização e produtividade

- Ações em lote na lista de credenciais: selecionar várias de uma vez
  (`Ctrl`+clique em cada linha) libera uma barra com atalhos para
  favoritar, adicionar uma etiqueta ou mover todas para a lixeira de uma
  vez só, em vez de repetir a ação item por item.
- Tendência da pontuação de segurança do cofre ao longo do tempo: o
  relatório de segurança passou a guardar um histórico local (um ponto por
  dia, até 90 dias) e mostrar um pequeno gráfico de barras da evolução da
  pontuação, além do valor atual.
- Exportação seletiva: com um filtro de busca, categoria, etiqueta ou
  favoritos ativo na lista, a tela de exportação oferece exportar só os
  itens filtrados em vez do cofre inteiro sempre.
- Importação de CSV/JSON deixou de abortar o lote inteiro por causa de um
  campo inválido numa única linha — agora conta só aquela linha como
  inválida (com contador próprio) e segue importando o resto.
- Barra de progresso na exportação, reaproveitando o mesmo painel que a
  importação já tinha ganhado antes, já que o passo mais lento (ler e
  decifrar anexo por anexo) é comum aos dois.
- Foco inicial ajustado em diálogos que abriam sem nada selecionado por
  teclado (edição de credencial, atalhos de teclado, relatório de
  segurança, seleção de banco, backup).

#### Distribuição

- Atualização em um clique passou a funcionar no Linux também, via
  AppImage — antes só o Windows tinha essa opção, e no Linux a única forma
  de atualizar era baixar manualmente na página de releases. Usa a
  variável de ambiente `$APPIMAGE` (convenção do próprio runtime do
  AppImage) para localizar o arquivo de verdade a substituir, já que o
  caminho do processo em execução aponta para o ponto de montagem
  temporário, não para o `.AppImage` original.
- Manifests para o `winget` (gerenciador de pacotes do Windows) passaram a
  ser gerados automaticamente a cada release, prontos para submissão a
  `microsoft/winget-pkgs` — a submissão em si (abrir o PR no repositório
  de terceiros) continua manual, fora do alcance do pipeline deste
  projeto.

### Não lançado

- Restaurar o cofre local a partir de um banco de dados conectado: num
  dispositivo novo, sem cofre local ainda, a tela de primeiro uso oferece
  "Restaurar de um banco de dados" — digitando a senha mestra já em uso, o
  cofre inteiro é trazido do banco em vez de recriado do zero. Viabilizado
  por uma tabela de autenticação (`CofreDeSenhasAuth`) publicada
  automaticamente ao conectar a um banco externo, carregando o mesmo salt e
  verificador da senha mestra que antes só existiam em `auth.dat` local —
  ver [modelo de ameaça](THREAT_MODEL.md) para o que isso muda no cenário
  de banco externo compartilhado.

## Planejado

Ideias e melhorias consideradas para versões futuras, agrupadas por prioridade:

### Segurança e sincronização

#### Reforço de segurança

- Suporte a chave de hardware (FIDO2/YubiKey) como alternativa de
  desbloqueio, cobrindo também o Linux — hoje sem nenhuma opção além da
  senha mestra, diferente do Windows (Windows Hello). Já houve uma
  tentativa de implementação no Windows (extensão PRF do WebAuthn via
  `DSInternals.Win32.WebAuthn`/`webauthn.dll`), revertida por instabilidade:
  a cerimônia de registro/desbloqueio podia ficar pendurada indefinidamente
  sem lançar erro nem concluir, mesmo depois de corrigir um travamento de UI
  causado por chamadas síncronas e de adicionar um timeout explícito. Sem
  hardware físico disponível para diagnosticar a causa raiz ou validar o
  fluxo de ponta a ponta, o recurso não é seguro o bastante para expor na
  UI. Precisa de um authenticator físico à mão antes de tentar de novo, além
  de interop equivalente no Linux (bindings para `libfido2`, sem opção .NET
  pronta hoje).

### Novas funcionalidades a avaliar

- Compartilhamento seguro de credenciais (ex.: acesso de emergência,
  compartilhamento familiar), recurso comum em gerenciadores de senha e hoje
  ausente do roadmap.
- Suporte a passkeys/WebAuthn como tipo de credencial, à medida que mais
  serviços passam a exigir isso.
- CLI ou API local para automação por usuários avançados (buscar/copiar
  credenciais via script, sem abrir a interface).

### Baixa prioridade

#### Melhorias visuais e experiência de uso

Boa parte foi entregue nas duas repaginações de identidade visual (a
original, e depois "cofre, latão escovado" na 2.1.0), nas mensagens de erro
amigáveis e na importação com prévia e barra de progresso — essas duas
últimas também da 2.1.0. Continuam planejados:

- Melhorar tela de primeiro uso.
- Melhorar responsividade em telas menores.

#### Manutenção interna

Dívida técnica que não muda funcionalidades, mas reduz o custo de evoluir:

- Extrair as traduções embutidas em `Idioma.cs` (milhares de linhas de tuplas)
  para arquivos de recurso por idioma.
- Unificar a definição da paleta de cores, hoje duplicada entre os dicionários
  de tema do XAML e o mapa de cores da acessibilidade.
- Dividir o code-behind da janela principal, que concentra navegação, lista,
  detalhes, conexão de banco e importação em um único arquivo extenso.

Já executado desta lista: remoção dos apelidos duplicados de recursos (as duas
grafias do token de força "excelente"), de chaves de tradução e ícones sem uso
e de geometrias repetidas no code-behind do gerador. Nova rodada de limpeza:
eliminação de chaves de tradução órfãs, cores não referenciadas em `Tema.cs`,
campos vestigiais do modelo de credencial (`IV`/`AuthTag` de um esquema de
criptografia anterior) e de uma camada inteira de métodos de busca/contagem em
`IRepositorioSenha`/`IServicoSenha` que nunca chegou a ser usada pela interface
(a lista filtra tudo em memória) e só existia para os próprios testes.

### Futuro

#### Versão para macOS

- Avaliar suporte oficial ao macOS.
- Validar compatibilidade do Avalonia no macOS.
- Criar build para macOS.
- Avaliar assinatura e notarização.
- Adaptar atalhos, empacotamento e comportamento visual ao sistema.
- Testar armazenamento de dados no padrão do macOS.

#### Aplicativo móvel

- Avaliar versão mobile no futuro.
- Priorizar somente depois da estabilização desktop.
- Avaliar plataformas:
  - Android;
  - iOS.
- Reaproveitar domínio e regras de criptografia quando possível.
- Resolver sincronização antes de investir em mobile.

## Fora de escopo por enquanto

- Extensão de navegador. Chegou a ter um protótipo funcional (Native
  Messaging, host dedicado em `CofreDeSenhas.Nucleo`/`Bridge`/`Agent`,
  preenchimento só sob clique, testado de ponta a ponta), mas foi abandonada:
  o cofre é pensado como armazenamento, não como integração com o navegador
  — manter um segundo processo de fundo, registro de host nativo no sistema
  e superfície de ataque extra (Native Messaging, DOM da extensão) foge
  desse escopo. O código ficou no branch `feature/chromiumExt`, sem
  intenção de mesclar.
- IA integrada ao núcleo do cofre.
- Assistente educativo embutido no aplicativo (reavaliado: conteúdo educativo
  ficará na documentação, fora do app).
- Envio de senhas para qualquer serviço externo.
- Armazenamento obrigatório em nuvem.
- Conta obrigatória para usar o aplicativo.
- Recursos bloqueados atrás de pagamento.
- Coleta de telemetria sensível.
- Recuperação de senha mestra por servidor externo.
- Qualquer mecanismo que permita recuperar o cofre sem a senha mestra ou chave
  equivalente do usuário.

## Ordem sugerida de execução

1. macOS.
2. Aplicativo móvel.

## Como sugerir

Encontrou um problema ou tem uma ideia? Abra uma issue descrevendo o caso de uso.
Pull requests que avancem qualquer item desta lista são muito bem-vindos.
