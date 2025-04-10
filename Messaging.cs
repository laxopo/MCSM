using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSMapConv
{
    public class Messaging
    {
        public List<string> Messages { get; private set; } = new List<string>();
        public int Index { get; private set; }
        public bool Unread { get; private set; }

        public void Write(string text, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                text = text.Replace("{" + i + "}", args[i].ToString());
            }

            Messages.Add(text);
            Unread = true;
        }

        public void Read()
        {
            for (int i = Index; i < Messages.Count; i++)
            {
                Console.WriteLine(Messages[i]);
                Index++;
            }

            Unread = false;
        }
    }
}
