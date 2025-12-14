using UnityEngine;

public class Credits : MenuBase
{
    public void OnBtnBack()
    {
        MenuManager.Instance.CloseLastSubMenu();
    }
}
