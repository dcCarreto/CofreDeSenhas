# Windows Hello

Permite **desbloquear o cofre com biometria** (digital, rosto ou PIN do
Windows) em vez de digitar a senha mestra toda vez.

## Como ativar

Em **Configurações → Segurança → Ativar Windows Hello**. O Windows pede a
sua verificação e o aplicativo passa a oferecer o desbloqueio por Hello na
tela de abertura.

## O que muda e o que não muda

- A **senha mestra continua sendo a chave real** do cofre. O Hello apenas
  libera o acesso guardado de forma protegida pelo sistema.
- Operações sensíveis (como alterar a senha mestra) ainda pedem a senha
  mestra.
- Se o Hello falhar ou o dispositivo não suportar, você sempre pode entrar
  com a senha mestra.

## Escopo

A credencial do Hello é vinculada à **sua conta do Windows neste
computador**. Ela não acompanha o arquivo do cofre: em outro computador,
você ativa o Hello de novo (ou usa a senha mestra).

## Desativar

Na mesma tela, *Desativar Windows Hello*. O acesso protegido é removido e o
cofre volta a abrir só pela senha mestra.
