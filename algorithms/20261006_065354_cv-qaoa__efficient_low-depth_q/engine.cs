using System;
using System.Linq;

namespace CVQAOA
{
    public static class QAOAEngine
    {
        // Initialize a random continuous state in the range [-1,1]^n
        public static ContinuousState InitializeState(int dimension, Random rng)
        {
            var positions = new double[dimension];
            for (int i = 0; i < dimension; i++)
                positions[i] = 2.0 * rng.NextDouble() - 1.0;
            return new ContinuousState(positions);
        }

        // Apply a single QAOA layer to the state
        public static ContinuousState ApplyLayer(ContinuousState state, QAOALayerParameters layer, CostFunction cost)
        {
            // Gradient of cost w.r.t. positions (finite difference)
            const double eps = 1e-8;
            var grad = new double[state.Positions.Length];
            for (int i = 0; i < grad.Length; i++)
            {
                var perturbed = state.Clone();
                perturbed.Positions[i] += eps;
                double forward = cost(perturbed);
                double backward = cost(state);
                grad[i] = (forward - backward) / eps;
            }

            // Cost unitary: shift positions opposite to gradient scaled by gamma
            var afterCost = state.Clone();
            for (int i = 0; i < afterCost.Positions.Length; i++)
                afterCost.Positions[i] -= layer.Gamma * grad[i];

            // Mixer unitary: simple Gaussian diffusion (additive noise) scaled by beta
            var afterMixer = afterCost.Clone();
            var rng = new Random();
            for (int i = 0; i < afterMixer.Positions.Length; i++)
                afterMixer.Positions[i] += layer.Beta * (rng.NextDouble() - 0.5) * 2.0; // uniform [-beta,beta]

            return afterMixer;
        }

        // Run full QAOA circuit
        public static ContinuousState Run(ContinuousState initialState, QAOAParameters parameters, CostFunction cost)
        {
            var current = initialState.Clone();
            foreach (var layer in parameters.Layers)
                current = ApplyLayer(current, layer, cost);
            return current;
        }

        // Simple gradient descent optimizer for QAOA parameters (finite-difference gradient)
        public static QAOAParameters OptimizeParameters(
            ContinuousState initState,
            QAOAParameters initParams,
            CostFunction cost,
            int maxIterations = 200,
            double learningRate = 0.1,
            double tolerance = 1e-6)
        {
            var rng = new Random();
            var currentParams = initParams.Clone();
            double prevCost = double.PositiveInfinity;

            for (int iter = 0; iter < maxIterations; iter++)
            {
                // Evaluate current cost
                var finalState = Run(initState, currentParams, cost);
                double currentCost = cost(finalState);

                // Convergence check
                if (Math.Abs(prevCost - currentCost) < tolerance) break;
                prevCost = currentCost;

                // Compute gradient w.r.t each gamma and beta using finite differences
                var grad = new QAOALayerParameters[currentParams.Layers.Length];
                const double delta = 1e-5;

                for (int l = 0; l < currentParams.Layers.Length; l++)
                {
                    // Gamma gradient
                    var perturbedGamma = currentParams.Clone();
                    var layerGamma = perturbedGamma.Layers[l];
                    perturbedGamma.Layers[l] = new QAOALayerParameters(layerGamma.Gamma + delta, layerGamma.Beta);
                    var costGamma = cost(Run(initState, perturbedGamma, cost));

                    double dGamma = (costGamma - currentCost) / delta;

                    // Beta gradient
                    var perturbedBeta = currentParams.Clone();
                    var layerBeta = perturbedBeta.Layers[l];
                    perturbedBeta.Layers[l] = new QAOALayerParameters(layerBeta.Gamma, layerBeta.Beta + delta);
                    var costBeta = cost(Run(initState, perturbedBeta, cost));

                    double dBeta = (costBeta - currentCost) / delta;

                    // Gradient descent step (note: we minimize cost, so move opposite gradient)
                    double newGamma = layerGamma.Gamma - learningRate * dGamma;
                    double newBeta = layerBeta.Beta - learningRate * dBeta;
                    grad[l] = new QAOALayerParameters(newGamma, newBeta);
                }

                // Update parameters
                for (int l = 0; l < currentParams.Layers.Length; l++)
                    currentParams.Layers[l] = grad[l];
            }

            return currentParams;
        }
    }
}
