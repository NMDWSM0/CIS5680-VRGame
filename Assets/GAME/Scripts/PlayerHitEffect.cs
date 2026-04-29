using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerHitEffect : MonoBehaviour
{
    [Tooltip("Peak alpha value when hit.")]
    public float peakAlpha = 0.6f;
    [Tooltip("Time to reach peak alpha.")]
    public float fadeInTime = 0.05f;
    [Tooltip("Time to fade back to 0.")]
    public float fadeOutTime = 0.5f;

    private Material targetMaterial;
    private Coroutine flashCoroutine;
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

    private void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // Clone material so we don't affect other objects sharing this material
            targetMaterial = rend.material;
        }
        else
        {
            Image img = GetComponent<Image>();
            if (img != null)
            {
                // Clone UI material
                targetMaterial = new Material(img.material);
                img.material = targetMaterial;
            }
        }

        // Make sure it starts at 0
        if (targetMaterial != null)
        {
            targetMaterial.SetFloat(AlphaId, 0f);
        }
    }

    public void TriggerHit()
    {
        if (!gameObject.activeInHierarchy || targetMaterial == null) return;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float startAlpha = targetMaterial.GetFloat(AlphaId);
        float elapsed = 0f;

        // Fade in quickly
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInTime);
            targetMaterial.SetFloat(AlphaId, Mathf.Lerp(startAlpha, peakAlpha, t));
            yield return null;
        }

        targetMaterial.SetFloat(AlphaId, peakAlpha);
        elapsed = 0f;

        // Fade out slower
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutTime);
            targetMaterial.SetFloat(AlphaId, Mathf.Lerp(peakAlpha, 0f, t));
            yield return null;
        }

        targetMaterial.SetFloat(AlphaId, 0f);
    }
}
