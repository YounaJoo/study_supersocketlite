using System;
using System.Collections;
using System.Collections.Generic;
using ConnectToServer;
using UnityEngine;
using UnityEngine.UI;

// login UI Init
public class Login : MonoBehaviour
{
    private GameObject loginCanvas = null;
    public Button loginBtn = null;
    public Button signBtn = null;
    
    private void Start()
    {
        loginCanvas = this.gameObject;
        loginBtn = loginCanvas.transform.GetChild(2).transform.GetChild(7).gameObject.GetComponent<Button>();
        signBtn = loginCanvas.transform.GetChild(2).transform.GetChild(8).gameObject.GetComponent<Button>();
     
        loginBtn.onClick.AddListener(MainClient.Instance.loginBtn);
        signBtn.onClick.AddListener(MainClient.Instance.exitBtn);

        MainClient.Instance.room = GameObject.Find("roomText").GetComponent<Text>();
    }
    
    
}
