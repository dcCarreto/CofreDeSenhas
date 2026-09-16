# Compilando e testando

[← Voltar ao README](../README.md)

## Requisitos

- Windows 10 ou 11, ou uma distribuição Linux com X11 ou Wayland.
- Para compilar a partir do código-fonte: .NET 10 SDK.
- Para executar o binário publicado: nada além do próprio executável, que é
  autocontido.

## Compilação e execução

A partir da raiz do repositório, em qualquer plataforma:

```sh
dotnet run --project App/App.csproj
```

Para gerar o executável publicável ou o instalador do Windows, veja
[BUILD.md](BUILD.md).

## Testes

```sh
dotnet test
```

A suíte cobre testes unitários, testes de integração de ponta a ponta (criação,
persistência e recarga do cofre), testes de segurança (rejeição de dados
adulterados e de chave incorreta) e testes de desempenho com grande volume de
senhas.
