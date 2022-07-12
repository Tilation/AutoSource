using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSource.AutoSourceSystem
{
    internal enum TipoTransaccion
    {
        Crear,
        Modificar,
        Eliminar
    }

    internal class Transaccion<T>
    {
        public Transaccion(TipoTransaccion tipo, T item, T itemBase)
        {
            this.Tipo = tipo;
            this.Item = item;
            this.ItemBase = itemBase;
        }

        public TipoTransaccion Tipo { get; set; }
        public T Item { get; set; }
        public T ItemBase { get; set; }
    }
}
