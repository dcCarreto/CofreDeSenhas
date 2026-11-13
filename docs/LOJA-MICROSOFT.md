# Microsoft Store

O Cofre de Senhas vai para a Microsoft Store como pacote **MSIX**, gerado pelo
`App/distribuicao/gerar-msix.ps1` a cada release. O pacote sai sem assinatura:
a Loja assina com o certificado dela, então não é preciso comprar certificado de
assinatura de código.

## O que muda na versão da Loja

- **Atualizações vêm da Loja.** O app detecta que está empacotado
  (`PacoteLoja.Empacotado`), não consulta o GitHub e esconde a opção
  "Atualizar automaticamente" nas Configurações — a política da Loja proíbe
  app empacotado se atualizar por fora. Em vez de esperar o ritmo da Loja (que
  prefere o app fechado, e um cofre fica aberto na bandeja o dia todo), o app
  usa as APIs da Loja (`AtualizacaoLoja`): baixa a atualização assim que ela é
  oferecida e pede a instalação no próximo bloqueio do cofre; o Windows fecha e
  reabre o app. Se o usuário desligou as atualizações automáticas na Loja, nada
  acontece.
- **O cofre fica no mesmo lugar** (`%APPDATA%\GerenciadorSenhas`). Por padrão
  um app MSIX grava em uma cópia privada dessa pasta, que o Windows apaga ao
  desinstalar; o manifesto desliga esse desvio (`FileSystemWriteVirtualization`)
  para que desinstalar nunca apague o cofre e o CLI `cofre` continue enxergando
  os mesmos dados. Instalar pela Loja quem já usa o instalador ou o portátil
  reaproveita o cofre existente.
- **Versão:** o carimbo `aaaa.M.d[.N]` vira `aaaa.MMDD.N.0` no pacote (a Loja
  exige quatro partes e a última zero). Ex.: `2026.11.12.1` → `2026.1112.1.0`.

## Primeira publicação (manual, uma vez só)

1. **Conta de desenvolvedor.** Em <https://storedeveloper.microsoft.com>, crie
   uma conta individual (gratuita) com a sua conta Microsoft. A verificação de
   identidade pode levar alguns dias.
