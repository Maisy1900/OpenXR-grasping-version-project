using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using System.Collections;
using System.Linq;

public class GeneticAlgorithmScript : MonoBehaviour
{
    private int _populationSize;
    private int _numberOfGenerations;
    private float _crossoverProbability;
    private float _mutationProbability;
    private float _convergenceThreshold = 0.001f; // Minimum improvement for convergence
    private int _maxGenerationsWithoutImprovement = 10; // Max generations without improvement before stopping

    private List<Chromosome> _population;
    private List<GenerationData> _generationData; // List to store generation statistics
    private List<TrialData> _trialData; // List to store trial data
    private int _currentGeneration;
    private int _trialNumber;

    private MainExperimentsetup _experimentSetup;
    private float _bestFitness = float.MinValue;
    private float[] _bestPhysicsParams;
    private int _numberOfRuns;
    private int _generationsWithoutImprovement = 0; // Track consecutive generations without improvement

    public int NumTrials { get; set; } = 5; // Default to 5 trials, can be set externally
    public int CurrentTrialNumber { get; private set; }

    public GeneticAlgorithmScript(MainExperimentsetup experimentSetup, int populationSize, int numberOfGenerations, float crossoverProbability, float mutationProbability)
    {
        _experimentSetup = experimentSetup;
        _populationSize = populationSize;
        _numberOfGenerations = numberOfGenerations;
        _crossoverProbability = crossoverProbability;
        _mutationProbability = mutationProbability;
        PopulationSize = _populationSize;
        NumberOfGenerations = _numberOfGenerations;
        _generationData = new List<GenerationData>();  // Initialize generation data list
        _trialData = new List<TrialData>();            // Initialize trial data list
        CurrentTrialNumber = 0;
    }
    #region ga setup logic 
    public void Start()
    {
        // Run multiple trials
       // StartCoroutine(RunMultipleTrials(NumTrials)); // Running for 5 trials including 0
    }
    public void StartGA()
    {
        StartCoroutine(RunMultipleTrials(NumTrials)); // Ensures this runs as a Coroutine from this MonoBehaviour
    }

    private void InitializePopulation()
    {
        _population = new List<Chromosome>();

        for (int i = 0; i < _populationSize; i++)
        {
            Chromosome chromosome = new Chromosome(
                lowerBounds: new double[] { 20, 10, 0.0085, 0.0000005, 0.0005 },
                upperBounds: new double[] { 90, 60, 0.065, 0.007, 4 }
            );
            _population.Add(chromosome);
        }
    }
    public IEnumerator RunMultipleTrials(int numTrials)
    {
        for (int trial = 0; trial < numTrials; trial++)
        {
            Debug.Log($"Starting Trial {trial}...");
            CurrentTrialNumber = trial;

            // Set a new random seed for each trial
            UnityEngine.Random.InitState(trial);
            Debug.Log($"Random seed for Trial {trial}: {trial}");

            // Reset all relevant data for each trial
            ResetTrialData();  // Reset trial-specific variables
            yield return null;

            // Initialize a new population for this trial
            Debug.Log("Initializing population for this trial...");
            InitializePopulation();  // Reinitialize the population
            Debug.Log($"Population initialized for Trial {trial}.");

            // Run the genetic algorithm for the current trial
            yield return RunGA();  // Removed StartCoroutine here
            yield return null;

            // Save trial and generation data for this trial
            Debug.Log($"Saving trial data for Trial {trial}...");
            SaveTrialDataToCSV(CurrentTrialNumber);
            yield return null;
            SaveGenerationDataToCSV(CurrentTrialNumber);
            yield return null;

            Debug.Log($"Trial {trial} completed and data saved.");
        }

        Debug.Log("All trials completed.");
    }


