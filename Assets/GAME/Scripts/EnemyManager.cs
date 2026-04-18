using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [Tooltip("Add enemy prefabs here. A random one will be chosen for each spawn.")]
    public GameObject[] enemyPrefabs;

    private int currentWave = 0;
    
    private class ActiveGroupState
    {
        public EnemySpawnConfig config;
        public int totalSpawned = 0;
        public List<GameObject> aliveEnemies = new List<GameObject>();
        
        public bool IsFullySpawned => totalSpawned >= config.count;
        public bool IsCleared => IsFullySpawned && aliveEnemies.Count == 0;
    }

    private List<ActiveGroupState> activeGroups = new List<ActiveGroupState>();
    private bool isWaveActive = false;

    [Header("Buff Settings")]
    [Tooltip("The UI prefab to generate 3 choices of buffs.")]
    public GameObject buffUIPrefab;
    [Tooltip("Distance in front of the player to spawn buffs.")]
    public float buffSpawnDistance = 3f;
    [Tooltip("Distance between the buff options horizontally.")]
    public float buffSpawnSpacing = 1.2f;

    private bool isWaitingForBuff = false;
    private List<GameObject> activeBuffs = new List<GameObject>();
    private int remainingBuffsToChoose = 0;

    void Update()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        if (isWaveActive)
        {
            bool allCleared = true;

            foreach (var groupState in activeGroups)
            {
                // Clean up dead enemies
                groupState.aliveEnemies.RemoveAll(enemy => enemy == null);

                // Spawn more if under max on stage limit and haven't spawned total count
                while (groupState.aliveEnemies.Count < groupState.config.maxOnStage && !groupState.IsFullySpawned)
                {
                    SpawnEnemyForGroup(groupState);
                }

                if (!groupState.IsCleared)
                {
                    allCleared = false;
                }
            }

            if (allCleared)
            {
                // Wave is finished!
                isWaveActive = false;
                activeGroups.Clear();
                
                int finishedWaveIndex = currentWave - 1;
                if (finishedWaveIndex >= 0 && finishedWaveIndex < WaveData.Waves.Count)
                {
                    remainingBuffsToChoose = WaveData.Waves[finishedWaveIndex].buffsToChoose;
                }
                
                if (currentWave < WaveData.Waves.Count && !isWaitingForBuff)
                {
                    if (remainingBuffsToChoose > 0)
                    {
                        SpawnBuffs();
                    }
                    else
                    {
                        SpawnNextWave();
                    }
                }
                else if (currentWave == WaveData.Waves.Count && !isWaitingForBuff)
                {
                    PlayerStatus playerStatus = FindObjectOfType<PlayerStatus>();
                    if (playerStatus != null && !playerStatus.isGameOver)
                    {
                        playerStatus.Win();
                    }
                }
            }
        }
        else if (currentWave == 0 && !isWaitingForBuff)
        {
            // Start the very first wave automatically
            SpawnNextWave();
        }
    }

    private void SpawnNextWave()
    {
        if (currentWave >= WaveData.Waves.Count) return;

        WaveConfig config = WaveData.Waves[currentWave];
        currentWave++;
        Debug.Log($"Starting Wave {currentWave} of {WaveData.Waves.Count}...");

        activeGroups.Clear();
        foreach (var group in config.enemyGroups)
        {
            activeGroups.Add(new ActiveGroupState { config = group });
        }
        
        isWaveActive = true;
    }

    private void SpawnEnemyForGroup(ActiveGroupState groupState)
    {
        var group = groupState.config;
        if (group.enemyTypeId < 0 || group.enemyTypeId >= enemyPrefabs.Length)
        {
            Debug.LogWarning($"Invalid enemyTypeId {group.enemyTypeId} in WaveData for Wave {currentWave}.");
            groupState.totalSpawned++; // Count it as spawned so we don't infinitely retry
            return;
        }

        GameObject prefabToSpawn = enemyPrefabs[group.enemyTypeId];
        Transform playerTransform = Camera.main != null ? Camera.main.transform : transform;
        Vector3 spawnPosition = Vector3.zero;

        if (group.spawnPositionType == SpawnPositionType.Fixed)
        {
            spawnPosition = group.fixedPosition;
            // Add slight random offset to prevent enemies from stacking exactly on each other
            spawnPosition.x += Random.Range(-group.fixedSpawnRadius, group.fixedSpawnRadius);
            spawnPosition.z += Random.Range(-group.fixedSpawnRadius, group.fixedSpawnRadius);
        }
        else
        {
            // Calculate relative to player
            Vector3 playerPos = playerTransform.position;
            Vector3 playerForward = playerTransform.forward;
            playerForward.y = 0;
            if (playerForward.sqrMagnitude < 0.01f) playerForward = playerTransform.up;
            playerForward.Normalize();

            float distance = Random.Range(group.spawnDistanceMin, group.spawnDistanceMax);

            if (group.spawnPositionType == SpawnPositionType.InFrontOfPlayer)
            {
                spawnPosition = playerPos + playerForward * distance;
            }
            else if (group.spawnPositionType == SpawnPositionType.RandomArc)
            {
                // Random arc inside [-range/2, range/2]
                float halfAngle = group.arcAngleRange / 2f;
                float randomAngle = Random.Range(-halfAngle, halfAngle);
                
                Vector3 rotatedForward = Quaternion.Euler(0, randomAngle, 0) * playerForward;
                spawnPosition = playerPos + rotatedForward * distance;
            }
            
            // Set default Y height for relative spawns
            spawnPosition.y = group.fixedPosition.y;
        }

        GameObject spawnedEnemy = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

        // Apply Modifiers
        Enemy enemyScript = spawnedEnemy.GetComponentInChildren<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.health *= group.hpMultiplier;
        }

        EnemyShooter shooterScript = spawnedEnemy.GetComponentInChildren<EnemyShooter>();
        if (shooterScript != null)
        {
            shooterScript.bulletDamage *= group.atkMultiplier;
        }

        SuicideDrone droneScript = spawnedEnemy.GetComponentInChildren<SuicideDrone>();
        if (droneScript != null)
        {
            droneScript.impactDamage *= group.atkMultiplier;
        }

        groupState.aliveEnemies.Add(spawnedEnemy);
        groupState.totalSpawned++;
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
        
        remainingBuffsToChoose--;

        if (remainingBuffsToChoose > 0)
        {
            SpawnBuffs();
        }
        else
        {
            // Buff picking finished, move directly to next wave if not at max
            if (currentWave < WaveData.Waves.Count)
            {
                SpawnNextWave();
            }
        }
    }
}
