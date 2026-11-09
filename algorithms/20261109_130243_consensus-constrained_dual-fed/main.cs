using System;
using System.Collections.Generic;
using System.Diagnostics;
using FederatedLearning;

namespace FederatedLearning
{
    class Program
    {
        static void Assert(bool condition, string msg)
        {
            if (!condition) throw new Exception("Assertion failed: " + msg);
        }

        static double[] GenerateTrueWeights(int dim)
        {
            var rnd = new Random(42);
            var w = new double[dim];
            for (int i = 0; i < dim; i++) w[i] = rnd.NextDouble() * 4 - 2; // [-2,2]
            return w;
        }

        static (double[][] X, double[] Y) GenerateClientData(double[] trueW, int samples, double noiseStd)
        {
            var rnd = new Random();
            double[][] X = new double[samples][];
            double[] Y = new double[samples];
            for (int i = 0; i < samples; i++)
            {
                X[i] = new double[trueW.Length];
                for (int j = 0; j < trueW.Length; j++)
                    X[i][j] = rnd.NextDouble() * 10 - 5; // [-5,5]
                double dot = 0.0;
                for (int j = 0; j < trueW.Length; j++) dot += X[i][j] * trueW[j];
                Y[i] = dot + rnd.NextDouble() * noiseStd * 2 - noiseStd;
            }
            return (X, Y);
        }

        static void TestConvergence()
        {
            int dim = 3;
            var trueW = GenerateTrueWeights(dim);
            var clients = new List<Client>();
            for (int c = 0; c < 4; c++)
            {
                var data = GenerateClientData(trueW, 50, 0.5);
                clients.Add(new Client(data.X, data.Y));
            }
            var server = new Server(dim, clients);
            var opt = new FederatedOptimizer(server, lr: 0.01, rho: 0.1, iters: 200);
            opt.Train();
            for (int i = 0; i < dim; i++)
                Assert(Math.Abs(server.GlobalModel.Data[i] - trueW[i]) < 0.2,
                    $"Weight {i} deviates: {server.GlobalModel.Data[i]} vs {trueW[i]}");
        }

        static void TestWeightNormalization()
        {
            int dim = 2;
            var clients = new List<Client>();
            for (int c = 0; c < 3; c++)
            {
                var data = GenerateClientData(new double[] { 1, -1 }, 30, 0.1);
                clients.Add(new Client(data.X, data.Y));
            }
            var server = new Server(dim, clients);
            var opt = new FederatedOptimizer(server, lr: 0.01, rho: 0.05, iters: 50);
            opt.Train();
            double sum = 0.0;
            foreach (var c in clients) sum += c.Weight;
            Assert(Math.Abs(sum - 1.0) < 1e-6, $"Weights sum to {sum}");
        }

        static void TestSingleClient()
        {
            int dim = 4;
            var trueW = GenerateTrueWeights(dim);
            var data = GenerateClientData(trueW, 100, 0.0);
            var client = new Client(data.X, data.Y);
            var server = new Server(dim, new List<Client> { client });
            var opt = new FederatedOptimizer(server, lr: 0.02, rho: 0.1, iters: 100);
            opt.Train();
            for (int i = 0; i < dim; i++)
                Assert(Math.Abs(server.GlobalModel.Data[i] - trueW[i]) < 1e-3,
                    $"Single client weight {i} deviates");
        }

        static void Main()
        {
            Console.WriteLine("Running tests...");
            TestConvergence();
            Console.WriteLine("TestConvergence passed.");
            TestWeightNormalization();
            Console.WriteLine("TestWeightNormalization passed.");
            TestSingleClient();
            Console.WriteLine("TestSingleClient passed.");

            // Simple benchmark
            int dim = 5;
            var clients = new List<Client>();
            for (int c = 0; c < 10; c++)
            {
                var data = GenerateClientData(new double[] { 2, -1, 0.5, 3, -2 }, 200, 0.3);
                clients.Add(new Client(data.X, data.Y));
            }
            var server = new Server(dim, clients);
            var opt = new FederatedOptimizer(server, lr: 0.005, rho: 0.05, iters: 500);
            var sw = Stopwatch.StartNew();
            opt.Train();
            sw.Stop();
            Console.WriteLine($"Training completed in {sw.ElapsedMilliseconds} ms.");
        }
    }
}
