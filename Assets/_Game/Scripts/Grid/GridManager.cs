using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages the active grid layout during gameplay.
/// Handles grid selection, instantiation, destruction, and notifies listeners when the grid changes.
/// Also reacts to round-ending events to optionally swap grids based on game rules.
/// </summary>
public class GridManager : MonoBehaviour
{
    #region Nested Types

    /// <summary>
    /// Represents a single grid configuration entry.
    /// Contains an identifier, prefab reference, and weight used for random selection.
    /// </summary>
    [System.Serializable]
    public class GridElement
    {
        public int id;
        public GameObject prefab;
        public int weight = 1;
    }
    #endregion

    #region Inspector Fields
    public List<GridElement> gridElements = new List<GridElement>();
    #endregion

    #region Properties
    public int CurrentGridId { get; private set; } = -1;
    public GameObject CurrentGridObject { get; private set; }
    #endregion

    #region Events
    public event Action<int> OnGridChanged;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initializes the grid system by selecting a random grid
    /// and subscribing to round-ending events.
    /// </summary>
    void Start()
    {
        ChangeGridToRandom();

        GameManager.Instance.Timer.OnRoundEnding += Timer_OnRoundEnding;
    }

    /// <summary>
    /// Unsubscribes from external events to prevent memory leaks
    /// when this object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        GameManager.Instance.Timer.OnRoundEnding -= Timer_OnRoundEnding;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called when a round is about to end.
    /// Changes the grid on every even-numbered round.
    /// </summary>
    private void Timer_OnRoundEnding(object sender, OnRoundEndEventArgs e)
    {
        if(e.CurrentRound % 2 == 0)
        {
            ChangeGridToRandom();
        }
    }
    #endregion

    #region Grid Management

    /// <summary>
    /// Selects a random grid using weighted probability
    /// and activates it.
    /// </summary>
    public void ChangeGridToRandom()
    {
        GridElement element = GetRandomGridElement();
        if (element == null)
            return;

        SetGrid(element.id);
    }


    /// <summary>
    /// Activates a grid by its ID.
    /// Destroys the currently active grid (if any),
    /// instantiates the new one, and notifies listeners.
    /// </summary>
    /// <param name="newGridId">ID of the grid to activate.</param>
    public void SetGrid(int newGridId)
    {
        if (CurrentGridId == newGridId)
            return;

        if (CurrentGridObject != null)
        {
            Destroy(CurrentGridObject);
            CurrentGridObject = null;
        }

        GridElement element = gridElements.Find(e => e.id == newGridId);
        if (element == null || element.prefab == null)
        {
            Debug.LogWarning($"Grid with id '{newGridId}' not found.");
            return;
        }

        CurrentGridObject = Instantiate(
            element.prefab,
            transform.position,
            Quaternion.identity,
            transform
        );

        CurrentGridObject.transform.localPosition = Vector3.zero;

        CurrentGridId = newGridId;

        GameManager.Instance.RegisterGrid(CurrentGridObject.GetComponent<Grid>());
        OnGridChanged?.Invoke(CurrentGridId);
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Returns a randomly selected grid element using weighted probability.
    /// </summary>
    /// <returns>The selected GridElement, or null if none are available.</returns>
    GridElement GetRandomGridElement()
    {
        if (gridElements.Count == 0)
            return null;

        List<GridElement> availableElements = gridElements;

        if (gridElements.Count > 1 && CurrentGridId != -1)
        {
            availableElements = gridElements.FindAll(e => e.id != CurrentGridId);
        }

        int totalWeight = 0;
        foreach (var element in availableElements)
            totalWeight += element.weight;

        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var element in availableElements)
        {
            currentWeight += element.weight;
            if (randomValue < currentWeight)
                return element;
        }

        return null;
    }


    /// <summary>
    /// Returns a Grid component associated with the given grid ID.
    /// If the grid is currently active, returns the instantiated Grid.
    /// Otherwise, returns the Grid component from the prefab.
    /// </summary>
    /// <param name="id">ID of the grid.</param>
    /// <returns>The Grid component, or null if not found.</returns>
    public Grid GetGridById(int id)
    {
        if (CurrentGridId == id && CurrentGridObject != null)
            return CurrentGridObject.GetComponent<Grid>();

        GridElement element = gridElements.Find(e => e.id == id);
        if (element == null || element.prefab == null)
            return null;

        return element.prefab.GetComponent<Grid>();
    }
    #endregion
}
