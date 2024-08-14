using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using static UnityEditor.Rendering.InspectorCurveEditor;
using UnityEngine.XR;
using MathNet.Numerics.LinearAlgebra.Storage;
using Unity.VisualScripting;
using System;
using System.Linq;

class DataClass
{
    public List<Vector3> cubePos;
    public List<Vector3> wristPos;
    public List<float> time = new List<float>();
    public List<string> trial_condition = new List<string>();
    public List<int> trial_number = new List<int>();
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
    public ArticulationDriver_Real articulationDriver_real;
    public Transform cubeObj;
    public StackingTrial stack;
    public liftingTrial lift;
    public PushingTrial pushing;

    public Animator main_anim;
    public string[] animation_names = new string[] { "lift_1", "lift_2", "lift_3", "push_1", "push_2", "push_3", "stack_1", "stack_2", "stack_3" };
    private int[] shuffled_anim_indices;

    public ResetPosition[] cubeReseters;

    private Coroutine trial_sequencer;
    // Dictionary to store the preprocessed data
    private Dictionary<string, List<CubeState>> preprocessedData;
    public string[] csvPaths; 

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
    public struct CubeState
    {
        public Vector3 Position; //xyz corrds
        public Quaternion Rotation; //rotation values
        public float Time;
    }
    void PreprocessCSVFile(string csvPath){
        List<CubeState> cubeStates = new List<CubeState>();
        string[] csvLines = File.ReadAllLines(csvPath);
        DateTime beginning_timestamp = DateTime.Parse(csvLines[1].Split(',')[6]);
        //throw away header column
        for (int i = 1; i < csvLines.Length; i++){
            string[] columns = csvLines[i].Split(',');

            if (columns.Length < 7) continue; 
            // Parse position
            Vector3 position = new Vector3(
                float.Parse(columns[0]),//x
                float.Parse(columns[1]),//y
                float.Parse(columns[2])//z
            );

            // Parse rotation as Euler angles and convert to Quaternion
            Vector3 eulerRotation = new Vector3(
                float.Parse(columns[3]),
                float.Parse(columns[4]),
                float.Parse(columns[5])
            );
            Quaternion rotation = Quaternion.Euler(eulerRotation);

            // Parse timestamp
            DateTime timeStamp = DateTime.Parse(columns[6]);
            float time = (float)(timeStamp - beginning_timestamp).TotalSeconds;

            // Create a new CubeState and add it to the list
            CubeState state = new CubeState
            {
                Position = position,
                Rotation = rotation,
                Time = time
            };
            cubeStates.Add(state);
        }

        // Store or process the preprocessed cube states as needed
        preprocessedData[csvPath] = cubeStates;
    }

    /* record the cubes position and rotation at each timestep
    
    public List<CubeState> TrackSimulationData(Transform cubeTransform, float normedTime)
    {
        List<CubeState> simulationData = new List<CubeState>();

        float time = 0f;
        while (normedTime< 1simulation running)
        {
            simulationData.Add(new CubeState
            {
                Position = cubeTransform.position,
                Rotation = cubeTransform.rotation,
                Time = DateTime.Now // You might use another method to get time or normalized time
            });

            // Wait for the next frame
            yield return new WaitForFixedUpdate();
            time += Time.fixedDeltaTime;
        }

        return simulationData;
    }
    */


