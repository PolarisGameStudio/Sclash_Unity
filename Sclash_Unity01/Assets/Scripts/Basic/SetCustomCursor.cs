﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetCustomCursor : MonoBehaviour
{
    // CUSTOM CURSOR
    [Header("CUSTOM CURSOR")]
    [SerializeField] Texture2D cursor = null;
    [SerializeField] Vector2 cursorHotspot = new Vector2(0, 0);




    // BASE FUNCTIONS
    // Start is called before the first frame update
    void Start()
    {
        Cursor.SetCursor(cursor, cursorHotspot, CursorMode.Auto);
    }
}
