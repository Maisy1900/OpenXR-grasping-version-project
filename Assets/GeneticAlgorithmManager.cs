// using GeneticSharp.Domain;
// using GeneticSharp.Domain.Chromosomes;
// using GeneticSharp.Domain.Crossovers;
// using GeneticSharp.Domain.Mutations;
// using GeneticSharp.Domain.Populations;
// using GeneticSharp.Domain.Selections;
// using GeneticSharp.Domain.Terminations;
// using GeneticSharp.Domain.Fitnesses;
// using System.Linq;

// public class GeneticAlgorithmManager
// {
//     private GeneticAlgorithm _ga;
//     private FitnessEvaluator _fitnessEvaluator;

//     public GeneticAlgorithmManager(FitnessEvaluator fitnessEvaluator)
//     {
//         _fitnessEvaluator = fitnessEvaluator;
//             /*
//             // Setup physics parameters for this trial
//             Physics.defaultSolverIterations = 10; // [3-40] in step size of 1
//             Physics.defaultSolverVelocityIterations = 5; // [1-40]
//             Physics.defaultContactOffset = 0.01f; // [0.001,0.1]
//             Physics.defaultMaxDepenetrationVelocity = 10; // [1-100]
//             Physics.bounceThreshold = 2; // [0.1-4]
//             Debug.Log("Physics parameters adjusted!");
//             */
//         var chromosome = new CNGChromosome(
//             lowerBounds: new double[] { 3, 5, 0.001, 1, 0.1 },
//             upperBounds: new double[] { 40, 40, 0.1, 100, 4 },
//             totalBits: new int[] { 8, 8, 8, 8, 8 },
//             fractionalBits: new int[] { 4, 4, 4, 4, 4 }
//         );

//         var population = new Population(50, 100, chromosome);

//         var fitness = new FitnessFunction(_fitnessEvaluator);
//         var selection = new TournamentSelection();
//         var crossover = new OnePointCrossover(); // Corrected name
//         var mutation = new UniformMutation(); // Corrected name
//         var termination = new FitnessStagnationTermination(100);

//         _ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
//         {
//             Termination = termination
//         };
//     }

//     public void Start()
//     {
//         _ga.Start();
//     }
// }

// public class FitnessFunction : IFitness
// {
//     private FitnessEvaluator _evaluator;

//     public FitnessFunction(FitnessEvaluator evaluator)
//     {
//         _evaluator = evaluator;
//     }

//     public double Evaluate(IChromosome chromosome)
//     {
//         var floatChromosome = chromosome as FloatingPointChromosome;
//         var values = floatChromosome.ToFloatingPoints();
//         return _evaluator.EvaluateFitness(values.Select(v => (float)v).ToArray());
//     }
// }