using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class Powerup
{
    public GameObject Prefab;
    public int Count;
}

public class PowerupCollision : MonoBehaviour
{
    [Header("Pulse Animation")]
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float scaleAmount = 0.5f;

    [Header("Shadow Powerup Settings")]
    [SerializeField] private float speedBoost = 10f;
    [SerializeField] private float boostDuration = 5f;

    private Vector3 _baseScale;
    private bool _collected;

    private void OnEnable()
    {
        _baseScale = transform.localScale;
    }

    private void Update()
    {
        if (_collected) return;

        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float scaleMultiplier = Mathf.Lerp(1f - scaleAmount * 0.5f, 1f + scaleAmount * 0.5f, t);

        transform.localScale = _baseScale * scaleMultiplier;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_collected) return;

        // Light powerup
        if (other.CompareTag("PlayerLight"))
        {
            Debug.Log("Light Powerup collected!");
            _collected = true;

            GameManager.Instance.IsFrightenedShadowState = true;
            Destroy(gameObject);
            return;
        }

        // Shadow powerup
        if (other.TryGetComponent<ShadowController>(out var controller))
        {
            if (controller.CurrentState is ShadowActiveState)
            {
                Debug.Log("Shadow Powerup collected!");
                _collected = true;
                StartCoroutine(BoostShadowSpeed(controller));
                Destroy(gameObject);
            }
        }
    }

    private IEnumerator BoostShadowSpeed(ShadowController controller)
    {
        float originalSpeed = controller.Movement.SpeedMult;

        controller.Movement.SpeedMult = speedBoost;

        yield return new WaitForSeconds(boostDuration);

        // Reset (but only if the shadow still exists)
        if (controller != null)
            controller.Movement.SpeedMult = originalSpeed;
    }
}
