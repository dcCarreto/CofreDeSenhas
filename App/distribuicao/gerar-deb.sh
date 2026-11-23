#!/usr/bin/env bash
# Gera o pacote .deb do Cofre de Senhas (amd64, autocontido): o que o Linux Mint,
# Ubuntu e derivados instalam com dois cliques. Depois de instalado, o app se
# atualiza sozinho pelo atualizar-pacote (ver deb/).
# Uso: ./App/distribuicao/gerar-deb.sh [versao]
set -euo pipefail

raiz="$(cd "$(dirname "$0")/../.." && pwd)"
versao="${1:-$(date -u +%Y.%-m.%-d)}"
distribuicao="$raiz/App/distribuicao"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "Erro: o SDK do .NET não foi encontrado. Instale-o em https://dotnet.microsoft.com/download" >&2
    exit 1
fi

pacote="$raiz/publish-deb/cofre-de-senhas_${versao}_amd64"
opt="$pacote/opt/cofre-de-senhas"
rm -rf "$pacote"
mkdir -p "$opt" "$pacote/DEBIAN"

echo "Publicando o aplicativo (linux-x64, autocontido, atualização $versao)..."
dotnet publish "$raiz/App/App.csproj" -f net10.0 -c Release -r linux-x64 \
    --self-contained true -p:Version="$versao" -o "$opt"

echo "Montando o pacote..."
install -m 0755 "$distribuicao/deb/atualizar-pacote" "$opt/atualizar-pacote"

# A chave pública vem do próprio código (ServicoAtualizacao), para o app e o
# atualizar-pacote nunca conferirem com chaves diferentes.
sed -n '/BEGIN PUBLIC KEY/,/END PUBLIC KEY/p' "$raiz/App/Infraestrutura/ServicoAtualizacao.cs" \
    | sed 's/^[[:space:]]*//' > "$opt/chave-atualizacao.pem"
openssl pkey -pubin -in "$opt/chave-atualizacao.pem" -noout

install -D -m 0644 "$distribuicao/deb/io.github.dccarreto.cofredesenhas.policy" \
    "$pacote/usr/share/polkit-1/actions/io.github.dccarreto.cofredesenhas.policy"
install -D -m 0644 "$raiz/App/Ativos/app.png" \
    "$pacote/usr/share/icons/hicolor/256x256/apps/cofre-de-senhas.png"
mkdir -p "$pacote/usr/share/applications"
sed "s|^Exec=.*|Exec=/opt/cofre-de-senhas/CofreDeSenhas|" "$distribuicao/cofre-de-senhas.desktop" \
    > "$pacote/usr/share/applications/cofre-de-senhas.desktop"

# Permissões previsíveis, independentes do umask de quem gera.
find "$pacote" -type d -exec chmod 0755 {} +
find "$pacote" -type f ! -perm -u+x -exec chmod 0644 {} +
find "$pacote" -type f -perm -u+x -exec chmod 0755 {} +

cat > "$pacote/DEBIAN/control" <<FIM
Package: cofre-de-senhas
Version: $versao
Architecture: amd64
Maintainer: dcCarreto <dcCarreto@users.noreply.github.com>
Installed-Size: $(du -sk "$pacote/opt" "$pacote/usr" | awk '{ s += $1 } END { print s }')
Depends: libc6, libfontconfig1, libx11-6, libice6, libsm6, openssl, pkexec | policykit-1
Section: utils
Priority: optional
Homepage: https://github.com/dcCarreto/CofreDeSenhas
Description: gerenciador de senhas com cofre local criptografado
 Gerador de senhas integrado, cofre local criptografado (AES-256-GCM) e
 sincronização opcional entre dispositivos. Instala-se no menu de aplicativos
 e se atualiza sozinho a partir das releases assinadas do projeto.
FIM

mkdir -p "$raiz/dist"
destino="$raiz/dist/cofre-de-senhas_${versao}_amd64.deb"
dpkg-deb --root-owner-group -Zxz --build "$pacote" "$destino"

echo
echo "Pronto! Pacote gerado em: $destino"
