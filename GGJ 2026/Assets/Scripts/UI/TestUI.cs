/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestUI : MonoBehaviour
{
    public string Time;
    [InspectorButton("UIChange")]
    public void TestSynClient()
    {
        UIMgr.SetLoopPanel<UITestEsc>();
        UIMgr.Change<UIChaser>();
    }
    [InspectorButton("UIAdd")]
    public void TestSynClient2()
    {
        UIMgr.Add<UITestMainAdd>();
    }
    [InspectorButton("UI返回")]
    public void Test1()
    {
        UIMgr.Back();
    }

    private void Update()
    {
        if(Network.NetworkManagerEx.Instance.Server != null)
        {
            Time = Network.NetworkManagerEx.Instance.Server.Time.ToString();
        }
    }
}
*/