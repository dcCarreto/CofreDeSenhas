#!/usr/bin/env bash
# Gera o AppImage do Cofre de Senhas (linux-x64, autocontido).
# Uso: ./App/distribuicao/gerar-appimage.sh [versao]
set -euo pipefail

raiz="$(cd "$(dirname "$0")/../.." && pwd)"
versao="${1:-}"

if [ -z "$versao" ]; then
    versao=$(grep -oP '(?<=<Version>)[^<]+' "$raiz/App/App.csproj")
fi
if [ -z "$versao" ]; then
    echo "Não foi possível determinar a versão em App/App.csproj. Informe como argumento." >&2
    exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
    echo "Erro: o SDK do .NET não foi encontrado. Instale-o em https://dotnet.microsoft.com/download" >&2
    exit 1
fi

trabalho="$raiz/publish-appimage"
appdir="$trabalho/CofreDeSenhas.AppDir"
rm -rf "$appdir"
mkdir -p "$appdir/usr/bin"

appimagetool="$(command -v appimagetool || true)"
if [ -z "$appimagetool" ]; then
    appimagetool="$trabalho/appimagetool"
    if [ ! -x "$appimagetool" ]; then
        echo "Baixando o appimagetool..."
        curl -fsSL -o "$appimagetool" \
            https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage
        chmod +x "$appimagetool"
    fi
fi

echo "Publicando o aplicativo (linux-x64, autocontido, versão $versao)..."
dotnet publish "$raiz/App/App.csproj" -f net10.0 -c Release -r linux-x64 \
    --self-contained true -o "$appdir/usr/bin"

echo "Montando o AppDir..."
cp "$raiz/App/Ativos/app.png" "$appdir/cofre-de-senhas.png"
sed "s|^Exec=.*|Exec=CofreDeSenhas|" "$raiz/App/distribuicao/cofre-de-senhas.desktop" \
    > "$appdir/cofre-de-senhas.desktop"

cat > "$appdir/AppRun" <<'FIM'
#!/usr/bin/env bash
aqui="$(dirname "$(readlink -f "$0")")"
exec "$aqui/usr/bin/CofreDeSenhas" "$@"
FIM
chmod +x "$appdir/AppRun"

mkdir -p "$raiz/dist"
echo "Empacotando o AppImage..."
"$appimagetool" --appimage-extract-and-run "$appdir" \
    "$raiz/dist/CofreDeSenhas-$versao-x86_64.AppImage"

echo
echo "Pronto! AppImage gerado em: $raiz/dist/CofreDeSenhas-$versao-x86_64.AppImage"
echo "Funciona em qualquer distribuição x86_64 com FUSE (ou com --appimage-extract-and-run"
echo "quando o FUSE não estiver disponível), sem exigir instalação do .NET."
