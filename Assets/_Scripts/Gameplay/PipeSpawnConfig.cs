using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PipeSpawnConfig", menuName = "ScriptableObjects/PipeSpawnConfig")]
public class PipeSpawnConfig : ScriptableObject
{
    [Header("Pipe Types")]
    public List<PipeTypeEntry> PipeTypes;

    [Header("Spawning")]
    public float SpawnDistance = 3.4f;
    public float GapSize = 2.7f;
    public Vector2 GapCenterYRange = new(-1.6f, 2.0f);
    public float MaxGapCenterDelta = 2.2f;
    public int PrewarmPerType = 4;

    [Header("Difficulty")]
    public float BaseSpeed = 2.4f;
    public float SpeedPerPoint = 0.05f;
    public float MaxSpeed = 4.0f;

    public PipeTypeEntry PickType() => WeightedRandom.Pick(PipeTypes, t => t.SpawnWeight);

    public float GetSpeed(int score) => Mathf.Min(BaseSpeed + SpeedPerPoint * score, MaxSpeed);

    private void OnValidate()
    {
        if (PipeTypes == null)
            return;

        foreach (PipeTypeEntry type in PipeTypes)
        {
            if (type.Prefab == null)
                Debug.LogWarning($"PipeSpawnConfig: '{type.Id}' has no prefab.", this);
            if (type.SpawnWeight < 0f)
                Debug.LogWarning($"PipeSpawnConfig: '{type.Id}' has a negative weight.", this);
        }
    }
}

[Serializable]
public class PipeTypeEntry
{
    public string Id;
    public PipePair Prefab;
    [Range(0f, 1f)] public float SpawnWeight;
    public int ScoreValue = 1;
}
