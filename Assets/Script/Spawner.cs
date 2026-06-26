using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float timeBetweenSpawns = 1.5f;
    public GameObject enemyPrefab;

    [Header("Spawn Area Offset")]
    public float topOffset = 2f;      // jarak di atas layar
    public float sidePadding = 1f;    // biar tidak nempel pinggir

    private float nextSpawnTime;

    void Update()
    {
        if (!GameManager.Instance.gameStarted) return;

        if (Time.time >= nextSpawnTime)
        {
            nextSpawnTime = Time.time + timeBetweenSpawns;
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        Camera cam = Camera.main;

        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        float x = Random.Range(-camWidth + sidePadding, camWidth - sidePadding);
        float y = camHeight + topOffset;

        Vector3 spawnPos = new Vector3(x, y, 0);

        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}