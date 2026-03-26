using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InGameUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Image healthBarFill;
    public TextMeshProUGUI ammoText;
    public Image crosshair;

    [Header("Health Bar Flash Settings")]
    public float lowHealthThreshold = 50f;
    public float maxFlashSpeed = 8f;  // Flash frequency at critical health
    public float minFlashSpeed = 2f;  // Flash frequency at threshold

    private float currentHealth;
    private float maxHealth;
    private bool isFlashing;
    private float flashTimer;
    private Color originalHealthBarColor;

    private void Start()
    {
        if (healthBarFill != null)
        {
            originalHealthBarColor = healthBarFill.color;
        }
    }

    private void Update()
    {
        if (isFlashing && healthBarFill != null)
        {
            flashTimer += Time.deltaTime;
            
            // Calculate flash speed based on health (lower = faster flash)
            float healthPercent = currentHealth / maxHealth;
            float flashSpeed = Mathf.Lerp(maxFlashSpeed, minFlashSpeed, healthPercent);
            
            // Create pulsing effect using sine wave (0.5 to 1.0 alpha range)
            float alpha = Mathf.Sin(flashTimer * flashSpeed * Mathf.PI) * 0.5f + 0.5f;
            
            Color flashColor = originalHealthBarColor;
            flashColor.a = alpha;
            healthBarFill.color = flashColor;
        }
    }

    // Hàm để đồng đội (code nhân vật) gọi khi bị bắn trúng
    public void UpdateHealth(float newHealth, float max)
    {
        currentHealth = newHealth;
        maxHealth = max;

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = currentHealth / maxHealth;
        }

        // Check if should flash based on threshold
        if (currentHealth < lowHealthThreshold && currentHealth > 0)
        {
            if (!isFlashing)
            {
                isFlashing = true;
                flashTimer = 0f;
            }
        }
        else
        {
            // Stop flashing and restore normal appearance
            if (isFlashing)
            {
                isFlashing = false;
                if (healthBarFill != null)
                {
                    Color color = originalHealthBarColor;
                    color.a = 1f;
                    healthBarFill.color = color;
                }
            }
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