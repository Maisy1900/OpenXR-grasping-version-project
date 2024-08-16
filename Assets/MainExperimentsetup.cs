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
    public List<Vector3> cubeRot; // Track cube rotations as Euler angles
    public List<Vector3> wristPos;
    public List<float> time = new List<float>();
    public List<string> trial_condition = new List<string>();
    public List<int> trial_number = new List<int>();
    public List<float> fps = new List<float>();
    public List<DateTime> timestamps = new List<DateTime>(); // Track DateTime for each data point

    public DataClass()
    {
        cubePos = new List<Vector3>();
        cubeRot = new List<Vector3>(); // Initialize rotation list
        wristPos = new List<Vector3>();
        time = new List<float>();
        trial_condition = new List<string>();
        fps = new List<float>();
        timestamps = new List<DateTime>(); // Initialize timestamp list
    }

    public void ClearAllLists()
    {
        cubePos.Clear();
        cubeRot.Clear(); // Clear rotation list
        wristPos.Clear();
        time.Clear();
        trial_condition.Clear();
        trial_number.Clear();
        fps.Clear();
        timestamps.Clear(); // Clear timestamp list
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
            writer.WriteLine("CubePosX,CubePosY,CubePosZ,CubeRotX,CubeRotY,CubeRotZ,HandPosX,HandPosY,HandPosZ,Time,TrialCondition,TrialNumber,FPS,Timestamp");

            for (int i = 0; i < cubePos.Count; i++)
            {
                writer.WriteLine($"{cubePos[i].x},{cubePos[i].y},{cubePos[i].z}," +
                                 $"{cubeRot[i].x},{cubeRot[i].y},{cubeRot[i].z}," +
                                 $"{wristPos[i].x},{wristPos[i].y},{wristPos[i].z}," +
                                 $"{time[i]},{trial_condition[i]},{trial_number[i]},{fps[i]},{timestamps[i]}");
            }
        }

        Debug.Log("Data saved to " + path);
    }
}

public class MainExperimentsetup : MonoBehaviour
{
    // Variables to store experiment data
    #region variables
    DataClass dataclassBase;
    DataClass dataclassMid;
    DataClass dataclassTop;
    public int number_of_simulation_blocks = 5; // Number of full sets of 9 trials
    private int number_of_simulations;
    public GameObject articulationObject;
    public ArticulationDriver_Real articulationDriver_real;
    public Transform cubeObj;
    public StackingTrial stack;
    public liftingTrial lift;
    public PushingTrial pushing;
    public bool trialCompleted { get; private set; } // Flag to signal trial completion


    public Animator main_anim;
    public string[] animation_names = new string[] { "lift_1", "lift_2", "lift_3", "push_1", "push_2", "push_3", "stack_1", "stack_2", "stack_3" };
    private int[] shuffled_anim_indices;
    public int currentTrialNumber = 0; // Store the current trial number
    private GeneticAlgorithmManager _geneticAlgorithmManager;

    public ResetPosition[] cubeReseters;

    private Coroutine trial_sequencer;
    // Dictionary to store the preprocessed data
    private Dictionary<string, List<CubeState>> preprocessedData;
    public string[] csvPaths;
    // Dictionary to track first touch and store positions
    private Dictionary<GameObject, bool> cubeTouchedStatus;
    private Dictionary<GameObject, CubeState> initialCubeStates;
    // Function to record results (check if this is a bottleneck)  
    void RecordResults(DataClass dataclass, Vector3 cubePosition, Quaternion cubeRotation, Vector3 handPose, float timing, int trialNumber, string trialCondition)
    {
        // Record cube position, rotation, hand position, timing, trial condition, and fps
        dataclass.trial_condition.Add(trialCondition);
        dataclass.trial_number.Add(trialNumber);

        // Add cube position
        dataclass.cubePos.Add(cubePosition);

        // Add cube rotation (we will store it as Euler angles for ease of saving)
        Vector3 cubeRotationEuler = cubeRotation.eulerAngles;
        dataclass.cubeRot.Add(cubeRotationEuler);

        // Add wrist/hand position
        dataclass.wristPos.Add(handPose);

        // Add timing (seconds since the trial started)
        dataclass.time.Add(timing);

        // Add current FPS
        dataclass.fps.Add(Time.captureFramerate); //max 90 

        // Add current DateTime as a timestamp (this is useful for more detailed tracking)
        dataclass.timestamps.Add(DateTime.Now);
    }
    private Vector3 GetWristPosition()
    {
        // Access the Transform component's position from the articulationObject
        return articulationObject.transform.position;
    }


