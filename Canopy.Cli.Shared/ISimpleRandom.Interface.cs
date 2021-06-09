using System;
using System.Collections.Generic;

namespace Canopy.Cli.Shared
{
    public interface ISimpleRandom
    {
        T Next<T>() where T : Enum;
        bool NextBoolean();
        void NextBytes(byte[] buffer);
        double NextDouble();
        int NextExclusive(int minValueInclusive, int maxValueExclusive);
        int NextInclusive(int minValueInclusive, int maxValueInclusive);
        bool Should(double probablility);
        IList<T> Shuffled<T>(IEnumerable<T> items);
        int Which(params double[] probablility);
    }
}