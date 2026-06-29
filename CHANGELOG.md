# Changelog

Todas as mudanças notáveis deste projeto são documentadas aqui.
O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/)
e o projeto adota [Versionamento Semântico](https://semver.org/lang/pt-BR/).

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
