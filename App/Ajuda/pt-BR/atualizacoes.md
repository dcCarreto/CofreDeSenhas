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

## Como desligar

Em *Configurações → Comportamento → Atualizar automaticamente*. Desligado, o
aplicativo não consulta nada, e uma atualização já baixada é descartada. Para
atualizar à mão, baixe a versão nova na página de lançamentos do projeto.

## Conforme a forma de instalação

- **Instalador ou winget**: como descrito acima. Quem instalou "para todos os
  usuários" (em Arquivos de Programas) precisa clicar em **Instalar agora**,
  porque o Windows pede permissão de administrador; o aviso do rodapé avisa
  quando for esse o caso.
- **Executável portátil**: o próprio arquivo é trocado pela versão nova. Ele
  precisa estar numa pasta em que você possa gravar, como Downloads ou a área
  de trabalho.
- **Microsoft Store**: quem atualiza é a Loja, conforme a configuração de
  atualização de aplicativos dela. O aplicativo baixa a atualização assim que
  a Loja a oferece e a instala no bloqueio do cofre. A opção "Atualizar
  automaticamente" não aparece.
- **Linux, AppImage**: como no Windows; o arquivo `.AppImage` é trocado pelo
  novo.
- **Linux, pacote `.tar.gz` ou compilado do código-fonte**: não se atualiza
  sozinho. Baixe a versão nova na página de lançamentos.

A linha de comando `cofre` e o gerador de senhas avulso
(`GeradorDeSenhas-*-portatil.exe`) são arquivos à parte e **não** se atualizam
sozinhos. Baixe as versões novas na página de lançamentos quando quiser.

## Seus dados durante a atualização

O cofre, os backups e as preferências não são tocados: ficam na pasta de dados
do aplicativo, e a versão nova abre o mesmo cofre. Desinstalar o aplicativo,
inclusive a versão da Loja, também não apaga o cofre. O arquivo baixado fica na
pasta temporária do sistema até ser instalado e é apagado na abertura seguinte
do aplicativo. Para se precaver, mantenha os [backups](backup) em dia.
