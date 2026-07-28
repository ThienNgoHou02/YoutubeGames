using System.Collections.Generic;
using UnityEngine;

public static class WaitForSecondCache
{
    private static Dictionary<float, WaitForSeconds> cache = new Dictionary<float, WaitForSeconds>();

    public static WaitForSeconds Get(float time)
    {
        if (!cache.ContainsKey(time)) cache[time] = new WaitForSeconds(time);
        return cache[time];
    }
}