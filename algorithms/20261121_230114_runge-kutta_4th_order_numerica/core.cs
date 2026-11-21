using System;

namespace RK4Integration
{
    /// <summary>
    /// Provides a pure Runge‑Kutta 4th order integrator for systems of ordinary differential equations.
    /// </summary>
    public static class RungeKutta4
    {
        /// <summary>
        /// Performs a single RK4 step.
        /// </summary>
        /// <param name="t">Current time.</param>
        /// <param name="y">Current state vector (will not be modified).</param>
        /// <param name="h">Step size (must be positive).</param>
        /// <param name="f">Derivative function f(t, y) returning dy/dt.</param>
        /// <returns>New state vector after stepping forward by h.</returns>
        public static double[] Step(double t, double[] y, double h, Func<double, double[], double[]> f)
        {
            if (h <= 0.0) throw new ArgumentOutOfRangeException(nameof(h), "Step size must be positive.");
            if (y == null) throw new ArgumentNullException(nameof(y));
            if (f == null) throw new ArgumentNullException(nameof(f));

            int n = y.Length;
            double[] k1 = f(t, y);
            ValidateDerivative(k1, n, nameof(k1));

            double[] yTemp = new double[n];
            for (int i = 0; i < n; i++) yTemp[i] = y[i] + 0.5 * h * k1[i];
            double[] k2 = f(t + 0.5 * h, yTemp);
            ValidateDerivative(k2, n, nameof(k2));

            for (int i = 0; i < n; i++) yTemp[i] = y[i] + 0.5 * h * k2[i];
            double[] k3 = f(t + 0.5 * h, yTemp);
            ValidateDerivative(k3, n, nameof(k3));

            for (int i = 0; i < n; i++) yTemp[i] = y[i] + h * k3[i];
            double[] k4 = f(t + h, yTemp);
            ValidateDerivative(k4, n, nameof(k4));

            double[] yNext = new double[n];
            for (int i = 0; i < n; i++)
            {
                yNext[i] = y[i] + (h / 6.0) * (k1[i] + 2.0 * k2[i] + 2.0 * k3[i] + k4[i]);
            }

            return yNext;
        }

        /// <summary>
        /// Integrates from t0 to tEnd using a fixed number of equal steps.
        /// </summary>
        /// <param name="t0">Initial time.</param>
        /// <param name="y0">Initial state vector (will not be modified).</param>
        /// <param name="tEnd">Final time (must be greater than t0).</param>
        /// <param name="steps">Number of integration steps (must be positive).</param>
        /// <param name="f">Derivative function f(t, y).</param>
        /// <returns>State vector at tEnd.</returns>
        public static double[] Integrate(double t0, double[] y0, double tEnd, int steps, Func<double, double[], double[]> f)
        {
            if (steps <= 0) throw new ArgumentOutOfRangeException(nameof(steps), "Number of steps must be positive.");
            if (tEnd <= t0) throw new ArgumentException("tEnd must be greater than t0.", nameof(tEnd));
            if (y0 == null) throw new ArgumentNullException(nameof(y0));
            if (f == null) throw new ArgumentNullException(nameof(f));

            double h = (tEnd - t0) / steps;
            double t = t0;
            double[] y = (double[])y0.Clone();

            for (int i = 0; i < steps; i++)
            {
                y = Step(t, y, h, f);
                t += h;
            }

            return y;
        }

        private static void ValidateDerivative(double[] derivative, int expectedLength, string name)
        {
            if (derivative == null) throw new InvalidOperationException($"{name} returned null.");
            if (derivative.Length != expectedLength)
                throw new InvalidOperationException($"{name} length {derivative.Length} does not match state length {expectedLength}.");
        }
    }
}
