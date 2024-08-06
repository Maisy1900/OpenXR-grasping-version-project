using QualisysRealTime.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QTMAutoConnect : MonoBehaviour
{
    public string IpAddress = "192.168.254.1";
    public float DelayBeforeConnecting = 2.0f;
    // Start is called before the first frame update
    void Start()
    {
        Invoke("AutoConnect", DelayBeforeConnecting);
    }

    public void AutoConnect()
    {
        RTClient.GetInstance().StartConnecting(IpAddress, -1, true, true, true, true, true, true, true);
    }
}
