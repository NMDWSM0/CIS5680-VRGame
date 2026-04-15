using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public int maxWaves = 10;
    public int enemiesPerWave = 3;

    [Header("Enemy Prefabs")]
    [Tooltip("Add enemy prefabs here. A random one will be chosen for each spawn.")]
    public GameObject[] enemyPrefabs;

    [Header("Spawn Location")]
    [Tooltip("The center point where enemies will spawn in the XZ plane.")]
    public Vector2 spawnCenterXZ = new Vector2(6f, -6f);
    
    [Tooltip("The fixed Y height for spawning enemies.")]
    public float spawnHeightY = 2f;
    
    [Tooltip("Radius around the given XZ center to randomly place the enemy so they don't overlap.")]
    public float spawnRadius = 2f;

    private int currentWave = 0;
    private List<GameObject> activeEnemies = new List<GameObject>();

    [Header("Buff Settings")]
    [Tooltip("The UI prefab to generate 3 choices of buffs.")]
    public GameObject buffUIPrefab;
    [Tooltip("Distance in front of the player to spawn buffs.")]
    public float buffSpawnDistance = 3f;
    [Tooltip("Distance between the buff options horizontally.")]
    public float buffSpawnSpacing = 1.2f;

    private bool isWaitingForBuff = false;
    private List<GameObject> activeBuffs = new List<GameObject>();

    void Update()
    {
        // Don't do anything if we haven't assigned any prefabs in the inspector
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            return;
        }

        // Unity automatically sets destroyed objects to null in C# collections if we check them.
        // We can clean up the list by removing all null references (enemies that were killed/destroyed).
        activeEnemies.RemoveAll(enemy => enemy == null);

        // If the list is empty, all enemies in the current wave are dead.
        // Proceed to generate the next wave, but give buffs first.
        if (activeEnemies.Count == 0 && currentWave < maxWaves && !isWaitingForBuff)
        {
            if (currentWave == 0)
            {
                // First wave starts immediately without buffs
                SpawnNextWave();
            }
            else
            {
                // After each wave finishes, generate buffs
                SpawnBuffs();
            }
        }
    }

    private void SpawnNextWave()
    {
        currentWave++;
        Debug.Log($"Spawning Wave {currentWave} of {maxWaves}...");

        for (int i = 0; i < enemiesPerWave; i++)
        {
            // Pick a fully random enemy prefab from the array
            GameObject selectedPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

            // Calculate a random position around (6, 0, -6) in the XZ plane, mapped to a Y of 2.
            float randomX = spawnCenterXZ.x + Random.Range(-spawnRadius, spawnRadius);
            float randomZ = spawnCenterXZ.y + Random.Range(-spawnRadius, spawnRadius);
            Vector3 spawnPosition = new Vector3(randomX, spawnHeightY, randomZ);

            // Instantiate and add to our tracking list
            GameObject spawnedEnemy = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
            activeEnemies.Add(spawnedEnemy);
        }
    }

    private void SpawnBuffs()
    {
        if (buffUIPrefab == null)
        {
            Debug.LogWarning("No BuffUI Prefab assigned! Skipping buffs and going to next wave...");
            SpawnNextWave();
            return;
        }

        isWaitingForBuff = true;
        
        // Find player's current position and forward vector (approximate using MainCamera)
        Transform player = Camera.main != null ? Camera.main.transform : transform;
        
        Vector3 forward = player.forward;
        forward.y = 0; // Flatten it so buffs stay perfectly upright
        if (forward.sqrMagnitude < 0.01f) forward = player.up; // Edge case
        forward.Normalize();
        
        // Calculate the angle to separate them so the arc length between them is roughly buffSpawnSpacing
        float angleSpread = (buffSpawnSpacing / buffSpawnDistance) * Mathf.Rad2Deg;

        // Randomly select 3 different buffs
        List<Buff> selectedBuffs = new List<Buff>();
        List<Buff> allBuffs = new List<Buff>(BuffDatabase.AvailableBuffs);
        for (int i = 0; i < 3 && allBuffs.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, allBuffs.Count);
            selectedBuffs.Add(allBuffs[randomIndex]);
            allBuffs.RemoveAt(randomIndex);
        }

        for (int i = 0; i < 3; i++)
        {
            // Calculate angle offset: i=0(left), i=1(center), i=2(right)
            float angleOffset = (i - 1) * angleSpread;
            
            // Rotate the forward vector radially
            Vector3 rotatedForward = Quaternion.Euler(0, angleOffset, 0) * forward;
            
            // Spawn perfectly on the edge of the circle
            Vector3 spawnPos = player.position + rotatedForward * buffSpawnDistance - new Vector3(0, 0.5f, 0);
            
            // Point the UI directly back into the center of the circle (at the player)
            GameObject buffObj = Instantiate(buffUIPrefab, spawnPos, Quaternion.LookRotation(-rotatedForward));
            activeBuffs.Add(buffObj);
            
            BuffUI buffScript = buffObj.GetComponent<BuffUI>();
            if (buffScript != null && i < selectedBuffs.Count)
            {
                buffScript.Initialize(selectedBuffs[i], this);
            }
        }
    }

    public void OnBuffSelected(BuffUI selectedBuff)
    {
        Debug.Log($"Player selected buff!");

        // Destroy the three generated UI objects
        foreach (var buff in activeBuffs)
        {
            if (buff != null) Destroy(buff);
        }
        activeBuffs.Clear();
        isWaitingForBuff = false;
        
        // Buff picked, move directly to next wave
        SpawnNextWave();
    }
}
