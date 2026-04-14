using UnityEngine;
using TMPro;

public class BuffUI : MonoBehaviour
{
    [Tooltip("The UI text element for the buff's name.")]
    public TMP_Text Name;

    [Tooltip("The UI text element for the buff's description.")]
    public TMP_Text Desc;

    private EnemyManager enemyManager;

    /// <summary>
    /// Call this from the object that generates this UI to set its text.
    /// </summary>
    public void Initialize(string buffName, string buffDesc, EnemyManager manager)
    {
        if (Name != null) Name.text = buffName;
        if (Desc != null) Desc.text = buffDesc;
        this.enemyManager = manager;
    }

    public void OnSelect()
    {
        Debug.Log($"BuffUI OnSelect called.");
        if (enemyManager != null)
        {
            enemyManager.OnBuffSelected(this);
        }
    }
}
