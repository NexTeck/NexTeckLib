using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace NexTeckLib
{
    /// <summary>
    /// Serve para tratar, exibir e relatar exceções do sistema, tanto exceções previstas como imprevistas pelo desenvolvedor. Pode exibir mensagens detalhadas para desenvolvedores ou simples para usuários e salvar relatórios de erros de acordo com a intensidade. Funciona atualmente com WindowsForms, WPF e Console. 
    /// leonardoteck 04/02/2016
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
        /// leonardoteck 28/10/2015
        /// </summary>
        private static BackgroundWorker recover = new BackgroundWorker();

        /// <summary>
        /// Variável que indica se qualquer detalhe sobre os erros será exibido
        /// leonardoteck 07/02/2016
        /// </summary>
        public static bool ExibeErrosProgramador { get; set; }

        /// <summary>
        /// Só pra não ficar salvando relatórios enquanto o sistema é desenvolvido
        /// leonardoteck 28/10/2015
        /// </summary>
        public static bool SalvarRelatoriosDeErros
        {
            get { return salvarRelatoriosDeErros; }
            set { salvarRelatoriosDeErros = value; }
        }

        /// <summary>
        /// Define se devem ser exibidas as InnerExceptions da Exception
        /// leonardoteck 28/10/2015
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
        /// Exibe uma mensagem de erro ao usuário ou programador e gera relatórios de erros, salvos num arquivo.
        /// leonardoteck 07/02/2016
        /// </summary>
        /// <param name="ex">Exceção a ser relatada</param>
        /// <param name="mensagemUsuario">Mensagem a ser exibida ao usuário. Se o parâmetro não for passado, se for vazio, nulo,
        /// e a intensidade do erro for diferente de gravissimo, não será exibida a mensagem</param>
        /// <param name="intensidade">Intensidade do erro</param>
        public static void ExibirErro(Exception ex, string mensagemUsuario = "", IntensidadeErro intensidade = IntensidadeErro.Grave)
        {
            if (intensidade > 0 && salvarRelatoriosDeErros)
            {
                fila.Enqueue(ex);
                if (!recover.IsBusy)
                    recover.RunWorkerAsync();
            }

            string mensagem = mensagemUsuario;
            if (ExibeErrosProgramador)
            {
                mensagem += ConstruirMensagemErro(ex);

                bool perguntar = ExibeDetalhesDeErros && ex.InnerException != null;
                if (perguntar)
                    mensagem += "Deseja mais detalhes?";

                bool detalhes = ExibirMensagem(mensagem, perguntar);

                if (detalhes && perguntar)
                {
                    mensagem = "Exceções internas:";
                    Exception inner = ex.InnerException;
                    int numEx = 1;
                    while (inner != null)
                    {
                        mensagem += "Ex. interna " + numEx++ + ": " + ConstruirMensagemErro(inner);
                        inner = inner.InnerException;
                    }

                    if (!(intensidade > 0 && salvarRelatoriosDeErros))
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

            if (intensidade == IntensidadeErro.Gravissimo && fila.Count == 0 && !recover.IsBusy)
            {
                Process.Start(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
                Process.GetCurrentProcess().Kill();
            }                
            else
                reiniciar = true;
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

            arquivo = !string.IsNullOrEmpty(arquivo) ? arquivo = System.IO.Path.GetFileName(arquivo) : "n/a";
            if (string.IsNullOrEmpty(linha)) linha = "n/a";

            return Environment.NewLine + Environment.NewLine + "Exceção principal: " + ex.Message + Environment.NewLine + "Arquivo: " + arquivo + Environment.NewLine + "Linha: " + linha;
        }

        /// <summary>
        /// Método universal para exibir mensagens de alerta ao usuário. Funciona com WFA, WPF e Console.
        /// leoanrdoteck 07/02/2016
        /// </summary>
        /// <param name="mensagem">Mensagem a ser exibida</param>
        /// <param name="perguntar">Perguntar sim (retorna true) ou não (retorna false)</param>
        /// <returns>Resposta sim retorna true e não retorna false</returns>
        /// <TODO>
        /// Corrigir exibição das mensagens em Console
        /// </TODO>
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
        public static void Iniciar(bool exibirErros = true, bool salvarErros= true, bool exibirDetalhes = true)
        {
            // Tratamento de exceções não tratadas e de exceções dos Threads dos Forms
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
            ExibeErrosProgramador = exibirErros;
            SalvarRelatoriosDeErros = salvarErros;
            exibeDetalhesDeErros = exibirDetalhes;
        }
        
        #region Eventos
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            // Gera o relatório do erro e reinicia o sistema
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
        /// Reinicia o sistema se a intensidade do erro é gravissima, assim que termina de salvar o relatório
        /// leonardoteck 30/10/2015
        /// </summary>
        private static void recover_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (fila.Count > 0)
                recover.RunWorkerAsync();
            else if (reiniciar)
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
            /// Erro banal, não é necessário interromper a atividade do sistema
            /// leonardoteck 28/10/2015
            /// </summary>
            Simples = 0,
            /// <summary>
            /// Salva tudo que puder
            /// leonardoteck 28/10/2015
            /// </summary>
            Grave,
            /// <summary>
            /// Erro fatal, toda atividade do sistema deve ser interrompida imediatamente e o programador deve ser contatado
            /// leonardoteck 28/10/2015
            /// </summary>
            Gravissimo
        }
    }
}