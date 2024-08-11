using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

class DataClass
{
    public List<Vector3> cubePos;
    public List<Vector3> wristPos;
    public List<float> time = new List<float>();
    public List<string> trial_condition = new List<string>();
    public List<int> trial_number= new List<int>();
    public List<float> fps = new List<float>();

    DataClass()
    {
        cubePos = new List<Vector3>();
        wristPos = new List<Vector3>();
        time = new List<float>();
        trial_condition = new List<string>();
        fps = new List<float>();

    }


    public void ClearAllLists()
    {
        cubePos.Clear();
        wristPos.Clear();
        time.Clear();
        trial_condition.Clear();
        trial_number.Clear();
        fps.Clear();
    }
}

public class MainExperimentsetup : MonoBehaviour
{
    // Variables to store experiment data
    DataClass dataclass;
    public int number_of_simulation_blocks = 5; // Number of full sets of 9 trials
    private int number_of_simulations;
    public GameObject articulationObject;
    ArticulationDriver_v2 articulationDriver;
    public Transform cubeObj;
    public StackingTrial stack;
    public liftingTrial lift;
    public PushingTrial pushing; 

    public Animation[] animationz; // 0 = Calibration anim, 1-3 = lift, 4-6 = push and 7-9 = stack

    int[] shuffledTrials = new int[30];

    // Function to record results (check if this is a bottleneck)  
    void RecordResults(Vector3 cubePosition, Vector3 handPose, float timing, int trialNumber)
    {
        // TODO: record cube  orientation and position

        dataclass.trial_condition.Add("Lifting");
        dataclass.trial_number.Add(trialNumber);
        dataclass.cubePos.Add(cubePosition);
        dataclass.wristPos.Add(handPose);
        dataclass.time.Add(timing);
        dataclass.fps.Add(Time.captureFramerate);

        
    }
    void SaveDataFile()
    {
        // Write results to CSV/JSON file



        // Clear all lists 
        dataclass.ClearAllLists(); 
    }
    void Start()
    {
        // Calculate the total number of simulations
        number_of_simulations = number_of_simulation_blocks * 9;

        // Skip the first animation (calibration) and start from index 1
        int originalCount = animationz.Length - 1; // Since we skip the first one

        // Create a list to hold the expanded array of animations
        List<Animation> expandedAnimations = new List<Animation>();

        // Duplicate the original animations (excluding the first one) to fill the required number of trials
        for (int i = 0; i < number_of_simulations; i++)
        {
            // Add the animations starting from index 1 to ignore calibration
            expandedAnimations.Add(animationz[(i % originalCount) + 1]);
        }

        // Convert the list back to an array and assign it to the animationz variable
        animationz = expandedAnimations.ToArray();

        // Now shuffle this expanded array
        Shuffle(animationz);

        // Start the trials
        StartCoroutine(ConductTrials());
    }

    // Function to conduct trials
    IEnumerator ConductTrials()
    {

        RunHandCalibration();
        yield return new WaitForSeconds(1.0f);
        for (int i = 0; i < number_of_simulations; i++)
        {
            // Setup physics parameters for this trial
            Physics.defaultSolverIterations = 10; // [3-40] in step size of 1
            Physics.defaultSolverVelocityIterations = 5; // [1-40]
            Physics.defaultContactOffset = 0.01f; // [0.001,0.1]
            Physics.defaultMaxDepenetrationVelocity = 10; // [1-100]
            Physics.bounceThreshold = 2; // [0.1-4]

            // Determine which trial type to run based on the current animation index
            if (i % 9 >= 0 && i % 9 <= 2)  // Lift animations (1-3)
            {
                lift.enabled = true;
                pushing.enabled = false;
                stack.enabled = false;

                lift.RunTrial();
            }
            else if (i % 9 >= 3 && i % 9 <= 5)  // Push animations (4-6)
            {
                lift.enabled = false;
                pushing.enabled = true;
                stack.enabled = false;

                pushing.RunTrial();
            }
            else if (i % 9 >= 6 && i % 9 <= 8)  // Stack animations (7-9)
            {
                lift.enabled = false;
                pushing.enabled = false;
                stack.enabled = true;

                stack.RunTrial();
            }

            // Play the corresponding animation
            animationz[i].Play();

            // Wait for the animation to complete
            while (animationz[i].isPlaying)
            {
                yield return null;
            }

            animationz[i].Stop();

            // Optionally, add a short delay between trials
            yield return new WaitForSeconds(1.0f);
        }
    // Play animation 
    //animationz[shuffledTrials[i]].Play();




    // TODO: Compute results after each trial (this means calculating the distances between the current position and the recorded (animation) position of the cube)


    // Update optimization so that the algorithm can decide how to change physics parameters


    // Record data into a file (not essential)
    SaveDataFile();

        yield return null; 

    }

    // Function to run hand calibration
    IEnumerator RunHandCalibration()
    {
        // Start the hand calibration animation
        //articulationDriver. 
        animationz[0].Play(); 

        // Wait for 2 seconds
        yield return new WaitForSeconds(1.0f);

        // Directly trigger the calibration logic that would normally be triggered by pressing "M"
        // Stop the calibration animation
        
        articulationDriver.MeasureInitialAngles();
        
        yield return new WaitForSeconds(1.0f);

        animationz[0].Stop();
        // Calibration logic is over
        Debug.Log("Calibration is over");
    }
    void Update()
    {
    }

    private System.Random _random = new System.Random();
    void Shuffle(Animation[] array)
    {
        int p = array.Length;
        for (int n = p - 1; n > 0; n--)
        {
            int r = _random.Next(0, n);
            Animation t = array[r];
            array[r] = array[n];
            array[n] = t;
        }
    }


}