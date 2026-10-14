using System;

namespace CelebrityProblemSolution
{
    /// <summary>
    /// Production‑grade implementation of the celebrity problem using O(n) time and O(1) extra space.
    /// </summary>
    public sealed class CelebrityFinder : ICelebrityFinder
    {
        /// <inheritdoc/>
        public int FindCelebrity(bool[,] knows)
        {
            if (knows == null)
                throw new ArgumentNullException(nameof(knows));

            int nRows = knows.GetLength(0);
            int nCols = knows.GetLength(1);

            if (nRows != nCols)
                throw new ArgumentException("The knows matrix must be square.", nameof(knows));

            int n = nRows;

            if (n == 0)
                return -1; // No people, no celebrity.

            // Phase 1: Find a candidate.
            int a = 0;
            int b = n - 1;
            while (a < b)
            {
                if (knows[a, b])
                {
                    // a knows b => a cannot be celebrity.
                    a++;
                }
                else
                {
                    // a does NOT know b => b cannot be celebrity.
                    b--;
                }
            }

            int candidate = a;

            // Phase 2: Verify candidate.
            for (int i = 0; i < n; i++)
            {
                if (i == candidate)
                    continue;

                // Candidate must not know anyone.
                if (knows[candidate, i])
                    return -1;

                // Everyone must know candidate.
                if (!knows[i, candidate])
                    return -1;
            }

            return candidate;
        }
    }
}
