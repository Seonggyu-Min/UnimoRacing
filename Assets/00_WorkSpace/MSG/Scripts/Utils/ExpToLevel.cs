using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExpToLevel
{
    public static int Convert(int exp)
    {
        return (int)Mathf.Floor(Mathf.Sqrt(exp / 100f) + 1);
    }
}
