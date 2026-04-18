using UnityEngine;
using System.Collections.Generic;

public enum SpawnPositionType
{
    Fixed,
    InFrontOfPlayer,
    RandomArc
}

public class EnemySpawnConfig
{
    // Index in EnemyManager's enemyPrefabs array
    public int enemyTypeId; 
    public int count = 1;
    public int maxOnStage = 99; // Defaults to no effective limit.

    // Multipliers for future implementation
    public float hpMultiplier = 1.0f;
    public float atkMultiplier = 1.0f;

    // Spawn parameters
    public SpawnPositionType spawnPositionType = SpawnPositionType.Fixed;
    public Vector3 fixedPosition = new Vector3(6f, 2f, -6f);
    public float spawnDistanceMin = 8.0f;
    public float spawnDistanceMax = 8.0f;
    public float arcAngleRange = 90.0f;
    public float fixedSpawnRadius = 1f;
}

public class WaveConfig
{
    [Tooltip("How many buffs the player can choose after this wave finishes.")]
    public int buffsToChoose = 1;
    public List<EnemySpawnConfig> enemyGroups = new List<EnemySpawnConfig>();
}

public static class WaveData
{
    // Directly define your waves here.
    // The EnemyManager can read this data via 'WaveData.Waves[currentWaveIndex]'
    public static List<WaveConfig> Waves = new List<WaveConfig>()
    {
        // ------------- WAVE 1 -------------
        new WaveConfig 
        {
            buffsToChoose = 1,
            enemyGroups = new List<EnemySpawnConfig> 
            {
                new EnemySpawnConfig 
                { 
                    enemyTypeId = 0, // Uses enemyPrefabs[0]
                    count = 1, 
                    maxOnStage = 1,
                    spawnPositionType = SpawnPositionType.InFrontOfPlayer, 
                    spawnDistanceMin = 14f,
                    spawnDistanceMax = 14f,
                    hpMultiplier = 0.5f
                }
            }
        },

        // ------------- WAVE 2 -------------
        new WaveConfig 
        {
            buffsToChoose = 1,
            enemyGroups = new List<EnemySpawnConfig> 
            {
                new EnemySpawnConfig 
                { 
                    enemyTypeId = 0, // Uses enemyPrefabs[0]
                    count = 2, 
                    maxOnStage = 2,
                    spawnPositionType = SpawnPositionType.RandomArc, 
                    spawnDistanceMin = 12f,
                    spawnDistanceMax = 16f,
                    arcAngleRange = 45f,
                    hpMultiplier = 0.5f
                }
            }
        },

        // ------------- WAVE 3 -------------
        new WaveConfig 
        {
            buffsToChoose = 2,
            enemyGroups = new List<EnemySpawnConfig> 
            {
                new EnemySpawnConfig 
                { 
                    enemyTypeId = 0, // Uses enemyPrefabs[0]
                    count = 3, 
                    maxOnStage = 2,
                    spawnPositionType = SpawnPositionType.RandomArc, 
                    spawnDistanceMin = 12f,
                    spawnDistanceMax = 16f,
                    arcAngleRange = 60f,
                    hpMultiplier = 0.75f
                }
            }
        },

        // ------------- WAVE 4 -------------
        new WaveConfig 
        {
            buffsToChoose = 2,
            enemyGroups = new List<EnemySpawnConfig> 
            {
                new EnemySpawnConfig 
                { 
                    enemyTypeId = 0, // Uses enemyPrefabs[0]
                    count = 4, 
                    maxOnStage = 2,
                    spawnPositionType = SpawnPositionType.RandomArc, 
                    spawnDistanceMin = 12f,
                    spawnDistanceMax = 16f,
                    arcAngleRange = 60f
                }
            }
        },

        // ------------- WAVE 5 -------------
        new WaveConfig 
        {
            buffsToChoose = 3,
            enemyGroups = new List<EnemySpawnConfig> 
            {
                new EnemySpawnConfig 
                { 
                    enemyTypeId = 1, // Uses enemyPrefabs[0]
                    count = 1, 
                    maxOnStage = 1,
                    spawnPositionType = SpawnPositionType.InFrontOfPlayer, 
                    spawnDistanceMin = 15f,
                    spawnDistanceMax = 15f,
                    atkMultiplier = 0.8f
                }
            }
        },

        // ------------- WAVE 6 -------------
        new WaveConfig 
        {
            buffsToChoose = 2,
            enemyGroups = new List<EnemySpawnConfig> 
            {
                new EnemySpawnConfig 
                { 
                    enemyTypeId = 0, // Uses enemyPrefabs[0]
                    count = 5, 
                    maxOnStage = 2,
                    spawnPositionType = SpawnPositionType.RandomArc, 
                    spawnDistanceMin = 12f,
                    spawnDistanceMax = 16f,
                    arcAngleRange = 60f,
                    hpMultiplier = 1.2f,
                    atkMultiplier = 1.1f
                }
            }
        },

        // ------------- WAVE 7 -------------
        new WaveConfig 
        {
            buffsToChoose = 2,
            enemyGroups = new List<EnemySpawnConfig> 
            {
                new EnemySpawnConfig 
                { 
                    enemyTypeId = 0, // Uses enemyPrefabs[0]
                    count = 6, 
                    maxOnStage = 2,
                    spawnPositionType = SpawnPositionType.RandomArc, 
                    spawnDistanceMin = 12f,
                    spawnDistanceMax = 16f,
                    arcAngleRange = 90f,
                    hpMultiplier = 1.2f,
                    atkMultiplier = 1.1f
                }
            }
        },

        // ------------- WAVE 8 -------------
        new WaveConfig 
        {
            buffsToChoose = 3,
            enemyGroups = new List<EnemySpawnConfig> 
            {
                new EnemySpawnConfig 
                { 
                    enemyTypeId = 0, // Uses enemyPrefabs[0]
                    count = 6, 
                    maxOnStage = 2,
                    spawnPositionType = SpawnPositionType.RandomArc, 
                    spawnDistanceMin = 12f,
                    spawnDistanceMax = 16f,
                    arcAngleRange = 90f,
                    hpMultiplier = 1.5f,
                    atkMultiplier = 1.2f
                }
            }
        },

        // ------------- WAVE 9 -------------
        new WaveConfig 
        {
            buffsToChoose = 3,
            enemyGroups = new List<EnemySpawnConfig> 
            {
                new EnemySpawnConfig 
                { 
                    enemyTypeId = 0, // Uses enemyPrefabs[0]
                    count = 6, 
                    maxOnStage = 3,
                    spawnPositionType = SpawnPositionType.RandomArc, 
                    spawnDistanceMin = 12f, 
                    spawnDistanceMax = 16f,
                    arcAngleRange = 90f,
                    hpMultiplier = 1.5f,
                    atkMultiplier = 1.2f
                }
            }
        },

        // ------------- WAVE 10 -------------
        new WaveConfig 
        {
            enemyGroups = new List<EnemySpawnConfig> 
            {
                new EnemySpawnConfig 
                { 
                    enemyTypeId = 1, // Uses enemyPrefabs[0]
                    count = 1, 
                    spawnPositionType = SpawnPositionType.InFrontOfPlayer, 
                    spawnDistanceMin = 15f,
                    spawnDistanceMax = 15f,
                    hpMultiplier = 2.0f,
                    atkMultiplier = 1.0f
                },
                new EnemySpawnConfig 
                { 
                    enemyTypeId = 0, // Uses enemyPrefabs[0]
                    count = 6, 
                    maxOnStage = 1,
                    spawnPositionType = SpawnPositionType.RandomArc, 
                    spawnDistanceMin = 12f,
                    spawnDistanceMax = 16f,
                    arcAngleRange = 60f,
                    hpMultiplier = 1.5f,
                    atkMultiplier = 1.0f
                }
            }
        },
    };
}
