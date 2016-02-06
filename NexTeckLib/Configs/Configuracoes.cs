using System;

namespace NexTeckLib
{
    /// <summary>
    /// Armazena as configurações gerais necessárias para o funcionamento da biblioteca
    /// </summary>
    public static class Configuracoes
    {
        private static string nomePrograma;
        /// <summary>
        /// Nome do programa
        /// </summary>
        public static string NomePrograma
        {
            get
            {
                if (!configurado && !alertado)
                {
                    alertado = true;
                    throw new Exception("O NexTeckLib não foi configurado! Programador, chame o método NexTeckLib.Configuracoes.Configurar() no começo do seu software.");
                }
                else return nomePrograma;
            }
            private set
            {
                nomePrograma = value;
            }
        }

        /// <summary>
        /// Descrimina se o projeto é Windows Forms, Console, WPF, etc
        /// </summary>
        public static TipoProjeto TipoProjeto { get; private set; }

        /// <summary>
        /// Configurações personalizadas do software
        /// </summary>
        public static ObjetoConfig ConfiguracoesPessoais;
        
        /// <summary>
        /// Flag de configuração
        /// </summary>
        private static bool configurado = false;
        /// <summary>
        /// Flag para não alertar mais de uma vez de que precisa configurar
        /// </summary>
        private static bool alertado = false;

        /// <summary>
        /// Define as variáveis de configuração
        /// </summary>
        /// <param name="nomePrograma">O nome deste software</param>
        /// <param name="tipoProjeto">Tipo do projeto</param>
        public static void Configurar(string nomePrograma, TipoProjeto tipoProjeto)
        {
            NomePrograma = nomePrograma == null ? "" : nomePrograma;
            TipoProjeto = tipoProjeto;
            configurado = true;
        }
    }
    
    /// <summary>
    /// Tipo do projeto, serve para a biblioteca resolver problemas de referências
    /// </summary>
    public enum TipoProjeto
    {
        WFA,
        WPF,
        Console
    }
}
