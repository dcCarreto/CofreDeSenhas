namespace CofreDeSenhas.Cli
{
    internal static class Ajuda
    {
        public static void Imprimir()
        {
            Console.WriteLine(
@"cofre — linha de comando somente-leitura do Cofre de Senhas

USO
  cofre <comando> [opções]

COMANDOS
  listar                    lista as credenciais (sem senhas)
  buscar <termo>            filtra por serviço, usuário, categoria, URL ou etiqueta
  mostrar <serviço>        detalhes de uma credencial (sem a senha)
  copiar <serviço>        copia a senha para a área de transferência e a apaga depois
  gerar                     gera senha(s) ou frase(s)-senha (não abre o cofre)

AUTENTICAÇÃO
  A senha mestra é pedida no terminal a cada comando que abre o cofre.
  Nada é armazenado. O bloqueio por tentativas erradas é o mesmo do aplicativo.

OPÇÕES DE listar / buscar
  --categoria <c>           trabalho | pessoal | financas | social | outro
  --favoritas               só as favoritas
  --etiqueta <e>            só credenciais com essa etiqueta

OPÇÕES DE mostrar / copiar
  --usuario <u>            desempata quando há vários serviços com o mesmo nome
  --limpar-apos <s>        segundos até apagar a área de transferência (padrão 30)
  --nao-limpar              não apaga a área de transferência automaticamente

OPÇÕES DE gerar
  --comprimento <n>        tamanho da senha (padrão 20)
  --quantidade <n>         quantas gerar (padrão 1)
  --sem-maiusculas | --sem-minusculas | --sem-numeros | --sem-simbolos
  --frase                   gera frase-senha em vez de senha
    --palavras <n>          nº de palavras (padrão 5)
    --separador <x>        separador entre palavras (padrão ""-"")
    --capitalizar           Palavras Com Inicial Maiúscula
    --sem-numero            não anexa um número ao final
  --copiar                  copia a senha gerada (só com --quantidade 1)

Para um valor que começa com ""-"" (ex.: separador), use --flag=valor:
  cofre gerar --frase --separador=-

O local do cofre segue o aplicativo. COFRE_BASE=<pasta> aponta para outro cofre.

CÓDIGOS DE SAÍDA
  0 ok   1 erro   2 resultado ambíguo   3 cofre bloqueado   4 sem cofre / sem clipboard");
        }
    }
}
