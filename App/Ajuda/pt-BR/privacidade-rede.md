# Privacidade e rede

Por padrão, o aplicativo **não acessa a internet**. Os recursos abaixo são
opcionais e você liga cada um sabendo o que ele faz.

## Ícones online dos serviços

Quando ligado, busca o ícone real de cada site num serviço público de
ícones, enviando **apenas o domínio** (por exemplo `github.com`). Nenhuma
senha, usuário ou nota sai do computador. Os ícones baixados ficam em
cache no disco. Desligado, o cofre mostra só as iniciais e não toca na
rede.

## Verificar atualizações

Quando ligado, consulta a página de lançamentos do projeto para avisar se
há uma versão mais nova. Nenhum download é feito automaticamente, e nada
além dessa consulta é enviado. Desligado por padrão.

## Verificação de vazamentos

Sob demanda, compara suas senhas com bases públicas de vazamentos usando
**k-anonymity**: só um prefixo do hash da senha é enviado. Veja
[Relatório de segurança](seguranca).

## Sincronização e banco de dados

Se você configurar [Sincronização por pasta](sincronizacao) ou
[Banco de dados](banco-de-dados), o tráfego vai para a **sua** pasta ou o
**seu** servidor — nunca para um serviço nosso.

## Modo privacidade

O botão de olho na barra de título embaça os valores sensíveis na tela,
para você abrir o cofre perto de outras pessoas sem expor nada.
