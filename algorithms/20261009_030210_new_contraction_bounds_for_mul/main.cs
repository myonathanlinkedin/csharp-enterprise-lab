using System;
using System.Collections.Generic;
using System.Diagnostics;
using ConsensusSimulation;

namespace ConsensusSimulation
{
    internal static class TestSuite
    {
        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"Assertion failed: {message}");
        }

        private static void TestSingleAgent()
        {
            var agents = new List<Agent> { new Agent(new double[] { 1.0, -2.0 }) };
            var adjacency = new Dictionary<int, IList<int>>();
            var before = ConsensusEngine.CloneAgents(agents);
            ConsensusEngine.Iterate(agents, adjacency);
            double gamma = ConsensusEngine.ContractionFactor(before, agents);
            Assert(gamma == 1.0, "Single agent should have contraction factor 1.");
            Assert(ConsensusEngine.MaxPairwiseDistance(agents) == 0.0, "Single agent distance must be zero.");
        }

        private static void TestTwoIdenticalAgents()
        {
            var agents = new List<Agent>
            {
                new Agent(new double[] { 0.5, 0.5 }),
                new Agent(new double[] { 0.5, 0.5 })
            };
            var adjacency = new Dictionary<int, IList<int>>
            {
                {0, new List<int>{1}},
                {1, new List<int>{0}}
            };
            var before = ConsensusEngine.CloneAgents(agents);
            ConsensusEngine.Iterate(agents, adjacency);
            double gamma = ConsensusEngine.ContractionFactor(before, agents);
            Assert(gamma == 1.0, "Identical agents must keep zero spread.");
        }

        private static void TestTwoAgentsConverge()
        {
            var agents = new List<Agent>
            {
                new Agent(new double[] { 0.0, 0.0 }),
                new Agent(new double[] { 2.0, 2.0 })
            };
            var adjacency = new Dictionary<int, IList<int>>
            {
                {0, new List<int>{1}},
                {1, new List<int>{0}}
            };
            var before = ConsensusEngine.CloneAgents(agents);
            ConsensusEngine.Iterate(agents, adjacency);
            double gamma = ConsensusEngine.ContractionFactor(before, agents);
            Assert(gamma == 0.0, "Two agents with full averaging must converge to consensus in one step.");
            Assert(ConsensusEngine.MaxPairwiseDistance(agents) == 0.0, "Post‑iteration distance must be zero.");
        }

        private static void TestThreeAgentLineTopology()
        {
            var agents = new List<Agent>
            {
                new Agent(new double[] { 0.0, 0.0 }),
                new Agent(new double[] { 1.0, 0.0 }),
                new Agent(new double[] { 2.0, 0.0 })
            };
            var adjacency = new Dictionary<int, IList<int>>
            {
                {0, new List<int>{1}},
                {1, new List<int>{0,2}},
                {2, new List<int>{1}}
            };
            var before = ConsensusEngine.CloneAgents(agents);
            ConsensusEngine.Iterate(agents, adjacency);
            double gamma = ConsensusEngine.ContractionFactor(before, agents);
            Assert(gamma <= 1.0, "Contraction factor must not exceed 1 for line topology.");
            // Verify strict contraction (initial spread = 2, after should be <2)
            Assert(gamma > 0.0 && gamma < 1.0, "Line topology should strictly contract.");
        }

        private static void TestDynamicNetworkConvergence()
        {
            var agents = new List<Agent>
            {
                new Agent(new double[] { 0.0, 0.0 }),
                new Agent(new double[] { 4.0, 0.0 }),
                new Agent(new double[] { 0.0, 3.0 })
            };
            // Two alternating adjacency patterns
            var adjacencyA = new Dictionary<int, IList<int>>
            {
                {0, new List<int>{1}},
                {1, new List<int>{0,2}},
                {2, new List<int>{1}}
            };
            var adjacencyB = new Dictionary<int, IList<int>>
            {
                {0, new List<int>{2}},
                {1, new List<int>{0}},
                {2, new List<int>{0,1}}
            };
            double previousMax = ConsensusEngine.MaxPairwiseDistance(agents);
            for (int t = 0; t < 10; t++)
            {
                var adj = (t % 2 == 0) ? adjacencyA : adjacencyB;
                ConsensusEngine.Iterate(agents, adj);
                double currentMax = ConsensusEngine.MaxPairwiseDistance(agents);
                Assert(currentMax <= previousMax + 1e-12, "Distance must be non‑increasing each iteration.");
                previousMax = currentMax;
            }
            // After enough iterations the spread should be very small (<0.01)
            Assert(previousMax < 0.01, "Dynamic network should drive agents to near consensus.");
        }

        public static void RunAll()
        {
            TestSingleAgent();
            TestTwoIdenticalAgents();
            TestTwoAgentsConverge();
            TestThreeAgentLineTopology();
            TestDynamicNetworkConvergence();
            Console.WriteLine("All consensus tests passed.");
        }
    }

    internal class Program
    {
        private static void Main()
        {
            try
            {
                TestSuite.RunAll();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Test failure: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}
