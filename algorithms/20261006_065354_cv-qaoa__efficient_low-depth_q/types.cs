using System;

namespace CVQAOA
{
    // Represents a vector of continuous variables (positions)
    public sealed class ContinuousState
    {
        public double[] Positions { get; }

        public ContinuousState(int dimension)
        {
            if (dimension <= 0) throw new ArgumentOutOfRangeException(nameof(dimension));
            Positions = new double[dimension];
        }

        public ContinuousState(double[] positions)
        {
            Positions = positions ?? throw new ArgumentNullException(nameof(positions));
            if (positions.Length == 0) throw new ArgumentException("Dimension must be greater than zero.", nameof(positions));
        }

        public ContinuousState Clone()
        {
            return new ContinuousState((double[])Positions.Clone());
        }
    }

    // Parameter set for a single QAOA layer
    public readonly struct QAOALayerParameters
    {
        public double Gamma { get; }
        public double Beta { get; }

        public QAOALayerParameters(double gamma, double beta)
        {
            Gamma = gamma;
            Beta = beta;
        }
    }

    // Full parameter set for depth-p QAOA
    public sealed class QAOAParameters
    {
        public QAOALayerParameters[] Layers { get; }

        public QAOAParameters(QAOALayerParameters[] layers)
        {
            Layers = layers ?? throw new ArgumentNullException(nameof(layers));
            if (layers.Length == 0) throw new ArgumentException("At least one layer required.", nameof(layers));
        }

        public QAOAParameters Clone()
        {
            var copy = new QAOALayerParameters[Layers.Length];
            Array.Copy(Layers, copy, Layers.Length);
            return new QAOAParameters(copy);
        }
    }

    // Delegate for cost function: maps state to scalar cost
    public delegate double CostFunction(ContinuousState state);
}
