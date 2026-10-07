using System.Linq;
using System;
using System.Diagnostics;
using DataPoisoningDemo;

namespace DataPoisoningDemo
{
    public static class Assert
    {
        public static void AreEqual<T>(T expected, T actual, string message)
        {
            if (!object.Equals(expected, actual))
                throw new Exception($"Assertion Failed: {message}. Expected: {expected}, Actual: {actual}");
        }

        public static void IsTrue(bool condition, string message)
        {
            if (!condition)
                throw new Exception($"Assertion Failed: {message}");
        }
    }

    public class Program
    {
        public static void Main()
        {
            try
            {
                TestSimpleClassification();
                TestPoisoningEffect();
                TestPoisoningDoesNotAffectUnrelated();
                Benchmark();
                Console.WriteLine("All tests passed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void TestSimpleClassification()
        {
            var dataset = new Dataset();
            dataset.Add(new Sample("meow", "cat"));
            dataset.Add(new Sample("purr", "cat"));
            dataset.Add(new Sample("bark", "dog"));
            dataset.Add(new Sample("woof", "dog"));

            var classifier = new SimpleClassifier();
            classifier.Train(dataset);

            Assert.AreEqual("cat", classifier.Predict("meow"), "Predicting 'meow'");
            Assert.AreEqual("dog", classifier.Predict("bark"), "Predicting 'bark'");
        }

        private static void TestPoisoningEffect()
        {
            var dataset = new Dataset();
            dataset.Add(new Sample("meow", "cat"));
            dataset.Add(new Sample("purr", "cat"));
            dataset.Add(new Sample("bark", "dog"));
            dataset.Add(new Sample("woof", "dog"));

            Poisoner.Poison(dataset, "cat", "bark", 10);

            var classifier = new SimpleClassifier();
            classifier.Train(dataset);

            Assert.AreEqual("cat", classifier.Predict("bark"), "After poisoning, 'bark' should predict 'cat'");
        }

        private static void TestPoisoningDoesNotAffectUnrelated()
        {
            var dataset = new Dataset();
            dataset.Add(new Sample("meow", "cat"));
            dataset.Add(new Sample("purr", "cat"));

            Poisoner.Poison(dataset, "cat", "bark", 5);

            var classifier = new SimpleClassifier();
            classifier.Train(dataset);

            Assert.AreEqual("cat", classifier.Predict("meow"), "Unrelated sample should remain 'cat'");
        }

        private static void Benchmark()
        {
            var dataset = new Dataset();
            var rnd = new Random(42);
            string[] words = { "alpha", "beta", "gamma", "delta", "epsilon" };
            string[] labels = { "A", "B" };

            for (int i = 0; i < 1000; i++)
            {
                var label = labels[rnd.Next(labels.Length)];
                var wordCount = rnd.Next(1, 5);
                var text = string.Join(" ", Enumerable.Range(0, wordCount).Select(_ => words[rnd.Next(words.Length)]));
                dataset.Add(new Sample(text, label));
            }

            var classifier = new SimpleClassifier();

            var sw = Stopwatch.StartNew();
            classifier.Train(dataset);
            sw.Stop();
            Console.WriteLine($"Training time: {sw.ElapsedMilliseconds} ms");

            sw.Restart();
            for (int i = 0; i < 100; i++)
            {
                var text = string.Join(" ", Enumerable.Range(0, 3).Select(_ => words[rnd.Next(words.Length)]));
                classifier.Predict(text);
            }
            sw.Stop();
            Console.WriteLine($"Prediction time for 100 samples: {sw.ElapsedMilliseconds} ms");
        }
    }
}
