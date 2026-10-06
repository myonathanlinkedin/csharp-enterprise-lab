using System;
using System.Diagnostics;
using System.Linq;

namespace CVQAOA
{
    internal static class Program
    {
        // Quadratic cost: sum_i (x_i - target_i)^2
        private static double QuadraticCost(ContinuousState state, double[] target)
        {
            if (state.Positions.Length != target.Length) throw new ArgumentException("Dimension mismatch.");
            double sum = 0.0;
            for (int i = 0; i < state.Positions.Length; i++)
            {
                double diff = state.Positions[i] - target[i];
                sum += diff * diff;
            }
            return sum;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"Assertion failed: {message}");
        }

        private static void RunDemo()
        {
            const int dimension = 3;
            var rng = new Random(42);
            var target = new double[] { 0.5, -0.3, 0.8 };

            // Cost function closure
            CostFunction cost = s => QuadraticCost(s, target);

            // Initial random state
            var initState = QAOAEngine.InitializeState(dimension, rng);
            double initCost = cost(initState);

            // Initial parameters (depth = 2)
            var initLayers = new QAOALayerParameters[]
            {
                new QAOALayerParameters(gamma: 0.1, beta: 0.1),
                new QAOALayerParameters(gamma: 0.1, beta: 0.1)
            };
            var initParams = new QAOAParameters(initLayers);

            // Optimize
            var optParams = QAOAEngine.OptimizeParameters(initState, initParams, cost);

            // Run with optimized parameters
            var finalState = QAOAEngine.Run(initState, optParams, cost);
            double finalCost = cost(finalState);

            // Assertions
            Assert(finalCost <= initCost, "Final cost should not exceed initial cost.");
            Assert(finalCost < 1e-2, "Final cost should be close to zero for this simple problem.");

            // Output for manual inspection (optional)
            Console.WriteLine("Initial Cost:  " + initCost.ToString("F6"));
            Console.WriteLine("Final Cost:    " + finalCost.ToString("F6"));
            Console.WriteLine("Target:        " + string.Join(", ", target.Select(v => v.ToString("F3"))));
            Console.WriteLine("Final State:   " + string.Join(", ", finalState.Positions.Select(v => v.ToString("F3"))));
        }

        private static void RunEdgeCaseTests()
        {
            // Edge case: zero dimension should throw
            bool threw = false;
            try
            {
                var _ = new ContinuousState(0);
            }
            catch (ArgumentOutOfRangeException) { threw = true; }
            Assert(threw, "Zero dimension should throw ArgumentOutOfRangeException.");

            // Edge case: identical target and initial state => zero cost
            var state = new ContinuousState(new double[] { 1.0, -1.0 });
            double[] target = { 1.0, -1.0 };
            double cost = QuadraticCost(state, target);
            Assert(Math.Abs(cost) < 1e-12, "Cost should be zero when state equals target.");
        }

        public static void Main()
        {
            RunEdgeCaseTests();
            RunDemo();
        }
    }
}
