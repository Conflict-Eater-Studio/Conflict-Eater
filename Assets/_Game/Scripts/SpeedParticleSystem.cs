using UnityEngine;

enum SpeedParticleSystemState {
    SkullBottomAnimation,
    SkullUpAnimation,
    SkullRightAnimation,
    SkullLeftAnimation
}
public class SpeedParticleSystem : MonoBehaviour {
    [SerializeField] private Animator _animator;
    [SerializeField] private ShadowController _shadowController;
    [SerializeField] private TrailRenderer _trail;
    public void PlayFor(float duration) {
        _trail.Clear();
        _trail.enabled = true;
        Invoke(nameof(Stop), duration);
    }
    private void Stop() {
        _trail.enabled = false;
    }
}
