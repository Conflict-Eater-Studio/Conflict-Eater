using UnityEngine;

public class ShadowSwitchUI : MonoBehaviour
{
    [Header("UI Indicators")]
    public ShadowIndicator activeUI;
    public ShadowIndicator inkyUI;
    public ShadowIndicator clydeUI;

    [Header("Colors")]
    public Color blinkyColor = Color.red;
    public Color inkyColor = Color.cyan;
    public Color clydeColor = new Color(1f, 0.7f, 0.3f);

    private ShadowType _activeGhost;

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        //UpdateUI();
    }

    public void UpdateUI()
    {
        activeUI.SetActive(true);
        //inkyUI.SetActive(active == ShadowType.Inky);
        //clydeUI.SetActive(active == ShadowType.Clyde);

        //blinkyUI.SetAvailable(blinkyAvailable, blinkyColor);
        clydeUI.SetAvailable(false);
        //clydeUI.SetAvailable(clydeAvailable, clydeColor);
    }
}
