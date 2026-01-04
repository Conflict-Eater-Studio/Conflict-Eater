using UnityEngine;
using System;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [System.Serializable]
    public class GridElement
    {
        public int id;
        public GameObject prefab;
        public int weight = 1;
    }

    public List<GridElement> gridElements = new List<GridElement>();

    public int CurrentGridId { get; private set; } = -1;
    public GameObject CurrentGridObject { get; private set; }

    public static event Action<int> OnGridChanged;

    void Start()
    {
        ChangeGridToRandom();
    }

    public void ChangeGridToRandom()
    {
        GridElement element = GetRandomGridElement();
        if (element == null)
            return;

        SetGrid(element.id);
    }

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

    GridElement GetRandomGridElement()
    {
        if (gridElements.Count == 0)
            return null;

        int totalWeight = 0;
        foreach (var element in gridElements)
            totalWeight += element.weight;

        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var element in gridElements)
        {
            currentWeight += element.weight;
            if (randomValue < currentWeight)
                return element;
        }

        return null;
    }
}
