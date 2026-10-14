using System;
using System.Collections.Generic;

namespace CelebrityProblemSolution
{
    internal static class Program
    {
        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException($"Assertion failed: {message}");
        }

        private static void RunAllTests()
        {
            var finder = new CelebrityFinder();

            // Test 1: Empty matrix.
            bool[,] empty = new bool[0, 0];
            Assert(finder.FindCelebrity(empty) == -1, "Empty matrix should return -1.");

            // Test 2: Single person, not a celebrity (by definition they are a celebrity because they know no one and are known by no one).
            bool[,] single = new bool[1, 1] { { false } };
            Assert(finder.FindCelebrity(single) == 0, "Single person should be identified as celebrity.");

            // Test 3: Celebrity exists.
            // 0 knows 2, 1 knows 2, 3 knows 2, 2 knows nobody.
            bool[,] matrixWithCelebrity = new bool[4, 4]
            {
                { false, false, true , false },
                { false, false, true , false },
                { false, false, false, false },
                { false, false, true , false }
            };
            Assert(finder.FindCelebrity(matrixWithCelebrity) == 2, "Person 2 should be identified as celebrity.");

            // Test 4: No celebrity (mutual knowledge).
            bool[,] noCelebrity = new bool[3, 3]
            {
                { false, true , false },
                { false, false, true  },
                { true , false, false }
            };
            Assert(finder.FindCelebrity(noCelebrity) == -1, "No celebrity should be reported as -1.");

            // Test 5: Larger matrix with celebrity at index 0.
            bool[,] largeWithCeleb = new bool[5,5];
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                    largeWithCeleb[i,j] = false; // initialise

            // Everyone knows 0, 0 knows nobody.
            for (int i = 1; i < 5; i++)
                largeWithCeleb[i,0] = true;

            // Add some random other relationships that do not affect celebrity property.
            largeWithCeleb[2,3] = true;
            largeWithCeleb[4,2] = true;

            Assert(finder.FindCelebrity(largeWithCeleb) == 0, "Person 0 should be identified as celebrity in larger matrix.");

            // Test 6: Invalid (non‑square) matrix throws.
            bool[,] nonSquare = new bool[2,3];
            bool threw = false;
            try
            {
                finder.FindCelebrity(nonSquare);
            }
            catch (ArgumentException)
            {
                threw = true;
            }
            Assert(threw, "Non‑square matrix should throw ArgumentException.");

            // Test 7: Null matrix throws.
            threw = false;
            try
            {
                finder.FindCelebrity(null);
            }
            catch (ArgumentNullException)
            {
                threw = true;
            }
            Assert(threw, "Null matrix should throw ArgumentNullException.");

            // Test 8: Celebrity candidate fails verification because they know someone.
            bool[,] knowsSomeone = new bool[3,3]
            {
                { false, false, false },
                { false, false, false },
                { false, true , false } // person 2 knows person 1, so cannot be celeb.
            };
            // Everyone knows 2 except person 2 themselves.
            knowsSomeone[0,2] = true;
            knowsSomeone[1,2] = true;
            Assert(finder.FindCelebrity(knowsSomeone) == -1, "Person who knows someone cannot be celebrity.");

            Console.WriteLine("All tests passed.");
        }

        static void Main()
        {
            RunAllTests();
        }
    }
}
