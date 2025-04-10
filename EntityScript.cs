using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSMapConv
{
    public class EntityScript
    {
        public string Macros { get; set; }
        public string ClassName { get; set; }
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();

        public class Parameter
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string ValueType { get; set; }
        }
    }
}
