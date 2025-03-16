using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NamedBinaryTag;
using Newtonsoft.Json;
using System.IO;

namespace MCSMapConv
{
    class Program
    {
        static string worldPath = @"F:\minecraft_new\UltimMC_5\instances\1.12.2_test\.minecraft\saves\mcsmap";
        static string mapOutput = @"D:\games\vhe\maps\out.map";

        static void Main(string[] args)
        {
            World world = new World(worldPath);
            var map = Converter.ConvertToMap(world, 455, 54, -354, 498, 64, -316);

            Console.WriteLine();
            if (!Converter.Aborted)
            {
                if (!Converter.Debuging)
                {
                    File.WriteAllText(mapOutput, map.Serialize());
                }

                var stat = map.GetSolidsCount();
                Console.WriteLine("Done! Blocks:{2}, Solids:{0}, Faces:{1} ", stat.Solids, stat.Faces, Converter.BlockCount);
            }
            else
            {
                Console.WriteLine("Operation aborted");
            }
            
            Console.WriteLine("Press \"Esc\" to exit");
            while (Console.ReadKey().Key != ConsoleKey.Escape) ;
        }
    }
}
