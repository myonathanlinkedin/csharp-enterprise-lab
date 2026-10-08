using System;

namespace BarelyMonotone
{
    public struct Sequence
    {
        public readonly int[] Values;
        public Sequence(int[] values) => Values = values;
        public int Length => Values.Length;
    }

    public interface IConvolutionEngine
    {
        int[] Convolve(Sequence a, Sequence b);
    }
}
