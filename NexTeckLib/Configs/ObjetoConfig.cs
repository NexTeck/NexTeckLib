using System;

namespace NexTeckLib
{
    /// <summary>
    /// Objeto Abstrato que possui as configurações para serem salvas
    /// lembrando que para toda classe filha dessa ela deve ser Serializavel
    /// Andrei 27/10/2015
    /// </summary>
    [Serializable()]
    public abstract class ObjetoConfig : ICloneable
    {
        /// <summary>
        /// Define as configurações padrão para esse objeto
        /// </summary>
        public abstract void DefinirPadrao();

        /// <summary>
        /// Salva a configuração atual
        /// </summary>
        public void Salvar()
        {
            ConfigController.Salvar(this);
        }

        /// <summary>
        /// Torna o objeto clonavel
        /// </summary>
        /// <returns>Clone do objeto</returns>
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
