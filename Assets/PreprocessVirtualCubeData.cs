using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PreprocessVirtualCubeData : MonoBehaviour
{
    public string[] csvPaths;  // Paths to the original CSV files
    public string outputDirectory;  // Directory to save preprocessed CSV files

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
        bool movementDetected = false;

        string[] csvLines = File.ReadAllLines(csvPath);

        // Assuming the first line is the header
        newCSVLines.Add(csvLines[0]);

        Vector3 previousPosition = Vector3.zero;

        for (int i = 1; i < csvLines.Length; i++)
        {
            string[] columns = csvLines[i].Split(',');

            // Assuming the columns follow the format: time, posX, posY, posZ, rotX, rotY, rotZ, rotW
            Vector3 position = new Vector3(
                float.Parse(columns[1]),
                float.Parse(columns[2]),
                float.Parse(columns[3])
            );

            if (!movementDetected && Vector3.Distance(position, previousPosition) > 0.01f)
            {
                movementDetected = true;
            }

            if (movementDetected)
            {
                newCSVLines.Add(csvLines[i]);
            }

            previousPosition = position;
        }

        // Save the new CSV file
        string fileName = Path.GetFileNameWithoutExtension(csvPath) + "_preprocessed.csv";
        string outputPath = Path.Combine(outputDirectory, fileName);
        File.WriteAllLines(outputPath, newCSVLines);

        Debug.Log($"Preprocessed CSV saved to: {outputPath}");
    }
}
