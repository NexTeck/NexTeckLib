using System;
using System.Collections.Generic;

namespace NexTeckLib
{
    /// <summary>
    /// Classe que será serializada com as exceções e suas datas
    /// Leonardo Costa 30/10/2015
    /// </summary>
    [Serializable()]
    class ErroConfig : ObjetoConfig
    {
        /// <summary>
        /// Struct que representa um erro
        /// </summary>
        [Serializable()]
        class Erro
        {
            public Exception Ex;
            public string NomePrograma = Configuracoes.NomePrograma;
            public DateTime Data;

            public Erro(Exception ex, DateTime data)
            {
                Ex = ex;
                Data = data;
            }
        }

        /// <summary>
        /// Lista de exceções e suas datas
        /// Leonardo Costa 30/10/2015
        /// </summary>
        private List<Erro> erros;

        /// <summary>
        /// Adiciona uma exceção no objeto.
        /// Leonardo Costa 30/10/2015
        /// </summary>
        /// <param name="ex">Exceção</param>
        public void AddError(Exception ex)
        {
            Erro erro = new Erro(ex, DateTime.Now);
            erros.Add(erro);
        }

        /// <summary>
        /// Adiciona um conjunto de Exceptions
        /// Leonardo Costa 30/10/2015
        /// </summary>
        /// <param name="ex">Conjunto de exceções. Não é necessário adicionar InnerExceptions, somente a BaseException</param>
        public void AddErrorRange(Exception[] ex)
        {
            foreach (Exception e in ex)
                AddError(e);
        }

        public override void DefinirPadrao()
        {
            erros = new List<Erro>();
        }

        /// <summary>
        /// Construtor que inicializa o objeto com uma exceção
        /// Leonardo Costa 30/10/2015
        /// </summary>
        /// <param name="ex">Primeira exceção no objeto</param>
        public ErroConfig(Exception ex)
        {
            DefinirPadrao();
            AddError(ex);
        }

        /// <summary>
        /// Construtor que inicializa um objeto vazio
        /// Leonardo Costa 30/10/2015
        /// </summary>
        public ErroConfig()
        {
            DefinirPadrao();
        }
    }
}
