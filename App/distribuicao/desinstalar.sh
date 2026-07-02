#!/usr/bin/env bash
# Remove o Cofre de Senhas instalado pelo instalar.sh.
# O cofre em ~/.config/GerenciadorSenhas é preservado.
set -euo pipefail

rm -rf "$HOME/.local/opt/cofre-de-senhas"
rm -f "$HOME/.local/share/applications/cofre-de-senhas.desktop"
rm -f "$HOME/.local/share/icons/hicolor/128x128/apps/cofre-de-senhas.png"

update-desktop-database "$HOME/.local/share/applications" 2>/dev/null || true

echo "Cofre de Senhas desinstalado. Seus dados continuam em ~/.config/GerenciadorSenhas"
