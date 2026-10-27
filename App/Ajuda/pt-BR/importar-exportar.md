# Importar e exportar

## Importar de outro gerenciador

Em **Configurações → Backup e sincronização → Importar de CSV**. A maioria
dos gerenciadores exporta um arquivo **CSV**; o aplicativo reconhece os
formatos mais comuns e deixa você conferir o mapeamento das colunas antes
de concluir.

> O CSV exportado por outros programas fica **em texto puro**, sem
> cifragem. Depois de importar, apague esse arquivo com segurança.

### Do KeePass

Use **Importar do KeePass (.kdbx)**, na mesma seção, e informe a senha do
banco do KeePass. Diferente do CSV, ele traz também o histórico de senhas,
os anexos e os campos personalizados (que vão para as notas). Os grupos
viram etiquetas, e a lixeira do KeePass fica de fora.

Funcionam bancos no formato **KDBX 4** protegidos só por senha. Se o banco
estiver em KDBX 3.1, o aplicativo explica como convertê-lo no KeePass ou no
KeePassXC.

## Exportar o seu cofre

A exportação gera um arquivo **cifrado**, protegido por uma senha que você
define na hora. Serve para migrar para outro computador ou guardar uma
cópia de longo prazo fora do formato de [Backup](backup).

Para reimportar, abra o aplicativo no destino e use a opção de importar o
arquivo exportado, informando a senha usada na exportação.

## Levar para outro gerenciador

Em **Configurações → Backup e sincronização → Exportar sem criptografia
(CSV / Bitwarden)**. Depois de confirmar a senha mestra, escolha o formato:

- **CSV**: aceito pelo Bitwarden e pela maioria dos gerenciadores.
- **JSON do Bitwarden**: leva também o histórico de senhas e os campos
  extras de cada tipo de credencial.

Itens da lixeira e anexos ficam de fora.

> Esse arquivo fica **em texto puro**: qualquer pessoa ou programa com
> acesso a ele lê todas as suas senhas. Apague-o assim que terminar de
> importar no outro gerenciador.

## Levar para outro computador

1. Exporte o cofre (ou copie um [Backup](backup)).
2. Instale o aplicativo no outro computador.
3. Importe o arquivo e informe a senha.
4. Se quiser manter os dois em dia, configure
   [Sincronização por pasta](sincronizacao).
