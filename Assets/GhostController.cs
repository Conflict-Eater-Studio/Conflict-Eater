using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GhostController : MonoBehaviour
{
    [SerializeField] private GameObject ghostPrefab; 
    [SerializeField] private int totalGhosts = 4;     
    [SerializeField] private float spawnDelay = 1f;
    private PlayerInput playerInput;

    private List<GameObject> ghosts = new List<GameObject>();
    private int activeGhostIndex = 0;
    private bool canSwitch = true;

    private Color inactiveColor = new Color(0.5f, 0.5f, 1f, 0.5f); 
    private Color activeColor = Color.darkBlue;

    [SerializeField] private float maxSwitchDistance = 10f;

    private void Start()
    {
        ghosts.Add(this.gameObject);

        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.actions["GhostSwitch"].performed += OnSwitch;
        }

        SetGhostColors();

        StartCoroutine(SpawnRemainingGhosts());
    }

    public void SetGhostPrefab(GameObject prefab)
    {
        ghostPrefab = prefab;
    }

    private IEnumerator SpawnRemainingGhosts()
    {
        for (int i = 1; i < totalGhosts; i++)
        {
            yield return new WaitForSeconds(spawnDelay);

            GameObject ghost = Instantiate(ghostPrefab, Vector3.zero, Quaternion.identity);
            ghost.name = $"Ghost_{i + 1}";

            ghost.GetComponent<PlayerController>().enabled = false;
            Destroy(ghost.GetComponent<CoinCollector>());

            ghosts.Add(ghost);

            SetGhostColors();
        }
    }

    public void OnSwitch(InputAction.CallbackContext context)
    {
        if (context.performed && canSwitch && ghosts.Count > 0)
        {
            StartCoroutine(SwitchGhost());
        }
    }

    private IEnumerator SwitchGhost()
    {
        canSwitch = false;

        GameObject currentGhost = ghosts[activeGhostIndex];
        currentGhost.GetComponent<PlayerController>().enabled = false;

        int closestIndex = activeGhostIndex;
        float closestDistance = maxSwitchDistance;

        for (int i = 0; i < ghosts.Count; i++)
        {
            if (i == activeGhostIndex) continue;

            float distance = Vector3.Distance(currentGhost.transform.position, ghosts[i].transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        if (closestIndex != activeGhostIndex)
        {
            activeGhostIndex = closestIndex;
            ghosts[activeGhostIndex].GetComponent<PlayerController>().enabled = true;

            Debug.Log($"Switched to ghost #{activeGhostIndex + 1}");
        }
        else
        {
            ghosts[activeGhostIndex].GetComponent<PlayerController>().enabled = true;
            Debug.Log("No ghost in range to switch to");
        }

        SetGhostColors();

        yield return new WaitForSeconds(0.3f);
        canSwitch = true;
    }

    private void SetGhostColors()
    {
        for (int i = 0; i < ghosts.Count; i++)
        {
            SpriteRenderer sr = ghosts[i].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = (i == activeGhostIndex) ? activeColor : inactiveColor;
            }
        }
    }
}
