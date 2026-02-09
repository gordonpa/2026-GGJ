using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestUIMain : MonoBehaviour
{
    public string Time;
    [InspectorButton("UINameInput")]
    public void NameUI()
    {
        var ui = UIMgr.Add<UINameInput>();
    }
    [InspectorButton("UIChange")]
    public void TestSynClient()
    {
        UIMgr.SetLoopPanel<UITestEsc>();
        var ui = UIMgr.Change<UIMain>();
        ui.InitCamp("", "阵营名称");
        ui.InitCountdown(Network.NetworkManagerEx.Instance.Server.Time.AddSeconds(60));
        ui.RefreshRanking();
        ui.InitLeftTip($"任务内容");
        ui.StartCountdown();
        ui.InitSkill(new UIMain.TotalSkill()
        {
            MainSkill = new UIMain.SkillBtn()
            {
                Data = new UIMain.SkillBtnData()
                {
                    Cd = 10,
                    Icon = $"SkillIcon/main"
                }
            },
            SubSkill = new UIMain.SkillBtn()
            {
                Data = new UIMain.SkillBtnData()
                {
                    Cd = 10,
                    Icon = $"SkillIcon/skill1"
                }
            },
            LayerSkill = new UIMain.SkillBtn()
            {
                Data = new UIMain.SkillBtnData()
                {
                    Cd = 10,
                    Icon = $"SkillIcon/skill2"
                },
                ExtendBtns = new List<UIMain.SkillBtnData>()
                {
                    new UIMain.SkillBtnData()
                    {
                        Icon = $"SkillIcon/0"
                    },
                    new UIMain.SkillBtnData()
                    {
                        Icon = $"SkillIcon/1"
                    },
                    new UIMain.SkillBtnData()
                    {
                        Icon = $"SkillIcon/2"
                    },
                    new UIMain.SkillBtnData()
                    {
                        Icon = $"SkillIcon/3"
                    },
                }
            },
        });
    }
    [InspectorButton("AddScore")]
    public void Skill1()
    {
        Network.NetworkManagerEx.Instance.Server.AddScoreServerRpc(Network.NetworkManagerEx.NetworkManager.LocalClientId);
    }
    [InspectorButton("Skill1")]
    public void Skill11()
    {
        UIMgr.Get<UIMain>().MainSkil.UseSkill();
    }
    [InspectorButton("Skill2")]
    public void Skill2()
    {
        UIMgr.Get<UIMain>().SubSkill.UseSkill();
    }
    [InspectorButton("Skill3")]
    public void Skill3()
    {
        UIMgr.Get<UIMain>().ShowOrHideLayerSkill(true);
    }
    [InspectorButton("Skill33")]
    public void Skill33()
    {
        UIMgr.Get<UIMain>().ShowOrHideLayerSkill(false);
        UIMgr.Get<UIMain>().LayerSkill.UseSkill();
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
        if (Network.NetworkManagerEx.Instance.Server != null)
        {
            Time = Network.NetworkManagerEx.Instance.Server.Time.ToString();
        }
    }
}
