using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.InputSystem;

public class BenchmarkOverlay : MonoBehaviour
{
    [Header("Configuration")]
    public Key toggleKey = Key.F12;
    public Key saveReportKey = Key.F11;
    
    private bool _isVisible = false;
    private bool _isRecording = false;

    // FPS Data
    private float _currentFps;
    private float _minFps = float.MaxValue;
    private float _maxFps = 0f;
    private float _fpsTimer;
    private int _frameCount;
    private List<float> _recordedFps = new List<float>();

    // Memory Data (in MB)
    private float _currentRamMB;
    private float _peakRamMB;
    private float _currentVramMB;
    private float _peakVramMB;

    // Low-level profiler recorders (work in Release Builds)
    private ProfilerRecorder _vramRecorder;

    // GUI Styles
    private GUIStyle _boxStyle;
    private GUIStyle _textStyle;

    // Automatic initialization on game start (no need to manually attach the script to a scene object)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        // Prevent duplicate instances if already placed in the scene manually
        if (FindFirstObjectByType<BenchmarkOverlay>() != null) return;

        GameObject go = new GameObject("SteamBenchmarkOverlay");
        go.AddComponent<BenchmarkOverlay>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        // Fallback if Inspector serialization reset the enum values to Key.None (0)
        if (toggleKey == Key.None) toggleKey = Key.F12;
        if (saveReportKey == Key.None) saveReportKey = Key.F11;
    }

    private void OnEnable()
    {
        // Start tracking graphics driver memory (VRAM)
        _vramRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GfxUsedMemory");
    }

    private void OnDisable()
    {
        // Dispose native recorder to prevent memory leaks
        _vramRecorder.Dispose();
    }

    private void Update()
    {
        // Safe input check preventing ArgumentOutOfRangeException on Key.None
        if (Keyboard.current != null)
        {
            if (IsKeyPressed(toggleKey))
            {
                _isVisible = !_isVisible;
            }

            if ((_isVisible || _isRecording) && IsKeyPressed(saveReportKey))
            {
                SaveReportToFile();
            }
        }

        // Measurements only run when the overlay is visible or recording is active
        if (!_isVisible && !_isRecording) return;

        // Calculate FPS (refreshed every 0.25 seconds for readability)
        _frameCount++;
        _fpsTimer += Time.unscaledDeltaTime;

        if (_fpsTimer >= 0.25f)
        {
            _currentFps = _frameCount / _fpsTimer;
            _frameCount = 0;
            _fpsTimer = 0f;

            if (_isRecording)
            {
                _recordedFps.Add(_currentFps);
                if (_currentFps < _minFps) _minFps = _currentFps;
                if (_currentFps > _maxFps) _maxFps = _currentFps;
            }
        }

        // Measure total RAM reserved by Unity (works in Release Builds!)
        _currentRamMB = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);
        if (_currentRamMB > _peakRamMB)
        {
            _peakRamMB = _currentRamMB;
        }

        // Measure VRAM usage via ProfilerRecorder (with fallback to GraphicsDriver API)
        long vramBytes = (_vramRecorder.Valid && _vramRecorder.LastValue > 0)
            ? _vramRecorder.LastValue
            : Profiler.GetAllocatedMemoryForGraphicsDriver();

        _currentVramMB = vramBytes / (1024f * 1024f);
        if (_currentVramMB > _peakVramMB)
        {
            _peakVramMB = _currentVramMB;
        }
    }

    private bool IsKeyPressed(Key key)
    {
        if (key <= Key.None || Keyboard.current == null) return false;
        return Keyboard.current[key].wasPressedThisFrame;
    }

    private void InitStyles()
    {
        if (_boxStyle == null)
        {
            _boxStyle = new GUIStyle(GUI.skin.box);
            Texture2D bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.85f));
            bg.Apply();
            _boxStyle.normal.background = bg;
        }

        if (_textStyle == null)
        {
            _textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                richText = true
            };
            _textStyle.normal.textColor = Color.white;
        }
    }

    private void OnGUI()
    {
        if (!_isVisible) return;
        InitStyles();

        GUILayout.BeginArea(new Rect(10, 10, 430, 490), _boxStyle);
        GUILayout.BeginVertical();

        GUILayout.Label("<b>=== STEAM SPECS BENCHMARK (F12 Hide) ===</b>", _textStyle);
        GUILayout.Space(5);

        // Hardware specifications
        GUILayout.Label($"<b>OS:</b> {SystemInfo.operatingSystem}", _textStyle);
        GUILayout.Label($"<b>CPU:</b> {SystemInfo.processorType} ({SystemInfo.processorCount} threads)", _textStyle);
        GUILayout.Label($"<b>GPU:</b> {SystemInfo.graphicsDeviceName}", _textStyle);
        GUILayout.Label($"<b>Total VRAM:</b> {SystemInfo.graphicsMemorySize} MB | <b>System RAM:</b> {SystemInfo.systemMemorySize} MB", _textStyle);
        GUILayout.Label($"<b>Resolution:</b> {Screen.currentResolution.width}x{Screen.currentResolution.height} @{Screen.currentResolution.refreshRateRatio.value:F0}Hz", _textStyle);
        
        GUILayout.Space(10);

        // Real-time performance metrics
        string fpsColor = _currentFps >= 60 ? "green" : (_currentFps >= 30 ? "yellow" : "red");
        GUILayout.Label($"<b>Current FPS:</b> <color={fpsColor}>{_currentFps:F1}</color> ({(1000f / Mathf.Max(_currentFps, 0.001f)):F1} ms)", _textStyle);
        GUILayout.Label($"<b>Game RAM (Current / Peak):</b> {_currentRamMB:F1} MB / <color=orange>{_peakRamMB:F1} MB</color>", _textStyle);
        GUILayout.Label($"<b>Game VRAM (Current / Peak):</b> {_currentVramMB:F1} MB / <color=cyan>{_peakVramMB:F1} MB</color>", _textStyle);

        GUILayout.Space(10);

        // Benchmark recording section
        if (_isRecording)
        {
            GUILayout.Label("<color=red><b>● RECORDING BENCHMARK...</b></color>", _textStyle);
            GUILayout.Label($"<b>Average FPS:</b> {GetAverageFps():F1}", _textStyle);
            GUILayout.Label($"<b>Min / Max FPS:</b> {_minFps:F1} / {_maxFps:F1}", _textStyle);
            GUILayout.Label($"<b>1% Low FPS (Stutters):</b> {GetOnePercentLow():F1}", _textStyle);
        }

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(_isRecording ? "Stop Recording" : "Start Recording", GUILayout.Height(30)))
        {
            ToggleRecording();
        }

        if (GUILayout.Button("Save Report (F11)", GUILayout.Height(30)))
        {
            SaveReportToFile();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void ToggleRecording()
    {
        _isRecording = !_isRecording;
        if (_isRecording)
        {
            // Reset recorded metrics when starting a new benchmark session
            _recordedFps.Clear();
            _minFps = float.MaxValue;
            _maxFps = 0f;
            _peakRamMB = _currentRamMB;
            _peakVramMB = _currentVramMB;
        }
    }

    private float GetAverageFps()
    {
        if (_recordedFps.Count == 0) return 0f;
        float sum = 0f;
        foreach (float fps in _recordedFps) sum += fps;
        return sum / _recordedFps.Count;
    }

    // Calculates the average of the lowest 1% FPS samples to detect micro-stutters
    private float GetOnePercentLow()
    {
        if (_recordedFps.Count == 0) return 0f;
        List<float> sorted = new List<float>(_recordedFps);
        sorted.Sort();
        int count = Mathf.Max(1, sorted.Count / 100);
        float sum = 0f;
        for (int i = 0; i < count; i++) sum += sorted[i];
        return sum / count;
    }

    // Exports hardware specs and recorded performance metrics to a local text file
    private void SaveReportToFile()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== GAME PERFORMANCE REPORT ===");
        sb.AppendLine($"Date: {System.DateTime.Now}");
        sb.AppendLine($"OS: {SystemInfo.operatingSystem}");
        sb.AppendLine($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} logical cores)");
        sb.AppendLine($"GPU: {SystemInfo.graphicsDeviceName} (Total VRAM: {SystemInfo.graphicsMemorySize} MB)");
        sb.AppendLine($"System RAM: {SystemInfo.systemMemorySize} MB");
        sb.AppendLine($"Resolution: {Screen.width}x{Screen.height} (FullScreen: {Screen.fullScreen})");
        sb.AppendLine("-------------------------------");
        sb.AppendLine($"Peak Game RAM Usage: {_peakRamMB:F1} MB");
        sb.AppendLine($"Peak Game VRAM Usage: {_peakVramMB:F1} MB");
        
        if (_recordedFps.Count > 0)
        {
            sb.AppendLine($"Average FPS: {GetAverageFps():F1}");
            sb.AppendLine($"Minimum FPS: {_minFps:F1}");
            sb.AppendLine($"1% Low FPS: {GetOnePercentLow():F1}");
            sb.AppendLine($"Maximum FPS: {_maxFps:F1}");
        }
        else
        {
            sb.AppendLine($"Instant FPS at save time: {_currentFps:F1}");
        }

        string path = Path.Combine(Application.persistentDataPath, $"Benchmark_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"Report saved at: {path}");
    }
}