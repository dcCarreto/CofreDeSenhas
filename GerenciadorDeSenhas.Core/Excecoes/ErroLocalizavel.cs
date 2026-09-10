namespace GerenciadorDeSenhas.Excecoes
{
    public sealed class ErroLocalizavel : Exception, ILocalizavel
    {
        public string Chave { get; }
        public object?[] Argumentos { get; }

        public ErroLocalizavel(string chave, params object?[] argumentos)
            : base(chave)
        {
            Chave = chave;
            Argumentos = argumentos;
        }

        public ErroLocalizavel(string chave, Exception causaRaiz, params object?[] argumentos)
            : base(chave, causaRaiz)
        {
            Chave = chave;
            Argumentos = argumentos;
        }
    }
}
