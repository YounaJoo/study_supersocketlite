using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using ConnectToServer;
using UnityEngine.SceneManagement;

public class Login : MonoBehaviour
{
    public Text room = null;
    
    public void loginBtn()
    {
        // get : input Id / pass
        GameObject input_id = GameObject.Find("input_id").gameObject;
        GameObject input_pass = GameObject.Find("input_pass").gameObject;
        GameObject input_ip = GameObject.Find("input_ip").gameObject;
        GameObject input_port = GameObject.Find("input_port").gameObject;
        string pass_txt = input_pass.GetComponentInChildren<Text>().gameObject.GetComponent <Text>().text;
        string id_txt = input_id.GetComponentInChildren<Text>().gameObject.GetComponent<Text>().text;
        string ip_txt = input_ip.GetComponentInChildren<Text>().gameObject.GetComponent<Text>().text; 
        int port_txt = Int32.Parse(input_port.GetComponentInChildren<Text>().gameObject.GetComponent<Text>().text);

        string roomNum = room.text;

        Debug.Log("ID : " + id_txt);
        Debug.Log("PASS : " + pass_txt);
        Debug.Log("IP : " + ip_txt);
        
        if ((id_txt != "") && (pass_txt != "") && (ip_txt != "") && (port_txt != null) && (roomNum != ""))
        {
            bool isConnected = connect(ip_txt, port_txt);
            if (isConnected)
            {
                Debug.Log("서버 접속 성공");
                
                // ID 접속
                ChatRoomManager.playerID = id_txt;
                SceneManager.LoadScene("Scenes/Omok");
            }
            else
            {
                Debug.Log("연결을 확인해 주세요");
            }
        }
        else
        {
            Debug.Log("ID, Pass, IP, Port 를 입력해주세요.");
        }
    }

    private bool connect(string ip, int port)
    {
        NetworkManager network = new NetworkManager();
        return network.connect(ip, port);
    }
}
