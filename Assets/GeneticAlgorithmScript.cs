using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;


public class GeneticAlgorithmScript : MonoBehaviour
{
    private int _populationSize;
    private int _numberOfGenerations;
    private float _crossoverProbability;
    private float _mutationProbability;

    private List<Chromosome> _population;
    private MainExperimentsetup _experimentSetup;
    private float _bestFitness = float.MinValue;
    private float[] _bestPhysicsParams;

    public GeneticAlgorithmScript(MainExperimentsetup experimentSetup, int populationSize, int numberOfGenerations, float crossoverProbability, float mutationProbability)
    {
        _experimentSetup = experimentSetup;
        _populationSize = populationSize;
        _numberOfGenerations = numberOfGenerations;
        _crossoverProbability = crossoverProbability;
        _mutationProbability = mutationProbability;
    }

    public void Start()
    {
        // Initialize population
        InitializePopulation();

        // Run the genetic algorithm loop
        _experimentSetup.StartCoroutine(RunGA());
    }

    private void InitializePopulation()
    {
        _population = new List<Chromosome>();

        for (int i = 0; i < _populationSize; i++)
        {
            Chromosome chromosome = new Chromosome(
                lowerBounds: new double[] { 3, 5, 0.001, 1, 0.1 },
                upperBounds: new double[] { 40, 40, 0.1, 100, 4 }
            );
            _population.Add(chromosome);
        }
    }

    private IEnumerator RunGA()
    {
        for (int generation = 0; generation < _numberOfGenerations; generation++)
        {
            Debug.Log($"Generation {generation} starting...");

            // Step 1: Evaluate population fitness
            yield return EvaluatePopulationFitness();

            // Step 2: Selection
            List<Chromosome> newPopulation = TournamentSelection();

            // Step 3: Crossover
            PerformCrossover(newPopulation);

            // Step 4: Mutation
            PerformMutation(newPopulation);

            // Step 5: Update population
            _population = newPopulation;

            Debug.Log($"Generation {generation} completed.");
        }

        Debug.Log("GA completed. Best fitness: " + _bestFitness);
    }

    private IEnumerator EvaluatePopulationFitness()
    {
        foreach (var chromosome in _population)
        {
            if (!chromosome.IsEvaluated)
            {
                // Convert genes to float[] to be used in the simulation
                float[] physicsParams = chromosome.Genes.Select(g => (float)g).ToArray();

                // Run the simulation for this chromosome and wait for the fitness result
                yield return _experimentSetup.StartCoroutine(_experimentSetup.TrialCoroutine(physicsParams, (fitness) =>
                               {
                                   chromosome.Fitness = fitness; // Fitness based on the trial result
                                   chromosome.IsEvaluated = true;

                                   // Update the best fitness if this one is better
                                   if (chromosome.Fitness > _bestFitness)
                                   {
                                       _bestFitness = chromosome.Fitness;
                                       _bestPhysicsParams = physicsParams;
                                       Debug.Log($"New best fitness: {_bestFitness} with params: {string.Join(", ", _bestPhysicsParams)}");
                                   }
                               }));
            }
        }
    }

    private List<Chromosome> TournamentSelection()
    {
        List<Chromosome> selected = new List<Chromosome>();
        int tournamentSize = 3;

        for (int i = 0; i < _populationSize; i++)
        {
            List<Chromosome> tournamentGroup = new List<Chromosome>();

            // Randomly select chromosomes for the tournament
            for (int j = 0; j < tournamentSize; j++)
            {
                int randomIndex = UnityEngine.Random.Range(0, _populationSize);
                tournamentGroup.Add(_population[randomIndex]);
            }

            // Select the best chromosome from the tournament group
            Chromosome best = tournamentGroup.OrderByDescending(c => c.Fitness).First();
            selected.Add(best.Clone());
        }

        return selected;
    }

    private void PerformCrossover(List<Chromosome> population)
    {
        for (int i = 0; i < population.Count - 1; i += 2)
        {
            if (UnityEngine.Random.value < _crossoverProbability)
            {
                Chromosome parent1 = population[i];
                Chromosome parent2 = population[i + 1];

                Chromosome child1, child2;
                OnePointCrossover(parent1, parent2, out child1, out child2);

                population[i] = child1;
                population[i + 1] = child2;
            }
        }
    }

    private void OnePointCrossover(Chromosome parent1, Chromosome parent2, out Chromosome child1, out Chromosome child2)
    {
        int crossoverPoint = UnityEngine.Random.Range(1, parent1.Genes.Length - 1);

        child1 = parent1.Clone();
        child2 = parent2.Clone();

        for (int i = crossoverPoint; i < parent1.Genes.Length; i++)
        {
            child1.Genes[i] = parent2.Genes[i];
            child2.Genes[i] = parent1.Genes[i];
        }
    }

    private void PerformMutation(List<Chromosome> population)
    {
        foreach (var chromosome in population)
        {
            for (int i = 0; i < chromosome.Genes.Length; i++)
            {
                if (UnityEngine.Random.value < _mutationProbability)
                {
                    chromosome.Genes[i] = chromosome.GenerateRandomGene(i);
                }
            }
        }
    }
    public int PopulationSize { get; private set; }
    public int NumberOfGenerations { get; private set; }

}
public class Chromosome
{
    public double[] Genes { get; private set; }
    public float Fitness { get; set; } = float.MinValue; // Initially, the fitness is undefined (min value)
    public bool IsEvaluated { get; set; } = false; // Flag to check if the chromosome is already evaluated

    private double[] _lowerBounds;
    private double[] _upperBounds;

    public Chromosome(double[] lowerBounds, double[] upperBounds)
    {
        _lowerBounds = lowerBounds;
        _upperBounds = upperBounds;
        Genes = new double[lowerBounds.Length];

        // Randomly initialize the genes (i.e., physics parameters) within the specified bounds
        for (int i = 0; i < lowerBounds.Length; i++)
        {
            Genes[i] = GenerateRandomGene(i);
        }
    }

    public double GenerateRandomGene(int index)
    {
        return UnityEngine.Random.Range((float)_lowerBounds[index], (float)_upperBounds[index]);
    }

    public Chromosome Clone()
    {
        var clone = new Chromosome(_lowerBounds, _upperBounds);
        Array.Copy(this.Genes, clone.Genes, this.Genes.Length);
        return clone;
    }
}
