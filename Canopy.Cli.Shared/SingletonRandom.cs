using System;
namespace Canopy.Cli.Shared
{
    public static class SingletonRandom
    {
        public static readonly SimpleRandom Instance = new SimpleRandom();
    }
}