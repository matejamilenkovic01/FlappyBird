using System;
using System.Collections.Generic;

public static class WeightedRandom
{
    // Cumulative pick: weights are relative, entries with weight <= 0 are never chosen.
    public static T Pick<T>(IList<T> items, Func<T, float> getWeight) where T : class
    {
        if (items == null || items.Count == 0)
            return null;

        float totalWeight = 0f;
        foreach (var item in items)
        {
            float weight = getWeight(item);
            if (weight > 0f)
                totalWeight += weight;
        }

        if (totalWeight <= 0f)
            return null;

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;
        T lastValid = null;

        foreach (var item in items)
        {
            float weight = getWeight(item);
            if (weight <= 0f)
                continue;

            cumulativeWeight += weight;
            lastValid = item;

            if (randomValue <= cumulativeWeight)
                return item;
        }

        // Float rounding can leave randomValue a hair above the final sum.
        return lastValid;
    }
}
