using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Globalization;

public class PreprocessVirtualCubeData : MonoBehaviour
{
    public string[] csvPaths;  // Paths to the original CSV files
    public string outputDirectory;  // Directory to save preprocessed CSV files
    public float movementThreshold = 0.01f;  // Threshold to detect significant movement
    public int linesToSkip = 50;  // Number of lines to skip at the start of the CSV

    void Start()
    {
        foreach (string csvPath in csvPaths)
        {
            PreprocessCSVFile(csvPath);
        }
    }

    void PreprocessCSVFile(string csvPath)
    {
        List<string> newCSVLines = new List<string>();
        List<System.DateTime> timeStamps = new List<System.DateTime>();
        List<Vector3> cubePositions = new List<Vector3>();
        List<Vector3> cubeRotations = new List<Vector3>();

        string[] csvLines = File.ReadAllLines(csvPath);

        // Assuming the first line is the header
        newCSVLines.Add(csvLines[0]);

        for (int i = 1; i < csvLines.Length; i++)
        {
            if (i <= linesToSkip) continue;

            string[] columns = csvLines[i].Split(',');

            // Check if the row has the expected number of columns
            if (columns.Length < 7)
            {
                Debug.LogWarning($"Skipping malformed line {i + 1} in file {csvPath}: not enough columns.");
                continue;
            }

            // Try to parse the position, rotation, and timestamp values
            if (float.TryParse(columns[0], out float posX) &&
                float.TryParse(columns[1], out float posY) &&
                float.TryParse(columns[2], out float posZ) &&
                float.TryParse(columns[3], out float rotX) &&
                float.TryParse(columns[4], out float rotY) &&
                float.TryParse(columns[5], out float rotZ) &&
                System.DateTime.TryParseExact(columns[6], "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out System.DateTime timeStamp))
            {
                Vector3 position = new Vector3(posX, posY, posZ);
                Vector3 rotation = new Vector3(rotX, rotY, rotZ);

                timeStamps.Add(timeStamp);
                cubePositions.Add(position);
                cubeRotations.Add(rotation);
            }
            else
            {
                Debug.LogWarning($"Skipping malformed line {i + 1} in file {csvPath}: could not parse numbers.");
            }
        }

        // Check if the lists have enough data to process
        if (timeStamps.Count < 2 || cubePositions.Count < 2 || cubeRotations.Count < 2)
        {
            Debug.LogError("Not enough valid data to process the CSV file.");
            return;  // Exit the method if there isn't enough data
        }

        // Use the DetectMovementStart function to find the first significant movement
        var movementStart = DetectMovementStart(timeStamps, cubePositions, cubeRotations, movementThreshold);

        // Find the index of the first significant movement
        int movementStartIndex = timeStamps.IndexOf(movementStart.time);

        if (movementStartIndex == -1)
        {
            Debug.LogError("Movement start not found, something went wrong.");
            return;  // Exit the method if movement start index is invalid
        }

        // Add all lines from the first significant movement onwards
        for (int i = movementStartIndex + 1; i < csvLines.Length; i++)
        {
            newCSVLines.Add(csvLines[i]);
        }

        // Save the new CSV file
        string fileName = Path.GetFileNameWithoutExtension(csvPath) + "_preprocessed.csv";
        string outputPath = Path.Combine(outputDirectory, fileName);
        File.WriteAllLines(outputPath, newCSVLines);

        Debug.Log($"Preprocessed CSV saved to: {outputPath}");
    }

    public (System.DateTime time, Vector3 position, Vector3 rotation) DetectMovementStart(
        List<System.DateTime> timeStamps,
        List<Vector3> cubePositions,
        List<Vector3> cubeRotations,
        float movementThreshold = 0.01f)
    {
        for (int i = 1; i < cubePositions.Count; i++)
        {
            if (Vector3.Distance(cubePositions[i], cubePositions[i - 1]) > movementThreshold)
            {
                return (timeStamps[i], cubePositions[i], cubeRotations[i]);
            }
        }
        // If no significant movement is detected, return default value
        return (timeStamps[0], cubePositions[0], cubeRotations[0]);
    }
}
