using GeneticSharp.Domain.Chromosomes;
using System;

public class CNGChromosome : ChromosomeBase
{
    private static long _seed = DateTime.Now.Ticks;
    private static long _modulus = (long)Math.Pow(2, 31);
    private static long _multiplier = 1103515245;
    private static long _increment = 12345;

    private double[] _lowerBounds;
    private double[] _upperBounds;

    public CNGChromosome(double[] lowerBounds, double[] upperBounds)
        : base(lowerBounds.Length) // Pass the length of the chromosome
    {
        _lowerBounds = lowerBounds;
        _upperBounds = upperBounds;

        for (int i = 0; i < lowerBounds.Length; i++)
        {
            var randomValue = GenerateRandomValueWithinBounds(i);
            ReplaceGene(i, new Gene(randomValue)); // Replace the gene with the random value
        }
    }

    // This method generates a random value within the specified bounds
    private double GenerateRandomValueWithinBounds(int index)
    {
        // Generate a random value between 0 and 1 using a linear congruential generator (LCG)
        _seed = (_multiplier * _seed + _increment) % _modulus;
        double value = (double)_seed / _modulus;

        // Scale the value to fit within the specified bounds for the index
        double scaledValue = _lowerBounds[index] + value * (_upperBounds[index] - _lowerBounds[index]);

        // Ensure that the scaled value is within bounds by clamping it
        scaledValue = Math.Max(_lowerBounds[index], Math.Min(scaledValue, _upperBounds[index]));

        // Round to 5 decimal places (you can adjust this)
        return Math.Round(scaledValue, 5);
    }

    // Create a new chromosome instance
    public override IChromosome CreateNew()
    {
        return new CNGChromosome(_lowerBounds, _upperBounds);
    }

    // Generate a new gene (random value within bounds) for the given index
    public override Gene GenerateGene(int geneIndex)
    {
        // Generate a new random value within bounds for the gene index
        return new Gene(GenerateRandomValueWithinBounds(geneIndex));
    }

    // Convert the genes into an array of floating-point values
    public double[] ToFloatingPoints()
    {
        var values = new double[Length];
        for (int i = 0; i < Length; i++)
        {
            values[i] = (double)GetGene(i).Value;
        }
        return values;
    }
}
