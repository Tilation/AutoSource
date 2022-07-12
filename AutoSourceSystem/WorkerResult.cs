using System.Collections.Generic;

namespace BindingSourceTests
{
    internal struct WorkerResult<T> where T : class
    {
        public List<T> ABorrar { get; set; }
        public List<T> Nuevos { get; set; }
    }
}
