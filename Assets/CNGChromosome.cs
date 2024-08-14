using GeneticSharp.Domain.Chromosomes;
using System;

public class CNGChromosome : FloatingPointChromosome
{
    private static long _seed = DateTime.Now.Ticks;
    private static long _modulus = (long)Math.Pow(2, 31);
    private static long _multiplier = 1103515245;
    private static long _increment = 12345;

    private double[] _lowerBounds;
    private double[] _upperBounds;
    private int[] _totalBits;
    private int[] _fractionalBits;

    public CNGChromosome(double[] lowerBounds, double[] upperBounds, int[] totalBits, int[] fractionalBits)
        : base(lowerBounds, upperBounds, totalBits, fractionalBits)
    {
        _lowerBounds = lowerBounds;
        _upperBounds = upperBounds;
        _totalBits = totalBits;
        _fractionalBits = fractionalBits;

        // Override the initial population with CNG-based initialization
        for (int i = 0; i < Length; i++)
        {
            var randomValue = CNG();
            ReplaceGene(i, new Gene(randomValue));
        }
    }

    private double CNG()
    {
        _seed = (_multiplier * _seed + _increment) % _modulus;
        return (double)_seed / _modulus;
    }

    public override IChromosome CreateNew()
    {
        return new CNGChromosome(_lowerBounds, _upperBounds, _totalBits, _fractionalBits);
    }
}
