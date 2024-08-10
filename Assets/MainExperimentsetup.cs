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
    public int number_of_simulations = 10;
    public GameObject articulationObject;
    ArticulationDriver_v2 articulationDriver;
    public Transform cubeObj;

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

    // Function to conduct trials
    IEnumerator ConductTrials()
    {

        RunHandCalibration();
        yield return new WaitForSeconds(1.0f);

        for (int i = 0; i<number_of_simulations; i++)
        {
            // Setup scene (cubes etc) 
            //shuffledTrials[[i]

            // TODO: Change physics parameters here based on the defined metric
            Physics.defaultSolverIterations = 10; // [3-40] in step size of 1
            Physics.defaultSolverVelocityIterations = 5; // [1-40]
            Physics.defaultContactOffset = 0.01f; // [0.001,0.1]
            Physics.defaultMaxDepenetrationVelocity = 10; // [1-100]
            Physics.bounceThreshold = 2; // [0.1-4]

            // TODO: Run hand animations for each trial per condition (lift, push and stack) 
            
            
            // Play animation 
            animationz[shuffledTrials[i]].Play();

            // Wait for trial to complete
            while(animationz[shuffledTrials[i]].isPlaying)
            {
                //RecordResults(Vector3 cubePosition, Vector3 handPose, float timing, int trialNumber); 
                yield return null; 
            }
            animationz[shuffledTrials[i]].Stop();


            // TODO: Compute results after each trial (this means calculating the distances between the current position and the recorded (animation) position of the cube)


            // Update optimization so that the algorithm can decide how to change physics parameters


            // Record data into a file (not essential)
            SaveDataFile();


        }

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


    // Function to run hand animations for each trial
    void RunHandAnimations()
    {
        // TODO: Implement hand animations here
    }
    void Start()
    {
        // Reference the Animator component

        // Find the ArticulationDriver script on the specified GameObject
        articulationDriver = articulationObject.GetComponent<ArticulationDriver_v2>();

        // Start the calibration coroutine
    }

    // Update is called once per frame
    void Update()
    {
        // TODO: Any per-frame updates
    }

    private System.Random _random = new System.Random();
    void Shuffle(int[] array)
    {
        int p = array.Length;
        for (int n = p - 1; n > 0; n--)
        {
            int r = _random.Next(0, n);
            int t = array[r];
            array[r] = array[n];
            array[n] = t;
        }
    }

}