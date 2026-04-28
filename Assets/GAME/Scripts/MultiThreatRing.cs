using UnityEngine;
using System.Collections.Generic;

public class MultiThreatRing : MonoBehaviour
{
    public Material ringMaterial;

    public List<Transform> activeEnemies = new List<Transform>();

    public Transform ringObj;

    private static readonly int EnemyDirsId = Shader.PropertyToID("_EnemyDirs");
    private static readonly int EnemyCountId = Shader.PropertyToID("_EnemyCount");
    private static readonly int PlayerDirId = Shader.PropertyToID("_PlayerDir");
    
    private const int MAX_ENEMIES = 10;
    private Vector4[] enemyDirsArray = new Vector4[MAX_ENEMIES];

    public float yOffset = -0.5f;

    void Update()
    {
        if (ringObj != null && Camera.main != null)
        {
            Vector3 camPos = Camera.main.transform.position;
            ringObj.position = new Vector3(camPos.x, camPos.y + yOffset, camPos.z);
        }
        if (ringMaterial == null || activeEnemies == null) return;

        int count = Mathf.Min(activeEnemies.Count, MAX_ENEMIES);

        for (int i = 0; i < count; i++)
        {
            if (activeEnemies[i] == null) continue;

            Vector3 worldDirToEnemy = activeEnemies[i].position - ringObj.position;
            
            worldDirToEnemy.y = 0;
            worldDirToEnemy.Normalize();

            Vector3 localDir = transform.InverseTransformDirection(worldDirToEnemy);

            enemyDirsArray[i] = new Vector4(localDir.x, localDir.z, 0, 0);
        }

        ringMaterial.SetInt(EnemyCountId, count);
        ringMaterial.SetVectorArray(EnemyDirsId, enemyDirsArray);
        
        Vector3 pForward = Camera.main.transform.forward;
        pForward.y = 0;
        pForward.Normalize();
        Vector3 localPForward = transform.InverseTransformDirection(pForward);
        ringMaterial.SetVector(PlayerDirId, new Vector4(localPForward.x, localPForward.z, 0, 0));
    }
}