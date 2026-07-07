using Tribulation.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Tribulation.UI
{
    [RequireComponent(typeof(Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        private Text targetText;

        public string Key
        {
            get => key;
            set
            {
                key = value;
                Refresh();
            }
        }

        private void Awake()
        {
            targetText = GetComponent<Text>();
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            targetText ??= GetComponent<Text>();
            if (targetText != null)
            {
                targetText.text = ConfigCenter.Text(key);
            }
        }
    }
}
