#!/usr/bin/env bash
# Abre no microsoft/winget-pkgs o PR com os manifests de uma versão já publicada.
#
# Uso: GH_TOKEN=<token> enviar_winget.sh <versao> <pasta_dos_manifests>
#
# <pasta_dos_manifests> é a saída de gerar_manifesto_winget.py. Tudo pela API do
# GitHub (sem clonar o winget-pkgs, que tem gigabytes): cria a branch no fork,
# grava os três YAML e abre o PR. O token precisa poder escrever no fork e abrir
# PR no repositório da Microsoft (PAT clássico só com public_repo;
# token fine-grained não abre PR em repositório de outra conta).
set -euo pipefail

versao="$1"
pasta="$2"
upstream="microsoft/winget-pkgs"
fork="dcCarreto/winget-pkgs"
pacote="dcCarreto.CofreDeSenhas"
branch="$pacote-$versao"
destino="manifests/d/dcCarreto/CofreDeSenhas/$versao"

if [ -n "$(gh pr list -R "$upstream" --head "$branch" --state all --json number -q '.[].number')" ]; then
  echo "Já existe PR no winget-pkgs para $versao — nada a fazer."
  exit 0
fi

# A branch nasce direto do master da Microsoft (o fork compartilha os objetos com
# ele). Sincronizar o master do fork exigiria o escopo workflow sempre que a
# Microsoft mexe nos workflows dela, e o GitHub não deixa marcar workflow sem repo.
base=$(gh api "repos/$upstream/git/ref/heads/master" -q .object.sha)

# Sobra de uma tentativa anterior que falhou no meio: recomeça do zero.
gh api -X DELETE "repos/$fork/git/refs/heads/$branch" > /dev/null 2>&1 || true
gh api -X POST "repos/$fork/git/refs" -f ref="refs/heads/$branch" -f sha="$base" > /dev/null

for arquivo in "$pasta/$destino"/*.yaml; do
  nome=$(basename "$arquivo")
  gh api -X PUT "repos/$fork/contents/$destino/$nome" \
    -f message="New version: $pacote version $versao" \
    -f branch="$branch" \
    -f content="$(base64 -w0 "$arquivo")" > /dev/null
  echo "Enviado $destino/$nome"
done

corpo="## Description

New version of **Cofre de Senhas** ($versao). Release: https://github.com/dcCarreto/CofreDeSenhas/releases/tag/v$versao

## Checklist

- [x] Signed the [Contributor License Agreement](https://cla.opensource.microsoft.com)
- [ ] Linked to an issue (if applicable)

## Manifest Checklist

- [x] Checked that there aren't other open pull requests for the same manifest update/change
- [x] This PR only modifies one (1) manifest
- [ ] Validated manifest locally with \`winget validate --manifest <path>\`
- [ ] Tested manifest locally with \`winget install --manifest <path>\`
- [x] Manifest conforms to the schema declared in the files (1.28.0)"

gh pr create -R "$upstream" --base master --head "dcCarreto:$branch" \
  --title "New version: $pacote version $versao" --body "$corpo"