    private bool baseCubeFirstTouched = false;
    private bool middleCubeFirstTouched = false;
    private bool topCubeFirstTouched = false;
    #endregion
    // This method returns a boolean indicating if the cube was touched for the first time
    public bool CubeTouched(GameObject touchedCube, int cubeIndex, int trialNumber)
    {
        if (cubeIndex == 0 && !baseCubeFirstTouched)
        {
            baseCubeFirstTouched = true;
            Debug.Log($"[{Time.time}] Base Cube was touched for the first time in trial {trialNumber}.");
            return true;
        }
        else if (cubeIndex == 1 && !middleCubeFirstTouched)
        {
            middleCubeFirstTouched = true;
            Debug.Log($"[{Time.time}] Middle Cube was touched for the first time in trial {trialNumber}.");
            return true;
        }
        else if (cubeIndex == 2 && !topCubeFirstTouched)
        {
            topCubeFirstTouched = true;
            Debug.Log($"[{Time.time}] Top Cube was touched for the first time in trial {trialNumber}.");
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
    void SaveDataFile(int trialNumber)
    {
        Debug.Log($"Saving data for trial {trialNumber}");

        if (dataclassBase.cubePos.Count > 0)
        {
            dataclassBase.SaveToCSV($"BaseCube_Trial_{trialNumber}");
            Debug.Log($"BaseCube data saved for trial {trialNumber}");
            dataclassBase.ClearAllLists();
        }

        if (dataclassMid.cubePos.Count > 0)
        {
            dataclassMid.SaveToCSV($"MiddleCube_Trial_{trialNumber}");
            Debug.Log($"MiddleCube data saved for trial {trialNumber}");
            dataclassMid.ClearAllLists();
        }

        if (dataclassTop.cubePos.Count > 0)
        {
            dataclassTop.SaveToCSV($"TopCube_Trial_{trialNumber}");
            Debug.Log($"TopCube data saved for trial {trialNumber}");
            dataclassTop.ClearAllLists();
        }
        else
        {
            Debug.Log($"Error saving data for trial {trialNumber}");
        }
    }

    void Start()
    {
        cubeTouchedStatus = new Dictionary<GameObject, bool>();
        initialCubeStates = new Dictionary<GameObject, CubeState>();
        dataclassBase = new DataClass();
        dataclassMid = new DataClass();
        dataclassTop = new DataClass();
        // Read all cube animation data files (.csv)
        // And put these into float arrays e.g. cube_pos_x[0][0], or cube_rot_y
        // obj  [task_index] [animation_index]
        // Initialize the dictionary
        preprocessedData = new Dictionary<string, List<CubeState>>();
        _geneticAlgorithmManager = new GeneticAlgorithmManager(this);

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
        // Create the GeneticAlgorithmManager and start the genetic algorithm
        _geneticAlgorithmManager = new GeneticAlgorithmManager(this);

        // Start the genetic algorithm
        _geneticAlgorithmManager.Start();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {

            // Start the trials
            if (trial_sequencer != null)
                StopCoroutine(trial_sequencer);
            //  trial_sequencer = StartCoroutine(ConductTrials());
        }

    }
    public void StartTrials(float[] physicsParams, Action<float> onTrialComplete)
    {
        trialCompleted = false;
        StartCoroutine(ConductTrials(physicsParams, onTrialComplete));
    }

    #region conductTrials
    // Function to conduct trials
    public IEnumerator ConductTrials(float[] physicsParams, Action<float> onTrialComplete)
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
        float totalError = 0f;

        //running the simulation 
        for (int i = 0; i < number_of_simulations; i++)
        {
            #region resetting values and setting values for the trial
            currentTrialNumber = i;
            // Reset cube objects for each trial 
            cubeReseters[0].ResetCubes();
            cubeReseters[1].ResetCubes();
            cubeReseters[2].ResetCubes();

            // Reset touch tracking for new trial
            baseCubeFirstTouched = false;
            middleCubeFirstTouched = false;
            topCubeFirstTouched = false;
            float normedTime = 0f;
            float trialError = 0f;
            Debug.Log("Trial: " + i.ToString() + " out of: " + number_of_simulations.ToString());
            Debug.Log("Anim index: " + shuffled_anim_indices[i].ToString());
            Debug.Log("Anim: " + animation_names[shuffled_anim_indices[i]]);
            int animIndex = shuffled_anim_indices[i];
            //Apply dynamic physics parameters
            Physics.defaultSolverIterations = (int)physicsParams[0];
            Physics.defaultSolverVelocityIterations = (int)physicsParams[1];
            Physics.defaultContactOffset = physicsParams[2];
            Physics.defaultMaxDepenetrationVelocity = physicsParams[3];
            Physics.bounceThreshold = physicsParams[4];
            Debug.Log("Starting trials with physics params: " + string.Join(",", physicsParams));

            Debug.Log("Physics parameters adjusted!");

            // Play the corresponding animation
            main_anim.Play(animation_names[shuffled_anim_indices[i]], 0);
            bool animstate = main_anim.GetCurrentAnimatorStateInfo(0).IsName(animation_names[shuffled_anim_indices[i]]);
            Debug.Log("Animation playing!");
            #endregion
            // Wait for the animation to complete
            while (normedTime < 1)
            {
                normedTime = main_anim.GetCurrentAnimatorStateInfo(0).normalizedTime;

                Vector3 wristPos = GetWristPosition();  // Get wrist position from articulationObject

                if (animIndex < 3 && baseCubeFirstTouched) // Lifting and Pushing trials
                {
                    Debug.Log($"Recording base cube results for trial {i} (Lifting)");
                    RecordResults(dataclassBase, cubeReseters[0].transform.position, cubeReseters[0].transform.rotation, wristPos, normedTime, i, "Lifting");
                }

                if (animIndex >= 3 && animIndex < 6 && baseCubeFirstTouched) // Lifting and Pushing trials
                {
                    Debug.Log($"Recording base cube results for trial {i} (Pushing)");
                    RecordResults(dataclassBase, cubeReseters[0].transform.position, cubeReseters[0].transform.rotation, wristPos, normedTime, i, "Pushing");
                }
                else // Stacking trials
                {
                    if (animIndex >= 6 && baseCubeFirstTouched)
                    {
                        Debug.Log($"Recording base cube results for trial {i} (Stacking)");
                        RecordResults(dataclassBase, cubeReseters[0].transform.position, cubeReseters[0].transform.rotation, wristPos, normedTime, i, "Stacking_Base");
                    }

                    if (animIndex >= 6 && middleCubeFirstTouched)
                    {
                        Debug.Log($"Recording middle cube results for trial {i} (Stacking)");
                        RecordResults(dataclassMid, cubeReseters[1].transform.position, cubeReseters[1].transform.rotation, wristPos, normedTime, i, "Stacking_Middle");
                    }

                    if (animIndex >= 6 && topCubeFirstTouched)
                    {
                        Debug.Log($"Recording top cube results for trial {i} (Stacking)");
                        RecordResults(dataclassTop, cubeReseters[2].transform.position, cubeReseters[2].transform.rotation, wristPos, normedTime, i, "Stacking_Top");
                    }
                }

                yield return null;
            }

            // Once animation has finished, calculate total error (fitness) for this trial
            //match the current animation with the csv holding the preprocessed data 
            if (animIndex >= 0 && animIndex < 6)
            {
                // For lifting and pushing animations (index 0-5)
                string baseCsvKey = csvPaths[animIndex];
                List<CubeState> baseCubeStates = preprocessedData[baseCsvKey];
                trialError = CalculateTrialError(dataclassBase, baseCubeStates);
                // Use cubeStates in your simulation or trial logic
            }
            else if (animIndex >= 6 && animIndex < 9)
            {
                // For stacking animations (index 6-8)

                // First cube (base) bases can either be 6 when i=6, 9 when i=7, or 12 when i=8
                string baseCsvKey = csvPaths[6 + (animIndex - 6) * 3];  // Correctly indexing into the base CSV
                List<CubeState> baseCubeStates = preprocessedData[baseCsvKey];

                // Second cube (middle) can either be 7 when i=6, 10 when i=7, or 13 when i=8
                string middleCsvKey = csvPaths[7 + (animIndex - 6) * 3];  // Correctly indexing into the middle CSV
                List<CubeState> middleCubeStates = preprocessedData[middleCsvKey];

                // Third cube (top) can either be 8 when i=6, 11 when i=7, or 14 when i=8
                string topCsvKey = csvPaths[8 + (animIndex - 6) * 3];  // Correctly indexing into the top CSV
                List<CubeState> topCubeStates = preprocessedData[topCsvKey];
                trialError = CalculateTrialError(dataclassBase, baseCubeStates, dataclassMid, middleCubeStates, dataclassTop, topCubeStates);


            }


            // Wait for the animation to complete
            //compute the difference between the trajectory of the virtual cube and the physics cube
            // Once animation is finished, calculate total error (fitness) for this trial
            // Collect simulated data

            // Calculate error between simulation and preprocessed data
            List<CubeState> preprocessedCubeStates = preprocessedData[csvPaths[i]];



            // Callback with the trial error
            Debug.Log("Animation Done");
            main_anim.StopPlayback();
            string trialType = DetermineTrialType(i);

            SaveDataFile(i);  // This will save the data from base, middle, and top cubes to separate CSV files

            // Optionally, add a short delay between trials
            yield return new WaitForSeconds(1.0f);
            Debug.Log("Trial " + i + " done");
            onTrialComplete(trialError);
            //wait for the physics parameters to update 
            yield return new WaitForSeconds(3.0f);
        }

        // Record data into a file (not essential)


        Debug.Log("All trials complete");
        yield return null;

    }
    #endregion 
    private float CalculateTrialError(DataClass baseTrialData, List<CubeState> baseCubeStates,
                                      DataClass middleTrialData = null, List<CubeState> middleCubeStates = null,
                                      DataClass topTrialData = null, List<CubeState> topCubeStates = null)
    {
        float totalPositionError = 0f;
        float totalRotationError = 0f;
        int numCubes = 0;  // Count how many cubes have valid data
        int count;

        // Check if base cube data exists and is valid
        if (baseTrialData.cubePos.Count > 0 && baseCubeStates.Count > 0)
        {
            count = Mathf.Min(baseTrialData.cubePos.Count, baseCubeStates.Count);
            for (int i = 0; i < count; i++)
            {
                Vector3 simulatedPos = baseTrialData.cubePos[i];
                Vector3 preprocessedPos = baseCubeStates[i].Position;
                totalPositionError += Vector3.Distance(simulatedPos, preprocessedPos);

                Quaternion simulatedRot = Quaternion.Euler(baseTrialData.cubeRot[i]);
                Quaternion preprocessedRot = baseCubeStates[i].Rotation;
                totalRotationError += Quaternion.Angle(simulatedRot, preprocessedRot);
            }
            numCubes++;  // Base cube data exists
        }

        // Check if middle cube data exists and is valid (only for stacking trials)
        if (middleCubeStates != null && middleTrialData != null && middleTrialData.cubePos.Count > 0 && middleCubeStates.Count > 0)
        {
            count = Mathf.Min(middleTrialData.cubePos.Count, middleCubeStates.Count);
            for (int i = 0; i < count; i++)
            {
                Vector3 simulatedPos = middleTrialData.cubePos[i];
                Vector3 preprocessedPos = middleCubeStates[i].Position;
                totalPositionError += Vector3.Distance(simulatedPos, preprocessedPos);

                Quaternion simulatedRot = Quaternion.Euler(middleTrialData.cubeRot[i]);
                Quaternion preprocessedRot = middleCubeStates[i].Rotation;
                totalRotationError += Quaternion.Angle(simulatedRot, preprocessedRot);
            }
            numCubes++;  // Middle cube data exists
        }

        // Check if top cube data exists and is valid (only for stacking trials)
        if (topCubeStates != null && topTrialData != null && topTrialData.cubePos.Count > 0 && topCubeStates.Count > 0)
        {
            count = Mathf.Min(topTrialData.cubePos.Count, topCubeStates.Count);
            for (int i = 0; i < count; i++)
            {
                Vector3 simulatedPos = topTrialData.cubePos[i];
                Vector3 preprocessedPos = topCubeStates[i].Position;
                totalPositionError += Vector3.Distance(simulatedPos, preprocessedPos);

                Quaternion simulatedRot = Quaternion.Euler(topTrialData.cubeRot[i]);
                Quaternion preprocessedRot = topCubeStates[i].Rotation;
                totalRotationError += Quaternion.Angle(simulatedRot, preprocessedRot);
            }
            numCubes++;  // Top cube data exists
        }

        // If no cubes had valid data, return a large default error value to indicate a failure
        if (numCubes == 0)
        {
            return float.MaxValue;  // Return a high error to indicate failure due to no data
        }

        // Calculate average error per cube to normalize the fitness
        float averagePositionError = totalPositionError / numCubes;
        float averageRotationError = totalRotationError / numCubes;

        float totalError = averagePositionError + averageRotationError;

        return totalError; // Return normalized error
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
}