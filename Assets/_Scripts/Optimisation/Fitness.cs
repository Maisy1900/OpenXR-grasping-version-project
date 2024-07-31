using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using UnityEngine;

public class Fitness : IFitness
{
    private Transform _virtualCube;
    private Transform _realCube;

    public Fitness(Transform virtualCube, Transform realCube)
    {
        _virtualCube = virtualCube;
        _realCube = realCube;
    }

    public double Evaluate(IChromosome chromosome)
    {
        // Extract parameters from the chromosome and apply them to the virtual cube

        // Calculate the distance between the virtual cube's position and the real cube's position
        float distance = Vector3.Distance(_virtualCube.position, _realCube.position);

        // Fitness is inversely related to distance; closer positions yield a higher fitness
        double fitness = 1.0 / (distance + 1); // Adding 1 to avoid division by zero

        return fitness;
    }
}
