# Senha mestra

A senha mestra é a única chave do cofre. A partir dela o aplicativo
deriva, em memória, a chave que decifra seus dados. Ela **não fica
gravada em lugar nenhum**.

## Sem chave de recuperação, não dá para recuperar

Por padrão, não existe "esqueci minha senha". Se você perder a senha
mestra sem ter ativado a **chave de recuperação** (veja abaixo), o
conteúdo do cofre fica inacessível — é assim que a cifragem protege você
de terceiros. Por isso:

- escolha uma **frase longa** (várias palavras), fácil de lembrar e
  difícil de adivinhar;
- salve o **QR code de backup** (menu *Segurança → Regerar QR code*) e
  guarde-o fora do computador;
- ative a **chave de recuperação** como uma segunda rede de segurança;
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

## Chave de recuperação

Em *Configurações → Segurança → Chave de recuperação*, ou já na criação do
cofre. Gera um segredo de 160 bits (em grupos de 4 caracteres) que permite
redefinir a senha mestra sem saber a antiga — quem tiver esse segredo
consegue. Por isso anote-o e guarde-o **longe do computador**, com o
mesmo cuidado da própria senha mestra: ele vale tanto quanto ela.

Esqueceu a senha mestra e tinha ativado a chave? Na tela de entrada,
clique em "Esqueci a senha mestra", digite o segredo anotado e escolha
uma senha nova — o cofre é re-cifrado com ela. Um segredo novo é gerado a
cada recuperação e a cada troca de senha mestra; anote sempre o mais
recente, o anterior deixa de valer. Ativar, gerar de novo ou desativar
sempre exige a senha mestra atual, mesmo com o cofre já desbloqueado.

## Windows Hello

O [Windows Hello](windows-hello) permite desbloquear com biometria em vez
de digitar a senha mestra — mas a senha mestra continua sendo a chave real
e ainda é necessária para operações sensíveis.
