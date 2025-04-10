using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSMapConv
{
    public class BlockMissMsg
    {
        private List<string> Messages { get; set; } = new List<string>();
        private List<BlockIDs> BlockID { get; set; } = new List<BlockIDs>();
        private Messaging Messaging { get; set; }

        public BlockMissMsg(Messaging messaging)
        {
            Messaging = messaging;
        }

        public enum Result
        {
            None,
            Retry,
            Skip,
            Abort
        }

        public Result Message(int blockID, int blockData, string text, bool onlyIDs)
        {
            string msg = "Warning: Block=" + blockID + ":" + blockData + " " + text;
            bool present;

            if (onlyIDs)
            {
                present = BlockID.Find(x => x.ID == blockID && x.Data == blockData) != null;
            }
            else
            {
                present = Messages.Contains(msg);
            }

            if (!present)
            {
                Messaging.WriteUrgent(msg);
                Messaging.WriteUrgent("[R]etry, [S]kip, [A]bort ?");

                ConsoleKeyInfo k = new ConsoleKeyInfo();
                while (k.Key != ConsoleKey.R && k.Key != ConsoleKey.S && k.Key != ConsoleKey.A)
                {
                    k = Console.ReadKey();
                }

                Messaging.UrgentState = false;

                switch (k.Key)
                {
                    case ConsoleKey.R:
                        return Result.Retry;

                    case ConsoleKey.S:
                        if (onlyIDs)
                        {
                            BlockID.Add(new BlockIDs() { ID = blockID, Data = blockData });
                        }
                        else
                        {
                            Messages.Add(msg);
                        }
                        return Result.Skip;

                    case ConsoleKey.A:
                        return Result.Abort;
                }
            }

            return Result.None;
        }
    }
}
