using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class MachingSystem : MonoBehaviour
{
    public int idx = 1;

    private void Awake()
    {
        idx = 1;
    }

    public void chkBtn()
    {
        switch (idx)
        {
            case 1 :
                Debug.Log("OnApplicationQuit");
                OnApplicationQuit();
                break;
            case 2 :
                Debug.Log("ChangeScene");
                SceneManager.LoadScene("SampleScene");
                break;
            default: 
                Debug.Log("none");
                break;
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("exit");
    }
}
