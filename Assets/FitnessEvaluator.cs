public class FitnessEvaluator
{
    private MainExperimentsetup experimentSetup;

    public FitnessEvaluator(MainExperimentsetup setup)
    {
        this.experimentSetup = setup;
    }
    /*add logic to FitnessEvaluator: Should evaluate fitness based on the simulation results and preprocessed data.#
     * Run the simulation using the provided physics parameters.
     * Compare the results of the simulation to your preprocessed data.
     * Return a fitness score based on how close the simulation results are to the preprocessed data.
     * Next Steps: This is where you'll need to focus your efforts:

Implement the Simulation Call: Ensure that ConductTrials in MainExperimentSetup can accept the physics parameters and run the trial with those settings.
Implement the Fitness Calculation: You need to calculate how closely the results of the simulation match your expected results. This typically involves comparing positions and rotations of objects (like cubes) and returning a score.
4. MainExperimentSetup
Status: This class still needs work, specifically in how it interacts with the genetic algorithm:

ConductTrials Method: Modify it to accept physics parameters and apply them to the simulation.
CalculateFitness Method: Implement this method to calculate and return a fitness score based on the trial results.
     */
    public double EvaluateFitness(float[] physicsParams)
    {
        // Run the simulation with the given parameters
        var trialCoroutine = experimentSetup.StartCoroutine(experimentSetup.ConductTrials(physicsParams));

        // Assuming the trials will update some result internally
        // Wait for trials to finish before evaluating (use some mechanism to check when it's done)

        // Here you would typically wait or yield until the trial is complete
        // and then calculate the fitness based on the results of the trial.
        // For simplicity, let's assume there's a method to get the result:
        //double fitnessScore = experimentSetup.GetTrialResults(); // Implement this method

        return 0; //fitnessScore;*******************************
    }
}
