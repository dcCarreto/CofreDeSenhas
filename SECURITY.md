# Política de segurança

## Versões suportadas

O Cofre de Senhas segue lançamentos únicos, sem branches de manutenção paralelas:
correções de segurança são aplicadas somente à versão mais recente, disponível nas
[releases do GitHub](https://github.com/dcCarreto/CofreDeSenhas/releases). O próprio
aplicativo pode avisar sobre novas versões pelo menu de configurações
("Verificar atualizações"), recurso opcional e desligado por padrão.

## Como reportar uma vulnerabilidade

**Não abra uma issue pública** para relatar uma vulnerabilidade — issues são públicas
por padrão, o que exporia o problema antes de existir uma correção.

Use o aviso de segurança privado do GitHub: na aba "Security" do repositório,
["Report a vulnerability"](https://github.com/dcCarreto/CofreDeSenhas/security/advisories/new).
Ele abre uma conversa privada entre quem reporta e o mantenedor, sem tornar o problema
público até haver uma correção.

Ao reportar, inclua o quanto conseguir:

- Versão do aplicativo e sistema operacional (Windows ou Linux).
- Passos para reproduzir o problema.
- Impacto esperado — o que um atacante conseguiria fazer.
- Prova de conceito, se houver, sem incluir dados reais de nenhum cofre.

## O que esperar

Este projeto é mantido por uma pessoa, no tempo livre — não há equipe de segurança
dedicada nem um prazo de resposta contratual. O esforço é responder e avaliar o relato o
quanto antes, e manter quem reportou informado enquanto uma correção é preparada.

## Escopo

Cobertos por esta política: o aplicativo desktop (`App` e `GerenciadorDeSenhas`, incluindo
criptografia, sincronização e persistência), os scripts de empacotamento/instalação em
`App/distribuicao/` e os workflows de CI/release em `.github/workflows/`.

Fora do escopo direto:

- Vulnerabilidades em dependências de terceiros (pacotes NuGet, o próprio .NET) — reporte
  diretamente ao projeto afetado. Se o cofre usa a dependência de um jeito que agrava o
  problema, isso sim é relevante aqui.
- A extensão de navegador, ainda em desenvolvimento em branch separada e não distribuída —
  trate como código experimental por enquanto.
- Engenharia social, phishing ou comprometimento prévio do sistema operacional do usuário
  (keylogger, malware já em execução com o cofre desbloqueado). Veja o
  [modelo de ameaça](THREAT_MODEL.md) para mais detalhes sobre o que o desenho de
  segurança do cofre cobre e o que fica deliberadamente fora dele.

## Reconhecimento

Relatos responsáveis que resultem em correção podem ser creditados nas notas da release
correspondente, se a pessoa que reportou topar.
