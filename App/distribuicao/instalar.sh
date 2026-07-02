#!/usr/bin/env bash
# Publica e instala o Cofre de Senhas para o usuário atual (sem sudo).
set -euo pipefail

raiz="$(cd "$(dirname "$0")/../.." && pwd)"
destino="$HOME/.local/opt/cofre-de-senhas"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "Erro: o SDK do .NET não foi encontrado. Instale-o em https://dotnet.microsoft.com/download" >&2
    exit 1
fi

echo "Publicando o aplicativo (linux-x64, autocontido)..."
dotnet publish "$raiz/App/App.csproj" -c Release -r linux-x64 --self-contained -o "$destino"

echo "Instalando ícone e atalho..."
mkdir -p "$HOME/.local/share/applications" "$HOME/.local/share/icons/hicolor/128x128/apps"
cp "$raiz/App/Ativos/app.png" "$HOME/.local/share/icons/hicolor/128x128/apps/cofre-de-senhas.png"
sed "s|^Exec=.*|Exec=$destino/CofreDeSenhas|" "$raiz/App/distribuicao/cofre-de-senhas.desktop" \
    > "$HOME/.local/share/applications/cofre-de-senhas.desktop"

update-desktop-database "$HOME/.local/share/applications" 2>/dev/null || true
gtk-update-icon-cache "$HOME/.local/share/icons/hicolor" 2>/dev/null || true

echo
echo "Pronto! O Cofre de Senhas foi instalado em $destino"
echo "Procure por \"Cofre de Senhas\" no menu de aplicativos, ou execute:"
echo "  $destino/CofreDeSenhas"
