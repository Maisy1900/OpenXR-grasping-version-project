using GeneticSharp.Domain;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;

public class GeneticAlgorithmManager
{
    private GeneticAlgorithm _ga;
    private FitnessEvaluator _fitnessEvaluator;

    public GeneticAlgorithmManager(MainExperimentsetup experimentSetup)
    { 
        _fitnessEvaluator = new FitnessEvaluator(experimentSetup); // Pass experimentSetup to the fitness evaluator
        
        /*            // Setup physics parameters for this trial
            Physics.defaultSolverIterations = 10; // [3-40] in step size of 1
            Physics.defaultSolverVelocityIterations = 5; // [1-40]
            Physics.defaultContactOffset = 0.01f; // [0.001,0.1]
            Physics.defaultMaxDepenetrationVelocity = 10; // [1-100]
            Physics.bounceThreshold = 2; // [0.1-4]
            Debug.Log("Physics parameters adjusted!");
        */
        // Define the chromosome and population as before
        var chromosome = new CNGChromosome(
            lowerBounds: new double[] { 3, 5, 0.001, 1, 0.1 },
            upperBounds: new double[] { 40, 40, 0.1, 100, 4 },
            totalBits: new int[] { 8, 8, 8, 8, 8 },
            fractionalBits: new int[] { 4, 4, 4, 4, 4 }
        );

        var population = new Population(50, 100, chromosome);

        // Setup fitness function
        var fitness = _fitnessEvaluator; 

        // Set the selection method
        var selection = new TournamentSelection(); // Or TournamentSelection, EliteSelection, etc.

        // Set the crossover method and probability
        var crossover = new OnePointCrossover();

        // Set the mutation method and probability
        var mutation = new UniformMutation(); // No need to set MutationProbability


        // Set a fixed number of generations for the algorithm
        var termination = new GenerationNumberTermination(50); // Stop after 50 generations

        // Initialize the GeneticAlgorithm object with the chosen operators
        _ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
        {
            Termination = termination
        };

        // Set the probabilities after initialization
        _ga.CrossoverProbability = 0.8f; // 80% crossover probability
        _ga.MutationProbability = 0.05f;  // 5% mutation probability

    }

    public void Start()
    {
        _ga.Start();

    }
}
