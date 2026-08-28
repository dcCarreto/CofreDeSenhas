using Avalonia.Controls;
using GerenciadorDeSenhas.Modelos;

namespace CofreDeSenhas.Controles
{
    // ItemsControl onde a própria LinhaSenha é o container reciclado pelo
    // VirtualizingStackPanel. Sem isto (um DataTemplate normal), o ContentPresenter
    // derruba e reconstrói a LinhaSenha a cada tick de rolagem — ~500 construções e
    // ~390 MB pra percorrer uma lista de 500. Aqui ~20 LinhaSenha são reusadas entre
    // todas as posições; rolar vira só troca de DataContext -> LinhaSenha.Vincular.
    public class ListaVirtualizada : ItemsControl
    {
        private static readonly object ChaveLinha = new();
        private static readonly object ChaveLixeira = new();

        public Func<Senha, Control>? FabricaLinha { get; set; }
        public Func<Senha, Control>? FabricaLixeira { get; set; }
        public bool ModoLixeira { get; set; }

        protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            if (item is Control)
            {
                recycleKey = null;
                return false;
            }

            // Lixeira monta Borders crus e descartáveis; não vale a pena poolá-los.
            recycleKey = ModoLixeira ? null : ChaveLinha;
            return true;
        }

        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            if (item is Senha s)
            {
                var fabrica = ModoLixeira ? FabricaLixeira : FabricaLinha;
                if (fabrica != null)
                    return fabrica(s);
            }
            return new ContentControl();
        }

        protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
        {
            if (container is LinhaSenha linha && item is Senha s)
                linha.AoRealizar(s);
            else
                base.PrepareContainerForItemOverride(container, item, index);
        }

        protected override void ClearContainerForItemOverride(Control container)
        {
            if (container is LinhaSenha linha)
                linha.PrepararReciclagem();
            else
                base.ClearContainerForItemOverride(container);
        }
    }
}
