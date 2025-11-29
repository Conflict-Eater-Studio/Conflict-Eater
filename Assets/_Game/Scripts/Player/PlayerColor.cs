using System;
using UnityEngine;

/// <summary>
/// Defines available player colors and manages player color assignments.
/// </summary>
[Serializable]
public class PlayerColor
{
    public enum ColorType
    {
        Red,
        Blue,
        Green,
        Yellow,
        Purple,
        Orange,
    }

    public ColorType Type;
    public Color Color;

    public PlayerColor(ColorType type, Color color)
    {
        Type = type;
        Color = color;
    }

    // Predefined color palette
    public static PlayerColor GetColor(ColorType type)
    {
        return type switch
        {
            ColorType.Red => new PlayerColor(ColorType.Red, new Color(0.9f, 0.2f, 0.2f)),
            ColorType.Blue => new PlayerColor(ColorType.Blue, new Color(0.2f, 0.4f, 0.9f)),
            ColorType.Green => new PlayerColor(ColorType.Green, new Color(0.2f, 0.8f, 0.3f)),
            ColorType.Yellow => new PlayerColor(ColorType.Yellow, new Color(1f, 0.9f, 0.2f)),
            ColorType.Purple => new PlayerColor(ColorType.Purple, new Color(0.7f, 0.2f, 0.9f)),
            ColorType.Orange => new PlayerColor(ColorType.Orange, new Color(1f, 0.5f, 0.1f)),
            _ => new PlayerColor(ColorType.Red, Color.white),
        };
    }
}

/// <summary>
/// Stores player color choices for both players.
/// </summary>
public static class PlayerColorManager
{
    public static PlayerColor.ColorType Player1Color { get; set; } = PlayerColor.ColorType.Red;
    public static PlayerColor.ColorType Player2Color { get; set; } = PlayerColor.ColorType.Blue;

    public static Color GetPlayer1Color() => PlayerColor.GetColor(Player1Color).Color;

    public static Color GetPlayer2Color() => PlayerColor.GetColor(Player2Color).Color;

    public static void SetPlayerColor(int playerIndex, PlayerColor.ColorType colorType)
    {
        if (playerIndex == 0)
            Player1Color = colorType;
        else if (playerIndex == 1)
            Player2Color = colorType;
    }
}
