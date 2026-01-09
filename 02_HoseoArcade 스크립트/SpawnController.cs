using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnController : MonoBehaviour
{
    [Header("Max Item Spawn Property")]
    public int maxFeathers;
    public int maxBackPacks;
    public int maxScrolls;

    [Header("Spawn Rate")]
    [Tooltip("0 is never spawn, 1 is always spawn")]
    [Range(0.1f, 1f)]public float spawnRate;
}