    private IEnumerator RunGA()
    {
        for (int generation = 0; generation < _numberOfGenerations; generation++)
        {
            Debug.Log($"Generation {generation} starting...");

            _currentGeneration = generation;  // Set the current generation

            // Step 1: Evaluate population fitness
            yield return EvaluatePopulationFitness();

            // Step 3: Selection
            List<Chromosome> newPopulation = TournamentSelection();
            yield return null; // Ensure this runs smoothly across frames

            // Step 4: Crossover
            PerformCrossover(newPopulation);
            yield return null;

            // Step 5: Mutation
            PerformMutation(newPopulation);
            yield return null;

            // Step 6: Update population
            _population = newPopulation;
            yield return null;

            // Step 7: Save generation statistics and trial data
            CalculateAndStoreGenerationStatistics(generation);

            yield return null;
            SaveGenerationDataToCSV(_trialNumber);  // Pass the trial number

            yield return null;

            Debug.Log($"Generation {generation} completed.");

            yield return null; // Allow other operations in the main thread to proceed
                               // Example: After evaluating fitness in EvaluatePopulationFitness
            foreach (var chromosome in _population)
            {
                PrintChromosome(chromosome);
            }
            yield return null;
        }

        Debug.Log("GA completed. Best fitness: " + _bestFitness);
        SaveTrialDataToCSV(_trialNumber); // Save the trial data
        yield return null;
    }


    private bool CheckConvergence()
    {
        // Ensure the algorithm runs for at least a minimum number of generations
        if (_currentGeneration < 10)  // Adjust as necessary
        {
            Debug.Log($"Generation {_currentGeneration}: Best fitness so far is {_bestFitness}. Not checking for convergence yet.");
            return false;
        }

        // Existing convergence logic
        float currentBestFitness = _population.Max(c => c.Fitness);
        if (_bestFitness - currentBestFitness < _convergenceThreshold)
        {
            _generationsWithoutImprovement++;

            if (_generationsWithoutImprovement >= _maxGenerationsWithoutImprovement)
            {
                Debug.Log("Convergence achieved. Stopping early.");
                return true; // Converged
            }
        }
        else
        {
            _generationsWithoutImprovement = 0;
        }

        // Print statement if not converging yet
        Debug.Log($"Generation {_currentGeneration}: Best fitness so far is {_bestFitness}. Not converging yet.");

        return false;
    }

    private IEnumerator EvaluatePopulationFitness()
    {
        int trialNumber = 1;

        foreach (var chromosome in _population)
        {
            if (!chromosome.IsEvaluated)
            {
                yield return null; // Prevent blocking

                float[] physicsParams = chromosome.Genes.Select(g => (float)g).ToArray();

                // Ensure you wait for the coroutine to fully complete
                bool evaluationCompleted = false;
                yield return _experimentSetup.StartCoroutine(_experimentSetup.TrialCoroutine(physicsParams, (fitness) =>
                {
                    chromosome.Fitness = fitness;
                    chromosome.IsEvaluated = true;
                    _trialData.Add(new TrialData(_currentGeneration, trialNumber, fitness, physicsParams));

                    if (chromosome.Fitness > _bestFitness)
                    {
                        _bestFitness = chromosome.Fitness;
                        _bestPhysicsParams = physicsParams;
                    }
                    evaluationCompleted = true;
                }));

                // Wait until the fitness evaluation is completed
                while (!evaluationCompleted)
                {
                    yield return null;
                }

                trialNumber++;
                yield return null; // Prevent blocking further
            }
        }
    }

    #endregion

    #region selection mutation crossover 

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
        Debug.Log("Before mutation:");
        foreach (var chromosome in population)
        {
            Debug.Log($"Chromosome Genes: [{string.Join(", ", chromosome.Genes.Select(g => g.ToString("F4")))}]");
        }

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

