using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSM
{
    public class Variable
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public static string[] GetLineList(List<Variable> variables)
        {
            var list = new List<string>();
            variables.ForEach(x => list.Add(GetLine(x)));
            return list.ToArray();
        }

        //public static List<Variable> GetVariableList()

        public static string GetLine(Variable variable)
        {
            return variable.Name + " = " + variable.Value;
        }

        public static string GetLine(string name, string value)
        {
            return name + " = " + value;
        }

        public static Variable GetVariable(string line)
        {
            var args = GetLineParams(line);
            return new Variable()
            {
                Name = args[0].Trim(),
                Value = args[1].Trim()
            };
        }

        public static string[] GetLineParams(string line)
        {
            var args = line.Split('=');
            args[0] = args[0].Trim();
            args[1] = args[1].Trim();
            return args;
        }

        public static string GetVariableValue(List<Variable> variables, string name)
        {
            if (name[0] == '@')
            {
                name = name.Remove(0, 1);
            }

            var v = variables.Find(x => x.Name.ToUpper() == name.ToUpper());
            if (v == null)
            {
                return null;
            }

            return v.Value;
        }
    }
}
