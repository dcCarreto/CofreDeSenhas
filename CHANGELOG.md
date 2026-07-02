# Changelog

Todas as mudanças notáveis deste projeto são documentadas aqui.
O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/)
e o projeto adota [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [Não lançado]

### Adicionado
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
  id, usuario, senha, dominio, descricao e excluido (exclusão lógica).
- Migração leve que adiciona a coluna `descricao` a tabelas já existentes ao
  reconectar.
- Memória do último perfil de conexão para pré-preencher a tela de conexão.
- Testes de banco (criação da tabela, migração da coluna `descricao` e CRUD com
  exclusão lógica) executados sobre SQLite.

### Segurança
- A senha gravada no banco é sempre o texto cifrado (AES-256-GCM derivado da
  senha mestra), nunca a senha em claro. A senha do servidor de banco não é
  gravada em disco.
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

[Não lançado]: https://github.com/dcCarreto/CofreDeSenhas/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/dcCarreto/CofreDeSenhas/releases/tag/v1.0.0
