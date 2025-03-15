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
                Console.WriteLine(msg);
                Console.WriteLine("[R]etry, [S]kip, [A]bort ?");

                ConsoleKeyInfo k = new ConsoleKeyInfo();
                while (k.Key != ConsoleKey.R && k.Key != ConsoleKey.S && k.Key != ConsoleKey.A)
                {
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write(' ');
                    Console.SetCursorPosition(0, Console.CursorTop);
                    k = Console.ReadKey();
                }

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
