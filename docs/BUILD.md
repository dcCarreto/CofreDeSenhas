# Compilando a partir do código-fonte

[← Voltar ao README](../README.md)

Para rodar o projeto direto do código-fonte ou rodar os testes, veja
[DESENVOLVIMENTO.md](DESENVOLVIMENTO.md). Este documento cobre a geração dos
executáveis publicáveis e do instalador do Windows.

## Geração do executável

Para gerar um executável único e autocontido para Windows x64:

```
dotnet publish App/App.csproj -f net10.0-windows10.0.19041.0 -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish
```

O arquivo `CofreDeSenhas.exe` será criado na pasta `publish`.

Para Linux x64:

```
dotnet publish App/App.csproj -f net10.0 -c Release -r linux-x64 --self-contained -o publish-linux
```

## Instalador do Windows

O instalador (`CofreDeSenhas-Setup-*.exe`) é gerado com o
[Inno Setup](https://jrsoftware.org/isinfo.php) a partir do executável já
publicado. Com o Inno Setup instalado (`winget install JRSoftware.InnoSetup`),
rode:

```
.\App\distribuicao\gerar-instalador.ps1
```

O script publica o aplicativo (mesmo comando acima) e compila
`App/distribuicao/cofre-de-senhas.iss`, deixando o instalador em `dist/`. O
nome do arquivo leva a data de hoje como carimbo (ou o valor passado em
`-Versao`).

O instalador não exige privilégios de administrador (instala só para o
usuário atual, em `%LocalAppData%\Programs\Cofre de Senhas`, com opção de
instalar para todos os usuários), cria o atalho no menu iniciar e registra a
desinstalação em "Aplicativos e recursos". Ao desinstalar, os dados do cofre
em `%APPDATA%\GerenciadorSenhas` são preservados por padrão — apagá-los exige
confirmação explícita numa caixa de diálogo (com "Não" como opção padrão);
desinstalações silenciosas (`/VERYSILENT`) nunca apagam o cofre.

## AppImage e instalação no Linux

O script `App/distribuicao/instalar.sh` faz o publish para Linux e ainda
registra o atalho e o ícone no ambiente de trabalho; `gerar-appimage.sh` gera
o `.AppImage`. Veja [Download e instalação](INSTALACAO.md#linux) para o uso
de cada um.
