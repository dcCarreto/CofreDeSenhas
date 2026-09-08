# Introdução

O **Cofre de Senhas** guarda suas credenciais cifradas no seu próprio
computador. Não há conta, servidor nem nuvem obrigatória: o cofre é um
arquivo no seu disco, aberto apenas pela sua senha mestra.

## Como seus dados ficam protegidos

- Tudo é cifrado com **AES-256-GCM**. A chave nunca é gravada: ela é
  derivada da sua senha mestra toda vez que você abre o cofre.
- Sem a senha mestra, o arquivo do cofre é ilegível — inclusive para quem
  tem acesso ao seu computador.
- Nada sai do seu computador por conta própria. Os poucos recursos que
  usam a rede são opcionais e estão descritos em
  [Privacidade e rede](privacidade-rede).

## Onde o cofre fica

O cofre e as preferências ficam na pasta de dados do aplicativo, no seu
perfil de usuário do sistema. Para levar tudo para outro computador, veja
[Importar e exportar](importar-exportar) e [Backup e restauração](backup).

## Por onde começar

Se é a primeira vez, siga [Primeiros passos](primeiros-passos). A peça
central é a [Senha mestra](senha-mestra) — escolha uma boa e não a perca.
