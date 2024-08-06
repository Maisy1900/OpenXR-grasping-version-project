using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static VRLab.QTMTracking.QTMStaticData;

namespace VRLab.QTMTracking
{
    public enum UnityTransformMarkers
{
    Unassigned,
    Forehead,
    BaseLeft,
    BaseRight,
    BaseRear,
    TripodRight,
    TripodFront,
    ChairBackLeft,
    ChairFrontRight
}

public enum UnityTransformObjects
{
    Brain,
    Headset,
    QRMarker,
    Coil
}

[CreateAssetMenu(fileName = "QTMStaticDataScriptable", menuName = "QTMData/QTMStaticDataScriptable", order = 0)]
public class QTMStaticDataScriptable : ScriptableObject
{
    public List<UnityTransformMarkers> availableMarkers;
    public List<string> correspondingQtmMarkers;
    public Dictionary<UnityTransformMarkers, string> UnityToQtmMarkerMapping = new Dictionary<UnityTransformMarkers, string>();


    public string GetQtmMarkerName(UnityTransformMarkers marker)
    {
        return UnityToQtmMarkerMapping != null ? UnityToQtmMarkerMapping[marker] : "MappingNotFound";
    }

    private void OnValidate() {

            // if (CurrentData != this)
            // {
            //     // QTMMarkerNames.Keys;
            //     if (CurrentData == null)
            //     {
            //         Debug.LogWarning($"Setting new static instance of {typeof(QTMStaticDataScriptable)}");
            //     } else {
            //         Debug.LogWarning($"Changing static instance of {typeof(QTMStaticDataScriptable)}");
            //     }
            //     ToUse = this;
            // }
    
        availableMarkers = Enumerable.Range(0, Enum.GetNames(typeof(UnityTransformMarkers)).Length).Select(n => (UnityTransformMarkers)n).ToList();

        for (int i = 0; i < availableMarkers.Count; i++) {
            string correspondingMarkerName = correspondingQtmMarkers.Count > i ? correspondingQtmMarkers[i] : "UnMapped";
            UnityToQtmMarkerMapping.Add(availableMarkers[i], correspondingMarkerName);
        }
    }
}

public static class QTMStaticData
{
    //Public Accessors
    /// <summary>This is the dict that allows the QTM Marker name to be accessed anywhere via a Unity Enum</summary>
    public static Dictionary<UnityTransformMarkers, string> UnityToQtmMarkerMapping;

    private static QTMStaticDataScriptable _instance;



    /// <summary>This is the instance of QTMStaticsData scriptable that is used when data is accessed</summary>
    //internal static QTMStaticDataScriptable CurrentData { get => _instance ? _instance : getCurrentData(); }

    //private static QTMStaticDataScriptable getCurrentData()
    //{
    //    var settingsAssets = AssetDatabase.FindAssets("QTM Static Data Settings.asset");
    //    if (settingsAssets.Length > 0)
    //    {
    //        if (settingsAssets.Length > 1)
    //        {
    //            Debug.LogWarning($"More than one QTM Static Data Settings file found. Using {settingsAssets[0]}");
    //        }
    //        var settingsFile = AssetDatabase.LoadAssetAtPath<QTMStaticDataSettings>(settingsAssets[0]);
    //        var DataScriptableToUse = settingsFile.QTMStaticDataScriptableToUse;
    //        if (DataScriptableToUse == null) {
    //            Debug.LogError($"No QTM Static Data Scriptable assigned in settings");
    //        }
    //        _instance = DataScriptableToUse;
    //        return DataScriptableToUse;

    //    } else {
    //        Debug.LogError("No QTM Static Data Settings file found");
    //        return null;
    //    }

    //}

    /// <summary>Marker names specified in Enum, generated as a consecutive List<></summary>

}
}
