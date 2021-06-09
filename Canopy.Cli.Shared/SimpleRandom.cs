using System;
using System.Collections.Generic;
using System.Linq;

namespace Canopy.Cli.Shared
{
    public class SimpleRandom : ISimpleRandom
    {
        public const int DefaultSeed = 101676;

        private readonly Random random;

        public SimpleRandom()
            : this(DefaultSeed)
        {
        }

        public SimpleRandom(int seed)
        {
            this.random = new Random(seed);
        }

        public bool Should(double probablility)
        {
            return this.NextDouble() < probablility;
        }

        public int Which(params double[] probablility)
        {
            var value = this.NextDouble();
            var total = 0.0;
            for (var i = 0; i < probablility.Length; ++i)
            {
                total += probablility[i];
                if (value < total)
                {
                    return i;
                }
            }

            return -1;
        }

        public int NextInclusive(int minValueInclusive, int maxValueInclusive)
        {
            return this.random.Next(minValueInclusive, maxValueInclusive + 1);
        }

        public int NextExclusive(int minValueInclusive, int maxValueExclusive)
        {
            return this.random.Next(minValueInclusive, maxValueExclusive);
        }

        public void NextBytes(byte[] buffer)
        {
            this.random.NextBytes(buffer);
        }

        public double NextDouble()
        {
            return this.random.NextDouble();
        }

        public bool NextBoolean()
        {
            return this.random.Next(0, 2) == 1;
        }

        public T Next<T>()
            where T : Enum
        {
            var values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(this.NextExclusive(0, values.Length));
        }

        // https://stackoverflow.com/a/1344258/37725
        public string NextString(int length = 30)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[this.NextExclusive(0, chars.Length)];
            }

            return new String(stringChars);
        }

        public IList<T> Shuffled<T>(IEnumerable<T> items)
        {
            var result = items.ToList();
            Shuffle(result, this);
            return result;
        }

        private static void Shuffle<T>(IList<T> data, ISimpleRandom random)
        {
            int swapIndex;
            T temp;
            for (int i = 0; i < data.Count; i++)
            {
                swapIndex = random.NextExclusive(i, data.Count);
                temp = data[i];
                data[i] = data[swapIndex];
                data[swapIndex] = temp;
            }
        }
    }
}