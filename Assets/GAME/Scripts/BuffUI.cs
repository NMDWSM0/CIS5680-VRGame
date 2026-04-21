using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffUI : MonoBehaviour
{
    [Tooltip("The UI text element for the buff's name.")]
    public TMP_Text Name;

    [Tooltip("The UI text element for the buff's description.")]
    public TMP_Text Desc;

    [Header("UI Components")]
    public Image image1;
    public Image image2;
    public Image image3;
    public Button selectButton;

    private EnemyManager enemyManager;
    public Buff buff;

    /// <summary>
    /// Call this from the object that generates this UI to set its text.
    /// </summary>
    public void Initialize(Buff newBuff, EnemyManager manager)
    {
        this.buff = newBuff;
        if (Name != null) Name.text = buff.name;
        if (Desc != null) Desc.text = buff.description;
        this.enemyManager = manager;
        
        ApplyRarityColors();
    }

    private void ApplyRarityColors()
    {
        float targetHue = 0f;
        switch (buff.rarity)
        {
            case BuffRarity.Green: targetHue = 120f / 360f; break;
            case BuffRarity.Blue: targetHue = 206f / 360f; break;
            case BuffRarity.Purple: targetHue = 275f / 360f; break;
            case BuffRarity.Gold: targetHue = 45f / 360f; break;
        }

        if (image1 != null) image1.color = ChangeHue(image1.color, targetHue);
        if (image2 != null) image2.color = ChangeHue(image2.color, targetHue);
        if (image3 != null) image3.color = ChangeHue(image3.color, targetHue);

        if (selectButton != null)
        {
            ColorBlock cb = selectButton.colors;
            cb.normalColor = ChangeHue(cb.normalColor, targetHue);
            cb.highlightedColor = ChangeHue(cb.highlightedColor, targetHue);
            cb.pressedColor = ChangeHue(cb.pressedColor, targetHue);
            cb.selectedColor = ChangeHue(cb.selectedColor, targetHue);
            selectButton.colors = cb;
        }
    }

    private Color ChangeHue(Color original, float newHue)
    {
        float h, s, v;
        Color.RGBToHSV(original, out h, out s, out v);
        Color newColor = Color.HSVToRGB(newHue, s, v);
        newColor.a = original.a;
        return newColor;
    }

    public void OnSelect()
    {
        Debug.Log($"BuffUI OnSelect called for {buff.name}.");
        
        PlayerStatus playerStatus = FindObjectOfType<PlayerStatus>();
        RightGun rightGun = FindObjectOfType<RightGun>();
        Shield shield = FindObjectOfType<Shield>();
        
        if (playerStatus != null)
        {
            switch (buff.type)
            {
                case BuffType.Heal:
                    playerStatus.Heal(buff.amount);
                    break;
                case BuffType.MaxHealth:
                    playerStatus.maxHealth += buff.amount;
                    playerStatus.Heal(buff.amount);
                    break;
                case BuffType.DamageBoost:
                    playerStatus.damageMultiplier += buff.amount;
                    break;
                case BuffType.CritChance:
                    playerStatus.critRate += buff.amount;
                    break;
                case BuffType.HealthSteal:
                    playerStatus.lifeSteal += buff.amount;
                    break;
                case BuffType.AmmoEfficiency:
                    playerStatus.ammoEfficiency += buff.amount;
                    break;
                case BuffType.AutoAmmoRecharge:
                    playerStatus.autoAmmoRecharge += buff.amount;
                    break;
                case BuffType.ShieldSize:
                    if (shield != null)
                    {
                        Vector3 currentScale = shield.transform.localScale;
                        shield.transform.localScale = new Vector3(
                            currentScale.x * (1f + buff.amount),
                            currentScale.y,
                            currentScale.z * (1f + buff.amount)
                        );
                    }
                    break;
                case BuffType.Penetration:
                    if (rightGun != null)
                    {
                        rightGun.penetrate = true;
                        rightGun.penetrationDamage += buff.amount;
                    }
                    break;
                case BuffType.Multishot:
                    if (rightGun != null)
                    {
                        rightGun.multishot += (int)buff.amount;
                        rightGun.multishot = Mathf.Min(7, rightGun.multishot);
                    }
                    break;
                case BuffType.HomingLasers:
                    if (rightGun != null)
                    {
                        rightGun.homing = true;
                    }
                    break;
            }
        }

        if (enemyManager != null)
        {
            enemyManager.OnBuffSelected(this);
        }
    }
}
