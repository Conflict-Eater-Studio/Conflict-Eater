using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(Grid))]
public class GridEditor : Editor
{
    #region Styles
    private readonly GUIStyle _styleHeader = new GUIStyle();
    #endregion

    private Grid _grid;
    private bool _isSelectingLightExclusion = false;
    private bool _isSelectingLightSpawns = false;
    private bool _isSelectingShadowSpawn = false;
    private bool _isSelectingPowerUpSpawns = false;
    private bool _isSelectingScatterTargets = false;
    private bool _isSelectingshadowBlockedCells = false;
    private bool _isSelectingHomeTargets = false;
    private bool _isSelectingPortals = false;

    // Serialized properties
    private SerializedProperty _tilemapWalls;
    private SerializedProperty _tilemapFloors;
    private SerializedProperty _tilemapLight;
    private SerializedProperty _tilemapPortals;
    private SerializedProperty _tilemapInactiveLight;
    private SerializedProperty _lightTile;
    private SerializedProperty _portalTile;
    private SerializedProperty _portalPrefab;
    private SerializedProperty _portalData;
    private SerializedProperty _lightInactiveTile;
    private SerializedProperty _lightExclusion;
    private SerializedProperty _lightSpawnCells;
    private SerializedProperty _shadowSpawnCell;
    private SerializedProperty _powerUpSpawnCells;
    private SerializedProperty _scatterTargets;
    private SerializedProperty _shadowBlockedCells;
    private SerializedProperty _homeTargets;

    private void OnEnable()
    {
        _styleHeader.fontSize = 20;
        _styleHeader.fontStyle = FontStyle.Bold;
        _styleHeader.normal.textColor = Color.white;
        _styleHeader.alignment = TextAnchor.MiddleCenter;

        _grid = (Grid)target;

        // Get serialized properties
        _tilemapWalls = serializedObject.FindProperty("_tilemapWalls");
        _tilemapFloors = serializedObject.FindProperty("_tilemapFloors");
        _tilemapLight = serializedObject.FindProperty("_tilemapLight");
        _tilemapInactiveLight = serializedObject.FindProperty("_tilemapInactiveLight");
        _tilemapPortals = serializedObject.FindProperty("_tilemapPortals");
        _lightTile = serializedObject.FindProperty("_lightTile");
        _portalTile = serializedObject.FindProperty("_portalTile");
        _portalPrefab = serializedObject.FindProperty("_portalPrefab");
        _portalData = serializedObject.FindProperty("_portalData");
        _lightInactiveTile = serializedObject.FindProperty("_lightInactiveTile");
        _lightExclusion = serializedObject.FindProperty("_lightExclusion");
        _lightSpawnCells = serializedObject.FindProperty("_lightSpawnCells");
        _shadowSpawnCell = serializedObject.FindProperty("_shadowSpawnCell");
        _powerUpSpawnCells = serializedObject.FindProperty("_powerUpSpawnCells");
        _scatterTargets = serializedObject.FindProperty("_scatterTargets");
        _shadowBlockedCells = serializedObject.FindProperty("_shadowBlockedCells");
        _homeTargets = serializedObject.FindProperty("_homeTargets");

        // Subscribe to scene view events
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        // Unsubscribe from scene view events
        SceneView.duringSceneGui -= OnSceneGUI;

        // Turn off any active selection modes
        _isSelectingLightExclusion = false;
        _isSelectingLightSpawns = false;
        _isSelectingShadowSpawn = false;
        _isSelectingPowerUpSpawns = false;
        _isSelectingPortals = false;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw default inspector
        EditorGUILayout.LabelField("Grid Settings", _styleHeader);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(_tilemapWalls);
        EditorGUILayout.PropertyField(_tilemapFloors);
        EditorGUILayout.PropertyField(_tilemapLight);
        EditorGUILayout.PropertyField(_tilemapPortals);
        EditorGUILayout.PropertyField(_tilemapInactiveLight);
        EditorGUILayout.PropertyField(_lightTile);
        EditorGUILayout.PropertyField(_portalTile);
        EditorGUILayout.PropertyField(_portalPrefab);
        EditorGUILayout.PropertyField(_lightInactiveTile);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor Tools", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Use the buttons below to interact with the tilemap in the Scene view. "
                + "Click on tiles to add/remove them from lists or set spawn points.",
            MessageType.Info
        );

