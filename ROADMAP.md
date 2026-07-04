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

## Planejado

Ideias e melhorias consideradas para versões futuras, agrupadas por prioridade:

### Alta prioridade

#### Limpeza automática da área de transferência

- Limpar automaticamente a área de transferência após copiar uma senha.
- Permitir configuração do tempo de limpeza:
  - 15 segundos;
  - 30 segundos;
  - 60 segundos;
  - desativado.
- Exibir aviso discreto informando que a senha copiada será removida.
- Evitar sobrescrever o clipboard se o usuário copiar outro conteúdo depois da
  senha.
- Adicionar testes para o comportamento de limpeza quando possível.

#### Backup automático local

- Criar backup automático do cofre em intervalos configuráveis.
- Permitir opções como:
  - diário;
  - semanal;
  - manual apenas.
- Definir quantidade máxima de backups mantidos.
- Exibir data do último backup realizado.
- Permitir restauração de backup pela interface.
- Avisar o usuário antes de restaurar um backup.
- Preservar o modelo offline do aplicativo.
- Manter backups sempre criptografados.

#### Lixeira criptografada

- Ao excluir uma credencial, mover primeiro para uma lixeira interna.
- Permitir restaurar credenciais excluídas.
- Permitir exclusão definitiva.
- Permitir limpar a lixeira.
- Manter os itens da lixeira criptografados junto com o cofre.
- Exibir data de exclusão do item.
- Evitar perda acidental de credenciais importantes.

#### Instalador profissional para Windows

- Criar instalador para Windows.
- Adicionar atalho no menu iniciar.
- Adicionar opção de desinstalação.
- Configurar ícone corretamente.
- Preservar dados do cofre ao desinstalar, salvo decisão explícita do usuário.
- Avaliar formatos:
  - MSI;
  - EXE;
  - MSIX.
- Documentar instalação e remoção.

#### Relatório de segurança do cofre

- Criar tela de resumo de segurança.
- Exibir pontuação geral do cofre.
- Mostrar quantidade de:
  - senhas fracas;
  - senhas repetidas;
  - senhas antigas;
  - senhas comprometidas;
  - contas sem TOTP cadastrado;
  - contas sem URL;
  - contas sem categoria.
- Permitir filtrar a lista a partir de cada problema.
- Exibir recomendações locais sem enviar dados para fora.
- Manter a auditoria funcionando offline, exceto quando o usuário ativar a
  verificação opcional de vazamentos.

### Média prioridade

#### Códigos de recuperação por credencial

- Adicionar campo próprio para códigos de recuperação.
- Permitir salvar múltiplos códigos por credencial.
- Permitir copiar um código individual.
- Permitir marcar código como usado.
- Permitir ocultar ou revelar os códigos.
- Armazenar todos os códigos de forma cifrada.
- Incluir códigos de recuperação na exportação protegida.
- Incluir códigos de recuperação na importação/exportação do banco, sempre
  cifrados.

#### Verificação de integridade dos releases

- Publicar hash SHA256 dos arquivos de release.
- Incluir instruções para o usuário verificar o download.
- Adicionar arquivo CHECKSUMS.txt nas releases.
- Avaliar assinatura dos arquivos de release.
- Avaliar assinatura de código no Windows no futuro.
- Melhorar a confiança no binário distribuído.

#### Empacotamento para Linux

- Criar AppImage.
- Avaliar pacote .deb.
- Avaliar Flatpak no futuro.
- Manter script de instalação atual.
- Documentar instalação por distribuição.
- Garantir que o cofre local seja preservado na remoção.
- Garantir compatibilidade com X11 e Wayland.

#### Modo privacidade

- Adicionar botão para ocultar rapidamente informações sensíveis.
- Permitir ocultar:
  - lista de credenciais;
  - usuários;
  - serviços;
  - categorias;
  - URLs.
- Permitir bloquear o cofre rapidamente.
- Adicionar atalho de teclado para ativar o modo privacidade.
- Limpar a área de transferência ao bloquear.
- Reduzir exposição visual em ambientes compartilhados.

#### Histórico operacional da credencial

- Registrar data de criação da credencial.
- Registrar data da última edição.
- Registrar data da última cópia de senha.
- Registrar data da última cópia de usuário.
- Registrar data da última cópia de TOTP.
- Exibir essas informações na tela de edição ou detalhes.
- Manter os registros locais e protegidos no cofre.
- Permitir que o usuário desative esse histórico, caso prefira menos
  rastreamento local.

#### Melhorias na página de releases

- Padronizar descrição de cada versão.
- Incluir changelog claro.
- Incluir capturas de tela.
- Separar downloads por sistema operacional.
- Incluir instruções de instalação.
- Incluir instruções de atualização.
- Incluir aviso de backup antes de atualizar.
- Incluir hashes dos arquivos publicados.

### Baixa prioridade

#### Anexos criptografados

