// using GeneticSharp.Domain;
// using GeneticSharp.Domain.Chromosomes;
// using GeneticSharp.Domain.Crossovers;
// using GeneticSharp.Domain.Mutations;
// using GeneticSharp.Domain.Populations;
// using GeneticSharp.Domain.Selections;
// using GeneticSharp.Domain.Terminations;
// using UnityEngine;
// using System;

// public class GeneticAlgorithmManager
// {
//     private GeneticAlgorithm _ga;
//     private MainExperimentsetup _experimentSetup;
//     private int _numberOfGenerations; // Declare _numberOfGenerations here at the class level

//     public GeneticAlgorithmManager(MainExperimentsetup experimentSetup)
//     {
//         if (experimentSetup == null)
//         {
//             throw new ArgumentNullException(nameof(experimentSetup), "MainExperimentsetup cannot be null.");
//         }

//         _experimentSetup = experimentSetup;

//         // Define the chromosome and population
//         var chromosome = new CNGChromosome(
//             lowerBounds: new double[] { 3, 5, 0.001, 1, 0.1 },
//             upperBounds: new double[] { 40, 40, 0.1, 100, 4 }
//         );

//         var population = new Population(20, 50, chromosome);

//         // Set the selection method
//         var selection = new TournamentSelection();

//         // Set the crossover method and probability
//         var crossover = new OnePointCrossover();

//         // Set the mutation method and probability
//         var mutation = new UniformMutation();

//         // Set the number of generations here
//         _numberOfGenerations = 50;

//         // Set a fixed number of generations for the algorithm
//         var termination = new GenerationNumberTermination(_numberOfGenerations);

//         // Pass the MainExperimentsetup instance as the fitness evaluator
//         _ga = new GeneticAlgorithm(population, _experimentSetup, selection, crossover, mutation)
//         {
//             Termination = termination,
//             CrossoverProbability = 0.8f,
//             MutationProbability = 0.05f
//         };
//     }

//     public void Start()
//     {
//         _ga.Start();
//         Debug.Log("GA started");
//     }

//     // Get the number of generations (from the stored value)
//     public int GetNumberOfGenerations()
//     {
//         return _numberOfGenerations;
//     }

//     public IPopulation GetPopulation()
//     {
//         return _ga.Population;
//     }
// }
