using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillPrefab : MonoBehaviour
{
    Image _uiIcon;
    Image _uiLock;
    Image _uiCDMask;
    Transform _uiCdNode;
    TextMeshProUGUI _uiCdText;
    TextMeshProUGUI _uiTip;
    public enum Status
    {
        None,
        CD,
        Ready,
    }
    public Status CurStatus => _status;
    Status _status;
    float _cd;
    float _leftCd;
    int _leftLockTime;
    public Action OnUseSkill;
    bool _inited;
    void OnInit()
    {
        if(_inited)
        {
            return;
        }
        _inited = true;
        if (Network.NetworkManagerEx.Instance != null && Network.NetworkManagerEx.Instance.Server != null)
            Network.NetworkManagerEx.Instance.Server.OnTimeTick += TimeTick;
        _uiCDMask = this.transform.FindNode<Image>("Mask");
        _uiLock = this.transform.FindNode<Image>("Lock");
        _uiIcon = this.transform.FindNode<Image>("Icon");
        _uiCdText = this.transform.FindNode<TextMeshProUGUI>("CDText");
        _uiTip = this.transform.FindNode<TextMeshProUGUI>("Btn");
        _uiCdNode = this.transform.FindNode<Transform>("CD");
    }

    private void Awake()
    {
        OnInit();
    }
    private void OnDestroy()
    {
        if(Network.NetworkManagerEx.Instance != null && Network.NetworkManagerEx.Instance.Server != null)
        {
            Network.NetworkManagerEx.Instance.Server.OnTimeTick -= TimeTick;
        }
    }
    /// <summary>若初始化时网络未就绪未订阅，这里补订。</summary>
    void EnsureTimeTickSubscribed()
    {
        if (Network.NetworkManagerEx.Instance != null && Network.NetworkManagerEx.Instance.Server != null)
        {
            Network.NetworkManagerEx.Instance.Server.OnTimeTick -= TimeTick;
            Network.NetworkManagerEx.Instance.Server.OnTimeTick += TimeTick;
        }
    }

    public void InitSkill(string icon, float cd, string tip)
    {
        OnInit();
        EnsureTimeTickSubscribed();
        _cd = cd;
        _leftCd = 0;
        _status = Status.Ready;
        _uiTip.text = tip;
        //_uiIcon.sprite = Resources.Load<Sprite>($"{icon}");
        RefreshStatus();
        LockOrUnlockSkill(false);
    }

    public void LockOrUnlockSkill(bool lockorUnlock)
    {
        _uiLock.gameObject.ChangeActive(lockorUnlock);
    }
    public void LockSkill(int time)
    {
        _leftLockTime = time;
        RefreshLock();
    }
    public void RefrshCD(float time)
    {
        _leftCd = time;
    }
    void RefreshLock()
    {
        _uiLock.gameObject.ChangeActive(_leftLockTime > 0);
    }
    void UpdateLock()
    {
        _uiLock.gameObject.ChangeActive(_leftLockTime > 0);
    }

    public void UseSkill()
    {
        if (_status == Status.CD || _leftLockTime > 0)
        {
            Debug.Log($"[SkillPrefab] 剩余:{_leftCd} 封锁中：{_leftLockTime > 0}");
            return;
        }
        _status = Status.CD;
        _leftCd = _cd;
        RefreshStatus();
        UpdateCD();
        OnUseSkill?.Invoke();
    }

    void RefreshStatus()
    {
        _uiCdNode.gameObject.ChangeActive(_status == Status.CD);
    }
    
    void UpdateCD()
    {
        _uiCdText.text = _leftCd.ToString();
        _leftCd-= Time.deltaTime;
        //_uiCDMask.fillAmount = _leftCd / _cd;
    }

    void TimeTick()
    {
        if (_status == Status.CD)
        {
            UpdateCD();
            if (_leftCd <= 0)
            {
                _status = Status.Ready;
                RefreshStatus();
            }
        }
        if (_leftLockTime > 0)
        {
            _leftLockTime--;
            UpdateLock();
        }
    }
}
