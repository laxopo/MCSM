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


        public EntityScript Copy()
        {
            var e = new EntityScript();
            e.Macros = Macros;
            e.ClassName = ClassName;

            Parameters.ForEach(x => e.Parameters.Add(x.Copy()));

            return e;
        }

        public class Parameter : ICloneable
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string ValueType { get; set; }

            public object Clone()
            {
                return MemberwiseClone();
            }

            public Parameter Copy()
            {
                return (Parameter)Clone();
            }
        }
    }
}
