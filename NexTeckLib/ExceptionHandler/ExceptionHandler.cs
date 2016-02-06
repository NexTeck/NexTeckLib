using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace NexTeckLib
{
    /// <summary>
    /// Serve para tratar, exibir e relatar exceções do sistema, tanto exceções previstas como imprevistas pelo desenvolvedor. Pode exibir mensagens detalhadas para desenvolvedores ou simples para usuários e salvar relatórios de erros de acordo com a intensidade. Funciona atualmente com WindowsForms, WPF e Console
    /// 
    /// leonardoteck 04/02/2015
    /// </summary>
    public static class ExceptionHandler
    {
        #region Atributos
        private static bool salvarRelatoriosDeErros = true;
        private static bool exibeDetalhesDeErros = true;
        // só pra saber se precisa reiniciar o software após salvar relatórios
        private static bool reiniciar;
        // fila de exceções 
        private static Queue<Exception> fila = new Queue<Exception>();

        /// <summary>
        /// Objeto que cria um thread pra tentar salvar o estado atual do sistema
        /// Leonardo 28/10/2015
        /// </summary>
        private static BackgroundWorker recover = new BackgroundWorker();

        /// <summary>
        /// Variável que indica se exibe erros de programação
        /// </summary>
        public static bool ExibeErrosProgramador { get; set; }

        /// <summary>
        /// Só pra não ficar salvando relatórios enquanto o sistema é desenvolvido
        /// Leonardo 28/10/2015
        /// </summary>
        public static bool SalvarRelatoriosDeErros
        {
            get { return salvarRelatoriosDeErros; }
            set { salvarRelatoriosDeErros = value; }
        }
        /// <summary>
        /// Define se devem ser exibidas as InnerExceptions da Exception
        /// Leonardo 28/10/2015
        /// </summary>
        public static bool ExibeDetalhesDeErros
        {
            get { return exibeDetalhesDeErros; }
            set
            {
                if (ExibeErrosProgramador && value)
                    exibeDetalhesDeErros = value;
                else
                    exibeDetalhesDeErros = false;
            }
        }
        #endregion

        /// <summary>
        /// Exibe uma mensagem de erro ao usuário e gera relatórios de erros, salvos num arquivo
        /// Leonardo Costa 30/10/2015
        /// </summary>
        /// <param name="ex">Exceção a ser relatada</param>
        /// <param name="mensagemUsuario">Mensagem a ser exibida ao usuário. Se o parâmetro não for passado, se for vazio, nulo,
        /// e a intensidade do erro for diferente de gravissimo, não será exibida a mensagem</param>
        /// <param name="intensidade">Intensidade do erro</param>
        public static void ExibirErro(Exception ex, string mensagemUsuario = "", IntensidadeErro intensidade = IntensidadeErro.Grave)
        {
            if (intensidade > IntensidadeErro.Simples && salvarRelatoriosDeErros)
            {
                fila.Enqueue(ex);
                if (!recover.IsBusy)
                {
                    recover.RunWorkerAsync();
                }
            }

            string mensagem = mensagemUsuario;
            if (ExibeErrosProgramador)
            {   
                mensagem += Environment.NewLine + Environment.NewLine + "Exceção principal: " + ex.Message + Environment.NewLine + Environment.NewLine + "Fonte: " + ex.Source + Environment.NewLine;

                /// <TODO>
                /// 1) Vê como capturar informações do StackTrace. Acredito que será preciso instanciar um StackTrace com a Exception de parâmetro no construtor. Depois no objeto instanciado pegar o nome do arquivo, a linha, etc.
                /// 3) Exibir só os detalhes básicos da exceção em um messagebox, e neste mesmo perguntar se o programador quer mais detalhes. Se quiser, mostra outro messagebox com o stacktrace e as inner exceptions
                /// </TODO>

                if (ExibeDetalhesDeErros)
                {
                    Exception inner = ex.InnerException;
                    int numEx = 1;
                    while (inner != null)
                    {
                        mensagem += Environment.NewLine + "Exceção interna " + numEx++ + ": " + inner.Message;
                        inner = inner.InnerException;
                    }
                }
            }
            else
                if (intensidade == IntensidadeErro.Gravissimo)
                    mensagem += Environment.NewLine + "Contate o desenvolvedor do sistema e informe que houve um erro gravíssimo.";

            if (!string.IsNullOrEmpty(mensagem))
                MessageBox.Show(mensagem, Configuracoes.NomePrograma, MessageBoxButton.OK, MessageBoxImage.Error);
            //if (intensidade == IntensidadeErro.Gravissimo && !salvarRelatoriosDeErros)
            //    Application.Restart();
        }

        public static bool ExibirMensagem(string mensagem, bool perguntar)
        {
            if (Configuracoes.TipoProjeto == TipoProjeto.WFA)
            {
                System.Windows.Forms.MessageBoxButtons botoes = perguntar ? System.Windows.Forms.MessageBoxButtons.YesNo : System.Windows.Forms.MessageBoxButtons.OK;
                System.Windows.Forms.DialogResult dr = System.Windows.Forms.MessageBox.Show(mensagem, Configuracoes.NomePrograma, botoes, System.Windows.Forms.MessageBoxIcon.Error);
                if (perguntar && dr == System.Windows.Forms.DialogResult.No)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Exibe um erro validação
        /// </summary>
        public static void ExibirErroValidacao(string mensagem)
        {
            MessageBox.Show(mensagem, Configuracoes.NomePrograma, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
        
        /// <summary>
        /// Configura o Excepti e os relatórios de erros.
        /// Deve ser chamado no inicio do programa
        /// Andrei 07/11/2015
        /// </summary>
        public static void Iniciar(bool exibirErros = true, bool salvarErros= true, bool exibirDetalhes = true)
        {
            // Tratamento de exceções não tratadas e de exceções dos Threads dos Forms
            AppDomain.CurrentDomain.UnhandledException += GlobalUnhandledExceptionHandler;
            //Application.ThreadException += GlobalThreadExceptionHandler;

            // Eventos para salvar relatórios
            recover.DoWork += recover_DoWork;
            recover.RunWorkerCompleted += recover_RunWorkerCompleted;
                        
            // Define as configurações básicas
            ExibeErrosProgramador = exibirErros;
            SalvarRelatoriosDeErros = salvarErros;
            exibeDetalhesDeErros = exibirDetalhes;
        }

        #region Eventos
        private static void GlobalThreadExceptionHandler(object sender, ThreadExceptionEventArgs e)
        {
            // Gera o relatório do erro e reinicia o sistema
            ExibirErro(e.Exception, "Ops, ocorreu um erro", IntensidadeErro.Gravissimo);
        }

        private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                // Tem um cast, pode dar erro, e se der erro, pode entrar num laço infinito de erros quando der cast de novo, então precisa de try..catch
                ExibirErro((Exception)e.ExceptionObject, "Ops, ocorreu um erro", IntensidadeErro.Gravissimo);
            }
            catch (Exception ex)
            {
                ExibirErro(ex, "Falha ao iniciar sistema. Contate um desenvolvedor.");
                // Finaliza o sistema da forma mais dramática possível, pra impedir qualquer outro erro
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        /// Evento executado de forma assíncrona que tenta salvar o relatório
        /// Leonardo Costa 30/10/2015
        /// </summary>
        private static void recover_DoWork(object sender, DoWorkEventArgs e)
        {
            ErroConfig erro = (ErroConfig)ConfigController.CarregarOuCriar(typeof(ErroConfig));
            while (fila.Count > 0)
                erro.AddError(fila.Dequeue());
            erro.Salvar();
        }

        /// <summary>
        /// Reinicia o sistema se a intensidade do erro é gravissima, assim que termina de salvar o relatório
        /// Leonardo Costa 30/10/2015
        /// </summary>
        private static void recover_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (fila.Count > 0)
                recover.RunWorkerAsync();
            else if (reiniciar)
                Application.Current.Shutdown();
        }
        #endregion

        /// <summary>
        /// Enumera a intensidade do erro, para definir o que deve ser feito
        /// Leonardo 28/10/2015
        /// </summary>
        public enum IntensidadeErro
        {
            /// <summary>
            /// Erro banal, não é necessário interromper a atividade do sistema
            /// Leonardo 28/10/2015
            /// </summary>
            Simples = 0,
            /// <summary>
            /// Salva tudo que puder
            /// Leonardo 28/10/2015
            /// </summary>
            Grave,
            /// <summary>
            /// Erro fatal, toda atividade do sistema deve ser interrompida imediatamente e o programador deve ser contatado
            /// Leonardo 28/10/2015
            /// </summary>
            Gravissimo
        }
    }
}