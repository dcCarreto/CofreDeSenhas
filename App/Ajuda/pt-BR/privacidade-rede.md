# Privacidade e rede

Por padrão, o aplicativo só usa a internet para procurar e baixar as
própias atualizações, sem enviar nenhum dado seu (veja
[Atualizações](atualizacoes)); dá para desligar. Os demais recursos abaixo
são opcionais e você liga cada um sabendo o que ele faz.

## Ícones online dos serviços

Quando ligado, busca o ícone real de cada site num serviço público de
ícones, enviando **apenas o domínio** (por exemplo `github.com`). Nenhuma
senha, usuário ou nota sai do computador. Os ícones baixados ficam em
cache no disco. Desligado, o cofre mostra só as iniciais e não toca na
rede.

## Atualizar automaticamente

Ligado por padrão. Consulta a página de lançamentos do projeto ao abrir o
cofre e a cada 6 horas; havendo versão nova, baixa em segundo plano, confere
a assinatura e o SHA256 e instala quando o cofre é bloqueado. O aplicativo
fecha e abre de novo já atualizado, na tela de desbloqueio. Para instalar na
hora, use **Instalar agora** no rodapé. Nenhum dado do cofre é enviado.
Instalado pela Microsoft Store, quem atualiza é a própria Loja e a opção não
aparece.

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
