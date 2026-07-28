#!/usr/bin/env bash
# Gera o AppImage do Cofre de Senhas (linux-x64, autocontido).
# Uso: ./App/distribuicao/gerar-appimage.sh [versao]
set -euo pipefail

raiz="$(cd "$(dirname "$0")/../.." && pwd)"
versao="${1:-}"

if [ -z "$versao" ]; then
    ocorrencias=$(grep -c '<Version>' "$raiz/App/App.csproj" || true)
    if [ "$ocorrencias" -ne 1 ]; then
        echo "Esperava exatamente uma tag <Version> em App/App.csproj, encontrei $ocorrencias. Informe a versão como argumento." >&2
        exit 1
    fi
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

# Versão fixa (não "continuous") com hash conferido, pra não rodar um binário
# de terceiro trocado sem aviso durante o build de release.
appimagetool_versao="1.9.1"
appimagetool_sha256="ed4ce84f0d9caff66f50bcca6ff6f35aae54ce8135408b3fa33abfc3cb384eb0"

appimagetool="$(command -v appimagetool || true)"
if [ -z "$appimagetool" ]; then
    appimagetool="$trabalho/appimagetool"
    if [ ! -x "$appimagetool" ]; then
        echo "Baixando o appimagetool $appimagetool_versao..."
        curl -fsSL -o "$appimagetool" \
            "https://github.com/AppImage/appimagetool/releases/download/$appimagetool_versao/appimagetool-x86_64.AppImage"

        hash_obtido=$(sha256sum "$appimagetool" | cut -d ' ' -f 1)
        if [ "$hash_obtido" != "$appimagetool_sha256" ]; then
            echo "Erro: hash do appimagetool não confere." >&2
            echo "Esperado: $appimagetool_sha256" >&2
            echo "Obtido:   $hash_obtido" >&2
            rm -f "$appimagetool"
            exit 1
        fi

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
