using Client;
using OwlLogging;
using Shared;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMain : BattleEntityModelMain
{
    public static PlayerMain Instance;

    public Camera UiCamera => _uiCamera;
    [SerializeField]
    private Camera _uiCamera;

    private HashSet<BattleEntityModelMain> _activeSkillIndicators = new();
    private HashSet<BattleEntityModelMain> _skillIndicatorBuffer = new();

    private bool _isDisplayingDeadState = false;

    public int Initialize(ACharacterEntity charData)
    {
        return base.Initialize(charData);

        // TODO
    }

    public new void Shutdown()
    {
        base.Shutdown();

        // TODO
    }

    // Start is called before the first frame update
    protected new void Start()
    {
        base.Start();

        // TODO: Move this out of here, into a proper Initialization flow, once player model lifetime has been reworked/decided
        Initialize(ClientMain.Instance.CurrentCharacterData);
    }

    protected new void Update()
    {
        base.Update();

        /*
        // This makes skill icons linger until after the attack's anim-Cd is finished - looks a bit weird atm, but keep it in mind
        foreach (ASkillExecution skill in _entity.CurrentlyExecutingSkills)
        {
            if (skill is not AEntitySkillExecution entitySkill)
                continue;

            BattleEntityModelMain bTarget = ClientMain.Instance.MapModule.GetComponentFromEntityDisplay<BattleEntityModelMain>(entitySkill.Target.Id);
            if(bTarget == null)
            {
                OwlLogger.LogWarning($"Player can't find BattleEntityModel for id {entitySkill.Target.Id} to show SkillTargetIndicator!", GameComponent.Character);
                continue;
            }

            bTarget.DisplaySkillTargetIndicator(skill.SkillId);
            _skillIndicatorBuffer.Add(bTarget);
        }
        */

        if(_entity.QueuedSkill != null)
        {
            if(_entity.QueuedSkill.Target.IsEntityTarget())
            {
                BattleEntityModelMain bTarget = ClientMain.Instance.MapModule.GetComponentFromEntityDisplay<BattleEntityModelMain>(_entity.QueuedSkill.Target.EntityTarget.Id);
                if (bTarget == null)
                {
                    OwlLogger.Log($"Player can't find BattleEntityModel for id {_entity.QueuedSkill.Target.EntityTarget.Id} to show SkillTargetIndicator!", GameComponent.Character, LogSeverity.VeryVerbose);
                }
                else
                {
                    bTarget.DisplaySkillTargetIndicator(_entity.QueuedSkill.SkillId);
                    _skillIndicatorBuffer.Add(bTarget);
                }
            }
        }

        // buffer now contains all units that still should have active indicators,
        // while _activeSkillIndicators contains old ones.

        foreach(BattleEntityModelMain activeBEntity in _activeSkillIndicators)
        {
            if(!_skillIndicatorBuffer.Contains(activeBEntity))
            {
                activeBEntity.DisplaySkillTargetIndicator(SkillId.Unknown);
            }
        }

        _activeSkillIndicators.Clear();
        _activeSkillIndicators.UnionWith(_skillIndicatorBuffer);

        _skillIndicatorBuffer.Clear();
    }

    public void DisplayAttackAnimation(float animationDuration)
    {
        //animator.speed = 1 / animationDuration;
        //animator.Play("Base Layer.Attack");
    }

    private void OnEnable()
    {
        if(Instance != null)
        {
            OwlLogger.LogError("Duplicate PlayerMain!", GameComponent.Other);
            return;
        }

        Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    public void StatIncreaseRequest(EntityPropertyType type)
    {
        if(type == EntityPropertyType.Unknown)
        {
            OwlLogger.LogError("Can't send StatIncreaseRequest for unknown stat type!", GameComponent.Character);
            return;
        }

        StatIncreaseRequestPacket packet = new()
        {
            StatType = type,
        };

        if(ClientMain.Instance == null
            || ClientMain.Instance.ConnectionToServer == null)
        {
            OwlLogger.LogError($"Can't send StatIncreaseRequest for type {type} - no ConnectionToServer available.", GameComponent.Character);
            return;
        }
        ClientMain.Instance.ConnectionToServer.Send(packet);
    }

    public void DisplayBaseLvlUp()
    {
        SetSkilltext("Base Level UP!", 5);
    }

    public void DisplayJobLvlUp()
    {
        SetSkilltext("Job Level UP!", 5);
    }

    public void DisplayDeathAnimation(bool newValue)
    {
        if (newValue == _isDisplayingDeadState)
            return;

        _isDisplayingDeadState = newValue;
        UpdateDeathStateDisplay();
    }

    private void UpdateDeathStateDisplay()
    {
        if (_model == null)
            return;

        if(_isDisplayingDeadState)
        {
            Vector3 angles = _model.transform.rotation.eulerAngles;
            Vector3 pos = _model.transform.localPosition;
            angles.x = -90;
            pos.y = 0;
            _model.transform.SetLocalPositionAndRotation(pos, Quaternion.Euler(angles));
        }
        else
        {
            Vector3 angles = _model.transform.rotation.eulerAngles;
            Vector3 pos = _model.transform.localPosition;
            angles.x = 0;
            pos.y = 1;
            _model.transform.SetLocalPositionAndRotation(pos, Quaternion.Euler(angles));
        }
    }
}
