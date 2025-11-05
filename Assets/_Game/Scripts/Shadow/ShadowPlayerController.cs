using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShadowPlayerController : MonoBehaviour
{
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private int totalGhosts = 4;
    [SerializeField] private float spawnDelay = 1f;

    private List<GameObject> ghosts = new List<GameObject>();
    private int activeGhostIndex = 0;
    private bool canSwitch = true;

    private Color inactiveColor = new Color(0.5f, 0.5f, 1f, 0.5f);
    private Color activeColor = Color.darkBlue;

    [SerializeField] private float maxSwitchDistance = 10f;

    private PlayerInput _playerInput;

    private void Start()
    {
        _playerInput = GetComponentInParent<PlayerInput>();
        if (_playerInput)
        {
            _playerInput.actions["GhostSwitch"].performed += OnSwitch;

            _playerInput.actions["Move"].performed += OnMove;
            _playerInput.actions["Move"].canceled += OnMove;
        }

        SetGhostColors();

        StartCoroutine(SpawnRemainingGhosts());
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (ghosts.Count == 0) return;

        Vector2 move = context.ReadValue<Vector2>();
        var activeGhost = ghosts[activeGhostIndex];
        activeGhost.GetComponent<ShadowController>().Move(move);
    }

    public void SetGhostPrefab(GameObject prefab)
    {
        ghostPrefab = prefab;
    }

    private IEnumerator SpawnRemainingGhosts()
    {
        for (int i = 0; i < 1; i++)
        {
            GameObject ghost = Instantiate(ghostPrefab, Vector3.zero, Quaternion.identity, gameObject.transform);
            ghost.name = $"Ghost_{i + 1}";

            if(i!=0)
            {
                ghost.GetComponent<ShadowController>().enabled = false;
                ghost.GetComponent<PlayerInput>().enabled = false;
            }

            ghosts.Add(ghost);

            SetGhostColors();

            yield return new WaitForSeconds(spawnDelay);
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

        currentGhost.GetComponent<ShadowController>().enabled = false;
        currentGhost.GetComponent<PlayerInput>().enabled = false;
        currentGhost.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

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

            GameObject shadow = ghosts[activeGhostIndex];
            shadow.GetComponent<ShadowController>().enabled = true;
            shadow.GetComponent<PlayerInput>().enabled = true;
            shadow.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;

            Debug.Log($"Switched to ghost #{activeGhostIndex + 1}");
        }
        else
        {
            currentGhost.GetComponent<ShadowController>().enabled = true;
            currentGhost.GetComponent<PlayerInput>().enabled = true;
            currentGhost.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;

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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerLight"))
        {
            // NOTE: Only end the round
            // Controller and score swapping is handled in GameManager.OnRoundEnd
            GameManager.Instance.Timer.EndRound();
        }
    }

    private void OnDestroy()
    {
        if (_playerInput != null)
        {
            _playerInput.actions["Move"].performed -= OnMove;
            _playerInput.actions["Move"].canceled -= OnMove;
            _playerInput.actions["GhostSwitch"].performed -= OnSwitch;
        }
    }


}
