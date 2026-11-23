<!-- arquivos-da-release -->
<!-- Modelo do fim das notas de cada release (montado pelo release.yml, que troca
     {versao}). O marcador acima precisa ficar na primeira linha: o app corta as notas
     a partir dele antes de mostrar "o que mudou" (ServicoAtualizacao). -->
## Qual arquivo baixar

| Arquivo | O que é |
|---|---|
| `CofreDeSenhas-Setup-{versao}.exe` | **Windows: é este.** Instalador do Cofre de Senhas. Não pede administrador e, depois de instalado, se atualiza sozinho. É o mesmo que `winget install dcCarreto.CofreDeSenhas` instala. |
| `CofreDeSenhas-{versao}-x86_64.AppImage` | **Linux: é este.** Baixe, dê permissão de execução (`chmod +x`) e rode. Também se atualiza sozinho. |
| `GeradorDeSenhas-{versao}-win-x64.exe` | Só o gerador de senhas, sem o cofre (Windows). Executável avulso: não instala nada nem salva senhas. |
| `cofre-{versao}-win-x64.exe`<br>`cofre-{versao}-linux-x64` | Linha de comando `cofre`, para consultar o cofre pelo terminal (somente leitura). Não é o aplicativo de janela. |
| `CofreDeSenhas-{versao}-win-x64-portatil.exe` | **Descontinuado, não baixe.** Fica aqui só para as cópias portáteis antigas conseguirem se atualizar; ao se atualizar, elas passam para a versão instalada. |

<details>
<summary>Arquivos para conferir os downloads (opcional)</summary>

| Arquivo | O que é |
|---|---|
| `CHECKSUMS.txt` | Hash SHA-256 de cada arquivo da release. |
| `CHECKSUMS.txt.sig` | Assinatura do `CHECKSUMS.txt`. O atualizador automático a confere antes de instalar qualquer coisa; para conferir à mão, veja o [SECURITY.md](https://github.com/dcCarreto/CofreDeSenhas/blob/prod/SECURITY.md#confiança-nos-binários-e-verificação-dos-downloads). |
| `CofreDeSenhas-{versao}-sbom.json` | Lista das bibliotecas usadas nesta versão (SBOM, formato CycloneDX). |
| Source code (zip / tar.gz) | Código-fonte desta versão, gerado automaticamente pelo GitHub. |

</details>
