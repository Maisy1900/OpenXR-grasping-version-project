using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Chromosomes;
using System.Linq;
using System.Threading;
using UnityEngine;

public class FitnessEvaluator : IFitness
{
    private MainExperimentsetup experimentSetup;
    private bool trialCompleted;
    private float trialError;

    public FitnessEvaluator(MainExperimentsetup setup)
    {
        this.experimentSetup = setup;
    }

    public double Evaluate(IChromosome chromosome)
    {
        // Convert the chromosome to physics parameters
    var floatChromosome = chromosome as FloatingPointChromosome;
    float[] physicsParams = floatChromosome.ToFloatingPoints().Select(x => Mathf.Clamp((float)x, 0f, 100f)).ToArray();  // Adjust these bounds


        // Reset error value
        trialError = 0f;
        trialCompleted = false;

        // Start the trials asynchronously using a callback
        experimentSetup.StartTrials(physicsParams, OnTrialComplete);
        Debug.Log("starting trials");

        // Wait for trial completion
        while (!trialCompleted)
        {
            Thread.Sleep(3);  // Small delay to prevent locking the main thread
        }

        // Log the fitness result for this chromosome
        Debug.Log($"Fitness for chromosome: {1 / (1 + trialError)} with error: {trialError}");

        // Return fitness value based on error (higher fitness = lower error)
        return 1 / (1 + trialError);  // Prevent division by zero
    }

    private void OnTrialComplete(float error)
    {
        // Log the trial error received from the simulation
        Debug.Log($"Trial complete with error: {error}");

        trialError = error;
        trialCompleted = true; // Signal that the trial has finished
    }
}
