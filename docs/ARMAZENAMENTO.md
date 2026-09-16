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
- `backups/`: cópias de segurança do cofre.

Esses arquivos não fazem parte do repositório e contêm dados sensíveis.

Ao rodar o projeto a partir do código-fonte (`dotnet run`, ou o executável de
uma compilação de Debug), os dados vão para `GerenciadorSenhas.dev/` ao lado
da pasta acima — nunca para a pasta do aplicativo instalado. A suíte de testes
usa uma pasta temporária descartável, apagada ao final. Só o binário publicado
para release usa `GerenciadorSenhas/`.
