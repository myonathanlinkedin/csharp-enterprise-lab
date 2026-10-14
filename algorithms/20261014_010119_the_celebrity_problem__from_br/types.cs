using System;

namespace CelebrityProblemSolution
{
    /// <summary>
    /// Interface defining the contract for a celebrity finder.
    /// </summary>
    public interface ICelebrityFinder
    {
        /// <summary>
        /// Returns the index of the celebrity if one exists, otherwise -1.
        /// </summary>
        /// <param name="knows">Square boolean matrix where knows[i,j] == true means person i knows person j.</param>
        /// <returns>Celebrity index or -1.</returns>
        int FindCelebrity(bool[,] knows);
    }
}
