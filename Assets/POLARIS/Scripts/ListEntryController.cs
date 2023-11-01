using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using UnityEngine.UIElements;

public class ListEntryController
{
    protected static string TruncateLongString(string str, int maxLength)
    {
        if (string.IsNullOrEmpty(str)) return str;

        return str.Substring(0, Math.Min(str.Length, maxLength));
    }
}