        // Light Exclusion Section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Light Exclusion Tiles", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = _isSelectingLightExclusion
            ? new Color(0.369f, 1f, 0.357f, 1f)
            : Color.white;
        if (
            GUILayout.Button(
                _isSelectingLightExclusion ? "Stop Selecting" : "Add/Remove",
                GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.5f - 10)
            )
        )
        {
            _isSelectingLightExclusion = !_isSelectingLightExclusion;
            _isSelectingLightSpawns = false;
            _isSelectingShadowSpawn = false;
            _isSelectingPowerUpSpawns = false;
            _isSelectingPortals = false;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        if (
            GUILayout.Button(
                "Clear All",
                GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.5f - 10)
            )
        )
        {
            if (
                EditorUtility.DisplayDialog(
                    "Clear Light Exclusions",
                    "Are you sure you want to clear all light exclusion tiles?",
                    "Yes",
                    "No"
                )
            )
            {
                _lightExclusion.ClearArray();
                serializedObject.ApplyModifiedProperties();
                SceneView.RepaintAll();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (_isSelectingLightExclusion)
        {
            EditorGUILayout.HelpBox(
                "Click on floor tiles in the Scene view to add/remove them from the light exclusion list.",
                MessageType.Info
            );
        }

        // Show light exclusion list
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_lightExclusion, true);
        EditorGUI.indentLevel--;

        // Spawn Points Section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spawn Points", EditorStyles.boldLabel);

        // Light Spawn Points
        EditorGUILayout.LabelField("Light Player Spawn Points", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "The first spawn point in the list (lime color) is the primary spawn point used for respawning. "
                + "Additional spawn points are used for random spawning. (green color)",
            MessageType.Info
        );

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = _isSelectingLightSpawns
            ? new Color(0.369f, 1f, 0.357f, 1f)
            : Color.white;

        if (
            GUILayout.Button(
                _isSelectingLightSpawns ? "Stop Selecting" : "Add/Remove",
                GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.5f - 10)
            )
        )
        {
            _isSelectingLightSpawns = !_isSelectingLightSpawns;
            _isSelectingLightExclusion = false;
            _isSelectingShadowSpawn = false;
            _isSelectingPowerUpSpawns = false;
            _isSelectingPortals = false;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        if (
            GUILayout.Button(
                "Clear All",
                GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.5f - 10)
            )
        )
        {
            if (
                EditorUtility.DisplayDialog(
                    "Clear Light Spawn Points",
                    "Are you sure you want to clear all light spawn points?",
                    "Yes",
                    "No"
                )
            )
            {
                _lightSpawnCells.ClearArray();
                serializedObject.ApplyModifiedProperties();
                SceneView.RepaintAll();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (_isSelectingLightSpawns)
        {
            EditorGUILayout.HelpBox(
                "Click on floor tiles in the Scene view to add/remove them from the light player spawn points.",
                MessageType.Info
            );
        }

        // Show light spawn points list
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_lightSpawnCells, true);
        EditorGUI.indentLevel--;

        // Shadow Spawn Point
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shadow Player Spawn Point", EditorStyles.boldLabel);

        GUI.backgroundColor = _isSelectingShadowSpawn
            ? new Color(0.231f, 0.667f, 1f, 1f)
            : Color.white;

        if (
            GUILayout.Button(
                _isSelectingShadowSpawn ? "Stop Setting Shadow Spawn" : "Set Shadow Spawn"
            )
        )
        {
            _isSelectingShadowSpawn = !_isSelectingShadowSpawn;
            _isSelectingLightExclusion = false;
            _isSelectingLightSpawns = false;
            _isSelectingPowerUpSpawns = false;
            _isSelectingPortals = false;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        if (_isSelectingShadowSpawn)
        {
            EditorGUILayout.HelpBox(
                "Click on a floor tile in the Scene view to set the shadow player spawn point.",
                MessageType.Info
            );
        }

        // Show shadow spawn point
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(_shadowSpawnCell, new GUIContent("Shadow Spawn Cell"));
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("PowerupCollision Spawn Points", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "The first spawn point in the list (orange color) is the primary spawn point used for spawning powerups. "
                + "Additional spawn points are used for random spawning. (yellow color)",
            MessageType.Info
        );

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = _isSelectingPowerUpSpawns
            ? new Color(0.369f, 1f, 0.357f, 1f)
            : Color.white;
        if (
            GUILayout.Button(
                _isSelectingPowerUpSpawns ? "Stop Selecting" : "Add/Remove",
                GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.5f - 10)
            )
        )
        {
            _isSelectingPowerUpSpawns = !_isSelectingPowerUpSpawns;
            _isSelectingLightSpawns = false;
            _isSelectingShadowSpawn = false;
            _isSelectingLightExclusion = false;
            _isSelectingPortals = false;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        if (
            GUILayout.Button(
                "Clear All",
                GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.5f - 10)
            )
        )
        {
            if (
                EditorUtility.DisplayDialog(
                    "Clear PowerupCollision Spawn Points",
                    "Are you sure you want to clear all powerup spawn points?",
                    "Yes",
                    "No"
                )
            )
            {
                _powerUpSpawnCells.ClearArray();
                serializedObject.ApplyModifiedProperties();
                SceneView.RepaintAll();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (_isSelectingPowerUpSpawns)
        {
            EditorGUILayout.HelpBox(
                "Click on floor tiles in the Scene view to add/remove them from the powerup spawn points.",
                MessageType.Info
            );
        }

        // Show powerup spawn points
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_powerUpSpawnCells, true);
        EditorGUI.indentLevel--;

        serializedObject.ApplyModifiedProperties();

        // Scatter Targets Section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scatter Targets", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Target points for ghosts when in Scatter state (cell coordinates).",
            MessageType.Info
        );

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = _isSelectingScatterTargets
            ? new Color(1f, 0.5f, 0.5f, 1f)
            : Color.white;
        if (
            GUILayout.Button(
                _isSelectingScatterTargets ? "Stop Selecting Scatter" : "Add/Remove Scatter"
            )
        )
        {
            _isSelectingScatterTargets = !_isSelectingScatterTargets;
            _isSelectingLightExclusion = false;
            _isSelectingLightSpawns = false;
            _isSelectingShadowSpawn = false;
            _isSelectingPowerUpSpawns = false;
            _isSelectingPortals = false;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Clear All"))
        {
            if (EditorUtility.DisplayDialog("Clear Scatter Targets", "Are you sure?", "Yes", "No"))
            {
                _scatterTargets.ClearArray();
                serializedObject.ApplyModifiedProperties();
                SceneView.RepaintAll();
            }
        }
        EditorGUILayout.EndHorizontal();

        // Show scatter targets list
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_scatterTargets, true);
        EditorGUI.indentLevel--;
        EditorGUILayout.LabelField($"Scatter Targets: {_scatterTargets.arraySize}");

        // Shadow Blocked Tiles Section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shadow Blocked Tiles", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = _isSelectingshadowBlockedCells
            ? new Color(1f, 0.4f, 0.4f, 1f)
            : Color.white;

        if (GUILayout.Button(_isSelectingshadowBlockedCells ? "Stop Selecting" : "Add/Remove"))
        {
            _isSelectingshadowBlockedCells = !_isSelectingshadowBlockedCells;

            // turn off other modes
            _isSelectingLightExclusion = false;
            _isSelectingLightSpawns = false;
            _isSelectingShadowSpawn = false;
            _isSelectingPowerUpSpawns = false;
            _isSelectingScatterTargets = false;
            _isSelectingPortals = false;

            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Clear All"))
        {
            if (
                EditorUtility.DisplayDialog(
                    "Clear Shadow Blocked Tiles",
                    "Are you sure you want to clear all shadow-blocked tiles?",
                    "Yes",
                    "No"
                )
            )
            {
                _shadowBlockedCells.ClearArray();
                serializedObject.ApplyModifiedProperties();
                SceneView.RepaintAll();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (_isSelectingshadowBlockedCells)
        {
            EditorGUILayout.HelpBox(
                "Click on floor tiles in the Scene view to add/remove them from shadow-blocked tiles.",
                MessageType.Info
            );
        }

        // Show list
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_shadowBlockedCells, true);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shadow Home Targets", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Click on floor tiles in the Scene view to add/remove shadow home targets.",
            MessageType.Info
        );

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = _isSelectingHomeTargets ? new Color(0.8f, 0.5f, 1f, 1f) : Color.white;
        if (
            GUILayout.Button(
                _isSelectingHomeTargets ? "Stop Selecting" : "Add/Remove",
                GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.5f - 10)
            )
        )
        {
            _isSelectingHomeTargets = !_isSelectingHomeTargets;

            // turn off other modes
            _isSelectingLightExclusion = false;
            _isSelectingLightSpawns = false;
            _isSelectingShadowSpawn = false;
            _isSelectingPowerUpSpawns = false;
            _isSelectingScatterTargets = false;
            _isSelectingshadowBlockedCells = false;
            _isSelectingPortals = false;

            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        if (
            GUILayout.Button(
                "Clear All",
                GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.5f - 10)
            )
        )
        {
            if (
                EditorUtility.DisplayDialog(
                    "Clear Shadow Home Targets",
                    "Are you sure you want to clear all shadow home targets?",
                    "Yes",
                    "No"
                )
            )
            {
                _homeTargets.ClearArray();
                serializedObject.ApplyModifiedProperties();
                SceneView.RepaintAll();
            }
        }
        EditorGUILayout.EndHorizontal();

        // Show home targets list
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_homeTargets, true);
        EditorGUI.indentLevel--;

        // Portal creation
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Portals", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Portals can be created by placing portal tiles on the portal tilemap in the Scene view. "
                + "Portals will automatically link in pairs based on their Portal ID. "
                + "IDs will be assigned automatically when placing the tiles in pairs.",
            MessageType.Info
        );

        GUILayout.BeginHorizontal();
        if (
            GUILayout.Button(
                _isSelectingPortals ? "Stop Selecting" : "Add/Remove",
                GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.5f - 10)
            )
        )
        {
            _isSelectingPortals = !_isSelectingPortals;
            _isSelectingHomeTargets = false;
            _isSelectingLightExclusion = false;
            _isSelectingLightSpawns = false;
            _isSelectingShadowSpawn = false;
            _isSelectingPowerUpSpawns = false;
            _isSelectingScatterTargets = false;
            _isSelectingshadowBlockedCells = false;
            SceneView.RepaintAll();
        }

        if (
            GUILayout.Button(
                "Clear All",
                GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.5f - 10)
            )
        )
        {
            if (
                EditorUtility.DisplayDialog(
                    "Clear Portals",
                    "Are you sure you want to clear all portals?",
                    "Yes",
                    "No"
                )
            )
            {
                // Clear portal data
                _portalData.ClearArray();

                // Clear portal tiles from tilemap
                Tilemap portalTilemap =
                    serializedObject.FindProperty("_tilemapPortals").objectReferenceValue
                    as Tilemap;
                portalTilemap.ClearAllTiles();

                serializedObject.ApplyModifiedProperties();
                SceneView.RepaintAll();
            }
        }
        GUILayout.EndHorizontal();

        // Show portals list
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_portalData, true);
        EditorGUI.indentLevel--;

        // Show statistics
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Light Spawn Points: {_lightSpawnCells.arraySize}");
        EditorGUILayout.LabelField(
            $"PowerupCollision Spawn Points: {_powerUpSpawnCells.arraySize}"
        );
        EditorGUILayout.LabelField($"Excluded Tiles: {_lightExclusion.arraySize}");

        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField($"Lit Tiles: {_grid.LitTileCount}");
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        // Only process input if one of the selection modes is active
        if (
            !_isSelectingLightExclusion
            && !_isSelectingLightSpawns
            && !_isSelectingShadowSpawn
            && !_isSelectingPowerUpSpawns
            && !_isSelectingScatterTargets
            && !_isSelectingshadowBlockedCells
            && !_isSelectingHomeTargets
            && !_isSelectingPortals
        )
            return;

        // Get the tilemap
        Tilemap floorTilemap =
            serializedObject.FindProperty("_tilemapFloors").objectReferenceValue as Tilemap;
        if (floorTilemap == null)
            return;

        // Handle mouse input in scene view
        Event e = Event.current;

        // Draw custom cursor or overlay
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            // Get mouse position in world space
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 worldPos = ray.origin;

            // Convert to cell position
            Vector3Int cellPos = floorTilemap.WorldToCell(worldPos);

            // Check if this is a valid floor tile
            Tilemap wallTilemap =
                serializedObject.FindProperty("_tilemapWalls").objectReferenceValue as Tilemap;
            bool isWalkable = !wallTilemap.HasTile(cellPos) && floorTilemap.HasTile(cellPos);

            if (isWalkable)
            {
                if (_isSelectingLightExclusion)
                {
                    ToggleLightExclusion(cellPos);
                    e.Use();
                }
                else if (_isSelectingLightSpawns)
                {
                    ToggleLightSpawn(cellPos);
                    e.Use();
                }
                else if (_isSelectingShadowSpawn)
                {
                    SetShadowSpawn(cellPos, floorTilemap);
                    e.Use();
                }
                else if (_isSelectingPowerUpSpawns)
                {
                    TogglePowerUpSpawn(cellPos);
                    e.Use();
                }
                else if (_isSelectingshadowBlockedCells)
                {
                    ToggleShadowBlocked(cellPos);
                    e.Use();
                }
                else if (_isSelectingHomeTargets)
                {
                    ToggleHomeTarget(cellPos);
                    e.Use();
                }
                else if (_isSelectingPortals)
                {
                    TogglePortal(cellPos);
                    e.Use();
                }
            }

            if (_isSelectingScatterTargets)
            {
                ToggleScatterTarget(cellPos);
                e.Use();
            }
        }

        // Force scene view to repaint
        sceneView.Repaint();
    }

    private void TogglePortal(Vector3Int cellPos)
    {
        Tilemap portalTilemap =
            serializedObject.FindProperty("_tilemapPortals").objectReferenceValue as Tilemap;
        TileBase portalTile =
            serializedObject.FindProperty("_portalTile").objectReferenceValue as AnimatedTile;

        if (portalTilemap == null || portalTile == null)
            return;

        Vector2Int cellPos2D = new Vector2Int(cellPos.x, cellPos.y);
        serializedObject.Update();

        // Check if there's already a portal at this position
        bool found = false;
        for (int i = 0; i < _portalData.arraySize; i++)
        {
            SerializedProperty portalElement = _portalData.GetArrayElementAtIndex(i);
            Vector2Int existingPos = portalElement
                .FindPropertyRelative("CellPosition")
                .vector2IntValue;

            if (existingPos == cellPos2D)
            {
                // Remove the portal data and tile
                _portalData.DeleteArrayElementAtIndex(i);
                portalTilemap.SetTile(cellPos, null);
                found = true;
                Debug.Log($"Removed portal at {cellPos}");
                break;
            }
        }

        if (!found)
        {
            // Add a new portal
            uint portalId = _grid.GetNextAvailablePortalId();

            // Create portal data
            int index = _portalData.arraySize;
            _portalData.InsertArrayElementAtIndex(index);
            SerializedProperty newPortal = _portalData.GetArrayElementAtIndex(index);
            newPortal.FindPropertyRelative("PortalId").intValue = (int)portalId;
            newPortal.FindPropertyRelative("CellPosition").vector2IntValue = cellPos2D;
            newPortal.FindPropertyRelative("PortalColor").colorValue = Color.white;

            // Place the portal tile
            portalTilemap.SetTile(cellPos, portalTile);

            Debug.Log($"Added portal at {cellPos} with ID {portalId}");
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        EditorUtility.SetDirty(portalTilemap);
        SceneView.RepaintAll();
    }

    private void ToggleLightExclusion(Vector3Int cellPos)
    {
        Vector2Int coord = new Vector2Int(cellPos.x, cellPos.y);

        serializedObject.Update();

        // Check if coordinate already exists
        bool found = false;
        for (int i = 0; i < _lightExclusion.arraySize; i++)
        {
            SerializedProperty element = _lightExclusion.GetArrayElementAtIndex(i);
            Vector2Int existingCoord = element.vector2IntValue;

            if (existingCoord == coord)
            {
                // Remove it
                _lightExclusion.DeleteArrayElementAtIndex(i);
                found = true;
                Debug.Log($"Removed light exclusion at {coord}");
                break;
            }
        }

        if (!found)
        {
            // Add it
            int index = _lightExclusion.arraySize;
            _lightExclusion.InsertArrayElementAtIndex(index);
            _lightExclusion.GetArrayElementAtIndex(index).vector2IntValue = coord;
            Debug.Log($"Added light exclusion at {coord}");
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        SceneView.RepaintAll();
    }

    private void ToggleLightSpawn(Vector3Int cellPos)
    {
        Vector2Int coord = new Vector2Int(cellPos.x, cellPos.y);

        serializedObject.Update();

        // Check if coordinate already exists
        bool found = false;
        for (int i = 0; i < _lightSpawnCells.arraySize; i++)
        {
            SerializedProperty element = _lightSpawnCells.GetArrayElementAtIndex(i);
            Vector2Int existingCoord = element.vector2IntValue;

            if (existingCoord == coord)
            {
                // Remove it
                _lightSpawnCells.DeleteArrayElementAtIndex(i);
                found = true;
                Debug.Log($"Removed light spawn at {coord}");
                break;
            }
        }

        if (!found)
        {
            // Add it
            int index = _lightSpawnCells.arraySize;
            _lightSpawnCells.InsertArrayElementAtIndex(index);
            _lightSpawnCells.GetArrayElementAtIndex(index).vector2IntValue = coord;
            Debug.Log($"Added light spawn at {coord}");
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        SceneView.RepaintAll();
    }

    private void SetShadowSpawn(Vector3Int cellPos, Tilemap tilemap)
    {
        serializedObject.Update();
        _shadowSpawnCell.vector2IntValue = new Vector2Int(cellPos.x, cellPos.y);
        serializedObject.ApplyModifiedProperties();
        Debug.Log(
            $"Set Shadow player spawn to {cellPos} (World: {tilemap.GetCellCenterWorld(cellPos)})"
        );
        EditorUtility.SetDirty(target);
        SceneView.RepaintAll();
    }

    private void TogglePowerUpSpawn(Vector3Int cellPos)
    {
        Vector2Int coord = new Vector2Int(cellPos.x, cellPos.y);

        serializedObject.Update();

        // Check if coordinate already exists
        bool found = false;
        for (int i = 0; i < _powerUpSpawnCells.arraySize; i++)
        {
            SerializedProperty element = _powerUpSpawnCells.GetArrayElementAtIndex(i);
            Vector2Int existingCoord = element.vector2IntValue;

            if (existingCoord == coord)
            {
                // Remove it
                _powerUpSpawnCells.DeleteArrayElementAtIndex(i);
                found = true;
                Debug.Log($"Removed powerup spawn at {coord}");
                break;
            }
        }

        if (!found)
        {
            // Add it
            int index = _powerUpSpawnCells.arraySize;
            _powerUpSpawnCells.InsertArrayElementAtIndex(index);
            _powerUpSpawnCells.GetArrayElementAtIndex(index).vector2IntValue = coord;
            Debug.Log($"Added powerup spawn at {coord}");
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        SceneView.RepaintAll();
    }

    private void ToggleScatterTarget(Vector3Int cellPos)
    {
        Vector2Int coord = new Vector2Int(cellPos.x, cellPos.y);

        serializedObject.Update();

        bool found = false;

        for (int i = 0; i < _scatterTargets.arraySize; i++)
        {
            SerializedProperty element = _scatterTargets.GetArrayElementAtIndex(i);
            SerializedProperty targetCellProp = element.FindPropertyRelative("targetCell");

            if (targetCellProp.vector2IntValue == coord)
            {
                _scatterTargets.DeleteArrayElementAtIndex(i);
                found = true;
                Debug.Log($"Removed scatter target at {coord}");
                break;
            }
        }

        if (!found)
        {
            int index = _scatterTargets.arraySize;
            _scatterTargets.InsertArrayElementAtIndex(index);

            SerializedProperty newElement = _scatterTargets.GetArrayElementAtIndex(index);
            newElement.FindPropertyRelative("targetCell").vector2IntValue = coord;

            ShadowType typeToAssign = (ShadowType)(index % 4);
            newElement.FindPropertyRelative("shadowType").enumValueIndex = (int)typeToAssign;

            Debug.Log($"Added scatter target at {coord} with type {typeToAssign}");
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        SceneView.RepaintAll();
    }

    private void ToggleShadowBlocked(Vector3Int cellPos)
    {
        Vector2Int coord = new Vector2Int(cellPos.x, cellPos.y);

        serializedObject.Update();

        bool found = false;
        for (int i = 0; i < _shadowBlockedCells.arraySize; i++)
        {
            SerializedProperty element = _shadowBlockedCells.GetArrayElementAtIndex(i);
            if (element.vector2IntValue == coord)
            {
                _shadowBlockedCells.DeleteArrayElementAtIndex(i);
                found = true;
                Debug.Log($"Removed shadow blocked tile at {coord}");
                break;
            }
        }

        if (!found)
        {
            int index = _shadowBlockedCells.arraySize;
            _shadowBlockedCells.InsertArrayElementAtIndex(index);
            _shadowBlockedCells.GetArrayElementAtIndex(index).vector2IntValue = coord;
            Debug.Log($"Added shadow blocked tile at {coord}");
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        SceneView.RepaintAll();
    }

    private void ToggleHomeTarget(Vector3Int cellPos)
    {
        Vector2Int coord = new Vector2Int(cellPos.x, cellPos.y);
        serializedObject.Update();

        bool found = false;
        for (int i = 0; i < _homeTargets.arraySize; i++)
        {
            SerializedProperty element = _homeTargets.GetArrayElementAtIndex(i);
            if (element.vector2IntValue == coord)
            {
                _homeTargets.DeleteArrayElementAtIndex(i);
                found = true;
                Debug.Log($"Removed shadow home target at {coord}");
                break;
            }
        }

        if (!found)
        {
            int index = _homeTargets.arraySize;
            _homeTargets.InsertArrayElementAtIndex(index);
            _homeTargets.GetArrayElementAtIndex(index).vector2IntValue = coord;
            Debug.Log($"Added shadow home target at {coord}");
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        SceneView.RepaintAll();
    }

    // Draw gizmos in the scene view
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void DrawGizmos(Grid grid, GizmoType gizmoType)
    {
        if (grid == null)
            return;

        // Get the tilemaps via reflection since they're private
        var tilemapFloorsField = typeof(Grid).GetField(
            "_tilemapFloors",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        var lightExclusionField = typeof(Grid).GetField(
            "_lightExclusion",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        var lightSpawnField = typeof(Grid).GetField(
            "_lightSpawnCells",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        var shadowSpawnField = typeof(Grid).GetField(
            "_shadowSpawnCell",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        var powerUpSpawnField = typeof(Grid).GetField(
            "_powerUpSpawnCells",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        var shadowBlockedField = typeof(Grid).GetField(
            "_shadowBlockedCells",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        var portalDataField = typeof(Grid).GetField(
            "_portalData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        if (tilemapFloorsField == null || lightExclusionField == null)
            return;

        Tilemap floorTilemap = tilemapFloorsField.GetValue(grid) as Tilemap;
        List<Vector2Int> lightExclusion = lightExclusionField.GetValue(grid) as List<Vector2Int>;
        List<Vector2Int> lightSpawns = lightSpawnField.GetValue(grid) as List<Vector2Int>;
        Vector2Int shadowSpawn = (Vector2Int)shadowSpawnField.GetValue(grid);
        List<Vector2Int> powerUpSpawns = powerUpSpawnField.GetValue(grid) as List<Vector2Int>;
        List<Vector2Int> shadowBlocked = shadowBlockedField.GetValue(grid) as List<Vector2Int>;
        List<PortalData> portalData = portalDataField?.GetValue(grid) as List<PortalData>;

        if (floorTilemap == null || lightExclusion == null)
            return;

        // Draw excluded tiles as a red wire rectangle around each tile
        foreach (Vector2Int coord in lightExclusion)
        {
            Vector3Int cellPos = new Vector3Int(coord.x, coord.y, 0);
            Vector3 worldPos = floorTilemap.GetCellCenterWorld(cellPos);

            // wire rectangle matching the tile cell size
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
            Vector3 size = new Vector3(floorTilemap.cellSize.x, floorTilemap.cellSize.y, 0.01f);
            Gizmos.DrawCube(worldPos, size);
        }

        float spawnRadius = 0.25f;

        // Draw light spawn points (green, 1st is lime)
        if (lightSpawns != null)
        {
            foreach (Vector2Int coord in lightSpawns)
            {
                Vector3Int cellPos = new Vector3Int(coord.x, coord.y, 0);
                Vector3 worldPos = floorTilemap.GetCellCenterWorld(cellPos);
                Gizmos.color =
                    lightSpawns.IndexOf(coord) == 0
                        ? new Color(0.675f, 1f, 0f, 1f)
                        : new Color(0.369f, 1f, 0.357f, 1f);
                Gizmos.DrawSphere(worldPos, spawnRadius);
            }
        }

        // Draw shadow spawn point (blue)
        if (shadowSpawnField != null)
        {
            Vector3Int cellPos = new Vector3Int(shadowSpawn.x, shadowSpawn.y, 0);
            Vector3 worldPos = floorTilemap.GetCellCenterWorld(cellPos);
            Gizmos.color = new Color(0.231f, 0.667f, 1f, 1f);
            Gizmos.DrawSphere(worldPos, spawnRadius);
        }

        // Draw powerup spawn points (yellow, 1st is orange)
        if (powerUpSpawns != null)
        {
            foreach (Vector2Int coord in powerUpSpawns)
            {
                Vector3Int cellPos = new Vector3Int(coord.x, coord.y, 0);
                Vector3 worldPos = floorTilemap.GetCellCenterWorld(cellPos);
                Gizmos.color =
                    powerUpSpawns.IndexOf(coord) == 0
                        ? new Color(1f, 0.584f, 0.0f, 1f)
                        : new Color(1f, 0.918f, 0.294f, 1f);
                Gizmos.DrawSphere(worldPos, spawnRadius);
            }
        }

        if (shadowBlocked != null)
        {
            foreach (Vector2Int coord in shadowBlocked)
            {
                Vector3Int cellPos = new Vector3Int(coord.x, coord.y, 0);
                Vector3 worldPos = floorTilemap.GetCellCenterWorld(cellPos);

                Gizmos.color = new Color(0.8f, 0f, 0.8f, 0.8f);
                Vector3 size = new Vector3(floorTilemap.cellSize.x, floorTilemap.cellSize.y, 0.01f);
                Gizmos.DrawCube(worldPos, size);
            }
        }

        // Draw portals (colored by ID, with labels)
        if (portalData != null)
        {
            foreach (PortalData portal in portalData)
            {
                Vector3Int cellPos = new Vector3Int(
                    portal.CellPosition.x,
                    portal.CellPosition.y,
                    0
                );
                Vector3 worldPos = floorTilemap.GetCellCenterWorld(cellPos);

                // Use portal color or generate color based on ID
                Color portalColor =
                    portal.PortalColor != Color.white
                        ? portal.PortalColor
                        : Color.HSVToRGB((portal.PortalId * 0.618034f) % 1f, 0.8f, 1f);

                Gizmos.color = portalColor;
                Gizmos.DrawWireSphere(worldPos, spawnRadius * 0.8f);
                Gizmos.DrawWireSphere(worldPos, spawnRadius * 1.2f);
            }
        }
    }
}
