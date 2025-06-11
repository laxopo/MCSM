using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSM
{
    public class Messaging
    {
        public List<string> Messages { get; private set; } = new List<string>();
        public List<string> UrgentMessages { get; private set; } = new List<string>();
        public int Index { get; private set; }
        public bool Unread { get; private set; }
        public bool UnreadUrgent { get; private set; }
        public bool UrgentState { get; set; }
        public bool Mute { get; set; }

        public enum MessageType
        {
            Default,
            Urgent
        }

        public void Write(string text, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                text = text.Replace("{" + i + "}", args[i].ToString());
            }

            Messages.Add(text);
            Unread = true;
        }

        public void WriteUrgent(string text, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                text = text.Replace("{" + i + "}", args[i].ToString());
            }

            UrgentMessages.Add(text);
            UnreadUrgent = true;
            UrgentState = true;
        }

        public void Read(MessageType messageType = MessageType.Default)
        {
            if (messageType == MessageType.Default)
            {
                if (Mute)
                {
                    return;
                }

                for (int i = Index; i < Messages.Count; i++)
                {
                    Console.WriteLine(Messages[i]);
                    Index++;
                }

                Unread = false;
            }
            else
            {
                UrgentMessages.ForEach(um => Console.WriteLine(um));
                UrgentMessages.Clear();
                UnreadUrgent = false;
            }
        }
    }
}
