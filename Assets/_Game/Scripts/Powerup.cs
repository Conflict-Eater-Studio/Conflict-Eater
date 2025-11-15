using UnityEngine;

public class Powerup : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.CompareTag("PlayerLight")) {
            Debug.Log("Light Powerup collected!");
            GameManager.Instance.IsFrightenedShadowState = true;
        }
        if (other.TryGetComponent(typeof(ShadowController), out Component component)) {
            Debug.Log("Shadow Powerup collected!");
            ShadowController _controller = (ShadowController) component;
            _controller.Movement.SpeedMult = 10f;
        }
        
    }
}
