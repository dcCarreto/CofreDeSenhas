"""Monta release-notes.md a partir de .github/RELEASE_TEMPLATE.md + CHANGELOG.md.

Uso: python3 montar_notas_release.py <versao>

Troca os placeholders "X.Y.Z" do modelo pela versão real e preenche as
seções "Destaques desta versão" e "Mudanças" com o conteúdo da seção
correspondente do CHANGELOG.md, em vez de publicar o modelo cru (com
placeholder literal e comentários HTML vazios) como nota da release.
"""

import re
import sys
import pathlib

RAIZ = pathlib.Path(__file__).resolve().parents[2]


def secao_do_changelog(versao: str) -> tuple[str, str]:
    changelog = (RAIZ / "CHANGELOG.md").read_text(encoding="utf-8")
    padrao = re.compile(
        rf"^## \[{re.escape(versao)}\][^\n]*\n(.*?)(?=^## \[|\Z)",
        re.MULTILINE | re.DOTALL,
    )
    m = padrao.search(changelog)
    if not m:
        raise SystemExit(f"Não encontrei a seção da versão {versao} em CHANGELOG.md")

    secao = m.group(1).strip("\n")
    partes = secao.split("\n### ", 1)
    resumo = partes[0].strip()
    mudancas = ("### " + partes[1]).strip() if len(partes) > 1 else ""
    return resumo, mudancas


def substituir_secao(texto: str, titulo: str, conteudo: str) -> str:
    padrao = re.compile(rf"(## {re.escape(titulo)}\n\n)<!--.*?-->\n", re.DOTALL)
    novo, trocas = padrao.subn(lambda m: m.group(1) + conteudo + "\n", texto, count=1)
    if trocas == 0:
        raise SystemExit(f"Não encontrei a seção '{titulo}' no modelo de release")
    return novo


def montar(versao: str) -> str:
    resumo, mudancas = secao_do_changelog(versao)

    modelo = (RAIZ / ".github" / "RELEASE_TEMPLATE.md").read_text(encoding="utf-8")
    modelo = modelo.replace("X.Y.Z", versao)

    modelo = substituir_secao(modelo, "Destaques desta versão", resumo)
    modelo = substituir_secao(
        modelo,
        "Mudanças",
        mudancas or "Sem mudanças de funcionalidade nesta versão.",
    )
    modelo = substituir_secao(
        modelo,
        "Capturas de tela",
        "Veja a seção [Capturas de tela](https://github.com/dcCarreto/CofreDeSenhas#capturas-de-tela) "
        "do README para as telas atuais do aplicativo.",
    )
    return modelo


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Uso: montar_notas_release.py <versao>")

    notas = montar(sys.argv[1])
    (RAIZ / "release-notes.md").write_text(notas, encoding="utf-8")
    print(notas)
