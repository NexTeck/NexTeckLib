using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace NexTeckLib
{
    /// <summary>
    /// Serve para tratar, exibir e relatar exceções do software, tanto exceções previstas como imprevistas pelo desenvolvedor. Pode exibir mensagens detalhadas para desenvolvedores ou simples para usuários e salvar relatórios de erros de acordo com a intensidade. Funciona atualmente com WindowsForms, WPF e Console. 
    /// leonardoteck 04/02/2016
    /// </summary>
    public static class ExceptionHandler
    {
        #region Atributos
        private static bool vaiReiniciar;
        /// <summary>
        /// Diz em tempo de execução se ocorreu alguma exceção gravíssima e a aplicação deve ser reiniciada
        /// </summary>
        private static bool VaiReiniciar
        {
            get { return vaiReiniciar; }
            set
            {
                if (Reiniciar && value)
                    vaiReiniciar = true;
                else
                    vaiReiniciar = false;
            }
        }            
            
        /// <summary>
        /// Define se o aplicativo deve ser reiniciado quando ocorrer um erro gravíssimo (ou seja, exceções não tratadas)
        /// </summary>
        public static bool Reiniciar { get; set; }

        /// <summary>
        /// Variável que indica se qualquer detalhe sobre os erros será exibido
        /// leonardoteck 07/02/2016
        /// </summary>
        public static bool ModoProgramador { get; set; }

        /// <summary>
        /// Define se os relatórios devem ser salvos automaticamente
        /// leonardoteck 28/10/2015
        /// </summary>
        public static bool SalvarRelatoriosDeErros { get; set; }

        /// <summary>
        /// Objeto que cria um thread pra tentar salvar o estado atual do aplicativo
        /// leonardoteck 28/10/2015
        /// </summary>
        private static BackgroundWorker recover = new BackgroundWorker();

        /// <summary>
        /// Lista de exceções lançadas que devem ser salvas no relatório
        /// </summary>
        private static Queue<Exception> fila = new Queue<Exception>();
        #endregion

        /// <summary>
        /// Exibe uma mensagem de erro ao usuário ou programador e gera relatórios de erros, salvos num arquivo.
        /// leonardoteck 07/02/2016
        /// </summary>
        /// <param name="ex">Exceção a ser relatada</param>
        /// <param name="mensagemUsuario">Mensagem a ser exibida ao usuário. Se o parâmetro não for passado, se for vazio, nulo,
        /// e a intensidade do erro for diferente de gravissimo, não será exibida a mensagem</param>
        /// <param name="intensidade">Intensidade do erro</param>
        public static void ExibirErro(Exception ex, string mensagemUsuario = "", IntensidadeErro intensidade = IntensidadeErro.Grave)
        {
            if (intensidade > 0 && SalvarRelatoriosDeErros)
            {
                fila.Enqueue(ex);
                if (!recover.IsBusy)
                    recover.RunWorkerAsync();
            }

            string mensagem = mensagemUsuario;
            if (ModoProgramador)
            {
                mensagem += Environment.NewLine + Environment.NewLine + ConstruirMensagemErro(ex);

                bool perguntar = ex.InnerException != null;
                if (perguntar)
                    mensagem += Environment.NewLine + "Deseja mais detalhes?";

                bool detalhes = ExibirMensagem(mensagem, perguntar);

                if (detalhes && perguntar)
                {
                    if (Configuracoes.TipoProjeto == TipoProjeto.Console)
                        mensagem = Environment.NewLine + "Exceções internas:";
                    else
                        mensagem = "Exceções internas:";
                    Exception inner = ex.InnerException;
                    int numEx = 1;
                    while (inner != null)
                    {
                        mensagem += Environment.NewLine + Environment.NewLine + "Ex. interna " + numEx++ + ": " + ConstruirMensagemErro(inner);
                        inner = inner.InnerException;
                    }

                    if (!(intensidade > 0 && SalvarRelatoriosDeErros))
                    {
                        mensagem += Environment.NewLine + Environment.NewLine + "Salvar relatório deste erro?";
                        bool salvar = ExibirMensagem(mensagem, true);

                        if (salvar)
                        {
                            fila.Enqueue(ex);
                            if (!recover.IsBusy)
                                recover.RunWorkerAsync();
                        }
                    }
                }
            }
            else
            {
                if (intensidade == IntensidadeErro.Gravissimo)
                    mensagem += Environment.NewLine + "Contate o desenvolvedor do sistema e informe que houve um erro gravíssimo.";

                if (!string.IsNullOrEmpty(mensagem))
                    ExibirMensagem(mensagem, false);
            }

            if (intensidade == IntensidadeErro.Gravissimo && fila.Count == 0 && !recover.IsBusy && Reiniciar)
            {
                Process.Start(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
                Process.GetCurrentProcess().Kill();
            }                
            else
                VaiReiniciar = true;
        }

        /// <summary>
        /// Reúne dados do StackTrace e da exceção em uma string.
        /// leonardoteck 07/02/2016
        /// </summary>
        /// <param name="ex">Exceção de onde serão extraídos os dados</param>
        /// <returns>Retorna a string formatada</returns>
        private static string ConstruirMensagemErro(Exception ex)
        {
            StackTrace st = new StackTrace(ex, true);

            StackFrame sf;
            string arquivo = "";
            string linha = "";

            if (st.FrameCount > 0)
            {
                sf = st.GetFrame(0);
                arquivo = sf.GetFileName();
                linha = sf.GetFileLineNumber().ToString();
            }

            arquivo = !string.IsNullOrEmpty(arquivo) ? System.IO.Path.GetFileName(arquivo) : "n/a";
            if (string.IsNullOrEmpty(linha)) linha = "n/a";

            return "Mensagem da exceção: " + ex.Message + Environment.NewLine + "Arquivo: " + arquivo + Environment.NewLine + "Linha: " + linha;
        }

        /// <summary>
        /// Método universal para exibir mensagens de alerta ao usuário. Funciona com WFA, WPF e Console.
        /// leoanrdoteck 07/02/2016
        /// </summary>
        /// <param name="mensagem">Mensagem a ser exibida</param>
        /// <param name="perguntar">Perguntar sim (retorna true) ou não (retorna false)</param>
        /// <returns>Resposta sim retorna true e não retorna false</returns>
        public static bool ExibirMensagem(string mensagem, bool perguntar)
        {
            if (Configuracoes.TipoProjeto == TipoProjeto.WFA)
            {
                System.Windows.Forms.MessageBoxButtons botoes = perguntar ? System.Windows.Forms.MessageBoxButtons.YesNo : System.Windows.Forms.MessageBoxButtons.OK;
                System.Windows.Forms.DialogResult dr = System.Windows.Forms.MessageBox.Show(mensagem, Configuracoes.NomePrograma, botoes, System.Windows.Forms.MessageBoxIcon.Error);
                if (perguntar && dr == System.Windows.Forms.DialogResult.No)
                    return false;
            }
            else if (Configuracoes.TipoProjeto == TipoProjeto.WPF)
            {
                System.Windows.MessageBoxButton botoes = perguntar ? System.Windows.MessageBoxButton.YesNo : System.Windows.MessageBoxButton.OK;
                System.Windows.MessageBoxResult mr = System.Windows.MessageBox.Show(mensagem, Configuracoes.NomePrograma, botoes, System.Windows.MessageBoxImage.Error);
                if (perguntar && mr == System.Windows.MessageBoxResult.No)
                    return false;
            }
            else // if (Configuracoes.TipoProjeto == TipoProjeto.Console)
            {
                Console.WriteLine(mensagem);
                if (perguntar)
                {
                    fazerPergunta:
                    Console.WriteLine("S: Sim / N: Não");
                    ConsoleKeyInfo resposta = Console.ReadKey();
                    switch (resposta.KeyChar)
                    {
                        case 'S':
                        case 's':
                            return true;
                        case 'N':
                        case 'n':
                            return false;
                        default:
                            goto fazerPergunta;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Configura o comportamento geral do ExceptionHandler e adiciona eventos para exceções não tratadas em todos os níveis da aplicação.
        /// Deve ser chamado no inicio do programa.
        /// leonardoteck 07/02/2016
        /// </summary>
        /// <param name="modoProgramador">Define se serão exibidos detalhes técnicos sobre os erros</param>
        /// <param name="salvarErros">Define se serão salvos relatórios sobre os erros</param>
        /// <param name="reiniciar">Define se a aplicação deve ser reiniciada após um erro gravíssimo (ou seja, exceção não tratada)</param>
        public static void Iniciar(bool modoProgramador = true, bool salvarErros= true, bool reiniciar = true)
        {
            // Tratamento de exceções não tratadas
            AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            if (Configuracoes.TipoProjeto == TipoProjeto.WFA)
                System.Windows.Forms.Application.ThreadException += Application_ThreadException;

            if (Configuracoes.TipoProjeto == TipoProjeto.WPF)
                System.Windows.Application.Current.DispatcherUnhandledException += Application_DispatcherUnhandledException;

            // Eventos para salvar relatórios
            recover.DoWork += recover_DoWork;
            recover.RunWorkerCompleted += recover_RunWorkerCompleted;
                        
            // Define as configurações básicas
            ModoProgramador = modoProgramador;
            SalvarRelatoriosDeErros = salvarErros;
            Reiniciar = reiniciar;
        }
        
        #region Eventos
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ExibirErro(e.Exception, "Ops, ocorreu um erro", IntensidadeErro.Gravissimo);
        }

        private static void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
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
                Process.GetCurrentProcess().Kill();
            }
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            ExibirErro(e.Exception, "Ops, ocorreu um erro", IntensidadeErro.Gravissimo);
        }

        private static void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ExibirErro(e.Exception, "Ops, ocorreu um erro", IntensidadeErro.Gravissimo);
        }

        /// <summary>
        /// Evento executado de forma assíncrona que tenta salvar o relatório
        /// leonardoteck 30/10/2015
        /// </summary>
        /// <TODO>
        /// Muda pra salvar os arquivos com a data (um arquivo de log por dia). Ex.: ErroConfig_08/02/16.bin
        /// E dentro de uma pasta. Ex.: ErroConfig Logs
        /// </TODO>
        private static void recover_DoWork(object sender, DoWorkEventArgs e)
        {
            ErroConfig erro = (ErroConfig)ConfigController.CarregarOuCriar(typeof(ErroConfig));
            while (fila.Count > 0)
                erro.AddError(fila.Dequeue());
            erro.Salvar();
        }

        /// <summary>
        /// Reinicia o aplicativo se a intensidade do erro é gravissima, assim que termina de salvar o relatório
        /// leonardoteck 30/10/2015
        /// </summary>
        private static void recover_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (fila.Count > 0)
                recover.RunWorkerAsync();
            else if (VaiReiniciar)
            {
                Process.Start(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
                Process.GetCurrentProcess().Kill();
            }
        }
        #endregion

        /// <summary>
        /// Enumera a intensidade do erro, para definir o que deve ser feito
        /// leonardoteck 28/10/2015
        /// </summary>
        public enum IntensidadeErro
        {
            /// <summary>
            /// Erro banal, não é necessário interromper a atividade da aplicação
            /// leonardoteck 28/10/2015
            /// </summary>
            Simples = 0,
            /// <summary>
            /// Salva tudo que puder
            /// leonardoteck 28/10/2015
            /// </summary>
            Grave,
            /// <summary>
            /// Erro fatal, toda atividade da aplicação deve ser interrompida imediatamente e o programador deve ser contatado
            /// leonardoteck 28/10/2015
            /// </summary>
            Gravissimo
        }
    }
}