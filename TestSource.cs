using AutoSource.AutoSourceSystem;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AutoSource
{
    public class TestSource : AutoSource<TestObject>
    {
        private readonly string filename = "data.json";

        private Dictionary<int, TestObject> db;

        protected override void BorrarItemEnDB(TestObject a)
        {
            db.Remove(a.Id);
            File.WriteAllText(filename, JsonConvert.SerializeObject(db, Formatting.Indented));
        }

        protected override int CrearItemEnDB(TestObject a)
        {
            var id = a.Id;
            db.Add(id, a);
            File.WriteAllText(filename, JsonConvert.SerializeObject(db, Formatting.Indented));
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

        protected override string ObtenerNombre(TestObject a)
        {
            return $"{a.Id} - {a.Name} - {a.Address}";
        }

        protected override string ObtenerNombrePropiedadID()
        {
            return nameof(TestObject.Id);
        }

        protected override IEnumerable<TestObject> ObtenerTodosLosItemsDesdeDB()
        {
            if (db == null)
            {
                if (File.Exists(filename))
                {
                    db = JsonConvert.DeserializeObject<Dictionary<int, TestObject>>(File.ReadAllText(filename));
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
            File.WriteAllText(filename, JsonConvert.SerializeObject(db, Formatting.Indented));
        }
    }
}
