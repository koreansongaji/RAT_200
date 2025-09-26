using UnityEngine;
using UnityEngine.UI;

public class NoiseUIBinder : MonoBehaviour
{
    public Slider slider;
    public Image fillImage;

    void Start()
    {
        var ns = NoiseSystem.Instance; if (!ns) return;
        ns.OnValueChanged += HandleChanged;
        HandleChanged(ns.current01);
    }
    void OnDestroy() { var ns = NoiseSystem.Instance; if (ns != null) ns.OnValueChanged -= HandleChanged; }
    void HandleChanged(float v) { if (slider) slider.value = v; if (fillImage) fillImage.fillAmount = v; }
}
