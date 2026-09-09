# Banco de dados compartilhado

Um recurso avançado para quem quer manter o cofre num **banco de dados que
você hospeda** (SQLite em rede, PostgreSQL ou MySQL), em vez de um arquivo
local.

## Para quem é

- Times ou famílias que compartilham um conjunto de credenciais.
- Quem já tem um servidor de banco e prefere centralizar ali.

Para sincronizar apenas os **seus próprios** dispositivos, a
[Sincronização por pasta](sincronizacao) costuma ser mais simples.

## Como funciona

- Você fornece o endereço e as credenciais do banco. A senha do servidor é
  guardada cifrada no cofre local.
- Os dados continuam cifrados com a sua senha mestra **antes** de irem
  para o banco — o servidor do banco nunca vê senhas em claro.
- O mesmo motor de mesclagem da sincronização (edição mais recente vence,
  conflitos vão para uma tela de decisão) mantém os dispositivos
  alinhados.

## Conectar e desconectar

Em **Configurações → Backup e sincronização**. Ao desconectar, o cofre
volta a operar só com a cópia local.

> É a sua responsabilidade proteger o servidor do banco (acesso, rede,
> backups). O aplicativo cuida da cifragem do conteúdo, não da segurança
> da sua infraestrutura.
