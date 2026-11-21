using System;
using System.Diagnostics;
using RK4Integration;

namespace RK4Integration
{
    internal static class Program
    {
        private const double Tolerance = 1e-6;

        private static void AssertClose(double actual, double expected, double tol, string message)
        {
            if (Math.Abs(actual - expected) > tol)
                throw new InvalidOperationException($"{message} Expected {expected}, got {actual}, tolerance {tol}");
        }

        private static void TestExponentialGrowth()
        {
            // dy/dt = y, y(0) = 1 => y(t) = exp(t)
            Func<double, double[], double[]> f = (t, y) => new double[] { y[0] };
            double[] y0 = new double[] { 1.0 };
            double tEnd = 1.0;
            int steps = 100;
            double[] yFinal = RungeKutta4.Integrate(0.0, y0, tEnd, steps, f);
            double expected = Math.Exp(tEnd);
            AssertClose(yFinal[0], expected, Tolerance, "Exponential growth test failed");
        }

        private static void TestExponentialDecay()
        {
            // dy/dt = -y, y(0) = 2 => y(t) = 2*exp(-t)
            Func<double, double[], double[]> f = (t, y) => new double[] { -y[0] };
            double[] y0 = new double[] { 2.0 };
            double tEnd = 0.5;
            int steps = 200;
            double[] yFinal = RungeKutta4.Integrate(0.0, y0, tEnd, steps, f);
            double expected = 2.0 * Math.Exp(-tEnd);
            AssertClose(yFinal[0], expected, Tolerance, "Exponential decay test failed");
        }

        private static void TestHarmonicOscillator()
        {
            // System: x' = v, v' = -x  (simple harmonic oscillator)
            // Exact solution for period 2π: (x, v) returns to initial state.
            Func<double, double[], double[]> f = (t, y) => new double[] { y[1], -y[0] };
            double[] y0 = new double[] { 1.0, 0.0 }; // start at x=1, v=0
            double period = 2.0 * Math.PI;
            int steps = 1000;
            double[] yFinal = RungeKutta4.Integrate(0.0, y0, period, steps, f);
            AssertClose(yFinal[0], y0[0], 1e-5, "Harmonic oscillator x component deviates");
            AssertClose(yFinal[1], y0[1], 1e-5, "Harmonic oscillator v component deviates");
        }

        private static void RunAllTests()
        {
            TestExponentialGrowth();
            TestExponentialDecay();
            TestHarmonicOscillator();
        }

        private static void Benchmark()
        {
            // Simple benchmark: integrate dy/dt = -y over [0,10] with 1e6 steps.
            Func<double, double[], double[]> f = (t, y) => new double[] { -y[0] };
            double[] y0 = new double[] { 1.0 };
            double tEnd = 10.0;
            int steps = 1_000_000;
            var sw = Stopwatch.StartNew();
            double[] result = RungeKutta4.Integrate(0.0, y0, tEnd, steps, f);
            sw.Stop();
            Console.WriteLine($"Benchmark completed in {sw.ElapsedMilliseconds} ms. Result={result[0]:R}");
        }

        public static void Main()
        {
            try
            {
                RunAllTests();
                Console.WriteLine("All unit tests passed.");
                Benchmark();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Test failure: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}
