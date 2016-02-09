using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NexTeckLib;

namespace ConsoleTests
{
    class Program
    {
        // exceção
        static Exception exc = new Exception("Testando uma exception ai", new Exception("inner exception ae rapeize!"));

        static void Main(string[] args)
        {
            // configurações
            Configuracoes.Configurar("ConsoleTests", NexTeckLib.TipoProjeto.Console, false);
            ExceptionHandler.Iniciar(true, false, false);

            #region primeiro teste: sucesso

            ExceptionHandler.ExibirErro(exc, "mensagem ao usuário ai gente!", ExceptionHandler.IntensidadeErro.Grave);
            
            #endregion

            #region segundo teste: sucesso

            throw exc;
            
            #endregion

            //     Testes com Tasks     //
            Task t = new Task(new Action( () => { throw exc; } ));

            #region terceiro teste: falha. Causa: precisa disso no arquivo app.config:
            // <configuration>
            // <runtime>
            // <ThrowUnobservedTaskExceptions enabled = "true"/>
            // </runtime>
            // </configuration>

            t.Start();

            #endregion

            #region quarto teste: falha e sucesso. Causa: a atividade do console termina antes de o ExceptionHandler ser executado dentro do Thread

            t.AddExceptionHandlerExt().Start();

            // Solução: esperar a atividade da task terminar
            t.Wait();

            #endregion
        }
    }
}
