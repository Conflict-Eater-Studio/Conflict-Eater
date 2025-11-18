using UnityEngine;

public class Powerup : MonoBehaviour
{
    [SerializeField] PowerupAnimation _animation;
    [SerializeField] PowerupCollision _collision;
    
    public void Deactivate() {
        gameObject.SetActive(false);
    }
    public void Activate() {
        gameObject.SetActive(true);
    }
}
