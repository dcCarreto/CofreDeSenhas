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

## Em andamento

### Extensão de navegador

Em desenvolvimento no branch `feature/chromiumExt`:

- Comunicação local segura entre extensão e aplicativo via Native Messaging,
  com host dedicado — o cofre nunca é exposto diretamente ao navegador.
- Preenchimento de login somente sob ação explícita do usuário (clique).
- Exige o cofre desbloqueado; sem cofre aberto, nada é preenchido.
- Alvo inicial em navegadores Chromium (Chrome e Edge); Firefox avaliado em
  seguida.
- Recurso opcional: o aplicativo continua completo sem a extensão.

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

#### Instalador para Windows

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

#### Releases confiáveis e bem documentados

Consolida a verificação de integridade e as melhorias da página de releases,
que se sobrepunham:

- Publicar hash SHA256 dos arquivos (CHECKSUMS.txt) com instruções de
  verificação, gerado pelo CI já existente.
- Avaliar assinatura dos arquivos e, no futuro, assinatura de código no
  Windows.
- Padronizar a descrição de cada versão: changelog claro, capturas de tela,
  downloads separados por sistema operacional.
- Incluir instruções de instalação e de atualização, com aviso de backup antes
  de atualizar.

#### Atalhos de teclado

- Definir atalhos para as ações mais frequentes: buscar, nova senha, abrir o
  gerador, bloquear agora e copiar usuário/senha da linha selecionada.
- Exibir uma folha de atalhos consultável dentro do aplicativo.
- Garantir que os atalhos não conflitem com leitores de tela e com os padrões
  de cada sistema.

#### Testes automatizados de interface

- Adotar Avalonia.Headless para testar os fluxos críticos da interface:
  desbloqueio, criação e edição de credencial, cópia, bloqueio automático e
  troca de tema/idioma.
- Integrar ao CI existente (Windows e Linux).
- Reduzir a dependência de verificação manual a cada mudança visual.

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

#### Aviso de nova versão

- Checagem opcional de novas versões contra as releases do GitHub.
- Desligada por padrão e sem envio de qualquer dado além da própria consulta.
- Aviso discreto na interface, sem download automático.
- Nunca interromper o uso do cofre por causa de atualização.

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

Boa parte foi entregue na repaginação da identidade visual (tela vazia
ilustrada, revisão de espaçamentos, contraste e consistência, e foco de teclado
visível). Continuam planejados:

- Melhorar mensagens de erro.
- Melhorar tela de primeiro uso.
- Melhorar experiência de importação.
- Melhorar responsividade em telas menores.

#### Organização avançada

Parte já foi entregue: favoritos e navegação por categoria têm seções próprias
na barra lateral, e etiquetas existem via categorias personalizadas em `Outro`.
Continuam planejados:

- Estender etiquetas a credenciais de qualquer categoria, com múltiplas
  etiquetas por item.
- Permitir filtros combinados (categoria + etiqueta + estado da auditoria).
- Permitir ordenação por qualquer coluna da lista.
- Permitir fixar itens importantes no topo.

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
e de geometrias repetidas no code-behind do gerador.

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

#### Aplicativo móvel

- Avaliar versão mobile no futuro.
- Priorizar somente depois da estabilização desktop.
- Avaliar plataformas:
  - Android;
  - iOS.
- Reaproveitar domínio e regras de criptografia quando possível.
- Resolver sincronização antes de investir em mobile.

## Fora de escopo por enquanto

- IA integrada ao núcleo do cofre.
- Assistente educativo embutido no aplicativo (reavaliado: conteúdo educativo
  ficará na documentação, fora do app).
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
4. Conclusão da extensão de navegador (em andamento).
5. Instalador profissional para Windows.
6. Relatório de segurança do cofre.
7. Códigos de recuperação por credencial.
8. Releases confiáveis e bem documentados.
9. Empacotamento para Linux.
10. Atalhos de teclado.
11. Testes automatizados de interface.
12. Modo privacidade.
13. Histórico operacional da credencial.
14. Aviso de nova versão.
15. Anexos criptografados.
16. Organização avançada com etiquetas.
17. Templates de credenciais.
18. Sincronização criptografada de ponta a ponta.
19. macOS.
20. Aplicativo móvel.

## Como sugerir

Encontrou um problema ou tem uma ideia? Abra uma issue descrevendo o caso de uso.
Pull requests que avancem qualquer item desta lista são muito bem-vindos.
