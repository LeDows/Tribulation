using UnityEngine;
using UnityEngine.UI;

namespace Tribulation.Enemies
{
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private EnemyHealth health;
        [SerializeField] private Image fillImage;

        private Camera targetCamera;

        private void Awake()
        {
            health ??= GetComponentInParent<EnemyHealth>();
            fillImage ??= transform.Find("HealthBarFill")?.GetComponent<Image>();
        }

        private void LateUpdate()
        {
            if (health == null || fillImage == null)
            {
                return;
            }

            fillImage.fillAmount = health.HealthFraction;

            targetCamera ??= Camera.main;
            if (targetCamera != null)
            {
                transform.rotation = targetCamera.transform.rotation;
            }
        }

    }
}
