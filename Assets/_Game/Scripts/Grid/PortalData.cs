using System;
using UnityEngine;

/// <summary>
/// Serializable data class for storing portal configuration in the editor.
/// Portal GameObjects are instantiated from this data at runtime.
/// </summary>
[Serializable]
public class PortalData
{
    [Tooltip("Unique portal ID - portals with the same ID are linked")]
    public uint PortalId;

    [Tooltip("Cell position of the portal on the grid")]
    public Vector2Int CellPosition;

    [Tooltip("Optional color for the portal (for visual distinction)")]
    public Color PortalColor = Color.white;

    [Tooltip("Cooldown time in seconds before the portal can be used again")]
    public float PortalCooldown = 1.0f;

    public PortalData(uint portalId, Vector2Int cellPosition, float cooldown = 1.0f)
    {
        PortalId = portalId;
        CellPosition = cellPosition;
        PortalCooldown = cooldown;
    }

    public PortalData(uint portalId, Vector2Int cellPosition, Color color, float cooldown = 1.0f)
    {
        PortalId = portalId;
        CellPosition = cellPosition;
        PortalColor = color;
        PortalCooldown = cooldown;
    }
}
