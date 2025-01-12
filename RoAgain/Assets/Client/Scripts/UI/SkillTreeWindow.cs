using Client;
using OwlLogging;
using Shared;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// TODO: Move to better file
public class SkillTreeEntry
{
    public SkillId SkillId;
    public int Tier;
    public int Position;
    public int MaxSkillLvl;
    public int LearnedSkillLvl;
    public bool CanPointLearn;
    public SkillCategory Category;
    public Dictionary<SkillId, int> Requirements = new();

    public SkillTreeEntry() { }

    public SkillTreeEntry(SkillTreeEntryPacket packet)
    {
        SkillId = packet.SkillId;
        Tier = packet.Tier;
        Position = packet.Position;
        MaxSkillLvl = packet.MaxSkillLvl;
        LearnedSkillLvl = packet.LearnedSkillLvl;
        CanPointLearn = packet.CanPointLearn;
        Category = packet.Category;
        Requirements = new();
        if(packet.Requirement1Id != SkillId.Unknown)
        {
            Requirements.Add(packet.Requirement1Id, packet.Requirement1Level);
        }
        if(packet.Requirement2Id != SkillId.Unknown)
        {
            Requirements.Add(packet.Requirement2Id, packet.Requirement2Level);
        }
        if(packet.Requirement3Id != SkillId.Unknown)
        {
            Requirements.Add(packet.Requirement3Id, packet.Requirement3Level);
        }
    }
}

public class SkillTreeWindow : MonoBehaviour, IPointerMoveHandler
{
    [SerializeField]
    private GameObject _skillIconContainer;

    [SerializeField]
    private GameObject _skillTreeEntryPrefab;

    [SerializeField]
    private GameObject _categoryButtonContainer;

    [SerializeField]
    private TMP_Text _remainingSkillPointText;

    [SerializeField]
    private Button _applyButton;

    [SerializeField]
    private Button _clearButton;

    [SerializeField]
    private Button _closeButton;

    private Dictionary<SkillId, SkillTreeEntry> _entries = new();
    private Dictionary<SkillId, SkillTreeEntryWidget> _skillWidgets = new();
    private List<GameObject> _allCreatedWidgets = new();
    private SkillTreeEntryWidget _lastHoveredSkill = null;

    private GridLayoutGroup _skillIconContainerLayout = null;

    private SkillCategory _currentCategory = SkillCategory.FirstClass;

    private Dictionary<SkillId, int> _plannedSkillPoints = new();
    private int _projectedRemaining = 99;

    public int UpdateDisplay(ICollection<SkillTreeEntry> entries)
    {
        _entries.Clear();

        if (entries == null)
        {
            OwlLogger.LogWarning($"SkillTreeWindow initialized with null entries-list - is this intentional?", GameComponent.UI);
            return -1;
        }

        foreach(SkillTreeEntry entry in entries)
        {
            _entries.Add(entry.SkillId, entry);
        }

        // Fill child list with entryWidgets & set to empty
        PopulateUiFromEntries();

        SetupCategoryButtons();

        UpdateRemainingSkillPoints();

        return 0;
    }

    private void PopulateUiFromEntries()
    {
        foreach (GameObject widget in _allCreatedWidgets)
        {
            widget.transform.SetParent(null);
            Destroy(widget);
        }
        _skillWidgets.Clear();
        _allCreatedWidgets.Clear();

        foreach (SkillTreeEntry entry in _entries.Values)
        {
            if (entry.Category != _currentCategory)
                continue;

            int desiredIndex = CalculateChildIndex(entry);
            FillChildrenForIndex(desiredIndex);

            GameObject widgetObj = _skillIconContainer.transform.GetChild(desiredIndex).gameObject;
            SkillTreeEntryWidget widget = widgetObj.GetComponent<SkillTreeEntryWidget>();
            widget.SetDisplayVisible();
            widget.SetSkillId(entry.SkillId);
            widget.SetMaxLevel(entry.LearnedSkillLvl);
            widget.SetCurrentLevel(entry.LearnedSkillLvl); // TODO: Store selected skill level clientside
            widget.SetPlannedSkillPoints(0);
            widget.SetRequiredSkillLevel(0);
            widget.Clicked += OnEntryWidgetClicked;
            _skillWidgets.Add(entry.SkillId, widget);
        }
    }

