# Atualizações

O aplicativo **se atualiza sozinho**. Você não precisa baixar nem clicar em
nada.

## Como funciona

1. Ao abrir o cofre, e depois a cada 6 horas, o aplicativo consulta a página
   de lançamentos do projeto.
2. Havendo versão nova, ela é baixada em segundo plano. Antes de aceitá-la, o
   aplicativo confere a assinatura digital e o SHA256 do arquivo; se algo não
   conferir, a atualização é descartada.
3. Com a atualização pronta, o rodapé mostra **Atualização pronta: será
   instalada quando o cofre for bloqueado**.
4. No próximo bloqueio do cofre (pelo item *Bloquear agora* ou por
   inatividade), o aplicativo fecha, instala a versão nova e abre de novo, já
   na tela de desbloqueio. É só digitar a senha mestra, como sempre.

Para instalar na hora, use **Instalar agora** no rodapé: ele mostra o que muda
na versão e pede confirmação antes de fechar o aplicativo. O **X** ao lado só
esconde o aviso; a atualização continua marcada para o próximo bloqueio.

Nenhum dado do cofre é enviado. A consulta e o download são leituras públicas,
sem nada que identifique você ou o computador. Veja também
[Privacidade e rede](privacidade-rede).

## Sempre ligada

Não há opção para desligar: é assim que as correções, inclusive as de
segurança, chegam a todo mundo sem depender de ninguém lembrar de atualizar.

## Conforme a forma de instalação

- **Instalador ou winget**: como descrito acima. Quem instalou "para todos os
  usuários" (em Arquivos de Programas) precisa clicar em **Instalar agora**,
  porque o Windows pede permissão de administrador; o aviso do rodapé avisa
  quando for esse o caso.
- **Executável portátil (descontinuado)**: na próxima atualização, o
  aplicativo é instalado só para o seu usuário, sem pedir administrador, com o
  mesmo cofre e as mesmas preferências. Depois disso, o arquivo portátil só
  abre a versão instalada e pode ser apagado.
- **Microsoft Store**: quem atualiza é a Loja, conforme a configuração de
  atualização de aplicativos dela. O aplicativo baixa a atualização assim que
  a Loja a oferece e a instala no bloqueio do cofre.
- **Linux, AppImage**: como no Windows; o arquivo `.AppImage` é trocado pelo
  novo.
- **Linux, compilado do código-fonte**: não se atualiza sozinho. Atualize o
  código e compile de novo.

A linha de comando `cofre` e o gerador de senhas avulso
(`GeradorDeSenhas-*-win-x64.exe`) são arquivos à parte e **não** se atualizam
sozinhos. Baixe as versões novas na página de lançamentos quando quiser.

## Seus dados durante a atualização

O cofre, os backups e as preferências não são tocados: ficam na pasta de dados
do aplicativo, e a versão nova abre o mesmo cofre. Desinstalar o aplicativo,
inclusive a versão da Loja, também não apaga o cofre. O arquivo baixado fica na
pasta temporária do sistema até ser instalado e é apagado na abertura seguinte
do aplicativo. Para se precaver, mantenha os [backups](backup) em dia.
