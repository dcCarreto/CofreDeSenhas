# Armazenamento de dados

[← Voltar ao README](../README.md)

Os arquivos da aplicação ficam em `%APPDATA%\GerenciadorSenhas\` no Windows e
em `~/.config/GerenciadorSenhas/` no Linux:

- `auth.dat`: salt e verificador da senha mestra.
- `senhas.json.enc`: cofre criptografado com as credenciais.
- `biometria.dat`: chave do cofre cifrada e vinculada a uma credencial do
  Windows Hello, para desbloqueio biométrico quando ativado neste dispositivo.
- `config.json`: preferências da interface (como o idioma e a acessibilidade) e
  o último perfil de conexão a banco, com a senha do servidor cifrada pela chave
  do cofre (nunca em texto puro).
- `anexos/`: um arquivo `.enc` por anexo, cada um cifrado com a chave do cofre.
- `backups/`: cópias de segurança do cofre.
- `recuperacao.dat`: só com a chave de recuperação ativada; a chave do cofre
  cifrada pelo segredo de recuperação.
- `pontuacao-historico.json`: datas e notas da pontuação de segurança, para o
  gráfico de evolução (sem nomes de serviço nem senhas).
- `logs/erros.log`: log de diagnóstico, sem dados do cofre.
- `icones/`: cache dos ícones dos sites, só com os ícones online ligados.
- `vozes/`: modelos de voz para leitura em voz alta, só se você baixá-los.

Esses arquivos não fazem parte do repositório e contêm dados sensíveis. A lista
completa, com o que é cifrado e o que não é, está na
[política de privacidade](../PRIVACY.md#1-dados-que-o-aplicativo-guarda-no-seu-computador).

A versão da **Microsoft Store** usa exatamente a mesma pasta: o pacote não
redireciona a gravação para uma pasta privada. Por isso o cofre é o mesmo do
instalador, do portátil e da linha de comando `cofre`, e desinstalar a versão
da Loja não o apaga.

Os **downloads de atualização** ficam numa pasta `CofreDeSenhas-update-*`
dentro da pasta temporária do sistema (`%TEMP%` no Windows, `/tmp` no Linux) até
serem instalados, e são apagados na abertura seguinte do aplicativo. São os
mesmos arquivos públicos das releases, sem nenhum dado seu. A atualização nunca
mexe na pasta de dados.

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
