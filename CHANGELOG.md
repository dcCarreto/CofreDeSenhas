# Changelog

Todas as mudanças notáveis deste projeto são documentadas aqui.
O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/)
e o projeto adota [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [Não lançado]

### Adicionado
- Extensão de navegador para Chromium (Chrome, Edge, Brave): preenchimento de
  usuário e senha **sob clique**, a partir do cofre local, via host nativo
  (Native Messaging). A senha mestra é pedida uma única vez por sessão, em um
  diálogo nativo, e a chave derivada permanece apenas na memória do processo Agent.
  - Popup com desbloqueio, lista das credenciais do site e cópia de usuário/senha.
  - Content script com ícone nos campos de senha e preenchimento sem auto-submit,
    com menu de escolha quando há mais de uma credencial.
  - Auto-bloqueio do host por inatividade (15 minutos).
  - Scripts de instalação e registro do host nativo para Chrome, Edge e Brave.
- Correspondência de domínio por eTLD+1, considerando sufixos públicos compostos
  (ex.: `com.br`, `co.uk`).

### Segurança
- O host nativo é somente-leitura sobre o cofre; a senha em claro só é entregue à
  extensão sob ação explícita do usuário (clique).
- `allowed_origins` restringe o host à extensão, cujo ID é fixado pelo campo `key`.

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

[1.0.0]: https://github.com/dcCarreto/CofreDeSenhas/releases/tag/v1.0.0
