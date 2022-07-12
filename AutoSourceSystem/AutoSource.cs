using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace AutoSource.AutoSourceSystem
{
    public class EditableItem<T> : IEditableObject
    {
        public T Object { get; set; }
        public void BeginEdit()
        {
            throw new NotImplementedException();
        }

        public void CancelEdit()
        {
            throw new NotImplementedException();
        }

        public void EndEdit()
        {
            throw new NotImplementedException();
        }
    }

    [DesignerCategory("")]
    public abstract partial class AutoSource<T> : BindingSource where T : class
    {
        private readonly Dictionary<int, Transaccion<T>> TransactionChanges = new Dictionary<int, Transaccion<T>>();

        private readonly BackgroundWorker Worker = new BackgroundWorker();
        private readonly System.Timers.Timer WorkerTimer = new System.Timers.Timer();
        private readonly SortableBindingList<T> _List;
        private bool isUsingTransactions;
        private bool itemUpdatedFromDatabase;
        private T selectedItemOriginal;
        private readonly PropertyInfo PropiedadID;

        protected bool IsInDesignMode => DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;
        protected T SelectedItemOriginal
        {
            get => selectedItemOriginal;
            private set
            {
                selectedItemOriginal = value;
            }
        }



        [Category("_"), Description("El id del formulario. También puede asignarse programaticamente. Es requerido para poder subir los cambios que haga el usuario.")]
        public int FormID { get; set; }

        [Category("_"), Description("Determina si se pueden editar y subir los cambios a la base de datos."), DefaultValue(false)]
        public bool SoloLectura { get; set; }

        [Bindable(true)]
        public bool TransactionOpen
        {
            get => isUsingTransactions;
            private set
            {
                isUsingTransactions = value;
                Console.WriteLine($"IsUsingTransactions: {TransactionOpen}");
            }
        }
        public bool ItemUpdatedFromDatabase
        {
            get => itemUpdatedFromDatabase;
            private set
            {
                itemUpdatedFromDatabase = value;
            }
        }
        public T SelectedItem => Current as T;


        protected AutoSource()
        {
            _List = new SortableBindingList<T>();

            DataSource = _List;
            if (IsInDesignMode) return;

            PropiedadID = typeof(T).GetProperty(ObtenerNombrePropiedadID());

            ActualizarItems();

            WorkerTimer.Interval = 10;
            WorkerTimer.Elapsed += WorkerTimer_Elapsed;

            Worker.DoWork += Worker_DoWork;
            Worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

            WorkerTimer.Start();
        }

        /// <summary>
        /// Deberia devolver el nombre de la propiedad que sea el identificador único del objeto.
        /// </summary>
        /// <example>
        /// Teniendo un tipo <b>MyObject</b> que su identificador único se encuentra en la propiedad <b>MyId</b>.
        /// <para>Creo un objeto temporal, y devuelvo el <b>nameof()</b> de la propiedad identificadora.</para>
        /// <para><b>Ejemplo:</b></para>
        /// <code>
        /// class MyObject
        /// {
        ///     public int MyId { get; set; }
        ///     public srting Name { get; set; }
        /// }
        /// 
        /// protected override string ObtenerNombrePropiedadID()
        /// {
        ///     MyObject obj = new MyObject();
        ///     return nameof(obj.MyId);
        /// }
        /// </code>
        /// <para>Es similar a devolver directamente el string "MyId", pero con el extra de que es una <b>referencia estática</b> ya que se genera a partir de una propiedad, y si se cambia el nombre de la propiedad identificadora, el programa te avisa con errores de compilacion, o si usas el renombrador, se va a actualizar automaticamente.</para>
        /// <para>Devolver un <b>string</b> no referenciado estaticamente, hace que surjan <b>errores en tiempo de ejecución</b>, que generalmente son dificiles de descubrir y pueden <b>averiar la experiencia del usuario</b>.</para>
        /// </example>
        /// <returns>El nombre exacto de la propiedad identificadora del objeto. Sensible a las mayúsculas y minúsculas.</returns>
        protected abstract string ObtenerNombrePropiedadID();

        /// <summary>
        /// Deberia devolver todos los objetos de la base de datos.
        /// </summary>
        /// <returns>Todos los objetos de la base de datos.</returns>
        protected abstract IEnumerable<T> ObtenerTodosLosItemsDesdeDB();

        /// <summary>
        /// Deberia devolver el nombre del objeto que fue pasado como parámetro.
        /// </summary>
        /// <param name="a">El objeto del cual se debe devolver el nombre.</param>
        /// <returns>El nombre del objeto <b>a</b>.</returns>
        protected abstract string ObtenerNombre(T a);

        /// <summary>
        /// Deberia subir el objeto <b>a</b> a la base de datos, similar a un <b>UPDATE</b> en la jerga de bases de datos.
        /// </summary>
        /// <param name="a">El objeto que contiene datos a modificar en la base de datos.</param>
        protected abstract void SubirModificacionesADB(T a);

        /// <summary>
        /// Deberia borrar el objeto <b>a</b> en la base de datos.
        /// </summary>
        /// <param name="a">El objeto a borrar.</param>
        protected abstract void BorrarItemEnDB(T a);

        /// <summary>
        /// Deberia devolver una version actualizada desde la base de datos, de un objeto <b>a</b>.
        /// </summary>
        /// <param name="a">El objeto desactualizado el cual necesita una actualizacion.</param>
        /// <returns>Una version actualizada del objeto <b>a</b>.</returns>
        protected abstract T ObtenerItemDesdeDB(T a);

        /// <summary>
        /// Deberia crear el objeto <b>a</b> en la base de datos y devolver su nuevo id.
        /// </summary>
        /// <param name="a">El objeto a ser creado.</param>
        /// <returns>El identificador unico del objeto <b>a</b>.</returns>
        protected abstract int CrearItemEnDB(T a);

        protected void CambiarId(T a, int nuevoID)
        {
            PropiedadID.SetValue(a, nuevoID);
        }
        protected int ObtenerId(T a)
        {
            return (int)PropiedadID.GetValue(a);
        }
        protected bool SonIguales(T a, T b)
        {
            if ((a == null && b != null) || (a != null && b == null)) return false;
            if (a == null && b == null) return true;

            var properties = typeof(T).GetProperties().Where(x => x.CanWrite && x.CanRead).ToArray();
            foreach (var prop in properties)
            {
                var valA = prop.GetValue(a);
                var valB = prop.GetValue(b);
                if (!Equals(valA, valB)) return false;
            }
            return true;
        }
        protected T CrearCopia(T a)
        {
            var copia = Activator.CreateInstance<T>();
            CopiarAEnB(a, copia);
            return copia;
        }

        /// <summary>
        /// Copia los valores de las propiedades de <paramref name="a"/> en las propiedades de <paramref name="b"/>.
        /// </summary>
        /// <param name="a">El objeto de origen de datos.</param>
        /// <param name="b">El objeto de destino de datos.</param>
        protected void CopiarAEnB(T a, T b)
        {
            var properties = typeof(T).GetProperties().Where(x => x.CanWrite && x.CanRead).ToArray();
            foreach (var prop in properties)
            {
                prop.SetValue(b, prop.GetValue(a));
            }
        }
        protected T ObtenerItem(int index)
        {
            return (T)this[index];
        }
        protected T ObtenerItem(T a)
        {
            if (TransactionOpen && TransactionChanges.TryGetValue(ObtenerId(a), out Transaccion<T> tran))
            {
                return tran.Item;
            }
            return ObtenerItemDesdeDB(a);
        }
        protected bool IsItemUpdated(T item)
        {
            var remote = ObtenerItem(item);
            return SonIguales(item, remote);
        }
        protected void ActualizarItems()
        {
            Console.WriteLine($"ActualizarItems: Start Position: {Position}");

            var dbitems = ObtenerTodosLosItemsDesdeDB();
            var localitems = List.OfType<T>();

            var locales = localitems.ToDictionary(x => ObtenerId(x), x => x);
            ItemUpdatedFromDatabase = true;

            foreach (var item in dbitems)
            {
                int id = ObtenerId(item);
                locales.TryGetValue(id, out T currentItem);
                if (currentItem == null)
                {
                    // Item nuevo
                    Add(item);
                }
                else
                {
                    // Actualizar Item
                    CopiarAEnB(item, currentItem);
                    locales.Remove(id);
                }
            }

            foreach (var item in locales.Values)
            {
                Remove(item);
            }

            ItemUpdatedFromDatabase = false;
            Console.WriteLine($"ActualizarItems: End Position: {Position}");
        }


        private void WorkerTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            WorkerTimer.Interval = 5000;
            if (Worker.IsBusy || TransactionOpen) return;
            WorkerTimer.Stop();
            Worker.RunWorkerAsync();
        }
        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (TransactionOpen) return;
            if (e.Result is WorkerResult<T> result)
            {
                ItemUpdatedFromDatabase = true;
                if (result.Nuevos.Count > 0)
                {
                    // Hay nuevos items en la base de datos.
                    foreach (var item in result.Nuevos)
                    {
                        Add(item);
                    }
                }
                if (result.ABorrar.Count > 0)
                {
                    // Se borraron en la base de datos.
                    foreach (var item in result.ABorrar)
                    {
                        if (SelectedItem != null &&
                            ObtenerId(SelectedItem) == ObtenerId(item) &&
                            Position > 0)
                        {
                            Position--;
                        }
                        Remove(item);
                    }
                }
                ItemUpdatedFromDatabase = false;
            }
            WorkerTimer.Start();
        }
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;

            List<T> nuevos = new List<T>();

            var dbitems = ObtenerTodosLosItemsDesdeDB();
            var localitems = List.OfType<T>();

            var locales = localitems.ToDictionary(x => ObtenerId(x), x => x);

            foreach (var item in dbitems)
            {
                int id = ObtenerId(item);
                if (!locales.ContainsKey(id))
                {
                    // Item nuevo
                    nuevos.Add(item);
                }
                else
                {
                    locales.Remove(id);
                }
            }

            e.Result = new WorkerResult<T>
            {
                Nuevos = nuevos,
                ABorrar = locales.Values.ToList()
            };
        }


        /// <summary>
        /// <para>Crea una transaccion, y las ediciones siguientes quedaran en un buffer, luego puede decidir si aplicar o descartar las modificaciones.</para>
        /// <para>Devuelve <see langword="true"/> si se pudo iniciar una transaccion.</para>
        /// <para>Devuelve <see langword="false"/> si <see cref="SoloLectura"/> == <see langword="true"/></para>
        /// </summary>
        /// <returns></returns>
        public bool EmpezarTransaccion()
        {
            if (SoloLectura) return false;
            TransactionChanges.Clear();
            TransactionOpen = true;
            return true;
        }

        /// <summary>
        /// Descarta una transaccion abierta. Si no hay ninguna abierta, no hace nada.
        /// </summary>
        public void DescartarTransaccion()
        {
            if (!TransactionOpen) return;
            foreach (var transaccion in TransactionChanges.Values)
            {
                if (transaccion.Tipo == TipoTransaccion.Crear)
                {
                    Remove(transaccion.Item);
                }
            }

            TransactionChanges.Clear();
            TransactionOpen = false;
            ActualizarItems();
            ResetBindings(false);
        }

        /// <summary>
        /// Aplica los cambios de una transaccion abierta. Si no hay ninguna abierta o no hay cambios, no hace nada.
        /// </summary>
        public void AplicarTransaccion()
        {
            if (!TransactionOpen) return;

            foreach (var transaccion in TransactionChanges.Values)
            {
                switch (transaccion.Tipo)
                {
                    case TipoTransaccion.Crear:
                        {
                            if (transaccion.Item != null)
                            {
                                ItemUpdatedFromDatabase = true;
                                var result = CrearItemEnDB(transaccion.Item);
                                ResetCurrentItem();
                                ItemUpdatedFromDatabase = false;

                                if (result >= 0) continue;

                                MessageBox.Show($"Error: {result}\nNo se pudo crear el item {ObtenerNombre(transaccion.Item)} en la base de datos.");
                            }
                            break;
                        }
                    case TipoTransaccion.Modificar:
                        {
                            T itemRemoto;
                            do
                            {
                                itemRemoto = ObtenerItemDesdeDB(transaccion.Item);

                                // Hubieron modificaciones remotas antes de aplicar los cambios locales
                                // Hay que decidir que cambios se aplican:
                                // 1- Mostrar formulario con los cambios
                                // 2- Tomar los cambios desde el formulario y ponerlos en Item y ItemBase
                                // 3- Continuar con el do-while
                                // var item = FormDiff.ShowDialog(transaccion);
                                // transaccion.Item = item;
                                // transaccion.ItemBase = item;

                            } while (!SonIguales(itemRemoto, transaccion.ItemBase));

                            if (!SonIguales(itemRemoto, transaccion.Item))
                            {
                                ItemUpdatedFromDatabase = true;
                                SubirModificacionesADB(transaccion.Item);
                                ItemUpdatedFromDatabase = false;
                            }
                            break;
                        }
                    case TipoTransaccion.Eliminar:
                        {
                            T itemRemoto = null;
                            do
                            {
                                var itemRemoto2 = ObtenerItemDesdeDB(transaccion.Item);
                                if (SonIguales(itemRemoto, itemRemoto2)) break;
                                itemRemoto = itemRemoto2;
                                // Hubieron modificaciones remotas antes de aplicar los cambios locales
                                // Hay que decidir que cambios se aplican:
                                // 1- Mostrar formulario con los cambios
                                // 2- Tomar los cambios desde el formulario y ponerlos en Item y ItemBase
                                // 3- Continuar con el do-while
                                // var item = FormDiff.ShowDialog(transaccion);
                                // transaccion.Item = item;
                                // transaccion.ItemBase = item;

                            } while (!SonIguales(itemRemoto, transaccion.ItemBase));

                            if (itemRemoto != null)
                            {
                                ItemUpdatedFromDatabase = true;
                                BorrarItemEnDB(transaccion.Item);
                                ItemUpdatedFromDatabase = false;
                            }
                            break;
                        }
                }
            }

            TransactionOpen = false;
            ItemUpdatedFromDatabase = true;
            TransactionChanges.Clear();
            ActualizarItems();
            ResetBindings(false);
            ItemUpdatedFromDatabase = false;
        }

        protected override void OnListChanged(ListChangedEventArgs e)
        {
            switch (e.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                    {
                        if (ItemUpdatedFromDatabase || !TransactionOpen) break;
                        var item = ObtenerItem(e.NewIndex);
                        if (!TransactionChanges.ContainsKey(ObtenerId(item)))
                        {
                            TransactionChanges.Add(ObtenerId(item), new Transaccion<T>(TipoTransaccion.Crear, CrearCopia(item), item));
                        }
                        break;
                    }
                case ListChangedType.ItemChanged:
                    {
                        if (ItemUpdatedFromDatabase || !TransactionOpen) break;
                        var item = ObtenerItem(e.NewIndex);
                        var id = ObtenerId(item);
                        if (!TransactionChanges.ContainsKey(id))
                        {
                            TransactionChanges.Add(id, new Transaccion<T>(TipoTransaccion.Modificar, CrearCopia(item), SelectedItemOriginal));
                        }
                        else
                        {
                            TransactionChanges[id].Item = CrearCopia(item);
                        }
                        break;
                    }
                case ListChangedType.ItemDeleted:
                    {
                        if (ItemUpdatedFromDatabase || !TransactionOpen) break;
                        var item = ObtenerItem(e.NewIndex);
                        if (TransactionChanges.ContainsKey(ObtenerId(item)))
                        {
                            TransactionChanges.Add(ObtenerId(item), new Transaccion<T>(TipoTransaccion.Eliminar, CrearCopia(item), item));
                        }
                        break;
                    }
            }
            base.OnListChanged(e);
        }

        protected override void OnPositionChanged(EventArgs e)
        {
            if (IsInDesignMode) return;

            if (Position >= 0 && List.Count > Position)
            {
                int position = Position;
                var oldItem = ObtenerItem(position);
                T remoteItem = ObtenerItem(oldItem);

                SelectedItemOriginal = remoteItem != null ? CrearCopia(remoteItem) : null;

                if (remoteItem == null)
                {
                    // El item se borró de la base de datos.
                    ItemUpdatedFromDatabase = true;
                    List.RemoveAt(position);
                    ItemUpdatedFromDatabase = false;
                    return;
                }

                if (!SonIguales(remoteItem, oldItem))
                {
                    ItemUpdatedFromDatabase = true;
                    CopiarAEnB(remoteItem, List[position] as T);
                    ItemUpdatedFromDatabase = false;
                    ResetItem(Position);
                }

            }
            base.OnPositionChanged(e);
            //Console.WriteLine($"OnPositionChanged: End Position: {Position}");
        }
        public override int Count
        {
            get
            {
                int news = TransactionChanges.Count(x => x.Value.Tipo == TipoTransaccion.Crear);
                return base.Count + news;
            }
        }

        public override object AddNew()
        {
            if (!TransactionOpen) return null;
            T nuevo = Activator.CreateInstance<T>();
            int index = this.Count > 0 ? this.OfType<T>().Max(x => ObtenerId(x)) + 1 : 0;
            CambiarId(nuevo, index);
            Position = this.Add(CrearCopia(nuevo));
            return nuevo;
        }
        public override System.Collections.IEnumerator GetEnumerator()
        {
            foreach (var item in List)
            {
                yield return item;
            }
            foreach (var item in TransactionChanges
                        .Where(x => x.Value.Tipo == TipoTransaccion.Crear)
                        .OrderBy(x => x.Key))
            {
                yield return item;
            }
        }
        public override object this[int index]
        {
            get
            {
                if (index >= List.Count)
                {
                    return TransactionChanges
                        .Where(x => x.Value.Tipo == TipoTransaccion.Crear)
                        .OrderBy(x => x.Key)
                        .ElementAt(List.Count - index).Value.Item;
                }
                var item = List[index];
                if (TransactionChanges.TryGetValue(ObtenerId(item as T), out Transaccion<T> _item))
                {
                    return _item.Item;
                }
                return base[index];
            }
            set => base[index] = value;
        }
        public override void RemoveAt(int index)
        {
            if (!TransactionOpen) return;

            var item = ObtenerItem(index);
            string itemName = ObtenerNombre(item);

            var dr = MessageBox.Show($"¿Desea borrar el item: {itemName}?", "Glyms", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (dr == DialogResult.Yes)
            {
                int id = ObtenerId(item);
                if (TransactionChanges.ContainsKey(id))
                {
                    TransactionChanges[id].Tipo = TipoTransaccion.Eliminar;
                }
                else
                {
                    TransactionChanges.Add(id, new Transaccion<T>(TipoTransaccion.Eliminar, item, item));
                }
            }
        }
    }
}
