"""Gera os manifests winget (schema 1.6.0) para uma versão já publicada.

Uso: python3 gerar_manifesto_winget.py <versao> <sha256_do_instalador> <pasta_saida>

Escreve os três arquivos YAML no layout que o repositório microsoft/winget-pkgs
espera (manifests/d/dcCarreto/CofreDeSenhas/<versao>/), prontos para copiar num
fork e abrir PR — a submissão em si continua manual, esse script só evita
montar os YAML à mão a cada release.
"""

import re
import sys
import pathlib

PACKAGE_IDENTIFIER = "dcCarreto.CofreDeSenhas"
PUBLISHER = "Denis Cristino Cantagallo Carreto"
PACKAGE_NAME = "Cofre de Senhas"
REPO_URL = "https://github.com/dcCarreto/CofreDeSenhas"
MANIFEST_VERSION = "1.6.0"


def manifest_versao(versao: str) -> str:
    return f"""# Criado automaticamente por gerar_manifesto_winget.py — não editar à mão.
PackageIdentifier: {PACKAGE_IDENTIFIER}
PackageVersion: {versao}
DefaultLocale: pt-BR
ManifestType: version
ManifestVersion: {MANIFEST_VERSION}
"""


def manifest_instalador(versao: str, sha256: str) -> str:
    nome_exe = f"CofreDeSenhas-Setup-{versao}.exe"
    url = f"{REPO_URL}/releases/download/v{versao}/{nome_exe}"
    return f"""# Criado automaticamente por gerar_manifesto_winget.py — não editar à mão.
PackageIdentifier: {PACKAGE_IDENTIFIER}
PackageVersion: {versao}
InstallerLocale: pt-BR
InstallerType: inno
Scope: user
InstallModes:
  - interactive
  - silent
  - silentWithProgress
UpgradeBehavior: install
Installers:
  - Architecture: x64
    InstallerUrl: {url}
    InstallerSha256: {sha256}
ManifestType: installer
ManifestVersion: {MANIFEST_VERSION}
"""


def manifest_locale(versao: str) -> str:
    return f"""# Criado automaticamente por gerar_manifesto_winget.py — não editar à mão.
PackageIdentifier: {PACKAGE_IDENTIFIER}
PackageVersion: {versao}
PackageLocale: pt-BR
Publisher: {PUBLISHER}
PublisherUrl: {REPO_URL}
PublisherSupportUrl: {REPO_URL}/issues
PackageName: {PACKAGE_NAME}
PackageUrl: {REPO_URL}
License: PolyForm Noncommercial 1.0.0
LicenseUrl: {REPO_URL}/blob/prod/LICENSE
Copyright: Copyright © 2026 Denis Cristino Cantagallo Carreto
ShortDescription: Gerenciador de senhas local, com cofre cifrado (AES-256-GCM) e sincronização opcional por banco de dados ou pasta compartilhada.
Tags:
  - password-manager
  - security
  - encryption
ManifestType: defaultLocale
ManifestVersion: {MANIFEST_VERSION}
"""


def gerar(versao: str, sha256: str, pasta_saida: pathlib.Path) -> pathlib.Path:
    destino = pasta_saida / "manifests" / "d" / "dcCarreto" / "CofreDeSenhas" / versao
    destino.mkdir(parents=True, exist_ok=True)

    (destino / f"{PACKAGE_IDENTIFIER}.yaml").write_text(manifest_versao(versao), encoding="utf-8", newline="\n")
    (destino / f"{PACKAGE_IDENTIFIER}.installer.yaml").write_text(
        manifest_instalador(versao, sha256), encoding="utf-8", newline="\n")
    (destino / f"{PACKAGE_IDENTIFIER}.locale.pt-BR.yaml").write_text(
        manifest_locale(versao), encoding="utf-8", newline="\n")

    return destino


if __name__ == "__main__":
    if len(sys.argv) != 4:
        raise SystemExit("Uso: gerar_manifesto_winget.py <versao> <sha256_do_instalador> <pasta_saida>")

    versao_arg = re.sub(r"^[vV]", "", sys.argv[1])
    pasta = gerar(versao_arg, sys.argv[2], pathlib.Path(sys.argv[3]))
    print(f"Manifests winget gerados em: {pasta}")
