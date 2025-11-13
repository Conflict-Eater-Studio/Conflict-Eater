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

    [SerializeField] private float frightenedDuration = 5f;

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
            _controller.SetState(new ShadowScatterState());
            yield return RunPhase(_scatterDurations[_phaseIndex]);

            _controller.SetState(new ShadowChaseState());
            yield return RunPhase(_chaseDurations[_phaseIndex]);

            _phaseIndex++;
        }

        _controller.SetState(new ShadowChaseState());
        _runningCycle = false;
    }

    /// <summary>
    /// Obs³uguje fazê z mo¿liwoœci¹ przerwania przez stan Frightened.
    /// </summary>
    private IEnumerator RunPhase(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (GameManager.Instance.IsFrightenedShadowState)
            {
                yield return StartCoroutine(HandleFrightenedState());
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Tymczasowo w³¹cza stan przestraszenia i wstrzymuje cykl.
    /// </summary>
    private IEnumerator HandleFrightenedState()
    {
        _controller.SetState(new ShadowFrightenedState());

        yield return new WaitForSeconds(frightenedDuration);

        GameManager.Instance.IsFrightenedShadowState = false;
    }
}
