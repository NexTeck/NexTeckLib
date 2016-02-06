using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTests
{
    class Program
    {
        static void Main(string[] args)
        {
            NexTeckLib.Configuracoes.Configurar("ConsoleTests", NexTeckLib.TipoProjeto.Console);
            NexTeckLib.ExceptionHandler.Iniciar(true, false, true);

            Exception exc = new Exception("Testando uma exception ai");

            NexTeckLib.ExceptionHandler.ExibirErro(exc, "mensagem ao usuário ai gente!", NexTeckLib.ExceptionHandler.IntensidadeErro.Grave);
        }
    }
}