    void SaveDataFile()
    {
        // Write results to CSV/JSON file

        // Clear all lists 
        dataclass.ClearAllLists();
    }
    void Start()
    {
        // Read all cube animation data files (.csv)
        // And put these into float arrays e.g. cube_pos_x[0][0], or cube_rot_y
        // obj  [task_index] [animation_index]
        // Initialize the dictionary
        preprocessedData = new Dictionary<string, List<CubeState>>();

        // Preprocess each CSV file
        foreach (string csvPath in csvPaths)
        {
            PreprocessCSVFile(csvPath);
        }

        main_anim.StopPlayback();


        // Calculate the total number of simulations
        number_of_simulations = number_of_simulation_blocks * animation_names.Length;
        shuffled_anim_indices = new int[number_of_simulations];

        // Skip the first animation (calibration) and start from index 1
        int originalCount = animation_names.Length;

        // Create a list to hold the expanded array of animations
        List<int> expandedAnimations = new List<int>();

        // Duplicate the original animations (excluding the first one) to fill the required number of trials
        for (int i = 0; i < number_of_simulations; i++)
        {
            // Add the animations starting from index 1 to ignore calibration
            expandedAnimations.Add((i % originalCount));
        }

        // Convert the list back to an array and assign it to the animationz variable
        shuffled_anim_indices = expandedAnimations.ToArray();


        // Now shuffle this expanded array
        Shuffle(shuffled_anim_indices);

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Start the trials
            if (trial_sequencer != null)
                StopCoroutine(trial_sequencer);
            trial_sequencer = StartCoroutine(ConductTrials());
        }
    }

    // Function to conduct trials
    IEnumerator ConductTrials()
    {
        // Reset cube objects 
        cubeReseters[0].ResetCubes();
        cubeReseters[1].ResetCubes();
        cubeReseters[2].ResetCubes();

        //RunHandCalibration();
        main_anim.Play("Calibration hand", 0);
        yield return new WaitForSeconds(1.0f);
        articulationDriver_real.MeasureInitialAngles();
        yield return new WaitForSeconds(1.0f);
        main_anim.StopPlayback();
        Debug.Log("Calibration is over");

        for (int i = 0; i < number_of_simulations; i++)
        {
            // Reset cube objects for each trial 
            cubeReseters[0].ResetCubes();
            cubeReseters[1].ResetCubes();
            cubeReseters[2].ResetCubes();

            Debug.Log("Trial: " + i.ToString() + " out of: " + number_of_simulations.ToString());
            Debug.Log("Anim index: " + shuffled_anim_indices[i].ToString());
            Debug.Log("Anim: " + animation_names[shuffled_anim_indices[i]]);

            // Setup physics parameters for this trial
            Physics.defaultSolverIterations = 10; // [3-40] in step size of 1
            Physics.defaultSolverVelocityIterations = 5; // [1-40]
            Physics.defaultContactOffset = 0.01f; // [0.001,0.1]
            Physics.defaultMaxDepenetrationVelocity = 10; // [1-100]
            Physics.bounceThreshold = 2; // [0.1-4]
            Debug.Log("Physics parameters adjusted!");

            // Play the corresponding animation
            main_anim.Play(animation_names[shuffled_anim_indices[i]], 0);
            bool animstate = main_anim.GetCurrentAnimatorStateInfo(0).IsName(animation_names[shuffled_anim_indices[i]]);
            Debug.Log("Animation playing!");

            List<float> physics_cube_pos_x = new List<float>();
            List<float> physics_cube_pos_y = new List<float>();
            List<float> physics_cube_pos_z = new List<float>();

            // Wait for the animation to complete
            float normedTime = 0f;
            while (normedTime < 1)
            {
                // The next part is only for the optimisation algorithm 
                // ************************************************************************
                // ********************* OPTIMIZATION ALGORITHM ***************************
                // ************************************************************************
                if (true/*cube has moved*/)
                {
                    physics_cube_pos_x.Add(cubeReseters[0].transform.position.x);
                    physics_cube_pos_y.Add(cubeReseters[0].transform.position.y);
                    physics_cube_pos_z.Add(cubeReseters[0].transform.position.z);
                }


                // TODO: Compute results after each trial (this means calculating the distances between the current position and the recorded (animation) position of the cube)
                //match the positions based on when the cube begins to move and the final position of the cubes, we are looking to minimise the position. 


                // Update optimization so that the algorithm can decide how to change physics parameters

                // ************************************************************************
                // ************************************************************************
                // ************************************************************************


                normedTime = main_anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
                yield return null;
            }


            // Compute values after anim loop is done 
            physics_cube_pos_x.Sum();
            physics_cube_pos_y.Sum();
            physics_cube_pos_z.Sum();


            Debug.Log("Animation Done");
            main_anim.StopPlayback();

            // Optionally, add a short delay between trials
            yield return new WaitForSeconds(1.0f);
            Debug.Log("Trial " + i + " done");
        }

        // Record data into a file (not essential)
        SaveDataFile();

        yield return null;

    }

    /*each animation is played once 
     * cubes rotation and position are sampled at regular intervals 
     * collected in a list called cube states which contains the cube's position, rotation, and the corresponding timestamp within the animation.
     * 
     */
    void PreprocessCubeData()
    {
        //align the the time in the cube position recordings with the animation 
        //play the animation 
        //collect the time at which the cube first moves, store a list of the cube states at each point within the animation 

    }

    public float calculateDistannces()
    {
        //compare the distances with the final position of the vitual cubes
        //want to return an array with rotational and positional distances 
        return 0;
    }

    // Function to run hand calibration
    IEnumerator RunHandCalibration()
    {
        // Start the hand calibration animation
        main_anim.Play("Calibration hand", 0);

        // Wait for 2 seconds
        yield return new WaitForSeconds(1.0f);

        // Directly trigger the calibration logic that would normally be triggered by pressing "M"
        // Stop the calibration animation

        articulationDriver_real.MeasureInitialAngles();

        yield return new WaitForSeconds(1.0f);

        main_anim.StopPlayback();

        // Calibration logic is over
        Debug.Log("Calibration is over");
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
    void fitness()
    {
        //A lower difference could correspond to a higher fitness score.
        /*
        Compare with Preprocessed Data:

        The CompareWithPreprocessedData method compares the current position and rotation of the cube with the preprocessed data from the CSV.
        The Vector3.Distance function is used to measure the difference in position, and Quaternion.Angle is used to measure the difference in rotation.
        */
    }
    public void CubeTouched(GameObject touchedCube)
    {
        // Logic to handle when a cube is touched
        Debug.Log("Cube touched: " + touchedCube.name);
    }
}