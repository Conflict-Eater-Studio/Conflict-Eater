using UnityEngine;

public class Tutorial : MenuBase
{
    public void OnBtnBack()
    {
        MenuManager.Instance.CloseLastSubMenu();
    }
}
