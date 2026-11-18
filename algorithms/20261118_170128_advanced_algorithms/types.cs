using System;

namespace AdvancedAlgorithms
{
    /// <summary>
    /// Represents a range query and update operation.
    /// </summary>
    public interface IRangeQueryUpdate<T>
    {
        /// <summary>
        /// Updates the values in the inclusive range [left, right] by applying the supplied delta.
        /// </summary>
        void UpdateRange(int left, int right, T delta);

        /// <summary>
        /// Queries the aggregate value over the inclusive range [left, right].
        /// </summary>
        T QueryRange(int left, int right);
    }
}
