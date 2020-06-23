using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Omok : MonoBehaviour
{
    private static Omok instance = null;
    
    public const float MINX = -4.85f;
    public const float MAXX = 1.85f;
    public const float MINY = -3.35f;
    public const float MAXY = 3.35f;
    public const float DIS = 0.75f;
    public const int OMOKCOUNT = 10;
    
    public const float CHK_MIN_X = -5.0f;
    public const float CHK_MIN_Y = -3.5f;
    public const float CHK_MAX_X = 2.0f;
    public const float CHK_MAX_Y = 3.5f;

    private Vector2 hotSpot;
    private bool isActive = false;
    private short userPos = -1;
    
    public Texture2D[] omokTexture = new Texture2D[2];
    public GameObject[] omokObject = new GameObject[2];
    public GameObject omokPanMask;
    
    private GameObject omokPan;
    
    private float curTime = 0.0f;

    public float[,] omok = new float[OMOKCOUNT, OMOKCOUNT + 1];
    
    public static Omok Instance
    {
        get => instance;
        set => instance = value;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        omokPan = omokPanMask.transform.parent.gameObject;
        omok = SetOmokPan();
    }
    private void Update()
    {
        if (isActive && userPos != -1)
        {
            Debug.Log("코루틴 시작");
            StartCoroutine("OmokCursor");
        }
    }

    public void setOmokCurr(bool isActive, short userPos)
    {
        Debug.Log(isActive);
        this.isActive = isActive;
        this.userPos = userPos;
    }

    public void createOmok(short userPos, int x, int y)
    {
        GameObject _omok = (GameObject) Instantiate(omokObject[userPos], transform.position, transform.rotation);
        _omok.transform.parent = omokPan.transform;
        _omok.transform.position = new Vector3(omok[y, x+1], omok[y, 0], -0.15f);
    }

    IEnumerator OmokCursor()
    {
        yield return new WaitForEndOfFrame();

        hotSpot.x = omokTexture[userPos].width / 2;
        hotSpot.y = omokTexture[userPos].height / 2;
        
        Cursor.SetCursor (omokTexture[userPos], hotSpot, CursorMode.Auto);
    }

    private float[,] SetOmokPan()
    {
        float[,] omok = new float[OMOKCOUNT,OMOKCOUNT + 1];

        for (int y = 0; y < OMOKCOUNT; y++)
        {
            for (int x = 1; x < OMOKCOUNT + 1; x++)
            {
                omok[y, x] = (float)Math.Round(MINX + (DIS * (x-1)), 2);
            }

            omok[y, 0] = (float)Math.Round(MINY + (DIS * y), 2);
        }

        return omok;
    }
    
    public Tuple<int, int> GetOmokIndex(float x, float y)
    {
        float abs = 0.0f;
        float minX = MAXX;
        float minY = MAXY;
        int indexX = 0;
        int indexY = 0;
        
        for (int i = 0; i < OMOKCOUNT; i++)
        {
            for (int j = 1; j < OMOKCOUNT + 1; j++)
            {
                // x 
                abs = ((omok[i, j] - x) < 0) ? -(omok[i, j] - x) : (omok[i, j] - x);

                if (abs < minX)
                {
                    minX = abs;
                    indexX = j;
                }
            }

            // y
            abs = ((omok[i, 0] - y) < 0) ? -(omok[i, 0] - y) : (omok[i, 0] - y);

            if (abs < minY)
            {
                minY = abs;
                indexY = i;
            }
        }
        
        Tuple<int, int> result = new Tuple<int, int>(indexX-1, indexY);

        return result;
    }

    public bool ChkMouseOn(float x, float y)
    {
        if (x < CHK_MIN_X || x > CHK_MAX_X ||
            y < CHK_MIN_Y || y > CHK_MAX_Y)
        {
            return false;
        }

        return true;
    }
    
}
