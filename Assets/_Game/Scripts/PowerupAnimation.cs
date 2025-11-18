using UnityEngine;

public class PowerupAnimation : MonoBehaviour
{
    [Header("Pulse Animation")]
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float scaleAmount = 0.5f;

    private Vector3 _baseScale;
    void OnEnable() {
        _baseScale = transform.localScale;
    }
    private void Update() {
        if(gameObject.activeInHierarchy == false) return;
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float scaleMultiplier = Mathf.Lerp(1f - scaleAmount * 0.5f, 1f + scaleAmount * 0.5f, t);

        transform.localScale = _baseScale * scaleMultiplier;
    }

}
