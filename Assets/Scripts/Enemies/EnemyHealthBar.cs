using Tribulation.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Tribulation.Enemies
{
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private EnemyHealth health;
        [SerializeField] private Image fillImage;

        private Camera targetCamera;
        private Canvas targetCanvas;
        private bool shouldShow;

        private void Awake()
        {
            health ??= GetComponentInParent<EnemyHealth>();
            fillImage ??= transform.Find("HealthBarFill")?.GetComponent<Image>();
            targetCanvas = GetComponent<Canvas>();
            ResetDisplay();
        }

        private void OnDisable()
        {
            if (targetCanvas != null)
            {
                targetCanvas.enabled = false;
            }
        }

        public void ResetDisplay()
        {
            targetCanvas ??= GetComponent<Canvas>();
            shouldShow = false;
            if (targetCanvas != null)
            {
                targetCanvas.enabled = false;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = 1f;
            }

            enabled = false;
        }

        public void SetHealthFraction(float healthFraction)
        {
            shouldShow = healthFraction > 0f && healthFraction < 0.999f;
            if (fillImage != null && !Mathf.Approximately(fillImage.fillAmount, healthFraction))
            {
                fillImage.fillAmount = healthFraction;
            }

            enabled = shouldShow;
            if (targetCanvas != null)
            {
                targetCanvas.enabled = shouldShow && GameManager.IsSimulationRunning;
            }
        }

        private void LateUpdate()
        {
            if (!shouldShow || health == null || fillImage == null || targetCanvas == null)
            {
                return;
            }

            if (!GameManager.IsSimulationRunning)
            {
                targetCanvas.enabled = false;
                return;
            }

            if (!targetCanvas.enabled)
            {
                targetCanvas.enabled = true;
            }

            targetCamera ??= Camera.main;
            if (targetCamera != null && transform.rotation != targetCamera.transform.rotation)
            {
                transform.rotation = targetCamera.transform.rotation;
            }
        }

    }
}
