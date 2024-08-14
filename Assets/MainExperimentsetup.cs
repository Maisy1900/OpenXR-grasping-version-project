using System.Collections;
using System.Collections.Generic;
using System.IO;
using static UnityEditor.Rendering.InspectorCurveEditor;
using UnityEngine.XR;
using MathNet.Numerics.LinearAlgebra.Storage;
using Unity.VisualScripting;
using System;
using System.Linq;
using UnityEngine;

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
    public void SaveToCSV(string fileName)
    {
        string directoryPath = Path.Combine(Application.dataPath, "SimulationResults");

        // Check if directory exists, if not, create it
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string path = Path.Combine(directoryPath, fileName + ".csv");

        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("CubePosX,CubePosY,CubePosZ,Time,TrialCondition,TrialNumber");

            for (int i = 0; i < cubePos.Count; i++)
            {
                writer.WriteLine($"{cubePos[i].x},{cubePos[i].y},{cubePos[i].z},{time[i]},{trial_condition[i]},{trial_number[i]}");//save rotations too 
            }
        }

        Debug.Log("Data saved to " + path);
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
    // Dictionary to track first touch and store positions
    private Dictionary<GameObject, bool> cubeTouchedStatus;
    private Dictionary<GameObject, CubeState> initialCubeStates;
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
    private bool baseCubeFirstTouched = false;
    private bool middleCubeFirstTouched = false;
    private bool topCubeFirstTouched = false;
    // This method returns a boolean indicating if the cube was touched for the first time
    public bool CubeTouched(GameObject touchedCube, int cubeIndex)
    {
        // For stacking trials, each cube has its own "first touch" tracking
        if (cubeIndex == 0 && !baseCubeFirstTouched)
        {
            baseCubeFirstTouched = true;
            Debug.Log($"Base Cube was touched for the first time.");
            return true;
        }
        else if (cubeIndex == 1 && !middleCubeFirstTouched)
        {
            middleCubeFirstTouched = true;
            Debug.Log($"Middle Cube was touched for the first time.");
            return true;
        }
        else if (cubeIndex == 2 && !topCubeFirstTouched)
        {
            topCubeFirstTouched = true;
            Debug.Log($"Top Cube was touched for the first time.");
            return true;
        }

        return false;
    }
    public struct CubeState
    {
        public Vector3 Position; //xyz corrds
        public Quaternion Rotation; //rotation values
        public float Time;
    }
    void PreprocessCSVFile(string csvPath)
    {
        List<CubeState> cubeStates = new List<CubeState>();
        string[] csvLines = File.ReadAllLines(csvPath);
        DateTime beginning_timestamp = DateTime.Parse(csvLines[1].Split(',')[6]);
        //throw away header column
        for (int i = 1; i < csvLines.Length; i++)
        {
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
        // Clear all lists 
        dataclass.ClearAllLists();
    }
    void Start()
    {
        cubeTouchedStatus = new Dictionary<GameObject, bool>();
        initialCubeStates = new Dictionary<GameObject, CubeState>();

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
            //  trial_sequencer = StartCoroutine(ConductTrials()); *************************************
        }
    }

    // Function to conduct trials
    public IEnumerator ConductTrials(float[] physicsParams)
    {
        // Reset cube objects 
        cubeReseters[0].ResetCubes();
        cubeReseters[1].ResetCubes();
        cubeReseters[2].ResetCubes();

        //Running the hand calibration
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

            // Reset touch tracking for new trial
            baseCubeFirstTouched = false;
            middleCubeFirstTouched = false;
            topCubeFirstTouched = false;

            Debug.Log("Trial: " + i.ToString() + " out of: " + number_of_simulations.ToString());
            Debug.Log("Anim index: " + shuffled_anim_indices[i].ToString());
            Debug.Log("Anim: " + animation_names[shuffled_anim_indices[i]]);

            // Apply dynamic physics parameters
            Physics.defaultSolverIterations = (int)physicsParams[0];
            Physics.defaultSolverVelocityIterations = (int)physicsParams[1];
            Physics.defaultContactOffset = physicsParams[2];
            Physics.defaultMaxDepenetrationVelocity = physicsParams[3];
            Physics.bounceThreshold = physicsParams[4];

            // Play the corresponding animation
            main_anim.Play(animation_names[shuffled_anim_indices[i]], 0);
            bool animstate = main_anim.GetCurrentAnimatorStateInfo(0).IsName(animation_names[shuffled_anim_indices[i]]);
            Debug.Log("Animation playing!");

            // Wait for the animation to complete
            float normedTime = 0f;
            while (normedTime < 1)
            {
                normedTime = main_anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
                // The next part is only for the optimisation algorithm 
                // ************************************************************************
                // ********************* OPTIMIZATION ALGORITHM ***************************
                // ************************************************************************
                if (cubeFirstTouched)
                {
                    if (i < 6)
                    {
                        // For lifting and pushing animations (trial indices 0 to 5)
                        // Only one cube's position and rotation need to be recorded
                        physics_cube_pos_x.Add(cubeReseters[0].transform.position.x);
                        physics_cube_pos_y.Add(cubeReseters[0].transform.position.y);
                        physics_cube_pos_z.Add(cubeReseters[0].transform.position.z);

                        physics_cube_rot_x.Add(cubeReseters[0].transform.rotation.x);
                        physics_cube_rot_y.Add(cubeReseters[0].transform.rotation.y);
                        physics_cube_rot_z.Add(cubeReseters[0].transform.rotation.z);

                        // Store the simulated positions and rotations
                        simulatedPositions.Add(cubeReseters[0].transform.position);
                        simulatedRotations.Add(cubeReseters[0].transform.rotation);
                    }
                    else if (i >= 6)
                    {
                        // For stacking animations (trial indices 6 to 8)
                        // Record the position and rotation of all three cubes
                        for (int j = 0; j < 3; j++)
                        {
                            physics_cube_pos_x.Add(cubeReseters[j].transform.position.x);
                            physics_cube_pos_y.Add(cubeReseters[j].transform.position.y);
                            physics_cube_pos_z.Add(cubeReseters[j].transform.position.z);

                            physics_cube_rot_x.Add(cubeReseters[j].transform.rotation.x);
                            physics_cube_rot_y.Add(cubeReseters[j].transform.rotation.y);
                            physics_cube_rot_z.Add(cubeReseters[j].transform.rotation.z);

                            // Store the simulated positions and rotations
                            simulatedPositions.Add(cubeReseters[j].transform.position);
                            simulatedRotations.Add(cubeReseters[j].transform.rotation);
                        }
                    }

                }
                //we have the 

                // TODO: Compute results after each trial (this means calculating the distances between the current position and the recorded (animation) position of the cube)
                //match the positions based on when the cube begins to move and the final position of the cubes, we are looking to minimise the position. 


                // Update optimization so that the algorithm can decide how to change physics parameters

                // ************************************************************************
                // ************************************************************************
                // ************************************************************************

                yield return null;
            }
            // Once animation has finished, calculate total error (fitness) for this trial
            float totalPositionError = 0f;
            float totalRotationError = 0f;

            //match the current animation with the csv file of desired locations
            //csvs 0-5 we can use [i] 6-14
            if (i >= 0 && i <= 5)
            {
                // For lifting and pushing animations (index 0-5)
                string csvKey = csvPaths[i];
                List<CubeState> cubeStates = preprocessedData[csvKey];

                // Use cubeStates in your simulation or trial logic
            }
            else if (i >= 6 && i < 9)
            {
                // For stacking animations (index 6-8)

                // First cube (base) bases can either be 6 when i=6, 9 when i=7, or 12 when i=8
                string baseCsvKey = csvPaths[6 + (i - 6) * 3];  // Correctly indexing into the base CSV
                List<CubeState> baseCubeStates = preprocessedData[baseCsvKey];

                // Second cube (middle) can either be 7 when i=6, 10 when i=7, or 13 when i=8
                string middleCsvKey = csvPaths[7 + (i - 6) * 3];  // Correctly indexing into the middle CSV
                List<CubeState> middleCubeStates = preprocessedData[middleCsvKey];

                // Third cube (top) can either be 8 when i=6, 11 when i=7, or 14 when i=8
                string topCsvKey = csvPaths[8 + (i - 6) * 3];  // Correctly indexing into the top CSV
                List<CubeState> topCubeStates = preprocessedData[topCsvKey];

                // Use baseCubeStates, middleCubeStates, and topCubeStates in your simulation or trial logic
            }

            List<float> physics_cube_pos_x = new List<float>();
            List<float> physics_cube_pos_y = new List<float>();
            List<float> physics_cube_pos_z = new List<float>();
            List<float> physics_cube_rot_x = new List<float>();
            List<float> physics_cube_rot_y = new List<float>();
            List<float> physics_cube_rot_z = new List<float>();

            List<Vector3> simulatedPositions = new List<Vector3>();
            List<Quaternion> simulatedRotations = new List<Quaternion>();
            // Wait for the animation to complete



            // Compute values after anim loop is done, computing the overall distance travelled 
            physics_cube_pos_x.Sum();
            physics_cube_pos_y.Sum();
            physics_cube_pos_z.Sum();
            //compute the difference between the trajectory of the virtual cube and the physics cube

            Debug.Log("Animation Done");
            main_anim.StopPlayback();
            string trialType = DetermineTrialType(i);
            string fileName = $"Trial_{i}_{trialType}";
            dataclass.SaveToCSV(fileName);
            dataclass.ClearAllLists(); // Clear the data for the next trial

            // Optionally, add a short delay between trials
            yield return new WaitForSeconds(1.0f);
            Debug.Log("Trial " + i + " done");
        }

        // Record data into a file (not essential)
        SaveDataFile();

        yield return null;

    }
        private void CollectDataForCube(int cubeIndex)
    {
        // Record position and rotation of the specified cube
        Vector3 cubePosition = cubeReseters[cubeIndex].transform.position;
        Quaternion cubeRotation = cubeReseters[cubeIndex].transform.rotation;

        dataclass.cubePos.Add(cubePosition);
        // Add rotation data too if needed
        /dataclass.cubeRot.Add(cubeRotation.eulerAngles); 
    }
    public float CalculateTotalError()
    {
        float totalPositionError = 0f;
        float totalRotationError = 0f;

        // Compare positions and rotations of cubes between simulation and preprocessed data
        for (int i = 0; i < dataclass.cubePos.Count; i++)
        {
            // Retrieve expected data from preprocessedData
            Vector3 expectedPosition = /* Get the expected position from preprocessed data */;
            Quaternion expectedRotation = /* Get the expected rotation from preprocessed data */;

            // Compare the current cube positions/rotations with the expected values
            totalPositionError += CalculateDistance(dataclass.cubePos[i], expectedPosition);
            totalRotationError += CalculateRotationDifference(dataclass.cubeRot[i], expectedRotation);
        }

        // Combine the errors (you can weigh them if needed)
        return totalPositionError + totalRotationError;
    }


    private string DetermineTrialType(int trialIndex)
    {
        if (trialIndex < 3)
            return "lift";
        else if (trialIndex < 6)
            return "push";
        else
            return "stack";
    }

    /*each animation is played once 
     * cubes rotation and position are sampled at regular intervals 
     * collected in a list called cube states which contains the cube's position, rotation, and the corresponding timestamp within the animation.
     * 
     */
    // This method will calculate the Euclidean distance between two Vector3 positions
    private float CalculateDistance(Vector3 pos1, Vector3 pos2)
    {
        return Vector3.Distance(pos1, pos2);
    }

    // This method will calculate the difference in rotation (in degrees) between two quaternions
    private float CalculateRotationDifference(Quaternion rot1, Quaternion rot2)
    {
        return Quaternion.Angle(rot1, rot2);
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


    public CubeState GetInitialCubeState(GameObject cube)
    {
        if (initialCubeStates.ContainsKey(cube))
        {
            return initialCubeStates[cube];
        }
        return default(CubeState);
    }
}