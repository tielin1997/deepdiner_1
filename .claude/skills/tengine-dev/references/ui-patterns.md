# UI 开发模式与模板

## 完整 UIWindow 示例

```csharp
[Window(UILayer.UI, "BattleMainUI", fullScreen: true)]
public class BattleMainUI : UIWindow
{
    private Button    _btnBack;
    private Text      _textHp;
    private Text      _textGold;
    private Transform _tfSkillPanel;
    private readonly List<SkillSlotWidget> _skillSlots = new();

    protected override void ScriptGenerator()
    {
        _btnBack      = FindChildComponent<Button>("m_btn_Back");
        _textHp       = FindChildComponent<Text>("m_text_Hp");
        _textGold     = FindChildComponent<Text>("m_text_Gold");
        _tfSkillPanel = FindChild("m_tf_SkillPanel");
        RegisterButtonClick(_btnBack, OnBackClicked);
    }

    protected override void RegisterEvent()
    {
        AddUIEvent<int>(IBattleEvent_Event.OnHpChanged, RefreshHp);
        AddUIEvent<int>(IBattleEvent_Event.OnGoldChanged, RefreshGold);
    }

    protected override void OnCreate()
    {
        for (int i = 0; i < 4; i++)
        {
            var slot = CreateWidget<SkillSlotWidget>($"m_tf_SkillPanel/Slot_{i}");
            _skillSlots.Add(slot);
        }
    }

    protected override void OnRefresh()
    {
        RefreshHp(PlayerData.Hp);
        RefreshGold(PlayerData.Gold);
        for (int i = 0; i < _skillSlots.Count; i++)
            _skillSlots[i].SetData(PlayerData.Skills[i]);
    }

    private void RefreshHp(int hp)    => _textHp.text   = $"HP: {hp}";
    private void RefreshGold(int gold) => _textGold.text = $"Gold: {gold}";
    private void OnBackClicked() => GameModule.UI.CloseUI<BattleMainUI>();
}
```

---

## UIWidget 模板

```csharp
public class ItemWidget : UIWidget
{
    private Text  _textName;
    private Image _imgIcon;

    protected override void ScriptGenerator()
    {
        _textName = FindChildComponent<Text>("m_text_Name");
        _imgIcon  = FindChildComponent<Image>("m_img_Icon");
    }

    public void SetData(ItemConfig cfg)
    {
        _textName.text = cfg.Name;
        _imgIcon.SetSprite(cfg.IconPath);  // 内置缓存池，无需手动释放
    }
}
```

---

## UIWidget 创建方式

```csharp
var w1 = CreateWidget<ItemWidget>("path/to/node");                            // Prefab 中已有节点
var w2 = CreateWidgetByPath<ItemWidget>(parent, "ItemWidgetPrefab");          // 动态加载
var w3 = await CreateWidgetByPathAsync<ItemWidget>(parent, "ItemWidgetPrefab"); // 异步
var w4 = CreateWidgetByPrefab<ItemWidget>(prefab, parent);                    // Prefab 克隆（列表）
```

---

## 列表 Widget 复用

```csharp
// 自动增删 Widget 使数量匹配 count
AdjustIconNum<ItemWidget>(listIcon, count: items.Count, parent, prefab);

// 刷新每个 Widget
for (int i = 0; i < items.Count; i++)
    listIcon[i].SetData(items[i]);
```

---

## 手动绑定 API

```csharp
protected override void ScriptGenerator()
{
    var trans = FindChild("m_tf_Container");
    var btn   = FindChildComponent<Button>("m_btn_Start");
    var rect  = FindChildComponent<RectTransform>("m_rect_Panel");
    var tmp   = FindChildComponent<TextMeshProUGUI>("m_tmp_Name");
    RegisterButtonClick(btn, OnStartClicked);
}
```

节点前缀完整表见 [naming-rules.md](naming-rules.md#ui-节点命名规范)。
生命周期核心 API 见 [ui-lifecycle.md](ui-lifecycle.md)。
