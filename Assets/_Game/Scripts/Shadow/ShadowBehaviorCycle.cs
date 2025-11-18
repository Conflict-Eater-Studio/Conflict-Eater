using System.Collections;
using UnityEngine;

public class ShadowBehaviorCycle : MonoBehaviour
{
    private ShadowController _controller;
    private Coroutine _cycleRoutine;

    private readonly float[] _scatterDurations = { 7f, 7f, 5f, 5f };
    private readonly float[] _chaseDurations = { 10f, 10f, 15f, 9999f };

    private int _phaseIndex = 0;
    private bool _runningCycle = false;

    [SerializeField] private float frightenedDuration = 60f;

    private void Start()
    {
        _controller = GetComponent<ShadowController>();
    }

    public void StartBehaviorCycle()
    {
        if (!_runningCycle)
            _cycleRoutine = StartCoroutine(BehaviorCycleRoutine());
    }

    private IEnumerator BehaviorCycleRoutine()
    {
        _runningCycle = true;

        while (_phaseIndex < _scatterDurations.Length)
        {
            yield return RunPhaseWithInterrupt(
                new ShadowScatterState(),
                _scatterDurations[_phaseIndex]
            );

            yield return RunPhaseWithInterrupt(
                new ShadowChaseState(),
                _chaseDurations[_phaseIndex]
            );

            _phaseIndex++;
        }

        yield return RunPhaseWithInterrupt(
            new ShadowChaseState(),
            Mathf.Infinity
        );

        _runningCycle = false;
    }

    private IEnumerator RunPhaseWithInterrupt(IShadowState phaseState, float duration)
    {
        float elapsed = 0f;
        if (!_controller.IsShadowActive && _controller.CurrentState.State != ShadowState.Eaten)
        {
            _controller.SetState(phaseState);
        }

        while (elapsed < duration)
        {
            if (GameManager.Instance.IsFrightenedShadowState && _controller.CurrentState.State != ShadowState.Eaten)
            {
                yield return StartCoroutine(HandleFrightenedState());
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator HandleFrightenedState()
    {
        if (_controller.CurrentState.State == ShadowState.Eaten)
            yield break;

        IShadowState previousState = null;

        if (!_controller.IsShadowActive)
            previousState = _controller.CurrentState;

        if(!_controller.IsShadowActive && _controller.CurrentState.State != ShadowState.Eaten)
            _controller.SetState(new ShadowFrightenedState());

        yield return new WaitForSeconds(frightenedDuration);

        GameManager.Instance.IsFrightenedShadowState = false;

        if (previousState != null && !_controller.IsShadowActive && _controller.CurrentState.State != ShadowState.Eaten)
        {
            _controller.SetState(previousState);
        } else if(!_controller.IsShadowActive && _controller.CurrentState.State != ShadowState.Eaten)
        {
            _controller.SetState(new ShadowScatterState());
        }
    }
}
