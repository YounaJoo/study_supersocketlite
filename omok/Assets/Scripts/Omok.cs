using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Omok : MonoBehaviour
{
    private Vector2 hotSpot;
    private bool isActive = false;
    private short userPos = -1;
    public Texture2D[] omokTexture = new Texture2D[2];
    public GameObject[] omok = new GameObject[2];
    public GameObject omokPanMask;
    
    private GameObject omokPan;
    
    private float curTime = 0.0f;
    
    private void Start()
    {
        omokPan = omokPanMask.transform.parent.gameObject;
        Debug.Log(omokPan.name);
    }
    private void Update()
    {
        Debug.Log(1234);
        if (isActive && userPos != -1)
        {
            Debug.Log(5678);
            StartCoroutine("OmokCursor");
        }
    }

    public void setOmokCurr(bool isActive, short userPos)
    {
        Debug.Log(isActive);
        this.isActive = isActive;
        this.userPos = userPos;
    }

    public void createOmok(short userPos, Vector2 omokPos)
    {
        GameObject _omok = (GameObject) Instantiate(omok[userPos], transform.position, transform.rotation);
        _omok.transform.parent = omokPan.transform;
        _omok.transform.position = new Vector3(omokPos.x, omokPos.y, -0.15f);
    }

    IEnumerator OmokCursor()
    {
        yield return new WaitForEndOfFrame();

        hotSpot.x = omokTexture[userPos].width / 2;
        hotSpot.y = omokTexture[userPos].height / 2;
        
        Cursor.SetCursor (omokTexture[userPos], hotSpot, CursorMode.Auto);
    }
}
