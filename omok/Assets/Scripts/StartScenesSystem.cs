using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartScenesSystem : MonoBehaviour
{
    public Socket socket = null;

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

        Debug.Log("ID : " + id_txt);
        Debug.Log("PASS : " + pass_txt);
        Debug.Log("IP : " + ip_txt);
        
        if ((id_txt != "") && (pass_txt != "") && (ip_txt != "") && (port_txt != null))
        {
            bool isConnected = connect(ip_txt, port_txt);
            if (isConnected)
            {
                //SceneManager.LoadScene("Scenes/Omok");
                Debug.Log("서버 접속 성공");
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

    // 소켓 연결
    private bool connect(string ip, int port)
    {
        try
        {
            IPAddress serverIP = IPAddress.Parse(ip);
            int serverPort = port;
            
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(serverIP, serverPort));

            if (socket == null || socket.Connected == false)
            {
                return false;
            }

            Debug.Log("Connected");
            return true;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }

        return false;
    }
}
