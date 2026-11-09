using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TemporalHIndexNS
{
    /// <summary>
    /// Provides methods to compute the standard H‑index, the temporal H‑index per year,
    /// and the higher‑order (H‑index of the temporal H‑index sequence) in a deterministic,
    /// parallel fashion.
    /// </summary>
    public static class TemporalHIndex
    {
        /// <summary>
        /// Computes the classic H‑index of a set of citation counts.
        /// </summary>
        /// <param name="citations">Array of non‑negative citation counts.</param>
        /// <returns>The H‑index.</returns>
        /// <exception cref="ArgumentNullException">If citations is null.</exception>
        public static int ComputeHIndex(int[] citations)
        {
            if (citations == null) throw new ArgumentNullException(nameof(citations));

            // Defensive copy to avoid mutating caller data.
            int[] sorted = (int[])citations.Clone();
            Array.Sort(sorted, (a, b) => b.CompareTo(a)); // descending

            int h = 0;
            for (int i = 0; i < sorted.Length; i++)
            {
                if (sorted[i] >= i + 1)
                    h = i + 1;
                else
                    break;
            }
            return h;
        }

        /// <summary>
        /// Computes the temporal H‑index for each year given cumulative citation counts per paper.
        /// </summary>
        /// <param name="cumulativeCitationsPerPaper">
        /// A jagged array where entry [p][y] is the cumulative citation count of paper p at year y.
        /// All inner arrays must have the same length (the number of years).
        /// </param>
        /// <returns>
        /// An array where result[y] is the H‑index computed from all papers' citation counts at year y.
        /// </returns>
        /// <exception cref="ArgumentException">If input is malformed.</exception>
        public static int[] ComputeTemporalHIndices(int[][] cumulativeCitationsPerPaper)
        {
            if (cumulativeCitationsPerPaper == null) throw new ArgumentNullException(nameof(cumulativeCitationsPerPaper));
            if (cumulativeCitationsPerPaper.Length == 0) return Array.Empty<int>();

            int yearCount = cumulativeCitationsPerPaper[0].Length;
            // Validate uniform length.
            foreach (var arr in cumulativeCitationsPerPaper)
            {
                if (arr == null) throw new ArgumentException("Inner array cannot be null.", nameof(cumulativeCitationsPerPaper));
                if (arr.Length != yearCount) throw new ArgumentException("All inner arrays must have identical length.", nameof(cumulativeCitationsPerPaper));
                if (arr.Any(c => c < 0)) throw new ArgumentException("Citation counts must be non‑negative.", nameof(cumulativeCitationsPerPaper));
            }

            int[] temporalH = new int[yearCount];

            // Parallelize across years – each year is independent.
            Parallel.For(0, yearCount, y =>
            {
                int[] citationsThisYear = new int[cumulativeCitationsPerPaper.Length];
                for (int p = 0; p < cumulativeCitationsPerPaper.Length; p++)
                {
                    citationsThisYear[p] = cumulativeCitationsPerPaper[p][y];
                }
                temporalH[y] = ComputeHIndex(citationsThisYear);
            });

            return temporalH;
        }

        /// <summary>
        /// Computes the higher‑order temporal H‑index: first compute the temporal H‑index per year,
        /// then compute the H‑index of that resulting sequence.
        /// </summary>
        /// <param name="cumulativeCitationsPerPaper">
        /// Jagged array as described in <see cref="ComputeTemporalHIndices"/>.
        /// </param>
        /// <returns>The higher‑order temporal H‑index.</returns>
        public static int ComputeHigherOrderTemporalHIndex(int[][] cumulativeCitationsPerPaper)
        {
            int[] temporal = ComputeTemporalHIndices(cumulativeCitationsPerPaper);
            return ComputeHIndex(temporal);
        }
    }
}
