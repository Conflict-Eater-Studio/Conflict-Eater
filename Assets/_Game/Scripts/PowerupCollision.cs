using System;
using System.Collections;
using UnityEngine;

public class PowerupCollision : MonoBehaviour
{
    [Header("Shadow Powerup Settings")]
    [SerializeField] private float speedBoost = 10f;
    [SerializeField] private float boostDuration = 5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!gameObject.activeInHierarchy) return;
        
        if (other.CompareTag("PlayerLight")) {
            GameManager.Instance.IsFrightenedShadowState = true;
            gameObject.SetActive(false);
            return;
        }
        if (other.TryGetComponent<ShadowController>(out var controller)) {
            if (controller.CurrentState is ShadowActiveState) {
                StartCoroutine(BoostShadowSpeed(controller));
                gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator BoostShadowSpeed(ShadowController controller)
    {
        if (controller == null) yield break;
        float originalSpeed = controller.Movement.GetSpeed();
        
        controller.Movement.SetSpeed(13f);

        yield return new WaitForSeconds(boostDuration);

        if (controller != null)
            controller.Movement.SetSpeed(originalSpeed);
    }
}