    private void SetupCategoryButtons()
    {
        int childCount = _categoryButtonContainer.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var categoryButton = _categoryButtonContainer.transform.GetChild(i).GetComponentInChildren<SkillTreeCategoryButton>();
            if (categoryButton == null)
                continue;

            OwlLogger.Log($"SkillTreeWindow discovered CategoryButton for category {categoryButton.Category}: {categoryButton.gameObject.name}", GameComponent.UI);

            categoryButton.OnClick += OnCategoryButtonClicked;
        }
    }

    private void OnCategoryButtonClicked(SkillCategory category)
    {
        if (category == _currentCategory)
            return;

        _currentCategory = category;

        PopulateUiFromEntries();

        // have to clean up some internal values that we can't carry over through disabling & reenabling a category
        _plannedSkillPoints.Clear();
        UpdateRemainingSkillPoints();
    }

    private void FillChildrenForIndex(int index)
    {
        int missingChildCount = index + 1 - _skillIconContainer.transform.childCount;
        if (missingChildCount <= 0)
            return;

        for(int i = 0; i < missingChildCount; i++)
        {
            GameObject entryObj = Instantiate(_skillTreeEntryPrefab, _skillIconContainer.transform);
            SkillTreeEntryWidget widget = entryObj.GetComponent<SkillTreeEntryWidget>();
            if (widget == null)
            {
                OwlLogger.LogError($"SkillTreeWindow can't find SkillTreeEntryWidget component on SkillIconPrefab!", GameComponent.UI);
                Destroy(entryObj);
                return;
            }
            widget.SetDisplayEmpty();
            _allCreatedWidgets.Add(entryObj);
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        SkillTreeEntryWidget hoveredIcon = FindHoveredWidget(eventData);

        if (hoveredIcon == _lastHoveredSkill)
            return; // No update to visuals necessary

        EndHover(_lastHoveredSkill);
        BeginHover(hoveredIcon);
    }

    private SkillTreeEntryWidget FindHoveredWidget(PointerEventData eventData)
    {
        SkillTreeEntryWidget hoveredIcon = null;
        foreach (GameObject hoveredObj in eventData.hovered)
        {
            foreach (SkillTreeEntryWidget widget in _skillWidgets.Values)
            {
                if (widget.gameObject == hoveredObj)
                {
                    hoveredIcon = widget;
                    break;
                }
            }
            if (hoveredIcon != null)
                break;
        }
        return hoveredIcon;
    }

    private void BeginHover(SkillTreeEntryWidget hoverWidget)
    {
        _lastHoveredSkill = hoverWidget;
        if (hoverWidget == null)
            return;

        SkillId skillId = hoverWidget.SkillId;
        hoverWidget.SetHighlight(true);

        Dictionary<SkillId, int> allRequirements = FindAllRequirements(skillId);
        foreach(KeyValuePair<SkillId, int> pair in allRequirements)
        {
            if(!_skillWidgets.TryGetValue(pair.Key, out SkillTreeEntryWidget widget))
                continue; // Requirement might be in a different category
            
            widget.SetRequiredSkillLevel(pair.Value);
            widget.SetHighlight(true);
        }
    }

    private void EndHover(SkillTreeEntryWidget hoverWidget)
    {
        _lastHoveredSkill = null;
        if (hoverWidget == null)
            return;

        SkillId skillId = hoverWidget.SkillId;
        hoverWidget.SetHighlight(false);

        Dictionary<SkillId, int> allRequirements = FindAllRequirements(skillId);
        foreach (KeyValuePair<SkillId, int> pair in allRequirements)
        {
            if (!_skillWidgets.TryGetValue(pair.Key, out SkillTreeEntryWidget widget))
                continue; // Requirement might be in a different category

            widget.SetRequiredSkillLevel(0);
            widget.SetHighlight(false);
        }
    }

    private Dictionary<SkillId, int> FindAllRequirements(SkillId skillId)
    {
        Dictionary<SkillId, int> allRequirements = new();
        SkillTreeEntry startEntry = _entries[skillId];
        List<SkillTreeEntry> entriesToProcess = new()
        {
            startEntry
        };

        while (entriesToProcess.Count > 0 )
        {
            if(entriesToProcess.Count > 20)
            {
                OwlLogger.LogError("Aborting FindAllRequirements - SkillTreeEntry count too large, likely infinite loop!", GameComponent.UI);
                break;
            }
            SkillTreeEntry processedEntry = entriesToProcess[0];
            foreach (KeyValuePair<SkillId, int> requirement in processedEntry.Requirements)
            {
                allRequirements.TryGetValue(requirement.Key, out int prevRequirement);

                allRequirements[requirement.Key] = Mathf.Max(prevRequirement, requirement.Value);
                entriesToProcess.Add(_entries[requirement.Key]);
            }
            entriesToProcess.Remove(processedEntry);
        }

        return allRequirements;        
    }

    private SkillTreeEntryWidget FindWidgetForSkillId(SkillId skillId)
    {
        if (!_skillWidgets.ContainsKey(skillId))
            return null;

        return _skillWidgets[skillId];
    }

    void Awake()
    {
        OwlLogger.PrefabNullCheckAndLog(_skillTreeEntryPrefab, "skillTreeEntryPrefab", this, GameComponent.UI);

        if(!OwlLogger.PrefabNullCheckAndLog(_skillIconContainer, "skillIconContainer", this, GameComponent.UI))
        {
            _skillIconContainerLayout = _skillIconContainer.GetComponent<GridLayoutGroup>();
            if (_skillIconContainerLayout == null)
            {
                OwlLogger.LogError("SkillTreeWindow can't find GridLayoutGroup on SkillIconContainer!", GameComponent.UI);
            }
            else
            {
                if (_skillIconContainerLayout.constraint != GridLayoutGroup.Constraint.FixedColumnCount)
                {
                    OwlLogger.LogError("SkillTreeWindow expects SkillIconContainer's GridLayoutGroup to be set to FixedColumnCount!", GameComponent.UI);
                }
            }
        }

        OwlLogger.PrefabNullCheckAndLog(_categoryButtonContainer, "categoryButtonContainer", this, GameComponent.UI);

        OwlLogger.PrefabNullCheckAndLog(_remainingSkillPointText, "remainingSkillPointText", this, GameComponent.UI);

        if(!OwlLogger.PrefabNullCheckAndLog(_applyButton, "applyButton", this, GameComponent.UI))
            _applyButton.onClick.AddListener(OnApplyButtonClicked);
        
        if(!OwlLogger.PrefabNullCheckAndLog(_clearButton, "clearButton", this, GameComponent.UI))
            _clearButton.onClick.AddListener(OnClearButtonClicked);

        if (!OwlLogger.PrefabNullCheckAndLog(_closeButton, "closeButton", this, GameComponent.UI))
            _closeButton.onClick.AddListener(OnCloseButtonClicked);

        if (ClientMain.Instance.CurrentCharacterData == null)
        {
            OwlLogger.LogError("SkillTreeWindow opened before CurrentCharacterData was available!", GameComponent.UI);
        }
        else
        {
            ClientMain.Instance.CurrentCharacterData.SkillTreeUpdated += DisplayCurrentCharacterData;
            DisplayCurrentCharacterData();
        }
    }

    public void DisplayCurrentCharacterData()
    {
        UpdateDisplay(ClientMain.Instance.CurrentCharacterData.SkillTree.Values);
    }

    private int CalculateChildIndex(SkillTreeEntry entry)
    {
        return entry.Tier * _skillIconContainerLayout.constraintCount + entry.Position;
    }

    public void UpdateRemainingSkillPoints()
    {
        if (ClientMain.Instance.CurrentCharacterData == null)
        {
            OwlLogger.LogWarning("Can't update Remaining Skillpoints - CurrentCharacterData is null.", GameComponent.UI);
            return;
        }

        _projectedRemaining = ClientMain.Instance.CurrentCharacterData.RemainingSkillPoints;
        foreach (KeyValuePair<SkillId, int> pair in _plannedSkillPoints)
        {
            _projectedRemaining -= pair.Value;
        }

        if (_projectedRemaining < 0)
        {
            OwlLogger.LogError($"{_projectedRemaining} skill points remaining after planning!", GameComponent.UI);
        }

        if(_projectedRemaining == ClientMain.Instance.CurrentCharacterData.RemainingSkillPoints)
        {
            _remainingSkillPointText.text = ClientMain.Instance.CurrentCharacterData.RemainingSkillPoints.ToString();
        }
        else
        {
            _remainingSkillPointText.text = $"{ClientMain.Instance.CurrentCharacterData.RemainingSkillPoints} -> {_projectedRemaining}";
        }
    }

    private void OnEntryWidgetClicked(SkillTreeEntryWidget widget)
    {
        SkillTreeEntry entry = _entries[widget.SkillId];
        if (entry.LearnedSkillLvl == entry.MaxSkillLvl)
            return;

        if (!entry.CanPointLearn)
            return;

        if (ClientMain.Instance.CurrentCharacterData == null)
            return;

        if (ClientMain.Instance.CurrentCharacterData.RemainingSkillPoints == 0)
            return;

        _plannedSkillPoints.TryGetValue(entry.SkillId, out int currentPlannedPoints);
        bool planSuccessful = PlanSkillPoints(entry, currentPlannedPoints +1);
        UpdateRemainingSkillPoints();
    }

    private bool PlanSkillPoints(SkillTreeEntry skillEntry, int planAmount)
    {
        if(_plannedSkillPoints.TryGetValue(skillEntry.SkillId, out int alreadyPlannedPoints))
        {
            if (alreadyPlannedPoints >= planAmount)
                return true;
        }

        if(planAmount > skillEntry.MaxSkillLvl - skillEntry.LearnedSkillLvl)
            return false;

        bool allRequirementsFulfilled = true;
        foreach(SkillId reqId in skillEntry.Requirements.Keys)
        {
            if (_plannedSkillPoints.ContainsKey(reqId)
                && (_plannedSkillPoints[reqId] >= skillEntry.Requirements[reqId]
                    || _entries[reqId].LearnedSkillLvl >= skillEntry.Requirements[reqId]))
            {
                // requirement is already fulfilled or planned to fulfill
            }
            else
            {
                allRequirementsFulfilled &= PlanSkillPoints(_entries[reqId], skillEntry.Requirements[reqId]);
            }
        }

        UpdateRemainingSkillPoints();
        if(_projectedRemaining == 0)
            return false;

        if(!allRequirementsFulfilled)
            return false;

        int requiredSkillPointsForPlan = planAmount - alreadyPlannedPoints;
        int actualPlanAmount = planAmount;
        if (_projectedRemaining < requiredSkillPointsForPlan)
        {
            actualPlanAmount = alreadyPlannedPoints + _projectedRemaining;
        }
        
        _plannedSkillPoints[skillEntry.SkillId] = actualPlanAmount;
        SkillTreeEntryWidget widget = FindWidgetForSkillId(skillEntry.SkillId);
        if (widget != null)
        {
            // Update Widget if it's currently displayed
            _skillWidgets[skillEntry.SkillId].SetPlannedSkillPoints(actualPlanAmount);
        }
        return planAmount == actualPlanAmount;
    }

    private void OnApplyButtonClicked()
    {
        foreach(KeyValuePair<SkillId, int> pair in _plannedSkillPoints)
        {
            SkillPointAllocateRequestPacket packet = new()
            {
                SkillId = pair.Key,
                Amount = pair.Value
            };
            ClientMain.Instance.ConnectionToServer.Send(packet);
        }

        OnClearButtonClicked();
    }

    private void OnClearButtonClicked()
    {
        _plannedSkillPoints.Clear();
        foreach(SkillTreeEntryWidget widget in _skillWidgets.Values)
        {
            widget.SetPlannedSkillPoints(0);
        }
        UpdateRemainingSkillPoints();
    }

    private void OnCloseButtonClicked()
    {
        gameObject.SetActive(false);
    }
}
