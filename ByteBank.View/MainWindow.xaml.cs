using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ByteBank.View
{
    public partial class MainWindow : Window
    {
        private readonly ContaClienteRepository r_Repositorio;
        private readonly ContaClienteService r_Servico;

        public MainWindow()
        {
            InitializeComponent();

            r_Repositorio = new ContaClienteRepository();
            r_Servico = new ContaClienteService();
        }

        private void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            var taskSchedulerUI = TaskScheduler.FromCurrentSynchronizationContext();
            BtnProcessar.IsEnabled = false;

            var contas = r_Repositorio.GetContaClientes();

            AtualizarView(new List<string>(), TimeSpan.Zero);

            var inicio = DateTime.Now;


            //Aqui, encadeamos várias tarefas para serem executadas em ordem, a primeira (do primeiro ContinueWith), é responsável por atualizar a view
            //a segunda, é responsável por tornar o botão enabled de novo
            //OBS: para criarmos tarefas que mexam na UI, é necessário utilizarmos o taskScheduler da linha de execução principal, por isso, declaramos ele na linha 37, e estamos passando ele como o segundo parâmetro dos nossos ContinueWith
            ConsolidarContas(contas)
                .ContinueWith(task =>
                {
                    var fim = DateTime.Now;
                    var resultado = task.Result;
                    AtualizarView(resultado, fim - inicio);
                }, taskSchedulerUI)
                .ContinueWith(task =>
                {
                    BtnProcessar.IsEnabled = true;
                }, taskSchedulerUI);
        }

        //Esse método está retornando uma tarefa que contém uma lista de string dentro dela
        //OBS: o tipo definido dentro da task (no nosso caso a lista de string) pode ser recuperado através da propriedade ".result", como foi feito na linha 54
        private Task<List<string>> ConsolidarContas(IEnumerable<ContaCliente> contas)
        {
            var resultado = new List<string>();
            var tasks = contas.Select(conta =>
            {
                return Task.Factory.StartNew(() =>
                {
                    var contaResultado = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(contaResultado);
                });
            });

            //Aqui nós temos uma task que espera que as tasks sejam finalizadas, pra depois, retornar a lista de string
            return Task.WhenAll(tasks).ContinueWith(task =>
            {
                return resultado;
            });

        }

        private void AtualizarView(List<String> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count} clientes em {tempoDecorrido}";

            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }
    }
}
