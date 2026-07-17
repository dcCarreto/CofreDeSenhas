# Notas da release

<!--
  Modelo de descrição da release. Esta release foi criada como rascunho pelo
  workflow "Release" — revise e complete os itens abaixo antes de publicar.
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

## Verificando a integridade dos arquivos

Todo arquivo desta release tem seu hash SHA256 listado em `CHECKSUMS.txt`,
também anexado a esta release. Para conferir:

- Windows (PowerShell): `Get-FileHash .\arquivo-baixado -Algorithm SHA256`
- Linux/macOS: `sha256sum -c CHECKSUMS.txt` (executado na pasta onde os
  arquivos foram baixados)

Compare o valor obtido com o que consta em `CHECKSUMS.txt`. Os arquivos ainda
não são assinados digitalmente (assinatura de código no Windows exige um
certificado pago e está avaliada como item futuro do roadmap); o hash SHA256
é, por enquanto, a forma de confirmar que o arquivo baixado é exatamente o
que foi publicado nesta release, sem alteração no caminho.