2. **Reservar o nome.** No [Partner Center](https://partner.microsoft.com/dashboard/apps-and-games/overview),
   *Apps e jogos → Novo produto → Aplicativo MSIX ou PWA*. "Cofre de Senhas"
   já estava reservado por outra conta; o nome reservado foi **"Cofre de Senhas
   - Gerenciador de Senhas"** (ID da Loja `9ND5J5DZBH90`), e é o que vai em
   `Package/Properties/DisplayName` no manifesto. No menu Iniciar o app continua
   aparecendo como "Cofre de Senhas".
3. **Identidade do pacote.** Em *Gerenciamento do produto → Identidade do
   produto*, copie os três valores para variáveis do repositório
   (*Settings → Secrets and variables → Actions → Variables*):

   | Variável | Campo do Partner Center |
   |---|---|
   | `LOJA_IDENTIDADE_NOME` | Package/Identity/Name |
   | `LOJA_EDITOR` | Package/Identity/Publisher (começa com `CN=`) |
   | `LOJA_NOME_EDITOR` | Package/Properties/PublisherDisplayName |

   A partir da próxima release, o artefato `pacote-loja-microsoft` do workflow
   *Release* já sai com a identidade certa. Para gerar na hora, sem esperar uma
   release:

   ```powershell
   .\App\distribuicao\gerar-msix.ps1 -NomeIdentidade "<Name>" -Editor "CN=<...>" -NomeEditor "<PublisherDisplayName>"
   ```

4. **Envio.** Em *Envios → Iniciar envio*, preencha:
   - **Preços e disponibilidade:** gratuito, todos os mercados, visibilidade
     pública.
   - **Propriedades:**
     - Categoria *Segurança*.
     - Política de privacidade:
       `https://github.com/dcCarreto/CofreDeSenhas/blob/prod/PRIVACY.md`.
     - Site: `https://github.com/dcCarreto/CofreDeSenhas`.
     - Contato de suporte: `https://github.com/dcCarreto/CofreDeSenhas/issues`.
     - "Este produto acessa, coleta ou transmite informações pessoais?":
       **Sim**. Os nomes dos serviços vão ao Google se os ícones online forem
       ligados, e os prefixos de hash vão ao Have I Been Pwned. A PRIVACY.md
       descreve tudo; responder "Sim" é o que exige a política, e ela já existe.
     - Declarações do produto: marque **"Este produto foi testado para atender
       às diretrizes de acessibilidade"** (leitor de tela, alto contraste,
       paletas para daltonismo, escala de texto e navegação por teclado; ver a
       seção Interface de [FUNCIONALIDADES.md](FUNCIONALIDADES.md)). Deixe
       marcadas as opções padrão de instalar em outra unidade e de backup pelo
       Windows.
     - Requisitos do sistema: teclado e mouse; Windows 10 versão 2004 (19041)
       ou superior, x64.
   - **Classificação etária:** responda o questionário IARC (não há conteúdo
     sensível nem interação entre usuários; o resultado esperado é "Livre/3+").
   - **Pacotes:** envie o `.msix`.
   - **Páginas da Loja:** uma em **português (Brasil)** e uma em **inglês
     (Estados Unidos)**, os dois idiomas que o manifesto declara. Textos e
     palavras de busca abaixo. Capturas: as de `docs/*.png` servem (mínimo
     1366×768).
   - **Notas para a certificação:** cole o texto de [Notas para a
     certificação](#notas-para-a-certificação), abaixo.
5. A certificação costuma levar de algumas horas a 3 dias úteis.
6. Com o app no ar, a documentação aponta para a página da Loja:
   <https://apps.microsoft.com/detail/9ND5J5DZBH90>.

### Notas para a certificação

> Não é preciso conta nem login: na primeira abertura, crie um cofre com
> qualquer senha mestra (mínimo de 8 caracteres) e recuse as ofertas de QR de
> backup, chave de recuperação e Windows Hello. O app funciona totalmente
> offline; o acesso à rede é opcional (ícones dos serviços e verificação de
> senhas vazadas) e descrito na política de privacidade. Na versão da Loja, o
> atualizador próprio do app fica desligado: as atualizações vêm da Microsoft
> Store (APIs Windows.Services.Store).
>
> Capacidades restritas: runFullTrust, porque é um aplicativo de área de
> trabalho Win32 (.NET/Avalonia), não UWP. unvirtualizedResources, porque o
> aplicativo guarda o cofre de senhas criptografado do usuário em
> %APPDATA%\GerenciadorSenhas. Com a virtualização de gravação ativada, essa
> pasta seria apagada ao desinstalar o aplicativo, levando junto todas as
> senhas do usuário sem aviso. A mesma pasta é usada pela ferramenta de linha
> de comando do projeto e pelas versões instalador/portátil, para que o usuário
> possa migrar entre elas sem perder dados. Fora dessa pasta, o aplicativo só
> grava onde o usuário escolhe (exportações e cópias de backup) e em arquivos
> temporários.

### Textos da página da Loja

**Português (Brasil)**

- *Descrição curta:* Gerenciador de senhas local, gratuito e de código aberto,
  com criptografia AES-256-GCM.
- *Descrição:*

  > O Cofre de Senhas guarda suas senhas em um cofre criptografado no seu próprio
  > computador, protegido por uma senha mestra. Suas senhas nunca saem do
  > computador sem criptografia, e não há conta nem servidor do projeto: a
  > sincronização entre dispositivos é opcional e usa uma pasta ou um banco de
  > dados escolhido por você.
  >
  > • Cofre local com criptografia AES-256-GCM
  > • Gerador de senhas integrado, com indicador de força
  > • Relatório de segurança: senhas fracas, repetidas e antigas
  > • Histórico de senhas por credencial
  > • Backup e recuperação do cofre
  > • Desbloqueio com Windows Hello
  > • Tema claro e escuro, alto contraste, paletas para daltonismo e leitor de tela
  > • Interface em português, inglês, espanhol, francês, italiano e alemão
  >
  > Gratuito e de código aberto (licença MIT):
  > https://github.com/dcCarreto/CofreDeSenhas
- *Palavras de busca:* gerenciador de senhas, cofre de senhas, gerador de
  senhas, senhas, criptografia, código aberto, offline

**English (United States)**

- *Short description:* Free, open-source local password manager with AES-256-GCM
  encryption.
- *Description:*

  > Cofre de Senhas keeps your passwords in an encrypted vault on your own
  > computer, protected by a master password. Your passwords never leave the
  > computer unencrypted, and there is no account or project server: syncing
  > between devices is optional and uses a folder or a database of your choice.
  >
  > • Local vault encrypted with AES-256-GCM
  > • Built-in password generator with strength meter
  > • Security report: weak, reused and old passwords
  > • Per-credential password history
  > • Vault backup and recovery
  > • Windows Hello unlock
  > • Light and dark themes, high contrast, color-blind palettes and screen reader support
  > • Interface in Portuguese, English, Spanish, French, Italian and German
  >
  > Free and open source (MIT license):
  > https://github.com/dcCarreto/CofreDeSenhas
- *Search terms:* password manager, password vault, password generator,
  passwords, encryption, open source, offline

## Envio automático das próximas atualizações

Depois que a primeira versão for certificada, o job `publicar-loja` do workflow
*Release* envia cada nova release direto para a certificação. Para ligar:

1. No Partner Center, *Configurações da conta → Gerenciamento de usuários →
   Aplicativos do Microsoft Entra*, crie (ou associe) um aplicativo com a função
   **Gerente** e gere uma chave.
2. Configure no repositório:

   | Nome | Tipo | De onde vem |
   |---|---|---|
   | `LOJA_TENANT_ID` | secret | ID do locatário (tenant) do aplicativo Entra |
   | `LOJA_CLIENT_ID` | secret | ID do cliente do aplicativo Entra |
   | `LOJA_CLIENT_SECRET` | secret | a chave gerada no passo 1 |
   | `LOJA_SELLER_ID` | secret | *Configurações da conta → Configurações legais → ID do vendedor* |
   | `LOJA_ID_PRODUTO` | variável | *Identidade do produto → ID da Loja* (ex.: `9N...`) |

Sem `LOJA_ID_PRODUTO` o job nem roda; com ela e faltando algum dos outros
valores, ele falha dizendo qual falta. A página da Loja (textos, capturas) não é
tocada pelo envio automático — só o pacote.

## Testar o pacote localmente

O `.msix` gerado não é assinado, então não instala com dois cliques. Para rodar
o app empacotado antes de enviar, ative o *Modo de Desenvolvedor*
(*Configurações → Sistema → Para desenvolvedores*) e registre a pasta montada
pelo script:

```powershell
.\App\distribuicao\gerar-msix.ps1
Add-AppxPackage -Register .\publish-msix\pacote\AppxManifest.xml
```

O app aparece no menu Iniciar como "Cofre de Senhas". **Atenção:** é um build de
Release e usa o cofre real em `%APPDATA%\GerenciadorSenhas`. Para remover:

```powershell
Get-AppxPackage CofreDeSenhas.Local | Remove-AppxPackage
```
