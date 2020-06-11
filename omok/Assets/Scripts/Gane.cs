using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gane : MonoBehaviour
{
    public GameObject omokPanMask;
    public GameObject[] omok = new GameObject[2];
    private GameObject omokPan;

    private Vector3 mousePos;
    private float curTime = 0.0f;

    private void Start()
    {
        omokPan = omokPanMask.transform.parent.gameObject;
        Debug.Log(omokPan.name);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            Debug.Log(mousePos);
            
            createOmok(1, mousePos);
        }
    }

    public void createOmok(short userPos, Vector2 omokPos)
    {
        GameObject _omok = (GameObject) Instantiate(omok[userPos], transform.position, transform.rotation);
        _omok.transform.parent = omokPan.transform;
        _omok.transform.position = new Vector3(omokPos.x, omokPos.y, -0.15f);
    }
}
