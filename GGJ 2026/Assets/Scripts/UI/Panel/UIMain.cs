using System;
using LayerMap;
using System.Collections.Generic;
using System.Text;
using Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class UIMain : UIBase
{
    Image _uiCampIcon;
    TextMeshProUGUI _uiCampName;
    TextMeshProUGUI _uiCountdown;
    TextMeshProUGUI _uiTip;
    TextMeshProUGUI _uiRankingText;
    SkillPrefab _uiSkillPrefab;
    public SkillPrefab MainSkil;
    public SkillPrefab SubSkill;
    public SkillPrefab LayerSkill;
    Transform _uiSkillListNode;
    DateTime _countdownTarget;
    bool _countDown;
    FactionMember _localFaction;
    FactionMember LocalFaction
    {
        get
        {
            if(_localFaction == null)
            {
                var po = NetworkManager.Singleton.LocalClient?.PlayerObject;
                if (po != null)
                    _localFaction = po.GetComponent<FactionMember>();
            }
            return _localFaction;
        }
    }
    DateTime _curTime
    {
        get
        {
            if(NetworkManagerEx.Instance != null && NetworkManagerEx.Instance.Server != null)
            {
                return NetworkManagerEx.Instance.Server.Time;
            }
            return new DateTime();
        }
    }
    protected override void OnInit()
    {
        // 子节点均为可选：预制体缺少某节点时不报错，使用处已做 null 判断
        _uiCampIcon = this.transform.FindNode<Image>("CampImage", false);
        _uiCampName = this.transform.FindNode<TextMeshProUGUI>("CampText", false);
        _uiRankingText = this.transform.FindNode<TextMeshProUGUI>("RankingText", false);
        _uiCountdown = this.transform.FindNode<TextMeshProUGUI>("CountdownText", false);
        _uiTip = this.transform.FindNode<TextMeshProUGUI>("LeftTipText", false);
        MainSkil = this.transform.FindNode<SkillPrefab>("MainSkill", false);
        SubSkill = this.transform.FindNode<SkillPrefab>("SubSkill", false);
        LayerSkill = this.transform.FindNode<SkillPrefab>("LayerSkill", false);
        _uiSkillListNode = this.transform.FindNode<Transform>("SkillList", false);
        _uiSkillPrefab = this.transform.FindNode<SkillPrefab>("SkillPrefab", false);
        if (_uiSkillPrefab != null)
            _uiSkillPrefab.gameObject.ChangeActive(false);
    }

    public override void OnShowAfter()
    {
        if(LayerMapManager.Instance.Client != null)
        {
            LayerMapManager.Instance.Client.Server.ScoreChangeRecordCount.OnValueChanged += OnScoreChange;
        }
        else
        {
            Debug.LogError("[UIMain]　客户端数据不存在");
        }
        RefreshRanking();
        InitSkill(null);
        initSkill = false;
    }


    public override void OnHide()
    {
        if (LayerMapManager.Instance.Client != null)
        {
            LayerMapManager.Instance.Client.Server.ScoreChangeRecordCount.OnValueChanged -= OnScoreChange;
        }
    }

    

    void OnScoreChange(int oldValue, int newValue)
    {
        RefreshRanking();
    }

    public class SkillBtn
    {
        public List<SkillBtnData> ExtendBtns;
        public SkillBtnData Data;
    }
    public class SkillBtnData
    {
        public string Name;
        public float Cd;
        public string Icon;
        public string Tip;
    }
    public class TotalSkill
    {
        public SkillBtn MainSkill;
        public SkillBtn SubSkill;
        public SkillBtn LayerSkill;
    }

    public TotalSkill Skills;

    public void InitSkill(TotalSkill skills)
    {
        if(skills == null)
        {
            if (MainSkil != null) MainSkil.gameObject.ChangeActive(false);
            if (SubSkill != null) SubSkill.gameObject.ChangeActive(false);
            if (LayerSkill != null) LayerSkill.gameObject.ChangeActive(false);
            return;
        }
        Skills = skills;
        if (MainSkil != null) { MainSkil.gameObject.ChangeActive(Skills.MainSkill != null); if (Skills.MainSkill != null) MainSkil.InitSkill(Skills.MainSkill.Data.Icon, Skills.MainSkill.Data.Cd, Skills.MainSkill.Data.Tip); }
        if (SubSkill != null) { SubSkill.gameObject.ChangeActive(Skills.SubSkill != null); if (Skills.SubSkill != null) SubSkill.InitSkill(Skills.SubSkill.Data.Icon, Skills.SubSkill.Data.Cd, Skills.SubSkill.Data.Tip); }
        if (LayerSkill != null) { LayerSkill.gameObject.ChangeActive(Skills.LayerSkill != null); if (Skills.LayerSkill != null) LayerSkill.InitSkill(Skills.LayerSkill.Data.Icon, Skills.LayerSkill.Data.Cd, Skills.LayerSkill.Data.Tip); }
        if (_uiSkillListNode != null)
        {
            for (int i = 0; i < _uiSkillListNode.childCount; i++)
                Destroy(_uiSkillListNode.GetChild(i).gameObject);
            _uiSkillListNode.gameObject.ChangeActive(false);
        }
    }

    public void InitCamp(string path, string name)
    {
        if (_uiCampName != null) _uiCampName.text = name;
        if (_uiCampIcon != null) _uiCampIcon.sprite = Resources.Load<Sprite>(path);
    }

    public void LockAllSkill(int time)
    {
        if (MainSkil != null)
        {
            MainSkil.LockSkill(time);
        }
        if (SubSkill != null)
        {
            SubSkill.LockSkill(time);
        }
        if (LayerSkill != null)
        {
            LayerSkill.LockSkill(time);
        }
    }

    public class RankData
    {
        public ulong ClientId;
        public string Name;
        public int Score;
    }

    public void RefreshRanking()
    {
        List<RankData> ranks = new List<RankData>();
        foreach (var client in LayerMapManager.Instance.AllClient)
        {
            // 忽略没有分数的客户端，按理说追逐角色没有获取积分的途径，不会上榜
            if(client.Score.Value <= 0)
            {
                continue;
            }
            ranks.Add(new RankData()
            {
                ClientId = client.OwnerClientId,
                Name = client.PlayerName.Value.ToString(),
                Score = client.Score.Value,
            });
        }
        ranks.Sort((x, y) => y.Score.CompareTo(x.Score));
        RefreshRanking(ranks);
    }
    public void RefreshRanking(List<RankData> ranks)
    {
        if (_uiRankingText == null) return;
        StringBuilder sb = new StringBuilder();
        int i = 1;
        foreach (var rank in ranks)
        {
            sb.AppendLine($"{i}.{rank.Name} : {rank.Score}");
            i++;
        }
        _uiRankingText.text = sb.ToString();
    }

    public void ShowOrHideLayerSkill(bool show)
    {
        if (LayerSkill == null || _uiSkillListNode == null) return;
        if (LayerSkill.CurStatus != SkillPrefab.Status.Ready) return;
        _uiSkillListNode.gameObject.ChangeActive(show);
    }

    public void InitCountdown(DateTime targetTime)
    {
        _countdownTarget = targetTime;
    }
    public void StartCountdown()
    {
        _countDown = true;
    }
    
    public void InitLeftTip(string mission)
    {
        if (_uiTip != null) _uiTip.text = mission;
    }
    
    public void RefreshSkillCd()
    {
        if (Skills == null) return;
        if(Skills.MainSkill != null)
        {
            MainSkil.RefrshCD((int)(chaserShockwave.NextUseTime - Network.NetworkManagerEx.NetworkManager.ServerTime.Time));
        }
        if(Skills.SubSkill != null)
        {
            SubSkill.RefrshCD((int)(chaserUltimate.NextUseTime - Network.NetworkManagerEx.NetworkManager.ServerTime.Time));
        }
        if(Skills.LayerSkill != null)
        {
            LayerSkill.RefrshCD((int)(layerMove.NextUseTime - Network.NetworkManagerEx.NetworkManager.ServerTime.Time));
        }
    }

    void RefreshCountdown()
    {
        if (_uiCountdown == null || _countdownTarget == default || !_countDown) return;
        var remain = (_countdownTarget - _curTime).TotalSeconds;
        _uiCountdown.text = $"{remain}";
        if (remain <= 0) _countDown = false;
    }
    bool initSkill;
    void RefreshCamp()
    {
        if (!LocalFaction.HasMask)
        {
            InitCamp("", "未选阵营");
            return;
        }
        string faction = LocalFaction.IsSurvivor ? "渡者" : (LocalFaction.IsChaser ? "面砂" : $"阵营 {LocalFaction.FactionId}");
        string layerName = GetCurrentLayerName();
        var camp = string.IsNullOrEmpty(layerName) ? faction : $"{faction} · {layerName}";
        InitCamp("", camp);
        if(initSkill)
        {
            return;
        }
        initSkill = true;
        if (LocalFaction.IsChaser)
        {
            InitLeftTip($"任务\n淘汰所有渡者\n按E抓捕渡者\n按J切换图层\n按K召唤所有渡者至同一图层");
            InitSkill(new TotalSkill()
            {
                LayerSkill = new SkillBtn()
                {
                    Data = new SkillBtnData()
                    {
                        Cd = layerMove.ChaserCd,
                        Tip = "J"
                    }
                },
                MainSkill = new SkillBtn()
                {
                    Data = new SkillBtnData()
                    {
                        Cd = chaserShockwave.CooldownSeconds,
                        Tip = "E"
                    }
                },
                SubSkill = new SkillBtn()
                {
                    Data = new SkillBtnData()
                    {
                        Cd = chaserUltimate.CooldownSeconds,
                        Tip = "I"
                    }
                },
            });
        }
        else
        {
            InitLeftTip("任务\n躲避面纱抓捕并收集道具\n按E拾取交互物品\n按 J 切换图层\n拾取阵亡同伴的面具获得新图层选项");
            InitSkill(new TotalSkill()
            {
                LayerSkill = new SkillBtn()
                {
                    Data = new SkillBtnData()
                    {
                        Cd = layerMove.SurvivorCd,
                        Tip = "J"
                    }
                },
            });
        }
    }

    private FactionMember localFaction;
    private ChaserShockwaveAbility chaserShockwave;
    private LayerMoveAbility layerMove;
    private ChaserUltimateAbility chaserUltimate;

    float checkTime;
    private void Update()
    {
        checkTime += Time.deltaTime;
        RefreshCountdown();
        if(checkTime >= 1)
        {
            checkTime = 0;
            RefreshCamp();
        }
        RefreshSkillCd();
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;
        var po = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (po == null) return;
        if (localFaction == null) localFaction = po.GetComponent<FactionMember>();
        if (chaserShockwave == null) chaserShockwave = po.GetComponent<ChaserShockwaveAbility>();
        if (layerMove == null) layerMove = po.GetComponent<LayerMoveAbility>();
        if (chaserUltimate == null) chaserUltimate = po.GetComponent<ChaserUltimateAbility>();
    }

    private string GetCurrentLayerName()
    {
        if (LayerMapManager.Instance == null) return "";
        var client = LayerMapManager.Instance.Client;
        if (client == null) return "";
        return client.Layer.Value.ToName();
    }
}
