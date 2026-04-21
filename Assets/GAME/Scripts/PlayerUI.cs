using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class StatUI
{
    [Tooltip("Slider for visualizing the property value.")]
    public Slider slider;

    [Tooltip("Text to show the current value (e.g. 'Current 100').")]
    public TMP_Text currentText;

    [Tooltip("Text to show the max value (e.g. 'Max 100').")]
    public TMP_Text maxText;

    public void UpdateUI(float current, float max)
    {
        if (slider != null)
        {
            slider.maxValue = max;
            slider.value = current;
        }

        if (currentText != null)
        {
            currentText.text = $"Current {current:F1}";
        }

        if (maxText != null)
        {
            maxText.text = $"Max {max:F0}";
        }
    }
}

public class PlayerUI : MonoBehaviour
{
    [Tooltip("Reference to the player's status script.")]
    public PlayerStatus playerStatus;

    [Tooltip("UI elements for Health (HP).")]
    public StatUI hpUI;

    [Tooltip("UI elements for Ammunition.")]
    public StatUI ammoUI;

    void Update()
    {
        if (playerStatus != null)
        {
            hpUI.UpdateUI(playerStatus.health, playerStatus.maxHealth);
            ammoUI.UpdateUI(playerStatus.ammo, playerStatus.maxAmmo);
        }
    }
}
