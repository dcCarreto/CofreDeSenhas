# Modelo de segurança — detalhes técnicos

[← Voltar ao README](../README.md)

Tabela completa dos algoritmos e parâmetros criptográficos usados no Cofre de
Senhas. Para um resumo curto, veja a seção [Modelo de
segurança](../README.md#modelo-de-segurança) do README. Para os adversários e
cenários considerados (o que é protegido, contra quem e o que fica fora de
escopo), veja o [modelo de ameaça](../THREAT_MODEL.md); para como reportar uma
vulnerabilidade, veja a [política de segurança](../SECURITY.md).

| Item | Detalhe |
|------|---------|
| Criptografia do cofre | AES-256-GCM, garantindo confidencialidade e integridade/autenticidade |
| Derivação de chave | Argon2id (64 MiB de memória, 3 iterações, paralelismo 1), o padrão atual recomendado por resistir melhor a ataques por GPU/ASIC, com salt aleatório de 128 bits. Cofres ainda em PBKDF2-SHA256 (de versões anteriores) são migrados de forma transparente no próximo desbloqueio por senha mestra, com backup e rollback seguro |
| Senha mestra | Nunca é armazenada. O arquivo de autenticação guarda apenas o salt e um verificador (hash SHA-256 da chave derivada) |
| Exportação e sincronização por pasta | AES-256-GCM com chave derivada por Argon2id (64 MiB, 3 iterações, paralelismo 1 — os mesmos parâmetros do cofre local) a partir de uma senha separada. Arquivos gerados antes dessa versão, ainda em PBKDF2-SHA256 (200.000 iterações), continuam sendo lidos normalmente, sem migração forçada — diferente do cofre local, esses arquivos podem ser abertos por outro dispositivo/instalação que ainda não atualizou |
| Comparações sensíveis | Realizadas em tempo constante, evitando ataques de temporização |
| Verificação de vazamento | Have I Been Pwned por k-anonymity: apenas os 5 primeiros caracteres do hash SHA-1 da senha deixam a máquina |
| Ícones dos serviços | Fallback local por iniciais por padrão; a busca de favicons na internet é opcional, desligada por padrão e exige consentimento. Quando ativada, apenas o domínio de cada serviço é enviado ao serviço de ícones do Google (nunca senha, usuário ou nota), e os ícones ficam em cache no disco |
| Códigos TOTP | A chave 2FA é guardada cifrada (AES-256-GCM) como a senha; os códigos são calculados localmente (RFC 6238) e nada é enviado à rede |
| Histórico de senhas | Cada senha anterior é guardada cifrada (AES-256-GCM) como a senha atual e re-cifrada ao alterar a senha mestra; permanece somente no cofre e na exportação |
| Cofre em banco de dados | Quando conectado a um banco externo, a coluna de senha guarda apenas o texto cifrado (AES-256-GCM); a senha do servidor de banco não é gravada em disco. Nome de serviço, usuário, notas e etiquetas ficam em texto puro nas colunas correspondentes (ver [modelo de ameaça](../THREAT_MODEL.md)) |
| Conexão a banco de dados | Sempre cifrada em trânsito (TLS). Por padrão aceita certificado autoassinado, comum em banco local/LAN; a opção "Exigir certificado válido do servidor" na tela de conexão liga a validação contra uma autoridade confiável, recomendado para banco fora da rede local |
| Integridade do banco externo | HMAC-SHA256 (chaveado por uma subchave derivada da senha mestra via HKDF, independente da chave de cifra) assina cada credencial antes de gravar num banco externo; uma linha adulterada por quem tem acesso de escrita direto ao banco, mas não a senha mestra, é detectada e rejeitada na sincronização em vez de aceita como "mais recente" |
| Windows Hello | Opcional no Windows. A chave do cofre é cifrada (AES-256-GCM) com uma chave derivada da assinatura de uma credencial do Windows Hello (chave privada no TPM); o envelope em `biometria.dat` só pode ser aberto após a autenticação biométrica |
| Higiene de memória | A chave mestra e sua cópia interna são apagadas da memória (`CryptographicOperations.ZeroMemory`) ao bloquear ou fechar o cofre; o painel de detalhes e as linhas reveladas da lista não retêm a senha em texto claro além do necessário |
| Local dos dados | Pasta do usuário (`%APPDATA%\GerenciadorSenhas\` no Windows, `~/.config/GerenciadorSenhas/` no Linux), fora do repositório |

Observações importantes:

- A chave de criptografia é derivada da senha mestra a cada execução. Se a senha
  mestra for perdida, o cofre não pode ser recuperado, pois a chave não é
  armazenada em lugar nenhum.
- Ao alterar a senha mestra, uma nova chave é derivada e todas as entradas são
  re-criptografadas. A operação faz backup dos arquivos e os restaura caso algo
  falhe, evitando deixar o cofre inacessível.
- O QR code de backup contém uma versão senha-frase da senha mestra ao ser
  escaneado. Cada letra é representada por uma palavra, números permanecem como
  números e símbolos comuns viram nomes legíveis. Trata-se de uma codificação,
  não de criptografia. Por isso ele é opcional e acompanhado de aviso: deve ser
  guardado em local seguro e off-line.

## Fluxo da senha mestra

1. Na criação, é gerado um salt aleatório; a chave é derivada por Argon2id
   (64 MiB, 3 iterações, paralelismo 1) e dela se calcula um verificador
   SHA-256. Somente o salt, o verificador e os parâmetros de derivação são
   gravados em `auth.dat`. A chave nunca é persistida.
2. Na abertura, a chave é derivada novamente a partir da senha informada, do
   salt e dos parâmetros gravados; o verificador é comparado em tempo
   constante. Se confere, a chave passa a ser usada para descriptografar o
   cofre durante a sessão. Cofres ainda em PBKDF2-SHA256 (de versões
   anteriores) são migrados nesse momento: a chave é re-derivada com Argon2id
   e o cofre inteiro é re-criptografado, com backup e rollback automáticos.
3. Ao alterar a senha mestra, o cofre inteiro é descriptografado com a chave
   antiga e re-criptografado com a nova, com backup e rollback automáticos.
4. Ao bloquear ou fechar o cofre, a chave mestra e sua cópia interna são
   apagadas da memória.
