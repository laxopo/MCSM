using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace MCSMapConv
{
    public static class Random
    {
        private static RandomNumberGenerator rng = RandomNumberGenerator.Create();

        /*public static float Generate(float min, float max)
        {
            var randomBytes = new byte[2];
            rng.GetBytes(randomBytes);
            uint trueRandom = BitConverter.ToUInt16(randomBytes, 0);


        }*/
    }
}
