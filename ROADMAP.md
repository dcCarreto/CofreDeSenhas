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
- Distribuição como executável único e autocontido para Windows.

## Em desenvolvimento

- Extensão de navegador para Chromium (Chrome, Edge, Brave): preenchimento de
  usuário e senha sob clique, conversando com o cofre local por um host nativo
  (Native Messaging), com a senha mestra pedida uma única vez por sessão. Inclui
  bloqueio automático por inatividade no host da extensão e correspondência de
  domínio por eTLD+1.

## Planejado

Ideias e melhorias consideradas para versões futuras, sem ordem definitiva de
prioridade:

- Bloqueio automático do cofre após período de inatividade.
- Geração de frases-senha (passphrases) a partir de listas de palavras.
- Suporte a códigos TOTP (autenticação em duas etapas) por entrada.
- Auditoria do cofre: detecção de senhas fracas, repetidas ou antigas.
- Importação a partir de outros gerenciadores e de arquivos CSV.
- Organização por pastas ou etiquetas personalizadas, além das categorias fixas.
- Histórico de alterações por credencial.
- Internacionalização da interface (além do português).
- Sincronização opcional e criptografada de ponta a ponta entre dispositivos.
- Desbloqueio por biometria ou Windows Hello.
- Versões para outras plataformas (Linux e macOS).
- Empacotamento em instalador e publicação em gerenciadores de pacotes.

## Como sugerir

Encontrou um problema ou tem uma ideia? Abra uma issue descrevendo o caso de uso.
Pull requests que avancem qualquer item desta lista são muito bem-vindos.
