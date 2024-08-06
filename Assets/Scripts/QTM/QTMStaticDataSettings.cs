using UnityEngine;

namespace VRLab.QTMTracking
{
    [CreateAssetMenu(fileName = "QTMStaticDataSettings", menuName = "QTMData/QTMStaticDataSettings", order = 0)]
public class QTMStaticDataSettings : ScriptableObject
{
    public QTMStaticDataScriptable QTMStaticDataScriptableToUse;    
}
}
