using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InGameUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Image healthBarFill;
    public TextMeshProUGUI ammoText;
    public Image crosshair;

    // Hàm để đồng đội (code nhân vật) gọi khi bị bắn trúng
    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = currentHealth / maxHealth;
        }
    }

    // Hàm để đồng đội gọi khi bắn hoặc thay đạn
    public void UpdateAmmo(int currentAmmo, int maxAmmo)
    {
        if (ammoText != null)
        {
            ammoText.text = currentAmmo + " / " + maxAmmo;
        }
    }
}