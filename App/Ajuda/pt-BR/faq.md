# Perguntas frequentes

## Esqueci a senha mestra. E agora?

Não há como recuperá-la — nem por nós, nem por ninguém. É o preço da
cifragem que protege o cofre de terceiros. Se você salvou o **QR code de
backup**, use-o para reimportar a senha. Sem a senha nem o QR, o conteúdo
do cofre fica inacessível. Veja [Senha mestra](senha-mestra).

## Onde ficam os meus dados?

Num arquivo cifrado na pasta de dados do aplicativo, dentro do seu perfil
de usuário do sistema. Nada é enviado para servidores nossos. Detalhes em
[Introdução](introducao).

## Como levo o cofre para outro computador?

Exporte o cofre (arquivo cifrado) ou copie um backup, instale o
aplicativo no destino e importe. Passo a passo em
[Importar e exportar](importar-exportar).

## O cofre sincroniza sozinho na nuvem?

Não. Não existe nuvem nossa. Você pode configurar
[Sincronização por pasta](sincronizacao) ou um
[Banco de dados](banco-de-dados) que **você** controla — aí a
sincronização passa pela sua infraestrutura.

## É seguro? Que cifragem é usada?

AES-256-GCM, com a chave derivada da sua senha mestra a cada abertura e
nunca gravada. A força prática depende sobretudo de você escolher uma
senha mestra boa.

## Perdi o celular com o QR code. E o Windows Hello?

O [Windows Hello](windows-hello) é vinculado à sua conta do Windows neste
computador; o QR é independente. Se ainda consegue abrir o cofre (pela
senha ou pelo Hello), gere um **novo QR code** em
*Configurações → Segurança* e guarde-o.

## Posso usar sem instalar nada além do aplicativo?

Sim. O cofre local não precisa de banco, servidor nem conta. Os recursos
de rede são todos opcionais — veja
[Privacidade e rede](privacidade-rede).
