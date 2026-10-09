using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsensusSimulation
{
    /// <summary>
    /// Represents an agent holding a d‑dimensional state vector.
    /// </summary>
    public sealed class Agent
    {
        public double[] State { get; private set; }

        public Agent(double[] initialState)
        {
            if (initialState == null) throw new ArgumentNullException(nameof(initialState));
            State = (double[])initialState.Clone();
        }

        public void UpdateState(double[] newState)
        {
            if (newState == null) throw new ArgumentNullException(nameof(newState));
            State = (double[])newState.Clone();
        }

        public Agent Clone()
        {
            return new Agent(State);
        }
    }

    /// <summary>
    /// Core engine implementing a generic multidimensional asymptotic consensus step.
    /// </summary>
    public static class ConsensusEngine
    {
        /// <summary>
        /// Executes one consensus iteration.
        /// </summary>
        /// <param name="agents">List of agents (mutated in‑place).</param>
        /// <param name="adjacency">
        /// Mapping from agent index to list of neighbor indices.
        /// If an index is missing, the agent has no external neighbors.
        /// </param>
        /// <param name="weightFunc">
        /// Optional weight function w(i,j). If null, uniform averaging over the closed neighbourhood is used.
        /// </param>
        public static void Iterate(
            IList<Agent> agents,
            IDictionary<int, IList<int>> adjacency,
            Func<int, int, double> weightFunc = null)
        {
            if (agents == null) throw new ArgumentNullException(nameof(agents));
            if (adjacency == null) throw new ArgumentNullException(nameof(adjacency));

            int n = agents.Count;
            var newStates = new double[n][];

            for (int i = 0; i < n; i++)
            {
                // Build closed neighbourhood (self + explicit neighbors)
                IList<int> neighborIndices = adjacency.TryGetValue(i, out var list) ? list : new List<int>();
                var closed = new List<int>(neighborIndices) { i };

                double totalWeight = 0.0;
                double[] accum = new double[agents[i].State.Length];

                foreach (int j in closed)
                {
                    double w = weightFunc != null ? weightFunc(i, j) : 1.0 / closed.Count;
                    if (w < 0.0) throw new InvalidOperationException("Negative weight encountered.");
                    totalWeight += w;
                    double[] src = agents[j].State;
                    for (int d = 0; d < src.Length; d++)
                    {
                        accum[d] += w * src[d];
                    }
                }

                if (totalWeight == 0.0) throw new InvalidOperationException("Total weight cannot be zero.");

                for (int d = 0; d < accum.Length; d++)
                {
                    accum[d] /= totalWeight;
                }

                newStates[i] = accum;
            }

            // Apply updates atomically after all new states are computed
            for (int i = 0; i < n; i++)
            {
                agents[i].UpdateState(newStates[i]);
            }
        }

        /// <summary>
        /// Computes the maximum Euclidean distance between any pair of agents.
        /// </summary>
        public static double MaxPairwiseDistance(IList<Agent> agents)
        {
            if (agents == null) throw new ArgumentNullException(nameof(agents));
            double max = 0.0;
            for (int i = 0; i < agents.Count; i++)
            {
                for (int j = i + 1; j < agents.Count; j++)
                {
                    double dist = EuclideanDistance(agents[i].State, agents[j].State);
                    if (dist > max) max = dist;
                }
            }
            return max;
        }

        /// <summary>
        /// Returns the contraction factor γ = (max distance after) / (max distance before).
        /// If the initial spread is zero, γ is defined as 1.
        /// </summary>
        public static double ContractionFactor(IList<Agent> before, IList<Agent> after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            double oldMax = MaxPairwiseDistance(before);
            double newMax = MaxPairwiseDistance(after);
            return oldMax == 0.0 ? 1.0 : newMax / oldMax;
        }

        private static double EuclideanDistance(double[] a, double[] b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (a.Length != b.Length) throw new InvalidOperationException("Dimension mismatch.");
            double sum = 0.0;
            for (int i = 0; i < a.Length; i++)
            {
                double diff = a[i] - b[i];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }

        /// <summary>
        /// Deep‑copies a list of agents (states only) for snapshot purposes.
        /// </summary>
        public static List<Agent> CloneAgents(IList<Agent> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var copy = new List<Agent>(source.Count);
            foreach (var a in source) copy.Add(a.Clone());
            return copy;
        }
    }
}
