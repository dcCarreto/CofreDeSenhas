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

### Após a 1.0.0

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

## Planejado

Ideias e melhorias consideradas para versões futuras, sem ordem definitiva de
prioridade:

- Suporte a códigos TOTP (autenticação em duas etapas) por entrada.
- Auditoria do cofre: detecção de senhas fracas, repetidas ou antigas.
- Importação a partir de outros gerenciadores e de arquivos CSV.
- Organização por pastas ou etiquetas personalizadas, além das categorias fixas.
- Histórico de alterações por credencial.
- Internacionalização da interface (além do português).
- Sincronização opcional e criptografada de ponta a ponta entre dispositivos.
- Desbloqueio por biometria ou Windows Hello.
- Versão para macOS.
- Empacotamento em instalador e publicação em gerenciadores de pacotes.

## Como sugerir

Encontrou um problema ou tem uma ideia? Abra uma issue descrevendo o caso de uso.
Pull requests que avancem qualquer item desta lista são muito bem-vindos.
