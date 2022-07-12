# AutoSource
Es una libreria que provee una plantilla del componente `BindingSource`, pero que expone metodos simples, pensado para esos proyectos pequeños que no van a usar una bases de datos dedicada y que, por ejemplo, usen un diccionario escrito como json en el disco; pero que igualmente se puedan beneficiar de enlazado de datos.

Los beneficios de usar un `AutoSource` en comparacion a un `BindingSource` tradicional, es que este ultimo hay que configurarlo cada vez que se agrega a un formulario, en cambio un `AutoSource` se programa una sola vez y ya se puede usar en tu aplicación, solo queda enlazar controles desde el diseñador.

Aqui un ejemplo de un `AutoSource` que esta enlazado a diccionario que es persistido en un archivo de texto usando `Newtonsoft.Json`

```csharp
using AutoSource.AutoSourceSystem;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutoSource
{
    public class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }

    public class TestSource : AutoSource<TestObject>
    {
        private readonly string archivo = "datos.json";
        private Dictionary<int, TestObject> db;

        protected override void BorrarItemEnDB(TestObject a)
        {
            db.Remove(a.Id);
            File.WriteAllText(archivo, JsonConvert.SerializeObject(db, Formatting.Indented));
        }

        protected override int CrearItemEnDB(TestObject a)
        {
            var id = a.Id;
            db.Add(id, a);
            File.WriteAllText(archivo, JsonConvert.SerializeObject(db, Formatting.Indented));
            return id;
        }

        protected override TestObject ObtenerItemDesdeDB(TestObject a)
        {
            if (db.TryGetValue(a.Id, out TestObject b))
            {
                return b;
            }
            return null;
        }

        protected override string ObtenerNombre(TestObject a) => $"{a.Id} - {a.Name} - {a.Address}";

        protected override string ObtenerNombrePropiedadID() => nameof(TestObject.Id);

        protected override IEnumerable<TestObject> ObtenerTodosLosItemsDesdeDB()
        {
            if (db == null)
            {
                if (File.Exists(archivo))
                {
                    db = JsonConvert.DeserializeObject<Dictionary<int, TestObject>>(File.ReadAllText(archivo));
                }
                else
                {
                    db = new Dictionary<int, TestObject>();
                }
            }
            return db.Values.Select(x => CrearCopia(x));
        }

        protected override void SubirModificacionesADB(TestObject a)
        {
            db[a.Id] = CrearCopia(a);
            File.WriteAllText(archivo, JsonConvert.SerializeObject(db, Formatting.Indented));
        }
    }
}

```
