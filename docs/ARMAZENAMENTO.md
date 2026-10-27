# Armazenamento de dados

[← Voltar ao README](../README.md)

Os arquivos da aplicação ficam em `%APPDATA%\GerenciadorSenhas\` no Windows e
em `~/.config/GerenciadorSenhas/` no Linux:

- `auth.dat`: salt e verificador da senha mestra.
- `senhas.json.enc`: cofre criptografado com as credenciais.
- `biometria.dat`: chave do cofre cifrada e vinculada a uma credencial do
  Windows Hello, para desbloqueio biométrico quando ativado neste dispositivo.
- `config.json`: preferências da interface (como o idioma e a acessibilidade) e
  o último perfil de conexão a banco, sem a senha do servidor.
- `anexos/`: um arquivo `.enc` por anexo, cada um cifrado com a chave do cofre.
- `backups/`: cópias de segurança do cofre.

Esses arquivos não fazem parte do repositório e contêm dados sensíveis.

Com a sincronização por pasta ligada, a pasta compartilhada escolhida recebe:

- `sincronizacao.dat`: as credenciais, num único arquivo cifrado com a chave de
  sincronização (derivada da senha mestra).
- `anexos-sincronizados/`: só com a opção "Sincronizar anexos" ligada. Tem um
  `.enc` por anexo, cifrado com a mesma chave de sincronização, e não com a
  chave do cofre local.

Ao rodar o projeto a partir do código-fonte (`dotnet run`, ou o executável de
uma compilação de Debug), os dados vão para `GerenciadorSenhas.dev/` ao lado
da pasta acima — nunca para a pasta do aplicativo instalado. A suíte de testes
usa uma pasta temporária descartável, apagada ao final. Só o binário publicado
para release usa `GerenciadorSenhas/`.
