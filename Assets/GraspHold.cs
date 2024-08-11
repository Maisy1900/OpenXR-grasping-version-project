using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.IO;
using UnityEditor;
using System.Xml.Linq;
using System.Xml;
using MathNet.Numerics;
using Unity.VisualScripting;
using System;

public class GraspHold : MonoBehaviour
{
    public Transform index, thumb, wrist;
    private GameObject my_hand;
    public string cubeID;

    float grasp_dist = 0f;
    bool incontact = false;
    bool isGrasped = false; // To track if this object is currently grasped
    static bool globalGraspFlag = false; // Global flag to track if any object is currently grasped
    Rigidbody rb;
    Vector3 hand_vel = Vector3.zero;
    Vector3 current_pos, prev_pos;

    List<string> cube_pose = new List<string>();

    public string path;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        my_hand = new GameObject("Intermediary Object");
        my_hand.transform.position = Vector3.zero;
        my_hand.transform.rotation = Quaternion.identity;
        if (string.IsNullOrEmpty(path))
        {
            path = Application.dataPath + "/SavedPoses"; // Set default path if not set
        }
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        // Add CSV headers
        cube_pose.Add("PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,Timestamp");
    }

    private void Update()
    {
        grasp_dist = Vector3.Distance(index.position, thumb.position);

        cube_pose.Add(transform.position.x.ToString() + "," + transform.position.y.ToString() + "," + transform.position.z.ToString() + "," +
                      transform.eulerAngles.x.ToString() + "," + transform.eulerAngles.y.ToString() + "," + transform.eulerAngles.z.ToString() + "," + DateTime.UtcNow.ToString());

        // Update the position of my_hand to be at the midpoint between index and thumb
        my_hand.transform.position = (index.position + thumb.position) / 2;

        // Calculate the rotation from index to thumb
        Vector3 direction = (thumb.position - index.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);
            my_hand.transform.rotation = wrist.rotation;
        }
        if (gameObject.layer == LayerMask.NameToLayer("Locked"))
        {
            // If the object is in the Locked layer, it should not be graspable
            return;
        }
        // Handle parenting based on grasp distance and contact
        if (grasp_dist < 0.085f && incontact && !globalGraspFlag)
        {
            transform.parent = my_hand.transform;
            rb.useGravity = false;
            rb.isKinematic = true;
            isGrasped = true;
            globalGraspFlag = true; // Set the global flag
        }
        else if (grasp_dist > 0.085f && isGrasped)
        {
            ReleaseObject();
        }

        // Check if space key is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SaveCubePose();
        }
    }

    private void ReleaseObject()
    {
        transform.parent = null;
        rb.isKinematic = false;
        rb.useGravity = true;  // Re-enable gravity
        rb.velocity = Vector3.zero; // Reset velocity
        rb.angularVelocity = Vector3.zero; // Reset angular velocity
        isGrasped = false;
        globalGraspFlag = false; // Clear the global flag
    }

    public void ResetGraspState()
    {
        ReleaseObject();
        incontact = false;
    }

    private void SaveCubePose()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filePath = Path.Combine(path, "cube_pose_" + cubeID + "_" + timestamp + ".csv");

        try
        {
            File.WriteAllLines(filePath, cube_pose);
            Debug.Log("File saved successfully at " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save file: " + e.Message);
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Hand")
        {
            incontact = true;
            Debug.Log("Hand In Contact");
            rb.velocity = Vector3.zero;
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.tag == "Hand")
        {
            incontact = false;
            Debug.Log("Hand Exit");
        }
    }
}
