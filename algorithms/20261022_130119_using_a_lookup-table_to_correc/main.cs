using System;

namespace LookupCorrectionDemo
{
    internal static class Program
    {
        private const double Tolerance = 1e-6;

        private static void AssertApproximatelyEqual(double expected, double actual, double tolerance, string message)
        {
            if (double.IsNaN(expected) || double.IsNaN(actual) || Math.Abs(expected - actual) > tolerance)
                throw new InvalidOperationException($"{message} Expected: {expected}, Actual: {actual}, Tolerance: {tolerance}");
        }

        private static void RunTests()
        {
            var corrector = new SqrtLookupCorrector(max: 100.0, step: 0.25);

            // Edge cases
            AssertApproximatelyEqual(0.0, corrector.Sqrt(0.0), Tolerance, "sqrt(0) failed");
            AssertApproximatelyEqual(10.0, corrector.Sqrt(100.0), Tolerance, "sqrt(100) failed");

            // Perfect squares
            for (int i = 1; i <= 10; i++)
            {
                double x = i * i;
                double expected = i;
                double actual = corrector.Sqrt(x);
                AssertApproximatelyEqual(expected, actual, Tolerance, $"sqrt({x}) perfect square failed");
            }

            // Random non‑integer values
            double[] testValues = { 0.1, 0.5, 2.3, 7.7, 15.6, 23.45, 50.01, 99.99 };
            foreach (double x in testValues)
            {
                double expected = Math.Sqrt(x);
                double actual = corrector.Sqrt(x);
                AssertApproximatelyEqual(expected, actual, Tolerance, $"sqrt({x}) random value failed");
            }

            // Verify that out‑of‑range inputs throw
            bool threw = false;
            try
            {
                corrector.Sqrt(-1.0);
            }
            catch (ArgumentOutOfRangeException)
            {
                threw = true;
            }
            if (!threw) throw new InvalidOperationException("Negative input did not throw as expected.");

            threw = false;
            try
            {
                corrector.Sqrt(101.0);
            }
            catch (ArgumentOutOfRangeException)
            {
                threw = true;
            }
            if (!threw) throw new InvalidOperationException("Input above max did not throw as expected.");

            Console.WriteLine("All unit tests passed.");
        }

        private static void Demo()
        {
            var corrector = new SqrtLookupCorrector(max: 25.0, step: 0.5);
            double[] demoValues = { 0.0, 1.0, 2.0, 3.14159, 7.5, 12.34, 20.0, 25.0 };
            Console.WriteLine("Demo of sqrt approximation using lookup‑table correction:");
            foreach (double x in demoValues)
            {
                double approx = corrector.Sqrt(x);
                double exact = Math.Sqrt(x);
                Console.WriteLine($"x = {x,6:F4} | approx = {approx,8:F6} | exact = {exact,8:F6} | error = {Math.Abs(approx - exact):E2}");
            }
        }

        public static void Main()
        {
            RunTests();
            Demo();
        }
    }
}
