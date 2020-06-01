using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UI;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public Text txt;
    public Button btn;
    private MachingSystem ms;
    
    public void chkBtn()
    {
        ms = btn.GetComponent<MachingSystem>();
        string person = "youna";
        txt.text = "Sucess Maching : " + person;

        Text btn_txt = btn.GetComponentInChildren<Text>();
        btn_txt.text = "ok";
        ms.idx = 2;
    }
}
