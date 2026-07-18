# Changelog

Todas as mudanças notáveis deste projeto são documentadas aqui.
O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/)
e o projeto adota [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [Não lançado]

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
- Releases confiáveis e bem documentados: workflow de CI que, ao receber uma
  tag `vX.Y.Z`, publica o instalador e o executável portátil do Windows e o
  pacote do Linux, gera `CHECKSUMS.txt` com o hash SHA256 de cada arquivo e
  cria a release no GitHub como rascunho, seguindo um modelo padronizado
  (destaques, changelog, capturas de tela, downloads por sistema
  operacional, instalação/atualização com aviso de backup e verificação dos
  hashes). A publicação continua manual. README documenta como conferir os
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
- Aviso de nova versão: item "Verificar atualizações" no menu de
  configurações (desligado por padrão) consulta a release mais recente do
  GitHub e mostra um aviso discreto e dispensável na barra inferior quando
  há uma versão mais nova, com link para a página de releases — sem
  download automático e sem enviar nada além da própria consulta.

### Corrigido
- O painel de detalhes agora flutua sobre a lista de senhas em vez de
  espremê-la: antes, abrir os detalhes de uma credencial podia esconder por
  completo as colunas de força e de ações de todas as linhas, sem rolagem e
  sem aviso, em janelas no tamanho padrão.
- O botão "Salvar alterações" do painel de detalhes não corta mais o próprio
  texto.
- Contraste de cores revisado em quatro pontos que ficavam abaixo do mínimo
  recomendado (WCAG AA): a estrela de favorito preenchida, a cor de força
  "média", a cor de força "fraca" quando usada como texto pequeno (inclusive
  em mensagens de erro) e o roxo de destaque quando usado como texto no tema
  escuro.
- O botão "Nova senha" abre a tela de cadastro de uma credencial, como
  esperado — antes abria por engano o gerador de senha avulso.

### Segurança
- As iterações do PBKDF2-SHA256 usadas para derivar a chave a partir da senha
  mestra subiram de 100 mil para 600 mil (patamar recomendado pela OWASP).
  Cofres criados com a contagem antiga são migrados de forma transparente no
  próximo desbloqueio por senha mestra, com backup e rollback seguro, sem ação
  extra do usuário. Se o Windows Hello estiver habilitado, ele é desativado
  automaticamente nesse momento (o vínculo biométrico antigo perde a validade)
  e pode ser reativado logo em seguida.
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
- Nova identidade visual em toda a aplicação, preservando as funcionalidades:
  paleta e tokens de cor revisados nos temas claro e escuro, tipografia Plus
  Jakarta Sans (com Inter de reserva), escala tipográfica e raios/sombras
  padronizados.
- Catálogo unificado de ícones de traço e componentes (botões, campos,
  seletores, alternadores, controle deslizante e dicas) com estados consistentes
  de interação.
- Telas repaginadas: tela de senha mestra unificada com o gerador embutido,
  diálogos com medidor de força, anel de progresso do TOTP e estados vazios
  ilustrados. Novo ícone do aplicativo em múltiplas resoluções.
- Contrastes de texto e distintivos de categoria revisados para atender ao nível
  AA nos dois temas, com realce de foco de teclado visível em todos os controles.
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

[Não lançado]: https://github.com/dcCarreto/CofreDeSenhas/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/dcCarreto/CofreDeSenhas/compare/v1.0.0...v2.0.0
[1.0.0]: https://github.com/dcCarreto/CofreDeSenhas/releases/tag/v1.0.0
