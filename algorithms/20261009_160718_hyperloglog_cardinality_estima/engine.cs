using System.Collections.Generic;
using System;
using System.Numerics;

namespace HyperLogLogNS
{
    public class HyperLogLog
    {
        private readonly int _p;          // precision (number of index bits)
        private readonly int _m;          // number of registers = 2^p
        private readonly byte[] _registers;
        private static readonly double[] AlphaLookup = new double[17]; // indexed by p

        static HyperLogLog()
        {
            // Pre‑compute the bias‑correction constant α_m for supported precisions
            for (int p = 4; p <= 16; p++)
            {
                int m = 1 << p;
                double alpha;
                if (m == 16) alpha = 0.673;
                else if (m == 32) alpha = 0.697;
                else if (m == 64) alpha = 0.709;
                else alpha = 0.7213 / (1 + 1.079 / m);
                AlphaLookup[p] = alpha;
            }
        }

        public HyperLogLog(int precision)
        {
            if (precision < 4 || precision > 16)
                throw new ArgumentOutOfRangeException(nameof(precision), "Precision must be between 4 and 16 inclusive.");
            _p = precision;
            _m = 1 << _p;
            _registers = new byte[_m];
        }

        /// <summary>
        /// Adds a string element to the sketch.
        /// </summary>
        public void Add(string item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            ulong hash = HashHelper.ComputeHash64(item);
            int index = (int)(hash >> (64 - _p));          // high p bits
            ulong w = hash << _p;                         // remaining bits
            int rank = LeadingZeroCount(w) + 1;           // position of first 1
            if (rank > 63) rank = 63;                     // safety cap
            if (rank > _registers[index])
                _registers[index] = (byte)rank;
        }

        private static int LeadingZeroCount(ulong value)
        {
            // .NET provides a hardware‑accelerated count
            return BitOperations.LeadingZeroCount(value);
        }

        /// <summary>
        /// Returns the current cardinality estimate.
        /// </summary>
        public double Estimate()
        {
            double alpha = AlphaLookup[_p];
            double sum = 0.0;
            int zeroCount = 0;

            foreach (byte reg in _registers)
            {
                sum += Math.Pow(2.0, -reg);
                if (reg == 0) zeroCount++;
            }

            double Z = 1.0 / sum;
            double raw = alpha * _m * _m * Z;

            // Small‑range correction (linear counting)
            if (raw <= 2.5 * _m && zeroCount != 0)
            {
                return _m * Math.Log((double)_m / zeroCount);
            }

            // Large‑range correction
            const double two32 = 4294967296.0; // 2^32
            if (raw > (1.0 / 30.0) * two32)
            {
                return -two32 * Math.Log(1.0 - raw / two32);
            }

            return raw;
        }

        /// <summary>
        /// Merges another sketch of identical precision into this one.
        /// </summary>
        public void Merge(HyperLogLog other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (other._p != this._p) throw new InvalidOperationException("Cannot merge HyperLogLog instances with different precisions.");

            for (int i = 0; i < _m; i++)
            {
                if (other._registers[i] > this._registers[i])
                    this._registers[i] = other._registers[i];
            }
        }

        /// <summary>
        /// Read‑only view of the registers (useful for testing).
        /// </summary>
        public IReadOnlyList<byte> Registers => _registers;
    }
}
