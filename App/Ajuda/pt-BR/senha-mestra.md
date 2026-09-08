# Senha mestra

A senha mestra é a única chave do cofre. A partir dela o aplicativo
deriva, em memória, a chave que decifra seus dados. Ela **não fica
gravada em lugar nenhum**.

## Não dá para recuperar

Não existe "esqueci minha senha". Se você perder a senha mestra, o
conteúdo do cofre fica inacessível — é assim que a cifragem protege você
de terceiros. Por isso:

- escolha uma **frase longa** (várias palavras), fácil de lembrar e
  difícil de adivinhar;
- salve o **QR code de backup** (menu *Segurança → Regerar QR code*) e
  guarde-o fora do computador;
- opcionalmente, mantenha uma cópia escrita em local físico seguro.

## Alterar a senha mestra

Em **Configurações → Segurança → Alterar senha mestra**. O cofre inteiro é
recifrado com a chave nova. Se você usa banco de dados ou
[Sincronização por pasta](sincronizacao), a troca afeta os outros
dispositivos — releia a tela de confirmação antes de continuar.

> Depois que a troca reporta sucesso, o aplicativo se reinicia sozinho.
> Não mexa no cofre nesse intervalo.

## QR code de backup

É a sua senha mestra codificada em imagem, para reimportar caso você
esqueça a digitada. Trate o QR com o mesmo cuidado da senha: quem o
tiver, abre o seu cofre.

## Windows Hello

O [Windows Hello](windows-hello) permite desbloquear com biometria em vez
de digitar a senha mestra — mas a senha mestra continua sendo a chave real
e ainda é necessária para operações sensíveis.
