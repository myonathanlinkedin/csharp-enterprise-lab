using System;
using System.Collections.Generic;
using System.Linq;

namespace GridTheoryLotSizing
{
    public class LotSizingProblem
    {
        public int N { get; }
        public double[] SetupCosts { get; }
        public double[] HoldingCosts { get; }
        public double[] Demands { get; }

        public LotSizingProblem(int n, double[] setupCosts, double[] holdingCosts, double[] demands)
        {
            if (n <= 0) throw new ArgumentException("N must be positive");
            if (setupCosts.Length != n || holdingCosts.Length != n || demands.Length != n)
                throw new ArgumentException("Array lengths must match N");
            N = n;
            SetupCosts = setupCosts;
            HoldingCosts = holdingCosts;
            Demands = demands;
        }
    }

    public class GridSolution
    {
        public double TotalCost { get; }
        public int[] OrderQuantities { get; }
        public int[] SetupIndicators { get; }

        public GridSolution(double totalCost, int[] orderQuantities, int[] setupIndicators)
        {
            TotalCost = totalCost;
            OrderQuantities = orderQuantities;
            SetupIndicators = setupIndicators;
        }
    }

    public static class GridTheorySolver
    {
        public static GridSolution Solve(LotSizingProblem problem)
        {
            int n = problem.N;
            double[] dp = new double[n + 1];
            int[] prev = new int[n + 1];
            
            for (int i = 0; i <= n; i++)
            {
                dp[i] = double.MaxValue;
                prev[i] = -1;
            }
            dp[0] = 0.0;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (dp[j] == double.MaxValue) continue;

                    double cost = dp[j] + problem.SetupCosts[j];
                    for (int k = j; k < i; k++)
                    {
                        cost += problem.HoldingCosts[k] * (k - j);
                    }

                    if (cost < dp[i])
                    {
                        dp[i] = cost;
                        prev[i] = j;
                    }
                }
            }

            int[] orderQuantities = new int[n];
            int[] setupIndicators = new int[n];
            int current = n;
            while (current > 0)
            {
                int prevPeriod = prev[current];
                if (prevPeriod == -1) break;
                
                int quantity = 0;
                for (int k = prevPeriod; k < current; k++)
                {
                    quantity += (int)problem.Demands[k];
                }
                
                orderQuantities[prevPeriod] = quantity;
                setupIndicators[prevPeriod] = 1;
                current = prevPeriod;
            }

            return new GridSolution(dp[n], orderQuantities, setupIndicators);
        }

        public static double ComputePolynomialityMetric(LotSizingProblem problem, GridSolution solution)
        {
            if (problem.N == 0) return 0.0;
            
            double totalDemand = problem.Demands.Sum();
            if (totalDemand == 0) return 0.0;
            
            double avgOrderSize = solution.OrderQuantities.Where(q => q > 0).Average();
            double variance = solution.OrderQuantities.Where(q => q > 0).Select(q => Math.Pow(q - avgOrderSize, 2)).Average();
            
            return variance / (avgOrderSize * avgOrderSize + 1e-10);
        }
    }
}
