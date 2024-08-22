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

    public void SaveToCSV(string fileName, int physicsTrialNumber, int gaTrialNumber, int generationNumber)
    {
        // Construct the directory path for the current physics trial, GA trial, and generation
        string directoryPath = Path.Combine(Application.dataPath, "SimulationResults", $"Trial{gaTrialNumber}", $"Generation{generationNumber}");

        // Check if the directory exists, if not, create it
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Construct the complete path for the file
        string path = Path.Combine(directoryPath, fileName + ".csv");

        using (StreamWriter writer = new StreamWriter(path))
        {
            // Write the header
            writer.WriteLine("CubePosX,CubePosY,CubePosZ,CubeRotX,CubeRotY,CubeRotZ,HandPosX,HandPosY,HandPosZ,Time,TrialCondition,TrialNumber,FPS,Timestamp");

            // Write the data for each recorded point
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
    public GameObject articulationPrefab; 
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
    public int currentTrialNumber = 0; // temp trial number
    private GeneticAlgorithmScript _geneticAlgorithmManager;

    private int _currentGATrialNumber = 0;  // Start from trial 0 update 

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
    void RecordResults(DataClass dataclass, Vector3 cubePosition, Quaternion cubeRotation, Vector3 handPose, float timing, int trialNumber, string trialCondition, float fps)
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
        dataclass.fps.Add(fps);

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

    public void SaveDataFile(int physicsTrialNumber, int gaTrialNumber, int generationNumber)
    {
        Debug.Log($"Saving data for physics trial {physicsTrialNumber}, GA trial {gaTrialNumber}, generation {generationNumber}");

        if (dataclassBase.cubePos.Count > 0)
        {
            dataclassBase.SaveToCSV($"BaseCube_PhysicsTrial_{physicsTrialNumber}_GATrial_{gaTrialNumber}_Generation_{generationNumber}", physicsTrialNumber, gaTrialNumber, generationNumber);
            Debug.Log($"BaseCube data saved for physics trial {physicsTrialNumber}, GA trial {gaTrialNumber}, generation {generationNumber}");
            dataclassBase.ClearAllLists();
        }

        if (dataclassMid.cubePos.Count > 0)
        {
            dataclassMid.SaveToCSV($"MiddleCube_PhysicsTrial_{physicsTrialNumber}_GATrial_{gaTrialNumber}_Generation_{generationNumber}", physicsTrialNumber, gaTrialNumber, generationNumber);
            Debug.Log($"MiddleCube data saved for physics trial {physicsTrialNumber}, GA trial {gaTrialNumber}, generation {generationNumber}");
            dataclassMid.ClearAllLists();
        }

        if (dataclassTop.cubePos.Count > 0)
        {
            dataclassTop.SaveToCSV($"TopCube_PhysicsTrial_{physicsTrialNumber}_GATrial_{gaTrialNumber}_Generation_{generationNumber}", physicsTrialNumber, gaTrialNumber, generationNumber);
            Debug.Log($"TopCube data saved for physics trial {physicsTrialNumber}, GA trial {gaTrialNumber}, generation {generationNumber}");
            dataclassTop.ClearAllLists();
        }
    }




    #endregion
    void Start()
    {
        if (articulationPrefab == null)
        {
        Debug.LogError("Articulation Prefab is still null at Start.");
        }
        else
        {
        Debug.Log("Articulation Prefab is correctly assigned at Start.");
        }
        Debug.Log("calling start");
        // Start hand calibration; the rest of the experiment will follow after calibration is complete
       StartCoroutine(SetupExperimentCoroutine());
    }
    void ResetHand()
    {
        if (articulationObject != null)
            Destroy(articulationObject);

        articulationObject = Instantiate(articulationPrefab);

        // Find the ArticulationDriver_Real component from the newly instantiated object
        articulationDriver_real = articulationObject.GetComponent<ArticulationDriver_Real>();

        if (articulationDriver_real == null)
        {
            Debug.LogError("ArticulationDriver_Real component not found!");
        }
        StartCoroutine(DelayedSetup());
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
        
        // Step 3: Run trials and calculate scaling factors after experiment setup
        /* Here I ran the code for the scaling factors 
        Debug.Log("Running trials and computing scaling factors...");
         yield return StartCoroutine(PerformTrialsAndComputeScalingFactors());
        */
       
        // Step 3: Initialize and start the genetic algorithm after experiment setup
        Debug.Log("Initializing genetic algorithm...");
        yield return StartCoroutine(InitializeGeneticAlgorithmCoroutine());
        setupCompleted = true;
        yield return StartCoroutine(_ga.RunMultipleTrials(_ga.NumTrials)); // Run trials from MainExperimentsetup
        Debug.Log("Genetic algorithm completed.");
        
    }
  
    // Coroutine for hand calibration
    private IEnumerator HandCalibrationCoroutine()
    {
        ResetHand();
        yield return null;
        //Play calibration animation
        main_anim.Play("Calibration hand", 0);

        //Wait for the animation and calibration process
        yield return new WaitForSeconds(1.0f);  
        articulationDriver_real.MeasureInitialAngles();
        yield return new WaitForSeconds(1.0f);  

        // Stop the calibration animation
        main_anim.StopPlayback();
        Debug.Log("Hand calibration completed.");

        main_anim.Play("after calib", 0);
        yield return new WaitForSeconds(1.0f);

        yield return StartCoroutine(ResetPosition());
    }
    private IEnumerator ResetPosition()
    {
        main_anim.Play("Animation Clip_R_Wrist_001", 0);
        yield return new WaitForSeconds(1.0f);  // Wait 
        //Stop the animation
        main_anim.StopPlayback();
    }
    IEnumerator DelayedSetup()
    {
        yield return null;  
                            
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
        _ga = new GeneticAlgorithmScript(this, populationSize: 20, numberOfGenerations: 30, crossoverProbability: 0.7f, mutationProbability: 0.05f);
        //_ga = new GeneticAlgorithmScript(this, populationSize: 10, numberOfGenerations: 50, crossoverProbability: 0.8f, mutationProbability: 0.05f);

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
    private void RecordTrialResults(int animIndex, float normedTime, int trialIndex, float fps)
    {
        Vector3 wristPos = GetWristPosition();  // Get wrist position from articulationObject

        if (animIndex < 3 && baseCubeFirstTouched) // Lifting trials
        {
            //Debug.Log($"Recording base cube results for trial {trialIndex} (Lifting)");
            RecordResults(dataclassBase, cubeReseters[0].transform.position, cubeReseters[0].transform.rotation, wristPos, normedTime, trialIndex, "Lifting", fps);
        }
        else if (animIndex >= 3 && animIndex < 6 && baseCubeFirstTouched) // Pushing trials
        {
            //Debug.Log($"Recording base cube results for trial {trialIndex} (Pushing)");
            RecordResults(dataclassBase, cubeReseters[0].transform.position, cubeReseters[0].transform.rotation, wristPos, normedTime, trialIndex, "Pushing", fps);
        }
        else if (animIndex >= 6) // Stacking trials
        {
            if (baseCubeFirstTouched)
            {
                //Debug.Log($"Recording base cube results for trial {trialIndex} (Stacking)");
                RecordResults(dataclassBase, cubeReseters[0].transform.position, cubeReseters[0].transform.rotation, wristPos, normedTime, trialIndex, "Stacking_Base", fps);
            }
            if (middleCubeFirstTouched)
            {
                //Debug.Log($"Recording middle cube results for trial {trialIndex} (Stacking)");
                RecordResults(dataclassMid, cubeReseters[1].transform.position, cubeReseters[1].transform.rotation, wristPos, normedTime, trialIndex, "Stacking_Middle", fps);
            }
            if (topCubeFirstTouched)
            {
                //Debug.Log($"Recording top cube results for trial {trialIndex} (Stacking)");
                RecordResults(dataclassTop, cubeReseters[2].transform.position, cubeReseters[2].transform.rotation, wristPos, normedTime, trialIndex, "Stacking_Top", fps);
            }
        }
    }

    private float GetScalingFactorForTrial(int animIndex)
    {
        switch (animIndex)
        {
            case 0: // lift_1
                return 1.996766f;
            case 1: // lift_2
                return 4.611262f;
            case 2: // lift_3
                return 1f;
            case 3: // push_1
                return 26.47587f;
            case 4: // push_2
                return 43.51072f;
            case 5: // push_3
                return 84.12691f;
            case 6: // stack_1
                return 23.00733f;
            case 7: // stack_2
                return 39.56506f;
            case 8: // stack_3
                return 21.51627f;
            default:
                return 1f; // Default scaling factor if animIndex is out of range
        }
    }

    private float CalculateTrialErrorForAnimation(int animIndex)
    {
        float trialError = 0f;
        float scalingFactor = GetScalingFactorForTrial(animIndex); // Get the scaling factor based on trial type

        if (animIndex >= 0 && animIndex < 6) // Lifting and Pushing animations
        {
            string baseCsvKey = csvPaths[animIndex];
            List<CubeState> baseCubeStates = preprocessedData[baseCsvKey];
            trialError = CalculateTrialError(dataclassBase, baseCubeStates, scalingFactor, null, null, null, null); // Pass scaling factor
        }
        else if (animIndex >= 6) // Stacking animations
        {
            string baseCsvKey = csvPaths[6 + (animIndex - 6) * 3];
            string middleCsvKey = csvPaths[7 + (animIndex - 6) * 3];
            string topCsvKey = csvPaths[8 + (animIndex - 6) * 3];

            List<CubeState> baseCubeStates = preprocessedData[baseCsvKey];
            List<CubeState> middleCubeStates = preprocessedData[middleCsvKey];
            List<CubeState> topCubeStates = preprocessedData[topCsvKey];

            trialError = CalculateTrialError(dataclassBase, baseCubeStates, scalingFactor, dataclassMid, middleCubeStates, dataclassTop, topCubeStates); // Pass scaling factor
            Debug.Log($"Calculated Trial Error for Animation {animation_names[animIndex]}: {trialError}");

        }

        return trialError;
    }

    private float CalculateTrialError(DataClass baseTrialData, List<CubeState> baseCubeStates, float scalingFactor,
                                      DataClass middleTrialData = null, List<CubeState> middleCubeStates = null,
                                      DataClass topTrialData = null, List<CubeState> topCubeStates = null) // Accept scaling factor as parameter
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


        // Calculate average error per cube to normalize the fitness
        float averagePositionError = totalPositionError / numCubes;
        float averageRotationError = totalRotationError / numCubes;

        float totalError = averagePositionError + averageRotationError;
        totalError *= scalingFactor;
        return totalError; // Return normalized error
    }

    #endregion

    #region coroutines for animations


    public IEnumerator PlayAnimationCoroutine(int animIndex, int currentTrialNumber)
    {
        yield return null;
        // Play the corresponding animation
        main_anim.Play(animation_names[animIndex], 0);
        // Wait for the animation to finish
        float normedTime = 0f;
        yield return null;
        while (normedTime < 1)
        {

            // Get the normalized time of the animation (0 to 1, where 1 means animation is finished)
            normedTime = main_anim.GetCurrentAnimatorStateInfo(0).normalizedTime;

            // Calculate the current FPS based on the time taken for the last frame
            float currentFPS = 1.0f / Time.deltaTime;

            // Record the trial results, including FPS, at this frame (pass trialIndex to identify which trial it is)
            RecordTrialResults(animIndex, normedTime, currentTrialNumber, currentFPS);

            yield return null; // Wait for the next frame
        }

        //Debug.Log($"Animation {animation_names[animIndex]} complete.");
    }

    public IEnumerator TrialCoroutine(float[] physicsParams, Action<float> onComplete)
    {
        Debug.Log("WE ARE NOW MOVING ONTO trialNum : " + _ga.CurrentTrialNumber);
        // Ensure dataclassBase is initialized
        if (dataclassBase == null)
        {
            Debug.LogError("dataclassBase is not initialized.");
            yield break; // Stop the coroutine if dataclassBase is not initialized
        }

        yield return StartCoroutine(HandCalibrationCoroutine());
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
        yield return new WaitForSeconds(1.0f); // Ensure animation is complete

        // Step 4: Calculate fitness using a separate coroutine
        float fitness = 0f;
        yield return StartCoroutine(CalculateFitnessCoroutine(trialIndex, result => fitness = result));

        // Log the final fitness value
        Debug.Log($"Trial {currentTrialNumber} complete with fitness: {fitness}");

        // Step 5: Return the fitness via the callback
        onComplete(fitness);
        Debug.Log("NUMBER OF SIMS " + number_of_simulations + "current TRIAL " + currentTrialNumber);
        // Step 6: Save the data for the current trial (which already clears the lists)
        yield return StartCoroutine(Save(currentTrialNumber, _ga.CurrentGeneration));
        // Step 7: Check if currentTrialNumber exceeds number_of_simulations
        if (currentTrialNumber +1 >= number_of_simulations)//+1 bc currenttrial goes from 0 
        {
            Debug.Log("currentTrialNumber exceeded number_of_simulations, resetting to 0.");
            currentTrialNumber = 0; // Reset to 0 if it exceeds the limit
        }
        else
        {
            // Increment the current trial number for the next iteration
            currentTrialNumber++;
        }
    }

    private bool CalculateFpsStatistics(DataClass dataClass)
    {
        // Check if there are any FPS values recorded
        if (dataClass.fps.Count == 0)
        {
            //Debug.Log("No FPS data available.");
            return false; // Return false if there's no data
        }

        // Calculate the average FPS
        float averageFps = dataClass.fps.Average();

        // Output the results

        // Check if the average FPS is below 90 and return the appropriate boolean value
        if (averageFps < 90)
        {
            return false; // FPS is below 90, return false
        }
        else
        {

            return true; // FPS is 90 or above, return true
        }
    }

    private IEnumerator CalculateFitnessCoroutine(int trialIndex, Action<float> onFitnessCalculated)
    {
        // Declare trialError outside of the if-else block
        float trialError = 0f;

        // Step 1: Check FPS and calculate fitness accordingly
        bool isFpsAbove90 = CalculateFpsStatistics(dataclassBase); // Calculate FPS

        yield return null; // Allow the FPS calculation to complete

        float fitness;

        if (isFpsAbove90)
        {
            //  Calculate fitness based on error if FPS is below 90
            trialError = CalculateTrialErrorForAnimation(trialIndex);
            fitness = 1 / (1 + trialError); // Inverse error-based fitness calculation
            Debug.Log($"Trial {currentTrialNumber} fitness: {fitness} and trial error: {trialError}");
            yield return null;
        }
        else
        {
            // Assign maximum fitness (1.0) if FPS is above 90
            fitness = 1.0f;
            Debug.Log("FPS is above 90. Assigning max fitness of 1.0.");
            yield return null;
        }

        // Step 3: Return the calculated fitness via the callback
        onFitnessCalculated(fitness);
    }


    public IEnumerator SetupNext()
    {

        // Step 3: Reset cubes to their original positions
        ResetCubes();

        // Step 4: Clear any touch tracking or trial-specific flags to ensure a clean state
        baseCubeFirstTouched = false;
        middleCubeFirstTouched = false;
        topCubeFirstTouched = false;
        yield return null;
    }

    public IEnumerator Save(int trialNumber, int generationNumber)
    {
        Debug.Log("trialNum : " + _ga.CurrentTrialNumber);
        SaveDataFile(trialNumber, _ga.CurrentTrialNumber, generationNumber);
        yield return null;
    }

    #endregion

    #region scaling factor per animation RecordResults
    /*we will use default physcis parameters and calculate the average fitness for each of the animations then we will compute the scaling factors based on the average fitness per animation to account for 
     * different difficulty of manipulation
     */
    private IEnumerator PerformTrialsAndComputeScalingFactors()
    {
        // Dictionary to store fitness results for each animation
        Dictionary<int, List<float>> animationFitnessData = new Dictionary<int, List<float>>();

        // Initialize fitness data storage for each animation
        for (int i = 0; i < animation_names.Length; i++)
        {
            animationFitnessData[i] = new List<float>();
        }

        // Loop through each animation
        for (int animIndex = 0; animIndex < animation_names.Length; animIndex++)
        {
            Debug.Log($"Starting trials for animation: {animation_names[animIndex]}");

            // Run 30 trials for each animation
            for (int trial = 0; trial < 30; trial++)
            {
                Debug.Log($"Running trial {trial} for animation {animation_names[animIndex]}");

                // Ensure that the trial completes fully before starting the next one
                yield return StartCoroutine(RunTrialForAnimation(animIndex, animationFitnessData, trial));
                Debug.Log($"finished trial {trial} ");
            }

            // Output the average fitness for this animation
            float averageFitness = ComputeAverageFitnessForAnimation(animIndex, animationFitnessData);
            Debug.Log($"Average fitness for animation {animation_names[animIndex]}: {averageFitness} ");

            yield return new WaitForSeconds(1.0f); // Optional delay between different animations
        }

        // After completing all trials for all animations, calculate and output scaling factors
        CalculateAndOutputScalingFactors(animationFitnessData);
    }

    private IEnumerator RunTrialForAnimation(int animIndex, Dictionary<int, List<float>> animationFitnessData, int normalizedTrialNumber)
    {
        Debug.Log("basecubefirst " + baseCubeFirstTouched + "curentTrialnumbver" + currentTrialNumber);
        // Reset cubes before the trial
        ResetCubes();  // Setup the environment for the trial

        // Step 1: Play the animation
        Debug.Log($"Starting animation for trial {currentTrialNumber}, Animation: {animation_names[animIndex]}");
        //yield return StartCoroutine(PlayAnimationCoroutine(animIndex, currentTrialNumber)); ****************

        // Add a delay to ensure that animation effects have time to settle
        yield return new WaitForSeconds(1.0f);

        // Step 2: Calculate fitness after the animation completes
        float trialError = CalculateTrialErrorForAnimation(animIndex);
        float fitness = 1 / (1 + trialError);

        Debug.Log($"Trial {currentTrialNumber} (Normalized Trial: {normalizedTrialNumber}) completed. Trial Error: {trialError}, Fitness: {fitness}");

        // Store the fitness result
        animationFitnessData[animIndex].Add(fitness);

        // Step 3: Save trial results

        Debug.Log($"Trial {currentTrialNumber} complete, setting up next.");

        // Add a small delay between trials to prevent overlap
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(Save(currentTrialNumber, _ga.CurrentGeneration));
        yield return StartCoroutine(SetupNext());
        // Increment the current trial number for the next iteration
        currentTrialNumber++;
    }


    private float ComputeAverageFitnessForAnimation(int animIndex, Dictionary<int, List<float>> animationFitnessData)
    {
        List<float> fitnessResults = animationFitnessData[animIndex];
        float sumFitness = 0f;

        foreach (float fitness in fitnessResults)
        {
            sumFitness += fitness;
        }

        return sumFitness / fitnessResults.Count;
    }


    private void CalculateAndOutputScalingFactors(Dictionary<int, List<float>> animationFitnessData)
    {
        // Find the maximum average fitness across all animations
        float maxAverageFitness = float.MinValue;
        Dictionary<int, float> scalingFactors = new Dictionary<int, float>();

        for (int i = 0; i < animation_names.Length; i++)
        {
            float averageFitness = ComputeAverageFitnessForAnimation(i, animationFitnessData);
            if (averageFitness > maxAverageFitness)
            {
                maxAverageFitness = averageFitness;
            }
        }

        // Calculate the scaling factor for each animation relative to the maximum average fitness
        for (int i = 0; i < animation_names.Length; i++)
        {
            float averageFitness = ComputeAverageFitnessForAnimation(i, animationFitnessData);
            float scalingFactor = maxAverageFitness / averageFitness;  // Inverse proportion
            scalingFactors[i] = scalingFactor;

            Debug.Log($"Scaling Factor for animation {animation_names[i]}: {scalingFactor}");
        }

        // Optionally, store or output the scaling factors for future use
        Debug.Log("All scaling factors calculated and outputted.");
    }


    #endregion
}


