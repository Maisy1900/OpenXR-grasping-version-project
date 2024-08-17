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
    private GeneticAlgorithmScript _geneticAlgorithmManager;

    public ResetPosition[] cubeReseters;

    private Coroutine trial_sequencer;
    // Dictionary to store the preprocessed data
    private Dictionary<string, List<CubeState>> preprocessedData;
    public string[] csvPaths;
    // Dictionary to track first touch and store positions
    private Dictionary<GameObject, bool> cubeTouchedStatus;
    private Dictionary<GameObject, CubeState> initialCubeStates;

    private float bestFitness = float.MinValue;
    private float[] bestPhysicsParams;

    private bool isEvaluating = false; // Add this flag to manage concurrency
    // Function to record results (check if this is a bottleneck)  

    private GeneticAlgorithmScript _ga;
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
    #region data preprocessing 
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
    #endregion
    void Start()
    {
        Debug.Log("calling start");
        // Start hand calibration; the rest of the experiment will follow after calibration is complete
        StartCoroutine(SetupExperimentCoroutine());
    }
    #region start coroutine 
    private bool setupCompleted = false;
    // Main setup coroutine that runs the whole process in sequence
    private IEnumerator SetupExperimentCoroutine()
    {

        // Step 1: Run hand calibration and wait for it to complete
        Debug.Log("Starting hand calibration...");
        yield return StartCoroutine(HandCalibrationCoroutine());

        // Step 2: Set up the experiment after calibration
        Debug.Log("Setting up experiment...");
        yield return StartCoroutine(ExperimentSetupCoroutine());

        // Step 3: Initialize and start the genetic algorithm after experiment setup
        Debug.Log("Initializing genetic algorithm...");
        yield return StartCoroutine(InitializeGeneticAlgorithmCoroutine());
        setupCompleted = true;
        // Step 4: Start the genetic algorithm
        Debug.Log("Starting genetic algorithm..." + setupCompleted);
        _ga.Start();
        Debug.Log("Genetic algorithm started.");
    }

    // Coroutine for hand calibration
    private IEnumerator HandCalibrationCoroutine()
    {
        // Step 1: Play calibration animation
        main_anim.Play("Calibration hand", 0);

        // Step 2: Wait for the animation and calibration process
        yield return new WaitForSeconds(1.0f);  // Wait for calibration animation
        articulationDriver_real.MeasureInitialAngles();
        yield return new WaitForSeconds(1.0f);  // Wait for measurement to complete

        // Step 3: Stop the calibration animation
        main_anim.StopPlayback();
        Debug.Log("Hand calibration completed.");
    }

    // Coroutine to set up the experiment after calibration
    private IEnumerator ExperimentSetupCoroutine()
    {
        // Initialize data classes and cube state tracking
        cubeTouchedStatus = new Dictionary<GameObject, bool>();
        initialCubeStates = new Dictionary<GameObject, CubeState>();
        dataclassBase = new DataClass();
        dataclassMid = new DataClass();
        dataclassTop = new DataClass();

        // Read and preprocess all cube animation data files (.csv)
        preprocessedData = new Dictionary<string, List<CubeState>>();
        foreach (string csvPath in csvPaths)
        {
            PreprocessCSVFile(csvPath);
            yield return null;  // Allow one frame for each file processed to prevent blocking
        }
        Debug.Log("CSV preprocessing completed.");

        Debug.Log("Experiment setup completed.");
    }

    // Coroutine to initialize the genetic algorithm after the experiment setup
    private IEnumerator InitializeGeneticAlgorithmCoroutine()
    {
        // Step 1: Initialize the genetic algorithm
        _ga = new GeneticAlgorithmScript(this, populationSize: 20, numberOfGenerations: 50, crossoverProbability: 0.8f, mutationProbability: 0.05f);

        // Step 2: Calculate total number of simulations and shuffle animations
        int populationSize = _ga.PopulationSize;  // Access population size directly from the custom GA
        int numberOfGenerations = _ga.NumberOfGenerations;  // Access number of generations directly

        number_of_simulations = populationSize * numberOfGenerations;

        // Debug log to check population size, generations, and number of simulations
        Debug.Log($"Population Size: {populationSize}, Number of Generations: {numberOfGenerations}, Number of Simulations: {number_of_simulations}");

        shuffled_anim_indices = new int[number_of_simulations];
        int originalCount = animation_names.Length;

        // Create a list to hold the expanded array of animations
        List<int> expandedAnimations = new List<int>();

        // Duplicate the original animations (excluding the first one) to fill the required number of trials
        for (int i = 0; i < number_of_simulations; i++)
        {
            expandedAnimations.Add((i % originalCount));  // Fill expanded array
        }

        // Convert the expanded list to an array and shuffle it
        shuffled_anim_indices = expandedAnimations.ToArray();
        Shuffle(shuffled_anim_indices);
        Debug.Log("Genetic algorithm initialization and shuffle completed.");
        yield return null;
    }
    #endregion

    //conduct trials!!
    // public void StartTrials(float[] physicsParams, Action<float> onTrialComplete)
    // {
    //     trialCompleted = false;
    //     StartCoroutine(ConductTrials(physicsParams, onTrialComplete));
    // }


    // public IEnumerator EvaluateCoroutine(float[] physicsParams, Action<float> onTrialComplete)
    // {
    //     float totalError = 0f;
    //     bool fitnessReady = false;

    //     for (int trialIndex = 0; trialIndex < number_of_simulations; trialIndex++)
    //     {
    //         Debug.Log("Number of simulations to run: " + number_of_simulations);
    //         float trialError = 0f;
    //         fitnessReady = false;
    //         isEvaluating = true; // Start evaluating for this trial

    //         Debug.Log($"Starting trial {trialIndex} with physicsParams: {string.Join(", ", physicsParams)}");

    //         // Start the trial and set a callback to capture the error when the trial is done
    //         StartTrials(physicsParams, (error) =>
    //         {
    //             trialError = error;
    //             fitnessReady = true; // Mark the trial as complete
    //             Debug.Log($"Trial {trialIndex} complete with trialError: {trialError}");
    //         });

    //         // Wait until the trial completes
    //         while (!fitnessReady)
    //         {
    //             yield return null; // Wait for the next frame
    //         }

    //         // Accumulate the total error from all trials
    //         totalError += trialError;

    //         Debug.Log($"Total error after trial {trialIndex}: {totalError}");

    //         // Reset isEvaluating for the next trial
    //         isEvaluating = false;
    //     }

    //     // Calculate the average error across all trials
    //     float averageError = totalError / number_of_simulations;

    //     // Calculate the fitness based on the average error
    //     float fitness = 1 / (1 + averageError);

    //     // Log and return the final fitness
    //     Debug.Log($"Final fitness calculated from average error: {fitness}");
    //     onTrialComplete(averageError); // Invoke the callback with the average error

    //     // This marks the end of all evaluations
    //     isEvaluating = false;
    // }
    //#region conductTrials
    // // Function to conduct trials
    // public IEnumerator ConductTrials(float[] physicsParams, Action<float> onTrialComplete)
    // {
    //     // Reset cube objects 
    //     ResetCubes();
    //     //Running the hand calibration
    //     main_anim.Play("Calibration hand", 0);
    //     yield return new WaitForSeconds(1.0f);
    //     articulationDriver_real.MeasureInitialAngles();
    //     yield return new WaitForSeconds(1.0f);
    //     main_anim.StopPlayback();
    //     Debug.Log("Calibration is over");
    //     float totalError = 0f;

    //     //running the simulation 
    //     for (int i = 0; i < number_of_simulations; i++)
    //     {
    //         #region resetting values and setting values for the trial
    //         currentTrialNumber = i;
    //         // Reset cube objects for each trial 
    //         ResetCubes();
    //         // Reset touch tracking for new trial

    //         baseCubeFirstTouched = false;
    //         middleCubeFirstTouched = false;
    //         topCubeFirstTouched = false;
    //         float trialError = 0f;
    //         int animIndex = shuffled_anim_indices[i];

    //         Debug.Log("Trial: " + i.ToString() + " out of: " + number_of_simulations.ToString());
    //         //Debug.Log("Anim index: " + shuffled_anim_indices[i].ToString());
    //         //Debug.Log("Anim: " + animation_names[shuffled_anim_indices[i]]);

    //         //Apply dynamic physics parameters
    //         //Debug.Log($"Applying physics parameters: {string.Join(", ", physicsParams)}");
    //         ApplyPhysicsParameters(physicsParams);
    //         // Debug.Log("Starting trials with physics params: " + string.Join(",", physicsParams));
    //         Debug.Log("Physics parameters adjusted!");


    //         // Play the corresponding animation
    //         main_anim.Play(animation_names[shuffled_anim_indices[i]], 0);
    //         // bool animstate = main_anim.GetCurrentAnimatorStateInfo(0).IsName(animation_names[shuffled_anim_indices[i]]);
    //         Debug.Log("Animation playing!");
    //         float normedTime = 0f;
    //         #endregion
    //         // Wait for the animation to complete
    //         while (normedTime < 1)
    //         {
    //             normedTime = main_anim.GetCurrentAnimatorStateInfo(0).normalizedTime;

    //             RecordTrialResults(animIndex, normedTime, i);
    //             yield return null;
    //         }
    //         // Once animation has finished, calculate total error (fitness) for this trial
    //         //match the current animation with the csv holding the preprocessed data 
    //         trialError = CalculateTrialErrorForAnimation(animIndex);

    //         // Calculate error between simulation and preprocessed data
    //         List<CubeState> preprocessedCubeStates = preprocessedData[csvPaths[i]];

    //         // Callback with the trial error
    //         Debug.Log("Animation Done");
    //         main_anim.StopPlayback();
    //         string trialType = DetermineTrialType(i);

    //         SaveDataFile(i);  // This will save the data from base, middle, and top cubes to separate CSV files

    //         // Optionally, add a short delay between trials
    //         yield return new WaitForSeconds(1.0f);
    //         Debug.Log("Trial " + i + " done");
    //         Debug.Log($"Trial {i} complete with error {trialError}.");

    //         onTrialComplete(trialError);
    //         //wait for the physics parameters to update 
    //         trialCompleted = true;
    //         yield return new WaitForSeconds(3.0f);
    //     }

    //     // Record data into a file (not essential)


    //     Debug.Log("All trials complete");
    //     yield return null;

    // }
    //#endregion
    #region trial logic 
    private void ApplyPhysicsParameters(float[] physicsParams)
    {
        Physics.defaultSolverIterations = (int)physicsParams[0];
        Physics.defaultSolverVelocityIterations = (int)physicsParams[1];
        Physics.defaultContactOffset = physicsParams[2];
        Physics.defaultMaxDepenetrationVelocity = physicsParams[3];
        Physics.bounceThreshold = physicsParams[4];

        Debug.Log("Physics parameters adjusted!");
    }

    private void ResetCubes()
    {
        cubeReseters[0].ResetCubes();
        cubeReseters[1].ResetCubes();
        cubeReseters[2].ResetCubes();
    }
    private void RecordTrialResults(int animIndex, float normedTime, int trialIndex)
    {
        Vector3 wristPos = GetWristPosition();  // Get wrist position from articulationObject

        if (animIndex < 3 && baseCubeFirstTouched) // Lifting trials
        {
            //Debug.Log($"Recording base cube results for trial {trialIndex} (Lifting)");
            RecordResults(dataclassBase, cubeReseters[0].transform.position, cubeReseters[0].transform.rotation, wristPos, normedTime, trialIndex, "Lifting");
        }
        else if (animIndex >= 3 && animIndex < 6 && baseCubeFirstTouched) // Pushing trials
        {
            //Debug.Log($"Recording base cube results for trial {trialIndex} (Pushing)");
            RecordResults(dataclassBase, cubeReseters[0].transform.position, cubeReseters[0].transform.rotation, wristPos, normedTime, trialIndex, "Pushing");
        }
        else if (animIndex >= 6) // Stacking trials
        {
            if (baseCubeFirstTouched)
            {
                //Debug.Log($"Recording base cube results for trial {trialIndex} (Stacking)");
                RecordResults(dataclassBase, cubeReseters[0].transform.position, cubeReseters[0].transform.rotation, wristPos, normedTime, trialIndex, "Stacking_Base");
            }
            if (middleCubeFirstTouched)
            {
                //Debug.Log($"Recording middle cube results for trial {trialIndex} (Stacking)");
                RecordResults(dataclassMid, cubeReseters[1].transform.position, cubeReseters[1].transform.rotation, wristPos, normedTime, trialIndex, "Stacking_Middle");
            }
            if (topCubeFirstTouched)
            {
                //Debug.Log($"Recording top cube results for trial {trialIndex} (Stacking)");
                RecordResults(dataclassTop, cubeReseters[2].transform.position, cubeReseters[2].transform.rotation, wristPos, normedTime, trialIndex, "Stacking_Top");
            }
        }
    }

    private float GetScalingFactorForTrial(int animIndex)
    {
        if (animIndex < 3) // Lifting
            return 0.8f;
        else if (animIndex < 6) // Pushing
            return 1.0f;
        else // Stacking
            return 1.2f;
    }


    private float CalculateTrialErrorForAnimation(int animIndex)
    {
        float trialError = 0f;
        float scalingFactor = GetScalingFactorForTrial(animIndex); // Get the scaling factor based on trial type

        if (animIndex >= 0 && animIndex < 6) // Lifting and Pushing animations
        {
            string baseCsvKey = csvPaths[animIndex];
            List<CubeState> baseCubeStates = preprocessedData[baseCsvKey];
            trialError = CalculateTrialError(dataclassBase, baseCubeStates, null, null, null, null, scalingFactor) ; // Pass scaling factor
        }
        else if (animIndex >= 6) // Stacking animations
        {
            string baseCsvKey = csvPaths[6 + (animIndex - 6) * 3];
            string middleCsvKey = csvPaths[7 + (animIndex - 6) * 3];
            string topCsvKey = csvPaths[8 + (animIndex - 6) * 3];

            List<CubeState> baseCubeStates = preprocessedData[baseCsvKey];
            List<CubeState> middleCubeStates = preprocessedData[middleCsvKey];
            List<CubeState> topCubeStates = preprocessedData[topCsvKey];

            trialError = CalculateTrialError(dataclassBase, baseCubeStates, dataclassMid, middleCubeStates, dataclassTop, topCubeStates, scalingFactor); // Pass scaling factor
        }

        return trialError;
    }

    private float CalculateTrialError(DataClass baseTrialData, List<CubeState> baseCubeStates,
                                      DataClass middleTrialData = null, List<CubeState> middleCubeStates = null,
                                      DataClass topTrialData = null, List<CubeState> topCubeStates = null,
                                      float scalingFactor = 1.0f) // Accept scaling factor as parameter
    {
        float totalPositionError = 0f;
        float totalRotationError = 0f;
        int numCubes = 0;  // Count how many cubes have valid data
        int count;

        // Base cube error calculation
        if (baseTrialData.cubePos.Count > 0 && baseCubeStates.Count > 0)
        {
            count = Mathf.Min(baseTrialData.cubePos.Count, baseCubeStates.Count);
            for (int i = 0; i < count; i++)
            {
                Vector3 simulatedPos = baseTrialData.cubePos[i];
                Vector3 preprocessedPos = baseCubeStates[i].Position;
                totalPositionError += Vector3.Distance(simulatedPos, preprocessedPos); // No scaling yet

                Quaternion simulatedRot = Quaternion.Euler(baseTrialData.cubeRot[i]);
                Quaternion preprocessedRot = baseCubeStates[i].Rotation;
                totalRotationError += Quaternion.Angle(simulatedRot, preprocessedRot); // No scaling yet
            }
            numCubes++;
        }

        // Middle cube error calculation (only for stacking trials)
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
            numCubes++;
        }

        // Top cube error calculation (only for stacking trials)
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
            numCubes++;
        }

        // If no cubes had valid data, return a large default error value to indicate a failure
        if (numCubes == 0)
        {
            return float.MaxValue;  // Return a high error to indicate failure due to no data
        }

        // Apply the scaling factor to the total errors
        totalPositionError *= scalingFactor;
        totalRotationError *= scalingFactor;

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
#endregion


    #region coroutines for animations


    public IEnumerator PlayAnimationCoroutine(int animIndex, int currentTrialNumber)
    {
        // Play the corresponding animation
        Debug.Log($"Playing animation: {animation_names[animIndex]}");
        main_anim.Play(animation_names[animIndex], 0);

        // Wait for the animation to finish
        float normedTime = 0f;
        while (normedTime < 1)
        {
            // Get the normalized time of the animation (0 to 1, where 1 means animation is finished)
            normedTime = main_anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
                    // Record the trial results at this frame (pass trialIndex to identify which trial it is)
            RecordTrialResults(animIndex, normedTime, currentTrialNumber);

            yield return null; // Wait for the next frame
        }

        Debug.Log($"Animation {animation_names[animIndex]} complete.");
    }

    public IEnumerator TrialCoroutine(float[] physicsParams, Action<float> onComplete)
    {
        ResetCubes();
        // Step 1: Apply the physics parameters for the trial
        ApplyPhysicsParameters(physicsParams);

        // Print the physics parameters
        Debug.Log($"Physics parameters: {string.Join(", ", physicsParams)}");

        Debug.Log("current trial number: " + currentTrialNumber);

        // Step 2: Select a randomized trial based on the shuffled array
        int trialIndex = shuffled_anim_indices[currentTrialNumber]; // Randomized trial selection
        Debug.Log("anim index: " + trialIndex);

        // Step 3: Play the animation for the selected trial
        yield return StartCoroutine(PlayAnimationCoroutine(trialIndex, currentTrialNumber));
        yield return new WaitForSeconds(1.0f);
        // Step 4: Calculate the error for the trial based on the current trial number
        float trialError = CalculateTrialErrorForAnimation(trialIndex);

        // Step 5: Calculate fitness based on trial error
        float fitness = 1 / (1 + trialError); // Simple fitness calculation based on error

        Debug.Log($"Trial {currentTrialNumber} complete with fitness: {fitness} and trial error: {trialError}");

        // Step 6: Return the fitness via the callback
        onComplete(fitness);
        // Step 1: Save the data for the current trial (which already clears the lists)

        yield return StartCoroutine(Save(currentTrialNumber));
        yield return StartCoroutine(SetupNext());
        // Increment the current trial number for the next iteration
        currentTrialNumber++;
    }
    public IEnumerator SetupNext()
    {
        yield return null;
        // Step 3: Reset cubes to their original positions
        ResetCubes();

        // Step 4: Clear any touch tracking or trial-specific flags to ensure a clean state
        baseCubeFirstTouched = false;
        middleCubeFirstTouched = false;
        topCubeFirstTouched = false;
    }
    public IEnumerator Save(int currentTrialNumber)
    {
        SaveDataFile(currentTrialNumber);
        yield return null;
    }
        #endregion


    }