        Debug.Log("After mutation:");
        foreach (var chromosome in population)
        {
            Debug.Log($"Chromosome Genes: [{string.Join(", ", chromosome.Genes.Select(g => g.ToString("F4")))}]");
        }
    }

    #endregion

    /*Generation statistics
     * go through the chromosomes for the population, for that generation 
     * 
     */
    private void CalculateAndStoreGenerationStatistics(int generation)
    {
        if (_population == null || _population.Count == 0)
        {
            Debug.LogError("Population is empty. Cannot calculate generation statistics.");
            return;
        }

        // Get the best, average, worst, and standard deviation of fitness in the population
        float bestFitness = _population.Max(c => c.Fitness);
        float worstFitness = _population.Min(c => c.Fitness);
        float averageFitness = _population.Average(c => c.Fitness);
        float stdDevFitness = Mathf.Sqrt(_population.Sum(c => Mathf.Pow(c.Fitness - averageFitness, 2)) / _population.Count);

        // Find the chromosome with the best fitness
        Chromosome bestChromosome = _population.First(c => c.Fitness == bestFitness);

        // Convert the best parameters to float[]
        float[] bestParameters = bestChromosome.Genes.Select(g => (float)g).ToArray();

        // Store the generation statistics
        _generationData.Add(new GenerationData(
            generation,
            bestFitness,
            averageFitness,
            stdDevFitness,
            worstFitness,
            bestParameters
        ));

        Debug.Log($"Generation {generation} - Best Fitness: {bestFitness}, Avg Fitness: {averageFitness}, StdDev: {stdDevFitness}, Worst Fitness: {worstFitness}");
    }


    private void SaveGenerationDataToCSV(int trialNumber)
    {
        // Use _currentGeneration directly
        string directoryPath = Path.Combine(Application.dataPath, "SimulationResults", $"Trial{trialNumber}", $"Generation{_currentGeneration}");

        // Check if the directory exists, if not, create it
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Define the file name and path
        string filePath = Path.Combine(directoryPath, "GenerationStats.csv");

        using (StreamWriter writer = new StreamWriter(filePath, append: true)) // Use append to keep adding data
        {
            if (new FileInfo(filePath).Length == 0)
            {
                // Write the header if the file is empty (first write)
                writer.WriteLine("Generation,BestFitness,AverageFitness,StdDevFitness,WorstFitness,BestParameters");
            }

            foreach (var data in _generationData)
            {
                string paramString = string.Join(",", data.BestParameters);
                writer.WriteLine($"{data.GenerationNumber},{data.BestFitness},{data.AverageFitness},{data.StdDevFitness},{data.WorstFitness},{paramString}");
            }
        }

        Debug.Log("Generation data saved to " + filePath);
    }


    public int CurrentGeneration => _currentGeneration;

    private void ResetTrialData()
    {
        // Reset the best fitness and parameters for this trial
        _bestFitness = float.MinValue;  // Reset best fitness for new trial
        _bestPhysicsParams = null;      // Reset best parameters

        // Clear previous generation and trial data
        _generationData.Clear();  // Clear generation statistics
        _trialData.Clear();       // Clear trial statistics

        // Reset generation and trial counters
        _currentGeneration = 0;   // Reset current generation counter
        _generationsWithoutImprovement = 0;  // Reset the no improvement counter

        Debug.Log("Trial data, best fitness, and parameters reset.");
    }

    private void SaveTrialDataToCSV(int trialNumber)//all the trial data and parameters for each generation 
    {
        // Construct the directory path for this trial
        string directoryPath = Path.Combine(Application.dataPath, "SimulationResults", $"Trial{trialNumber}");

        // Check if the directory exists, if not, create it
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Define the file name and path
        string filePath = Path.Combine(directoryPath, "TrialStats.csv");

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("Generation,Trial,Fitness,Parameters");

            foreach (var data in _trialData)
            {
                string paramString = string.Join(",", data.Parameters);
                writer.WriteLine($"{data.GenerationNumber},{data.TrialNumber},{data.Fitness},{paramString}");
            }
        }

        Debug.Log("Trial data saved to " + filePath);
    }


    public int PopulationSize { get; private set; }
    public int NumberOfGenerations { get; private set; }
    public void PrintChromosome(Chromosome chromosome)
    {
        // Convert the genes array to a string for printing
        string genesString = string.Join(", ", chromosome.Genes.Select(g => g.ToString()).ToArray());

        // Print the chromosome details
        Debug.Log($"Chromosome | Genes: [{genesString}] | Fitness: {chromosome.Fitness}");
    }

}
#region chromosome 
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
#endregion
#region data 
public class GenerationData
{
    public int GenerationNumber;
    public float BestFitness;
    public float AverageFitness;
    public float StdDevFitness;
    public float WorstFitness;
    public float[] BestParameters;

    public GenerationData(int generationNumber, float bestFitness, float avgFitness, float stdDevFitness, float worstFitness, float[] bestParameters)
    {
        GenerationNumber = generationNumber;
        BestFitness = bestFitness;
        AverageFitness = avgFitness;
        StdDevFitness = stdDevFitness;
        WorstFitness = worstFitness;
        BestParameters = bestParameters;
    }
}

public class TrialData
{
    public int GenerationNumber;
    public int TrialNumber;
    public float Fitness;
    public float[] Parameters;

    public TrialData(int generationNumber, int trialNumber, float fitness, float[] parameters)
    {
        GenerationNumber = generationNumber;
        TrialNumber = trialNumber;
        Fitness = fitness;
        Parameters = parameters;
    }
}
#endregion 

