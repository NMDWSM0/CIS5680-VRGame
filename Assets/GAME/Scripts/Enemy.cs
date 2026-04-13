using UnityEngine;
using TMPro;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    [Tooltip("Starting health of the enemy.")]
    public float health = 100f;

    [Header("UI Setup")]
    [Tooltip("Optional: Assign a 3D TextMeshPro object here. If left null, UI will be auto-generated.")]
    public TextMeshPro healthText;

    [Tooltip("The position of the generated health UI relative to the enemy.")]
    public Vector3 healthUiPosition = new Vector3(0, 1.6f, 0);

    [Tooltip("Should it generate a physical health bar?")]
    public bool generateHealthBar = true;

    private float maxHealth;
    private Transform uiRoot; // A parent object to hold both the text and the bar
    private Transform healthBarForeground; // The green part of the bar that shrinks

    private void Start()
    {
        maxHealth = health;

        // If the user hasn't manually assigned a healthText, we generate the UI hierarchy automatically
        if (healthText == null)
        {
            uiRoot = new GameObject("FloatingHealthUI").transform;
            uiRoot.SetParent(this.transform);
            uiRoot.localPosition = healthUiPosition;

            // 1. Generate Text
            GameObject textObj = new GameObject("HealthText");
            textObj.transform.SetParent(uiRoot);
            textObj.transform.localPosition = Vector3.zero; 
            
            healthText = textObj.AddComponent<TextMeshPro>();
            healthText.alignment = TextAlignmentOptions.Center;
            healthText.fontSize = 3;
            healthText.color = Color.white;
            healthText.outlineWidth = 0.2f;
            healthText.outlineColor = Color.black;

            // 2. Generate Health Bar
            if (generateHealthBar)
            {
                // Shift text up slightly to make room for the bar
                textObj.transform.localPosition = new Vector3(0, 0.2f, 0);

                // Background Bar (Black)
                GameObject bgBar = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(bgBar.GetComponent<Collider>());
                bgBar.transform.SetParent(uiRoot);
                bgBar.transform.localPosition = Vector3.zero;
                bgBar.transform.localScale = new Vector3(1.0f, 0.15f, 1f);
                bgBar.GetComponent<Renderer>().material.color = Color.black;

                // Foreground Bar (Green)
                GameObject fgBar = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(fgBar.GetComponent<Collider>());
                fgBar.transform.SetParent(uiRoot);
                
                // Moved slightly "forward" (-0.01 on local Z) so it renders cleanly in front of the black background
                fgBar.transform.localPosition = new Vector3(0, 0, -0.01f);
                fgBar.transform.localScale = new Vector3(1.0f, 0.15f, 1f);
                
                Renderer fgRend = fgBar.GetComponent<Renderer>();
                fgRend.material.color = Color.green;
                fgRend.material.EnableKeyword("_EMISSION");
                fgRend.material.SetColor("_EmissionColor", Color.green * 0.5f);

                healthBarForeground = fgBar.transform;
            }
        }
        else
        {
            // If they provided their own text, we just make it the root for billboarding
            uiRoot = healthText.transform; 
        }

        UpdateHealthUI();
    }

    private void Update()
    {
        // Rotate the entire UI block to face the physical player coordinates
        if (uiRoot != null && Camera.main != null)
        {
            Vector3 directionToFace = uiRoot.position - Camera.main.transform.position;
            
            if (directionToFace != Vector3.zero)
            {
                uiRoot.rotation = Quaternion.LookRotation(directionToFace);
            }
        }
    }

    /// <summary>
    /// Called when the laser hits this enemy.
    /// </summary>
    public void Hit(float damage)
    {
        health -= damage;
        Debug.Log($"Enemy '{gameObject.name}' took {damage} damage! Remaining health: {health}");

        UpdateHealthUI();

        if (health <= 0)
        {
            Death();
        }
    }

    /// <summary>
    /// Refreshes the visual text and the health bar scale
    /// </summary>
    private void UpdateHealthUI()
    {
        float clampedHealth = Mathf.Max(0, health);

        // Update Text
        if (healthText != null)
        {
            healthText.text = $"{clampedHealth} / {maxHealth}";
        }

        // Update Bar
        if (healthBarForeground != null)
        {
            float healthPercent = clampedHealth / maxHealth;
            
            // Scale the foreground bar down horizontally
            healthBarForeground.localScale = new Vector3(healthPercent, 0.15f, 1f);
            
            // Because standard quads scale from their center, we move the local X position 
            // slightly to the left proportionally so the bar depletes from Right-to-Left,
            // staying anchored at the left edge.
            float offset = (1f - healthPercent) / 2f;
            healthBarForeground.localPosition = new Vector3(-offset, 0, -0.01f);
            
            // Optional: change color to red when low on health
            if (healthPercent < 0.3f)
            {
                Renderer fgRend = healthBarForeground.GetComponent<Renderer>();
                fgRend.material.color = Color.red;
                fgRend.material.SetColor("_EmissionColor", Color.red * 0.5f);
            }
        }
    }

    /// <summary>
    /// Handles the enemy's logic when health reaches 0.
    /// </summary>
    private void Death()
    {
        Debug.Log($"Enemy '{gameObject.name}' died. Destroying it...");
        Destroy(gameObject);
    }
}
