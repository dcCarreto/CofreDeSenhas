# Notas da release

<!--
  Modelo usado por .github/scripts/montar_notas_release.py para montar a nota
  de cada release automaticamente: ele troca "X.Y.Z" pela versão publicada e
  preenche "Destaques desta versão" e "Mudanças" com o trecho correspondente
  do CHANGELOG.md. Editar este arquivo muda o formato de todas as releases
  futuras.
-->

## Destaques desta versão

<!-- Resumo em 2-4 frases do que mudou de mais importante para quem usa o
     programa (não para quem lê código). -->

## Mudanças

<!-- Cole aqui o trecho correspondente do CHANGELOG.md (seção "Adicionado",
     "Corrigido" etc. desta versão). -->

## Capturas de tela

<!-- Se esta versão mudou a interface, atualize/inclua capturas relevantes.
     As capturas atuais do README ficam em docs/. -->

## Downloads

| Sistema | Arquivo | Observação |
| --- | --- | --- |
| Windows | `CofreDeSenhas-Setup-X.Y.Z.exe` | Instalador (recomendado). Não exige administrador. |
| Windows | `CofreDeSenhas-X.Y.Z-win-x64-portatil.exe` | Executável único, sem instalação. |
| Linux | `CofreDeSenhas-X.Y.Z-x86_64.AppImage` | Recomendado. Dê permissão de execução e rode — não exige o SDK do .NET instalado. |
| Linux | `CofreDeSenhas-X.Y.Z-linux-x64.tar.gz` | Extraia e execute `CofreDeSenhas`, ou use `App/distribuicao/instalar.sh` para registrar atalho e ícone. |

## Instalação

Veja a seção [Download e instalação](../README.md#download-e-instalação) do
README para o passo a passo completo em cada sistema.

## Atualizando de uma versão anterior

**Antes de atualizar, faça um backup do cofre** (menu de configurações →
"Backup e restauração..." → "Fazer backup agora", ou copie manualmente a
pasta `%APPDATA%\GerenciadorSenhas` no Windows / `~/.config/GerenciadorSenhas`
no Linux). Instalar por cima de uma versão anterior preserva o cofre
existente, mas ter um backup independente é sempre mais seguro antes de
qualquer atualização.

## Verificando a integridade e a procedência dos arquivos

Todo arquivo desta release tem seu hash SHA256 listado em `CHECKSUMS.txt`,
também anexado a esta release. Para conferir o hash:

- Windows (PowerShell): `Get-FileHash .\arquivo-baixado -Algorithm SHA256`
- Linux/macOS: `sha256sum -c CHECKSUMS.txt` (executado na pasta onde os
  arquivos foram baixados)

Além do hash, a release traz outras camadas de verificação, todas descritas no
[SECURITY.md](../SECURITY.md#confiança-nos-binários-e-verificação-dos-downloads):

- `CHECKSUMS.txt.sig` — assinatura RSA-4096 destacada do `CHECKSUMS.txt`, a
  mesma chave que o atualizador embutido exige (`openssl dgst -verify`).
- `CHECKSUMS.txt.asc` — assinatura GPG destacada do `CHECKSUMS.txt`
  (`gpg --verify`); a chave pública e a impressão digital estão no `SECURITY.md`.
- *Attestation* de proveniência SLSA/Sigstore por artefato
  (`gh attestation verify <arquivo> --repo dcCarreto/CofreDeSenhas`).
- Relatório do VirusTotal dos executáveis do Windows, com os links anexados ao
  final desta release.

Os executáveis ainda não têm assinatura de código do Windows (Authenticode
exige um certificado pago, avaliado como item futuro do roadmap); as camadas
acima são a forma de confirmar que o arquivo baixado é exatamente o que foi
publicado nesta release, sem alteração no caminho.
