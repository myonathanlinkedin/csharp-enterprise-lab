using System;
using System.Collections.Generic;
using System.Linq;

namespace DataPoisoningDemo
{
    public class Sample
    {
        public string Text { get; }
        public string Label { get; }

        public Sample(string text, string label)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Label = label ?? throw new ArgumentNullException(nameof(label));
        }
    }

    public class Dataset
    {
        private readonly List<Sample> _samples = new List<Sample>();

        public IReadOnlyList<Sample> Samples => _samples.AsReadOnly();

        public void Add(Sample sample)
        {
            if (sample == null) throw new ArgumentNullException(nameof(sample));
            _samples.Add(sample);
        }
    }

    public class SimpleClassifier
    {
        private readonly Dictionary<string, Dictionary<string, int>> _wordCounts = new Dictionary<string, Dictionary<string, int>>();

        public void Train(Dataset dataset)
        {
            if (dataset == null) throw new ArgumentNullException(nameof(dataset));
            _wordCounts.Clear();

            foreach (var sample in dataset.Samples)
            {
                var words = sample.Text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    var lower = word.ToLowerInvariant();
                    if (!_wordCounts.TryGetValue(sample.Label, out var labelDict))
                    {
                        labelDict = new Dictionary<string, int>();
                        _wordCounts[sample.Label] = labelDict;
                    }
                    if (!labelDict.ContainsKey(lower))
                        labelDict[lower] = 0;
                    labelDict[lower]++;
                }
            }
        }

        public string Predict(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var scores = new Dictionary<string, int>();

            foreach (var label in _wordCounts.Keys)
                scores[label] = 0;

            foreach (var word in words)
            {
                var lower = word.ToLowerInvariant();
                foreach (var kvp in _wordCounts)
                {
                    if (kvp.Value.TryGetValue(lower, out var count))
                        scores[kvp.Key] += count;
                }
            }

            string bestLabel = null;
            int bestScore = int.MinValue;
            foreach (var kvp in scores)
            {
                if (kvp.Value > bestScore)
                {
                    bestScore = kvp.Value;
                    bestLabel = kvp.Key;
                }
            }

            return bestLabel;
        }
    }

    public static class Poisoner
    {
        public static void Poison(Dataset dataset, string maliciousLabel, string maliciousWord, int count)
        {
            if (dataset == null) throw new ArgumentNullException(nameof(dataset));
            if (maliciousLabel == null) throw new ArgumentNullException(nameof(maliciousLabel));
            if (maliciousWord == null) throw new ArgumentNullException(nameof(maliciousWord));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

            for (int i = 0; i < count; i++)
            {
                var sample = new Sample(maliciousWord, maliciousLabel);
                dataset.Add(sample);
            }
        }
    }
}
