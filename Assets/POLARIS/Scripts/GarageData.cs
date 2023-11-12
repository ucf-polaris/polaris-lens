using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class GarageData
{
    public int available;
    public int occupied;
    public int total;
    public string timestamp;
    public string name;

    public GarageData(int available, int occupied, int total, string timestamp, string name)
    {
        this.available = available;
        this.occupied = occupied;
        this.total = total;
        this.timestamp = timestamp;
        this.name = name;
    }
}