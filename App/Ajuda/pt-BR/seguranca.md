# Relatório de segurança e auditoria

O aplicativo avalia a saúde do cofre sem que nada saia do computador
(exceto a verificação de vazamentos, descrita abaixo).

## Relatório de segurança

Dá uma **pontuação geral** e lista os problemas por tipo:

- **Senhas fracas**: curtas ou previsíveis. Troque pelo
  [Gerador de senhas](gerador).
- **Senhas repetidas**: a mesma senha em serviços diferentes. O maior
  risco prático — corrija primeiro estas.
- **Senhas antigas**: sem troca há muito tempo, quando o registro de uso
  está ativo.

Clicar num item filtra a lista principal para as senhas afetadas.

## Auditoria do cofre

Uma varredura mais detalhada, credencial por credencial, com o motivo de
cada apontamento. Útil para uma revisão completa de tempos em tempos.

## Verificação de vazamentos

Compara suas senhas com bases públicas de vazamentos conhecidos usando
**k-anonymity**: só um trecho do código de verificação (hash) da senha é
enviado, nunca a senha nem o serviço. Se aparecer uma correspondência,
troque a senha assim que possível.

## Histórico de pontuação

Se você mantém o registro de uso ligado, o aplicativo guarda a evolução da
pontuação ao longo do tempo, para você ver se o cofre está melhorando.
