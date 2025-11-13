using System.Collections;
using UnityEngine;

public class ShadowBehaviorCycle : MonoBehaviour
{
    private ShadowController _controller;

    private readonly float[] _scatterDurations = { 7f, 7f, 5f, 5f };
    private readonly float[] _chaseDurations = { 10f, 10f, 15f, 9999f };

    private int _phaseIndex = 0;
    private bool _runningCycle = false;

    private void Start()
    {
        _controller = GetComponent<ShadowController>();
    }

    public void StartBehaviorCycle()
    {
        if (!_runningCycle)
            StartCoroutine(BehaviorCycleRoutine());
    }

    private IEnumerator BehaviorCycleRoutine()
    {
        _runningCycle = true;

        while (_phaseIndex < _scatterDurations.Length)
        {
            _controller.SetState(new ShadowScatterState());
            yield return new WaitForSeconds(_scatterDurations[_phaseIndex]);

            _controller.SetState(new ShadowChaseState());
            yield return new WaitForSeconds(_chaseDurations[_phaseIndex]);

            _phaseIndex++;
        }

        _controller.SetState(new ShadowChaseState());
        _runningCycle = false;
    }
}

