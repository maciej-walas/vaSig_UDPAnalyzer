using System;

namespace UDPAnalyzer.Helpers
{
    public static class RandomGenerator
    {
        private static readonly Random GetRandom = new();

        public static uint GetRandomNumber()
        {
            lock (GetRandom)
            {
                var number = GetRandom.Next(int.MinValue, int.MaxValue);
                return (uint)(number + (uint)int.MaxValue);
            }
        }
    }
}