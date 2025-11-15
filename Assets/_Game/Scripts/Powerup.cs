using UnityEngine;

public class Powerup : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.CompareTag("PlayerLight")) {
            Debug.Log("Light Powerup collected!");
        }
        if (other.TryGetComponent(typeof(ShadowController), out Component component)) {
            ShadowController _controller = (ShadowController) component;
            if(!_controller.IsActive) return;
            _controller.Movement.SpeedMult = 10f;
        }
        
    }
}
