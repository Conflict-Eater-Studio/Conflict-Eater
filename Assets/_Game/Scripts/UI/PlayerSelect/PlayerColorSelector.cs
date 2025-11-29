using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Color selector UI for a player in the PlayerSelect screen.
/// Players cycle through colors using bumpers/triggers.
/// </summary>
public class PlayerColorSelector : MonoBehaviour
{
    [SerializeField]
    private Image _colorPreview;

    [SerializeField]
    private int _playerIndex = 0; // 0 or 1

    private PlayerColor.ColorType[] _availableColors = new[]
    {
        PlayerColor.ColorType.Red,
        PlayerColor.ColorType.Blue,
        PlayerColor.ColorType.Green,
        PlayerColor.ColorType.Yellow,
        PlayerColor.ColorType.Purple,
        PlayerColor.ColorType.Orange,
    };

    private int _currentColorIndex = 0;

    private void Start()
    {
        // Set default colors based on player index
        _currentColorIndex = _playerIndex == 0 ? 0 : 1; // P1=Red, P2=Blue by default
        UpdateColorDisplay();
        SaveColor();
    }

    /// <summary>
    /// Cycles to the next color in the palette.
    /// </summary>
    public void CycleColorNext()
    {
        _currentColorIndex = (_currentColorIndex + 1) % _availableColors.Length;
        UpdateColorDisplay();
        SaveColor();
        AnimateColorChange();
    }

    /// <summary>
    /// Cycles to the previous color in the palette.
    /// </summary>
    public void CycleColorPrevious()
    {
        _currentColorIndex--;
        if (_currentColorIndex < 0)
            _currentColorIndex = _availableColors.Length - 1;

        UpdateColorDisplay();
        SaveColor();
        AnimateColorChange();
    }

    private void UpdateColorDisplay()
    {
        if (_colorPreview != null)
        {
            PlayerColor.ColorType colorType = _availableColors[_currentColorIndex];
            _colorPreview.color = PlayerColor.GetColor(colorType).Color;
        }
    }

    private void SaveColor()
    {
        PlayerColorManager.SetPlayerColor(_playerIndex, _availableColors[_currentColorIndex]);
    }

    private void AnimateColorChange()
    {
        // Small punch scale for feedback
        if (_colorPreview != null)
        {
            _colorPreview.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);
        }
    }

    public PlayerColor.ColorType GetCurrentColor()
    {
        return _availableColors[_currentColorIndex];
    }
}
