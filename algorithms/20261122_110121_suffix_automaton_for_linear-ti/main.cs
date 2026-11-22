using System;
using System.Diagnostics;

namespace SuffixAutomatonDemo
{
    internal static class Program
    {
        private static void RunAllTests()
        {
            // Basic functionality tests
            var sam = new SuffixAutomaton("ababa");

            Debug.Assert(sam.Contains("a") == true, "Single character 'a' should be found.");
            Debug.Assert(sam.Contains("b") == true, "Single character 'b' should be found.");
            Debug.Assert(sam.Contains("ab") == true, "'ab' should be found.");
            Debug.Assert(sam.Contains("ba") == true, "'ba' should be found.");
            Debug.Assert(sam.Contains("aba") == true, "'aba' should be found.");
            Debug.Assert(sam.Contains("bab") == true, "'bab' should be found.");
            Debug.Assert(sam.Contains("ababa") == true, "Full string should be found.");
            Debug.Assert(sam.Contains("c") == false, "'c' should not be found.");
            Debug.Assert(sam.Contains("") == true, "Empty pattern should always be true.");

            // Edge cases: repeated characters
            var sam2 = new SuffixAutomaton("aaaaa");
            Debug.Assert(sam2.Contains("a") == true, "Single 'a' in repeated string.");
            Debug.Assert(sam2.Contains("aa") == true, "'aa' in repeated string.");
            Debug.Assert(sam2.Contains("aaa") == true, "'aaa' in repeated string.");
            Debug.Assert(sam2.Contains("aaaaa") == true, "Full repeated string.");
            Debug.Assert(sam2.Contains("aaaaaa") == false, "Longer than source should be false.");

            // Edge case: empty source string
            var samEmpty = new SuffixAutomaton(string.Empty);
            Debug.Assert(samEmpty.Contains("") == true, "Empty pattern in empty source.");
            Debug.Assert(samEmpty.Contains("a") == false, "Non-empty pattern in empty source.");

            // Random longer test
            string longText = GenerateRandomString(1000, seed: 42);
            var samLong = new SuffixAutomaton(longText);
            // Verify that every prefix of the text is a substring
            for (int i = 0; i <= longText.Length; i += 100)
            {
                string prefix = longText.Substring(0, i);
                Debug.Assert(samLong.Contains(prefix) == true, $"Prefix length {i} should be found.");
            }
            // Verify a pattern that is known not to exist
            Debug.Assert(samLong.Contains("~") == false, "Character not present in source should be false.");

            Console.WriteLine("All unit tests passed.");
        }

        private static string GenerateRandomString(int length, int seed = 0)
        {
            var rnd = new Random(seed);
            char[] chars = new char[length];
            const string alphabet = "abcdefghijklmnopqrstuvwxyz";
            for (int i = 0; i < length; i++)
            {
                chars[i] = alphabet[rnd.Next(alphabet.Length)];
            }
            return new string(chars);
        }

        public static void Main()
        {
            try
            {
                RunAllTests();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Test failure: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}
