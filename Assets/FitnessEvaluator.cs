// using GeneticSharp.Domain.Fitnesses;
// using GeneticSharp.Domain.Chromosomes;
// using System;  // For InvalidOperationException and Math
// using System.Linq;  // For LINQ methods like Select()
// using UnityEngine;
// using System.Collections;

// public class FitnessEvaluator : IFitness
// {
//     private MainExperimentsetup experimentSetup;
//     private float trialError;
//     private bool fitnessReady;

//     public FitnessEvaluator(MainExperimentsetup setup)
//     {
//         this.experimentSetup = setup;
//     }

//     public double Evaluate(IChromosome chromosome)
//     {
//         // Ensure the chromosome is of type CNGChromosome
//         var cngChromosome = chromosome as CNGChromosome;
//         if (cngChromosome == null)
//         {
//             throw new InvalidOperationException("Chromosome is not a CNGChromosome.");
//         }

//         // Get the gene values as an array of floats (physics parameters)
//         double[] physicsParamsDouble = cngChromosome.ToFloatingPoints();
//         float[] physicsParams = physicsParamsDouble.Select(x => (float)Math.Round(x, 5)).ToArray();

//         // Reset fitness-related values
//         fitnessReady = false;
//         trialError = 0f;

//         // Start the trials asynchronously using a coroutine
//         experimentSetup.StartCoroutine(ConductTrialsAndCalculateFitness(physicsParams));

//         // We return 0 here temporarily until the fitness is calculated
//         // We'll update the system when the coroutine finishes, which you can track
//         return 0; // Temporary return value
//     }

//     private IEnumerator ConductTrialsAndCalculateFitness(float[] physicsParams)
//     {
//         // Trigger the trials
//         experimentSetup.StartTrials(physicsParams, OnTrialComplete);

//         // Wait for the trial to complete
//         while (!fitnessReady)
//         {
//             yield return null; // Wait for the next frame
//         }

//         // Calculate and log fitness once the trial is done
//         double fitness = 1 / (1 + trialError);
//         Debug.Log($"Fitness for chromosome: {fitness} with error: {trialError}");
//     }

//     private void OnTrialComplete(float error)
//     {
//         trialError = error;
//         fitnessReady = true; // Mark the fitness as ready
//     }
// }