- Permitir anexar arquivos pequenos a uma credencial.
- Usos possíveis:
  - QR code de 2FA;
  - chave de recuperação;
  - PDF de backup;
  - documento relacionado;
  - imagem ou texto sensível.
- Armazenar anexos sempre criptografados.
- Definir limite de tamanho por anexo.
- Definir limite total de anexos no cofre.
- Permitir exportar e importar anexos junto com o cofre.
- Avaliar impacto no desempenho e no tamanho do arquivo criptografado.

#### Melhorias visuais e experiência de uso

- Melhorar tela vazia quando não há credenciais.
- Melhorar mensagens de erro.
- Melhorar tela de primeiro uso.
- Melhorar experiência de importação.
- Adicionar confirmação visual mais clara após cópia.
- Melhorar responsividade em telas menores.
- Revisar espaçamentos, contraste e consistência visual.
- Melhorar navegação por teclado.

#### Organização avançada

- Adicionar tags além de categorias.
- Permitir múltiplas tags por credencial.
- Permitir filtros combinados.
- Permitir ordenação avançada.
- Permitir fixar itens importantes.
- Permitir visualização por categoria.
- Permitir favoritos em seção separada.

#### Templates de credenciais

- Criar modelos para tipos diferentes de entrada:
  - login comum;
  - cartão;
  - chave de licença;
  - Wi-Fi;
  - servidor;
  - banco de dados;
  - documento seguro.
- Permitir campos específicos por tipo.
- Manter todos os campos sensíveis criptografados.

### Futuro

#### Sincronização criptografada de ponta a ponta

- Implementar sincronização opcional entre dispositivos.
- Manter criptografia de ponta a ponta.
- Garantir que o servidor nunca tenha acesso às senhas em texto puro.
- Permitir uso de provedores escolhidos pelo usuário.
- Avaliar sincronização via arquivo em nuvem do próprio usuário.
- Resolver conflitos de forma clara e segura.
- Manter funcionamento offline como padrão.

#### Versão para macOS

- Avaliar suporte oficial ao macOS.
- Validar compatibilidade do Avalonia no macOS.
- Criar build para macOS.
- Avaliar assinatura e notarização.
- Adaptar atalhos, empacotamento e comportamento visual ao sistema.
- Testar armazenamento de dados no padrão do macOS.

#### Extensão de navegador

- Avaliar criação de extensão para navegadores.
- Suporte futuro possível para:
  - Chrome;
  - Edge;
  - Firefox.
- Permitir preenchimento de login com autorização explícita do usuário.
- Impedir exposição indevida do cofre ao navegador.
- Usar comunicação local segura entre extensão e aplicativo.
- Exigir cofre desbloqueado.
- Manter a extensão como recurso opcional.

#### Aplicativo móvel

- Avaliar versão mobile no futuro.
- Priorizar somente depois da estabilização desktop.
- Avaliar plataformas:
  - Android;
  - iOS.
- Reaproveitar domínio e regras de criptografia quando possível.
- Resolver sincronização antes de investir em mobile.

#### Assistente educativo opcional

- Criar, no futuro, um assistente local ou módulo educativo sem acesso às
  senhas.
- Explicar conceitos de segurança.
- Ajudar o usuário a entender o relatório do cofre.
- Dar dicas de boas práticas.
- Nunca acessar:
  - senha mestra;
  - senhas salvas;
  - banco criptografado;
  - chaves TOTP;
  - códigos de recuperação;
  - notas privadas.
- Manter desligado por padrão.
- Nunca depender de IA online para funcionamento essencial do app.

## Fora de escopo por enquanto

- IA integrada ao núcleo do cofre.
- Envio de senhas para qualquer serviço externo.
- Armazenamento obrigatório em nuvem.
- Conta obrigatória para usar o aplicativo.
- Assinatura paga.
- Recursos bloqueados atrás de pagamento.
- Coleta de telemetria sensível.
- Recuperação de senha mestra por servidor externo.
- Qualquer mecanismo que permita recuperar o cofre sem a senha mestra ou chave
  equivalente do usuário.

## Ordem sugerida de execução

1. Limpeza automática da área de transferência.
2. Backup automático local.
3. Lixeira criptografada.
4. Instalador profissional para Windows.
5. Relatório de segurança do cofre.
6. Códigos de recuperação por credencial.
7. Verificação SHA256 dos releases.
8. Empacotamento para Linux.
9. Modo privacidade.
10. Histórico operacional da credencial.
11. Melhorias na página de releases.
12. Anexos criptografados.
13. Organização avançada com tags.
14. Templates de credenciais.
15. Sincronização criptografada de ponta a ponta.
16. macOS.
17. Extensão de navegador.
18. Aplicativo móvel.

## Como sugerir

Encontrou um problema ou tem uma ideia? Abra uma issue descrevendo o caso de uso.
Pull requests que avancem qualquer item desta lista são muito bem-vindos.
