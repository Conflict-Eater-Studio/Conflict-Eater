using UnityEngine;

public class Powerup : MonoBehaviour
{
    [SerializeField] PowerupAnimation _animation;
    [SerializeField] PowerupCollision _collision;
    
    public void PickUp()
    {
        
    }
    public void Deactivate() {
        throw new System.NotImplementedException();
    }
}
