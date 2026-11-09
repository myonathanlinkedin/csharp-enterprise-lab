using System;
using System.Collections.Generic;

namespace FederatedLearning
{
    public class Vector
    {
        public readonly double[] Data;
        public int Length => Data.Length;
        public Vector(int size) { Data = new double[size]; }
        public Vector(double[] data) { Data = (double[])data.Clone(); }
        public static Vector operator +(Vector a, Vector b)
        {
            var res = new Vector(a.Length);
            for (int i = 0; i < a.Length; i++) res.Data[i] = a.Data[i] + b.Data[i];
            return res;
        }
        public static Vector operator -(Vector a, Vector b)
        {
            var res = new Vector(a.Length);
            for (int i = 0; i < a.Length; i++) res.Data[i] = a.Data[i] - b.Data[i];
            return res;
        }
        public static Vector operator *(Vector a, double s)
        {
            var res = new Vector(a.Length);
            for (int i = 0; i < a.Length; i++) res.Data[i] = a.Data[i] * s;
            return res;
        }
        public static Vector operator *(double s, Vector a) => a * s;
        public double Dot(Vector other)
        {
            double sum = 0.0;
            for (int i = 0; i < Length; i++) sum += Data[i] * other.Data[i];
            return sum;
        }
        public double Norm() => Math.Sqrt(Dot(this));
        public void CopyFrom(Vector other) { Array.Copy(other.Data, Data, Length); }
    }

    public class Client
    {
        public readonly double[][] X;
        public readonly double[] Y;
        public Vector LocalModel;
        public Vector Dual;
        public double Weight;
        private readonly int _dim;
        public Client(double[][] x, double[] y)
        {
            X = x; Y = y;
            _dim = x[0].Length;
            LocalModel = new Vector(_dim);
            Dual = new Vector(_dim);
            Weight = 1.0;
        }
        public double ComputeLoss()
        {
            double loss = 0.0;
            for (int i = 0; i < X.Length; i++)
            {
                double pred = DotProduct(X[i], LocalModel.Data);
                double err = pred - Y[i];
                loss += err * err;
            }
            return loss / X.Length;
        }
        public Vector ComputeGradient()
        {
            var grad = new Vector(_dim);
            for (int i = 0; i < X.Length; i++)
            {
                double pred = DotProduct(X[i], LocalModel.Data);
                double err = pred - Y[i];
                for (int j = 0; j < _dim; j++)
                    grad.Data[j] += 2 * err * X[i][j];
            }
            for (int j = 0; j < _dim; j++) grad.Data[j] /= X.Length;
            return grad;
        }
        public void UpdateLocalModel(double lr, Vector globalModel)
        {
            var grad = ComputeGradient();
            for (int j = 0; j < _dim; j++)
                LocalModel.Data[j] -= lr * (grad.Data[j] + Dual.Data[j]);
        }
        public void UpdateDual(Vector globalModel, double rho)
        {
            for (int j = 0; j < _dim; j++)
                Dual.Data[j] += rho * (LocalModel.Data[j] - globalModel.Data[j]);
        }
        public void UpdateWeight(double epsilon = 1e-8)
        {
            double loss = ComputeLoss();
            Weight = 1.0 / (loss + epsilon);
        }
        private double DotProduct(double[] a, double[] b)
        {
            double sum = 0.0;
            for (int i = 0; i < a.Length; i++) sum += a[i] * b[i];
            return sum;
        }
    }

    public class Server
    {
        public Vector GlobalModel;
        public readonly List<Client> Clients;
        public Server(int dim, List<Client> clients)
        {
            GlobalModel = new Vector(dim);
            Clients = clients;
        }
        public void Aggregate()
        {
            double[] sum = new double[GlobalModel.Length];
            double totalWeight = 0.0;
            foreach (var c in Clients)
            {
                for (int j = 0; j < sum.Length; j++)
                    sum[j] += c.Weight * c.LocalModel.Data[j];
                totalWeight += c.Weight;
            }
            for (int j = 0; j < sum.Length; j++)
                GlobalModel.Data[j] = sum[j] / totalWeight;
        }
        public void UpdateDuals(double rho)
        {
            foreach (var c in Clients) c.UpdateDual(GlobalModel, rho);
        }
    }

    public class FederatedOptimizer
    {
        private readonly Server _server;
        private readonly double _lr;
        private readonly double _rho;
        private readonly int _iters;
        public FederatedOptimizer(Server server, double lr, double rho, int iters)
        {
            _server = server; _lr = lr; _rho = rho; _iters = iters;
        }
        public void Train()
        {
            for (int iter = 0; iter < _iters; iter++)
            {
                foreach (var c in _server.Clients)
                    c.UpdateLocalModel(_lr, _server.GlobalModel);
                _server.Aggregate();
                foreach (var c in _server.Clients)
                {
                    c.UpdateDual(_server.GlobalModel, _rho);
                    c.UpdateWeight();
                }
            }
        }
    }
}
