using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    // 플레이어 스탯
    public string playerName;

    [Header ("Player Stats")]
    [HideInInspector] public float playerSpeed;
    [HideInInspector] public int numberOfBombs;
    [HideInInspector] public int bombLength;

    [Header("Player Start Stats")]
    [SerializeField] float startPlayerSpeed;
    [SerializeField] int startNumberOfBombs;
    [SerializeField] int startBombLength;

    [Header ("Player Max Stats")]
    [Range(0f, 10f)] [SerializeField] private float MaxSpeed;
    [Range(1, 10)] [SerializeField] private int MaxBombs;
    [Range(1, 10)] [SerializeField] private int MaxBombStream;

    private void Start()
    {
        playerSpeed = startPlayerSpeed;
        numberOfBombs = startNumberOfBombs;
        bombLength = startBombLength;
    }

    private void Update()
    {
        playerSpeed = Mathf.Clamp(playerSpeed, startPlayerSpeed, MaxSpeed);
        numberOfBombs = Mathf.Clamp(numberOfBombs, startNumberOfBombs, MaxBombs);
        bombLength = Mathf.Clamp(bombLength, startBombLength, MaxBombStream);
    }
}
