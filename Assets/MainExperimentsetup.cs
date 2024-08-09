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
}

public class MainExperimentsetup : MonoBehaviour
{
    // Variables to store experiment data
    DataClass dataclass;
    public int number_of_simulations = 10;
    public ArticulationDriver articulationDriver; 

    // Function to record results
    void RecordResults(Vector3 cubePosition, Vector3 handPose, float timing, int trialNumber)
    {
        // TODO: record cube  orientation and position

        dataclass.trial_condition.Add("Lifting");
        dataclass.trial_number.Add(trialNumber);
        dataclass.cubePos.Add(cubePosition);
        dataclass.wristPos.Add(handPose);
        dataclass.time.Add(timing);
        dataclass.fps.Add(Time.captureFramerate);

        // Write results to CSV file

    }

    // Function to conduct trials
    IEnumerator ConductTrials()
    {
        for (int i = 0; i<number_of_simulations; i++)
        {
            // TODO: Change physics parameters here based on the defined metric
            Physics.defaultSolverIterations = 10;
            Physics.defaultSolverVelocityIterations = 5;
            Physics.defaultContactOffset = 0.01f;
            Physics.defaultMaxDepenetrationVelocity = 10;
            Physics.bounceThreshold = 2;
            
            // TODO: Run hand animations for each trial

            // Wait for trial to complete

            // TODO: Record results after each trial


        }

        return null; 

    }

    // Function to run hand calibration
    IEnumerator RunHandCalibration()
    {
        // Start the hand calibration animation
        //articulationDriver. 

        // Wait for 2 seconds
        yield return new WaitForSeconds(2.0f);

        // Directly trigger the calibration logic that would normally be triggered by pressing "M"
        // Stop the calibration animation

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
        //articulationDriver = GameObject.FindWi thTag("RightHandDoNotUse").GetComponent<ArticulationDriver>();

        // Start the calibration coroutine
    }

    // Update is called once per frame
    void Update()
    {
        //articulationDriver.MeasureAn();
        // TODO: Any per-frame updates
    }
}
