using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UINameInput : UIBase
{
    TMP_InputField _input;

    protected override void OnInit()
    {
        _input = this.transform.FindNode<TMP_InputField>("Input");
        this.transform.BindButton("Btn", () =>
        {
            var name = _input.text;
            if (string.IsNullOrEmpty(name))
            {
                name = $"玩家 {Network.NetworkManagerEx.NetworkManager.LocalClientId}";
            }
            Network.NetworkManagerEx.Instance.Server.SetNameServerRpc(Network.NetworkManagerEx.NetworkManager.LocalClientId, name);
            UIMgr.Back();
        });
    }

    public override void OnShowAfter()
    {
        _input.text = "";
    }
}
