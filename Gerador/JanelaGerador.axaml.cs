using Avalonia.Controls;

namespace CofreDeSenhas.Gerador
{
    public partial class JanelaGerador : Window
    {
        public JanelaGerador()
        {
            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);

            Gerador.PermiteSalvar = false;

            Acessibilidade.Alterado += (s, e) => Gerador.AtualizarTema();
        }
    }
}
