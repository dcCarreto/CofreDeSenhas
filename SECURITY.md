# Política de segurança

## Versões suportadas

O Cofre de Senhas segue lançamentos únicos, sem branches de manutenção paralelas:
correções de segurança são aplicadas somente à versão mais recente, disponível nas
[releases do GitHub](https://github.com/dcCarreto/CofreDeSenhas/releases). O próprio
aplicativo pode avisar sobre novas versões pelo menu de configurações
("Verificar atualizações"), recurso opcional e desligado por padrão.

## Confiança nos binários e verificação dos downloads

Os binários das releases são compilados pelo GitHub Actions a partir deste
repositório e publicados por Denis Cristino Cantagallo Carreto, autor e
responsável pelo projeto. Eles **não são assinados com um certificado de
assinatura de código pago** — este é um projeto pessoal, gratuito e sem fins
comerciais, e um certificado desses tem custo anual e verificação por uma
autoridade certificadora. Na prática:

- **Windows**: ao executar o instalador ou o portátil pela primeira vez, o
  SmartScreen pode mostrar "O Windows protegeu o computador" e o controle de
  conta de usuário pode exibir "Editor: desconhecido". Para prosseguir, clique
  em "Mais informações" e depois em "Executar assim mesmo". A reputação do
  SmartScreen tende a se acumular sozinha conforme mais gente baixa a mesma
  versão.
- **Linux**: o `.AppImage` precisa de permissão de execução (`chmod +x`) e não
  há, por ora, pacote assinado em repositório de distribuição nem loja.

Em vez de confiar no aviso do sistema, dá para verificar a autenticidade do que
foi baixado. Toda release traz, além dos binários:

- `CHECKSUMS.txt` — hash SHA-256 de cada artefato.
- `CHECKSUMS.txt.sig` — assinatura RSA-4096 destacada do `CHECKSUMS.txt`, a
  mesma chave que o atualizador embutido exige antes de aplicar qualquer
  atualização. A chave pública correspondente está versionada no repositório em
  [`update-signing-public.pem`](update-signing-public.pem).
- `CHECKSUMS.txt.asc` — assinatura GPG destacada do `CHECKSUMS.txt`. A chave
  pública está versionada em
  [`update-signing-gpg-public.asc`](update-signing-gpg-public.asc); a impressão
  digital é `A5C8 5888 D40B 03F8 A91E  38F2 9366 CF1E 74DE 2C7D`.

Conferir o hash e as assinaturas:

```sh
# 1. o binário bate com o hash publicado
sha256sum -c CHECKSUMS.txt --ignore-missing

# 2. o CHECKSUMS.txt foi assinado pela chave RSA do projeto
openssl dgst -sha256 -verify update-signing-public.pem \
  -signature CHECKSUMS.txt.sig CHECKSUMS.txt

# 3. alternativa: conferir a assinatura GPG
gpg --import update-signing-gpg-public.asc
gpg --verify CHECKSUMS.txt.asc CHECKSUMS.txt
```

No Windows, o passo 1 equivale a `Get-FileHash <arquivo> -Algorithm SHA256` e
comparar com a linha correspondente do `CHECKSUMS.txt`.

Cada artefato também tem uma *attestation* de proveniência (SLSA/Sigstore) que
prova que ele saiu deste workflow, a partir de um commit específico:

```sh
gh attestation verify <arquivo> --repo dcCarreto/CofreDeSenhas
```

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
