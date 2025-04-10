using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Timers;

namespace MCSMapConv
{
    class Program
    {
        static string worldPath = @"F:\minecraft_new\UltimMC_5\instances\1.12.2_test\.minecraft\saves\mcsmap";
        static string mapOutput = @"D:\games\vhe\maps\out.map";
        static Timer timer = new Timer(100);
        static bool done = false;

        static void Main(string[] args)
        {
            Converter.LoadConfig("config.json");

            Console.CursorVisible = false;
            timer.Elapsed += TimerEvent;
            timer.Start();

            var map = Converter.ConvertToMap(worldPath, 0, 454, 54, -354, 498, 64, -316);
            //454, 54, -354, 498, 64, -316

            while (!done) { }
            Console.WriteLine();
            Console.CursorVisible = true;

            if (!Converter.Aborted)
            {
                if (!Converter.Debuging)
                {
                    File.WriteAllText(mapOutput, map.Serialize());
                }

                var stat = map.GetSolidsCount();
                Console.WriteLine("Done! Blocks:{2}, Solids:{0}, Faces:{1}, Entities:{3}", 
                    stat.Solids, stat.Faces, Converter.BlockProcessed, Converter.Map.Data.Count - 1);
            }
            else
            {
                Console.WriteLine("Operation aborted");
            }
            
            Console.WriteLine("Press \"Esc\" to exit");
            while (Console.ReadKey().Key != ConsoleKey.Escape) ;
        }

        static Converter.ProcessType pt = Converter.ProcessType.Idle;
        static int row = 0;
        static bool frame = false;
        static void TimerEvent(object source, ElapsedEventArgs e)
        {
            if (Converter.Aborted)
            {
                done = true;
                return;
            }

            if (Converter.Process == Converter.ProcessType.Idle)
            {
                return;
            }

            timer.Stop();

            var b = Converter.BlockCurrent;
            var bc = Converter.BlockCount;
            var bp = Converter.BlockProcessed;
            var gc = Converter.GroupCurrent;
            var bgc = Converter.BlockGroups.Count;
            var sc = Converter.SolidsCurrent;
            var ec = Converter.EntitiesCurrent;

            if (pt != Converter.Process)
            {
                pt = Converter.Process;

                if (frame)
                {
                    EraseRender();
                }

                switch (pt)
                {
                    case Converter.ProcessType.ScanBlocks:
                        Console.WriteLine();
                        Console.WriteLine("Scanning world area...");
                        row = Console.CursorTop;
                        break;

                    case Converter.ProcessType.GenerateSolids:
                        Console.WriteLine("Blocks:{0}, Used:{1}, Groups:{2}", bc, bp, bgc);
                        Console.WriteLine();
                        Console.WriteLine("Generating cs objects...");
                        row = Console.CursorTop;
                        break;

                    case Converter.ProcessType.Done:
                        Console.WriteLine("Created solids:{0} (S:{1} E:{2})", sc + ec, sc, ec);
                        done = true;
                        return;
                }
            }

            if (Converter.Message.Unread)
            {
                if (frame)
                {
                    EraseRender();
                }

                Converter.Message.Read();
                row = Console.CursorTop;
            }

            RenderProgress();

            timer.Start();
        }

        static void EraseRender()
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            EraseSpace();
            EraseSpace();
            Console.SetCursorPosition(0, row);
            frame = false;
        }

        static void RenderProgress()
        {
            var b = Converter.BlockCurrent;
            var bc = Converter.BlockCount;
            var bp = Converter.BlockProcessed;
            var gc = Converter.GroupCurrent;
            var bgc = Converter.BlockGroups.Count;
            var sc = Converter.SolidsCurrent;
            var ec = Converter.EntitiesCurrent;

            Console.SetCursorPosition(0, row + 2);

            switch (pt)
            {
                case Converter.ProcessType.ScanBlocks:
                    ConsoleWriteRow("{0} / {1} Blocks, Used:{2}, Groups:{3}", b, bc, bp, bgc);
                    PrintProgressBar(b, bc);
                    break;

                case Converter.ProcessType.GenerateSolids:
                    ConsoleWriteRow("{0} / {1} Groups, Solids:{2} (S:{3} E:{4})", gc, bgc, sc + ec, sc, ec);
                    PrintProgressBar(gc, bgc);
                    break;
            }

            frame = true;
        }

        static void ConsoleWriteRow(string text, params object[] args)
        {
            Console.Write(text, args);
            EraseSpace();
        }

        static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        static void EraseSpace()
        {
            Console.WriteLine(new string(' ', Console.WindowWidth - Console.CursorLeft - 1));
        }

        static void PrintProgressBar(int current, int count)
        {
            int barWidth = Console.WindowWidth - 4;
            double proc = (double)current / count;
            int done = (int)(barWidth * proc);
            if (done > barWidth)
            {
                done = barWidth;
            }

            int rem = barWidth - done;
            Console.Write("{0}{1}{2}%", new string('#', done), new string('-', rem), (int)(proc * 100));
        }
    }
}
