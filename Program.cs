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
        static Timer timer = new Timer(200);

        static void Main(string[] args)
        {
            timer.Elapsed += TimerEvent;
            timer.Start();

            var map = Converter.ConvertToMap(worldPath, 454, 54, -354, 498, 64, -316);
            //454, 54, -354, 498, 64, -316

            timer.Stop();

            Console.WriteLine();
            if (!Converter.Aborted)
            {
                if (!Converter.Debuging)
                {
                    File.WriteAllText(mapOutput, map.Serialize());
                }

                var stat = map.GetSolidsCount();
                Console.WriteLine("Done! Blocks:{2}, Solids:{0}, Faces:{1} ", stat.Solids, stat.Faces, Converter.BlockProcessed);
            }
            else
            {
                Console.WriteLine("Operation aborted");
            }
            
            Console.WriteLine("Press \"Esc\" to exit");
            while (Console.ReadKey().Key != ConsoleKey.Escape) ;
        }

        static Converter.ProcessType pt = Converter.ProcessType.Idle;
        static void TimerEvent(object source, ElapsedEventArgs e)
        {
            if (pt == Converter.ProcessType.Idle)
            {
                Console.WriteLine();
            }

            if (pt != Converter.Process)
            {
                pt = Converter.Process;
                Console.WriteLine();
                switch (pt)
                {
                    case Converter.ProcessType.ScanBlocks:
                        Console.WriteLine("Scanning world area...");
                        break;

                    case Converter.ProcessType.GenerateSolids:
                        Console.WriteLine("Generating cs objects...");
                        break;
                }
                Console.WriteLine();
                Console.WriteLine();
            }

            ClearCurrentConsoleLine();
            ClearCurrentConsoleLine();
            switch (pt)
            {
                case Converter.ProcessType.ScanBlocks:
                    Console.WriteLine("{0} / {1} Blocks, Groups:{2}", 
                        Converter.BlockCurrent, Converter.BlockCount, Converter.BlockGroups.Count);
                    PrintProgressBar(Converter.BlockCurrent, Converter.BlockCount);
                    break;

                case Converter.ProcessType.GenerateSolids:
                    Console.WriteLine("{0} / {1} Groups, Solids:{2}",
                        Converter.GroupCurrent, Converter.BlockGroups.Count, Converter.SolidsCurrent);
                    PrintProgressBar(Converter.GroupCurrent, Converter.BlockGroups.Count);
                    break;
            }

            Console.WriteLine("");
        }

        static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));

            if (currentLineCursor > 0)
            {
                currentLineCursor--;
            }
            Console.SetCursorPosition(0, currentLineCursor);
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
            Console.Write("{0}{1}{2}%", new string('#', done), new string('.', rem), (int)(proc * 100));
        }
    }
}
