using UnityEngine;

public class PulseBullet : MonoBehaviour
{
    private float maxDistance = 100f;
    private float bulletDamage = 0f;
    private bool penetrate = false;
    private PlayerStatus playerStatus = null;
    private float penetrationDamageMulti = 0f;
    private float speed = 40f;

    private float traveledDistance = 0f;
    private bool hitFirstEnemy = false;
    private bool isInitialized = false;

    public void Initialize(float damage, bool penetrateBullets, PlayerStatus status, float penDamageMulti, float pulseSpeed)
    {
        this.bulletDamage = damage;
        this.penetrate = penetrateBullets;
        this.playerStatus = status;
        this.penetrationDamageMulti = penDamageMulti;
        this.speed = pulseSpeed;
        
        SetupVisuals();
        isInitialized = true;
    }

    private void SetupVisuals()
    {
        // Auto-generate visual if there are no children
        if (transform.childCount == 0)
        {
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(cyl.GetComponent<Collider>());
            cyl.transform.SetParent(this.transform);
            
            // Align to face forward (Z axis)
            cyl.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            
            // Make it a short, thin segment to look like a pulse/blaster bolt
            cyl.transform.localScale = new Vector3(0.04f, 0.4f, 0.04f); 
            cyl.transform.localPosition = Vector3.zero;

            Renderer rend = cyl.GetComponent<Renderer>();
            if (rend != null)
            {
                // Make it glow cyan to distinguish from the red laser
                rend.material.color = Color.cyan;
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", Color.cyan * 2f);
            }
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        float moveDistance = speed * Time.deltaTime;
        
        // Raycast ahead to see if we hit anything this frame
        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, moveDistance);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit h in hits)
        {
            string layerName = LayerMask.LayerToName(h.collider.gameObject.layer);
            
            // Ignore these layers
            if (layerName == "UI" || layerName == "Player" || layerName == "EnemyBullet" || layerName == "PlayerBullet" || layerName == "Shield")
            {
                continue;
            }

            // If we hit an enemy
            if (h.collider.CompareTag("Enemy") || h.collider.CompareTag("Gun"))
            {
                IEnemy enemyScript = h.collider.GetComponentInParent<IEnemy>();
                if (enemyScript != null)
                {
                    float dmg = hitFirstEnemy ? (bulletDamage * penetrationDamageMulti) : bulletDamage;
                    float realDamage = enemyScript.Hit(dmg, h.collider.gameObject);
                    hitFirstEnemy = true;
                    
                    if (playerStatus != null)
                    {
                        playerStatus.OnDamageDealt(realDamage);
                    }
                }
            }

            // Stop the bullet if it does not penetrate
            if (!penetrate)
            {
                // Move position strictly to the hit point before destroying (useful if there's an explosion effect later)
                transform.position = h.point;
                Destroy(gameObject);
                return;
            }
        }

        // Move the bullet forward
        transform.position += transform.forward * moveDistance;
        traveledDistance += moveDistance;

        // Destroy if it flew too far
        if (traveledDistance >= maxDistance)
        {
            Destroy(gameObject);
        }
    }
}
