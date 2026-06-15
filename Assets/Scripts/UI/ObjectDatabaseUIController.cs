using System;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ObjectDatabaseUIController : MonoBehaviour
{
    public static ObjectDatabaseUIController Instance { get; private set; }

    const string MainPanelName = "Panel";
    const string ClosePanelButtonName = "ClosePanel";
    const string ObjectNameLabelName = "ObjectNameTitle";
    const string SearchFieldName = "SearchField";
    const string DatabaseTableName = "DatabaseTable";
    const string TraitsPanelName = "TraitsDisplayPanel";
    const string TraitsSearchBarName = "SearchBar";
    const string TraitsClosePanelName = "CloseTraitsPanel";
    const string EphemerisPanelName = "EphemerisDisplayPanel";
    const string EphemerisSearchBarName = "SearchBar";
    const string EphemerisClosePanelName = "CloseEphemerisPanel";

    [SerializeField] ObjectDatabaseManager _databaseManager;
    [SerializeField] PanelSettings _panelSettings;
    [SerializeField] TextMeshProUGUI _tabText;

    UIDocument _uiDocument;
    VisualElement _mainPanel;
    TextField _searchField;
    MultiColumnListView _mainTable;

    Label _ephemerisPanelObjectNameLabel;
    Label _traitsPanelObjectNameLabel;

    VisualElement _traitsPanel;
    TextField _traitsSearchField;
    MultiColumnListView _traitsTable;

    VisualElement _ephemerisPanel;
    TextField _ephemerisSearchField;
    MultiColumnListView _ephemerisTable;

    readonly List<string> _allObjectNames = new();
    readonly List<string> _filteredObjectNames = new();

    string _traitsObjectName = string.Empty;
    readonly List<PhysicalTraitsEntryJSON> _allTraits = new();
    readonly List<PhysicalTraitsEntryJSON> _filteredTraits = new();

    string _ephemerisObjectName = string.Empty;
    readonly List<EphemerisEntryJSON> _allEphemeris = new();
    readonly List<EphemerisEntryJSON> _filteredEphemeris = new();
    readonly List<EphemerisRowGroup> _groupedEphemerisRows = new();

    EventCallback<ChangeEvent<string>> _onSearchChanged;
    EventCallback<ChangeEvent<string>> _onTraitsSearchChanged;
    EventCallback<ChangeEvent<string>> _onEphemerisSearchChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (_databaseManager == null)
        {
            Debug.LogError("[ObjectDatabaseUIController] ObjectDatabaseManager not assigned.");
            enabled = false; return;
        }

        if (!TryGetComponent(out _uiDocument))
        {
            Debug.LogError("[ObjectDatabaseUIController] UIDocument component missing.");
            enabled = false; return;
        }

        VisualElement root = _uiDocument.rootVisualElement;

        _mainPanel = root.Q<VisualElement>(MainPanelName);
        _searchField = root.Q<TextField>(SearchFieldName);
        _mainTable = root.Q<MultiColumnListView>(DatabaseTableName);

        if (_mainPanel == null || _searchField == null || _mainTable == null)
        {
            Debug.LogError("[ObjectDatabaseUIController] One or more main panel UXML elements not found.");
            enabled = false; return;
        }

        _mainPanel.style.display = DisplayStyle.None;

        _mainPanel.Q<Button>(ClosePanelButtonName)?.RegisterCallback<ClickEvent>(_ => ClosePanel());

        _searchField.label = string.Empty;
        _onSearchChanged = evt => ApplyMainFilter(evt.newValue);
        _searchField.RegisterValueChangedCallback(_onSearchChanged);

        ConfigureMainTable();

        _traitsPanel = root.Q<VisualElement>(TraitsPanelName);

        if (_traitsPanel == null)
        {
            Debug.LogWarning("[ObjectDatabaseUIController] TraitsDisplayPanel not found – traits detail disabled.");
            return;
        }

        // #TraitsDisplayPanel > #SearchBar (VisualElement) > #SearchField (TextField)
        VisualElement traitsSearchBar = _traitsPanel.Q<VisualElement>(TraitsSearchBarName);
        _traitsSearchField = traitsSearchBar?.Q<TextField>(SearchFieldName);
        _traitsPanelObjectNameLabel = _traitsPanel.Q<Label>(ObjectNameLabelName);

        // #TraitsDisplayPanel > #DatabaseTable (MultiColumnListView)
        _traitsTable = _traitsPanel.Q<MultiColumnListView>(DatabaseTableName);

        if (_traitsSearchField == null || _traitsTable == null)
        {
            Debug.LogWarning("[ObjectDatabaseUIController] Traits panel child elements not found – traits detail disabled.");
            _traitsPanel = null;
            return;
        }

        _traitsPanel.style.display = DisplayStyle.None;

        _traitsPanel.Q<Button>(TraitsClosePanelName)?.RegisterCallback<ClickEvent>(_ => CloseTraitsPanel());

        _traitsSearchField.label = string.Empty;
        _onTraitsSearchChanged = evt => ApplyTraitsFilter(evt.newValue);
        _traitsSearchField.RegisterValueChangedCallback(_onTraitsSearchChanged);

        ConfigureTraitsTable();

        _ephemerisPanel = root.Q<VisualElement>(EphemerisPanelName);

        if (_ephemerisPanel == null)
        {
            Debug.LogWarning("[ObjectDatabaseUIController] EphemerisDisplayPanel not found – ephemeris detail disabled.");
            return;
        }

        VisualElement ephemerisSearchBar = _ephemerisPanel.Q<VisualElement>(EphemerisSearchBarName);
        _ephemerisSearchField = ephemerisSearchBar?.Q<TextField>(SearchFieldName);
        _ephemerisTable = _ephemerisPanel.Q<MultiColumnListView>(DatabaseTableName);
        _ephemerisPanelObjectNameLabel = _ephemerisPanel.Q<Label>(ObjectNameLabelName);

        if (_ephemerisSearchField == null || _ephemerisTable == null)
        {
            Debug.LogWarning("[ObjectDatabaseUIController] Ephemeris panel child elements not found – ephemeris detail disabled.");
            _ephemerisPanel = null;
            return;
        }

        _ephemerisPanel.style.display = DisplayStyle.None;
        _ephemerisPanel.Q<Button>(EphemerisClosePanelName)?.RegisterCallback<ClickEvent>(_ => CloseEphemerisPanel());

        _ephemerisSearchField.label = string.Empty;
        _onEphemerisSearchChanged = evt => ApplyEphemerisFilter(evt.newValue);
        _ephemerisSearchField.RegisterValueChangedCallback(_onEphemerisSearchChanged);

        ConfigureEphemerisTable();
    }

    void Start() => RefreshFromManager();

    void OnDisable()
    {
        if (_searchField != null && _onSearchChanged != null)
            _searchField.UnregisterValueChangedCallback(_onSearchChanged);

        if (_traitsSearchField != null && _onTraitsSearchChanged != null)
            _traitsSearchField.UnregisterValueChangedCallback(_onTraitsSearchChanged);

        if (_ephemerisSearchField != null && _onEphemerisSearchChanged != null)
            _ephemerisSearchField.UnregisterValueChangedCallback(_onEphemerisSearchChanged);
    }

    public void OpenPanel(int sortOrder = -1)
    {
        if (_mainPanel.style.display != DisplayStyle.None) return;
        if (sortOrder != -1) SetSortingOrder(sortOrder);
        RefreshFromManager();
        _mainPanel.style.display = DisplayStyle.Flex;
        if (_tabText != null) _tabText.fontStyle = FontStyles.Underline;
    }

    public void ClosePanel()
    {
        CloseTraitsPanel();
        CloseEphemerisPanel();
        _mainPanel.style.display = DisplayStyle.None;
        SetSortingOrder(0);
        if (_tabText != null) _tabText.fontStyle = FontStyles.Normal;
    }

    public void OpenClosePanel()
    {
        if (_ephemerisPanel.style.display == DisplayStyle.Flex) CloseEphemerisPanel();
        if (_traitsPanel.style.display == DisplayStyle.Flex) CloseTraitsPanel();
        if (_mainPanel.style.display == DisplayStyle.None) OpenPanel(sortOrder: 200);
        else ClosePanel();
    }

    public void RefreshFromManager()
    {
        _databaseManager.PopulateFromSavedData();
        _allObjectNames.Clear();
        _allObjectNames.AddRange(_databaseManager.GetAllObjectNames());
        ApplyMainFilter(_searchField?.value ?? string.Empty);
    }

    void ConfigureMainTable()
    {
        _mainTable.itemsSource = _filteredObjectNames;
        _mainTable.selectionType = SelectionType.None;
        _mainTable.fixedItemHeight = 36f;
        _mainTable.sortingMode = ColumnSortingMode.Custom;

        _mainTable.columnSortingChanged += () =>
        {
            ApplyMainSort();
            _mainTable.RefreshItems();
        };

        _mainTable.sortColumnDescriptions.Clear();
        _mainTable.sortColumnDescriptions.Add(
            new SortColumnDescription("ObjectName", SortDirection.Ascending));

        BuildMainColumns();
        _mainTable.Rebuild();
    }

    void BuildMainColumns()
    {
        _mainTable.columns.Clear();
        _mainTable.showAlternatingRowBackgrounds = AlternatingRowBackground.None;

        _mainTable.columns.Add(new Column
        {
            name = "ObjectName",
            title = "Object Name",
            width = 260,
            minWidth = 160,
            resizable = true,
            stretchable = true,
            sortable = true,
            makeCell = () =>
            {
                var label = MakeMainCellLabel(TextAnchor.MiddleLeft);
                label.RegisterCallback<PointerUpEvent>(evt => OnMainRowContextClick(evt, label));
                return label;
            },
            bindCell = (e, i) =>
            {
                if (!IndexValid(i, _filteredObjectNames)) return;
                string name = _filteredObjectNames[i];
                var label = (Label)e;
                label.style.fontSize = 16;
                label.text = name;
                label.userData = name;
            }
        });

        _mainTable.columns.Add(new Column
        {
            name = "Ephemeris",
            title = "Ephemeris",
            width = 220,
            minWidth = 160,
            resizable = true,
            stretchable = true,
            sortable = true,
            makeCell = () =>
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.paddingLeft = 8;

                var countLabel = new Label { name = "EphCountLabel" };
                countLabel.style.flexGrow = 1;
                countLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

                var viewBtn = new Button { name = "EphViewBtn", text = "View" };
                viewBtn.style.fontSize = 16;
                viewBtn.style.marginRight = 6;

                row.Add(countLabel);
                row.Add(viewBtn);
                return row;
            },
            bindCell = (e, i) =>
            {
                if (!IndexValid(i, _filteredObjectNames)) return;
                string name = _filteredObjectNames[i];
                int count = _databaseManager.GetEphemerisCount(name);

                var countLabel = e.Q<Label>("EphCountLabel");
                var viewBtn = e.Q<Button>("EphViewBtn");

                countLabel.text = count == 0 ? "No data" : $"{count} entr{(count == 1 ? "y" : "ies")}";
                viewBtn.SetEnabled(count > 0);

                viewBtn.clicked -= OnEphemerisViewClicked;
                viewBtn.clicked += OnEphemerisViewClicked;

                void OnEphemerisViewClicked() => OpenEphemerisPanel(name);
            }
        });

        _mainTable.columns.Add(new Column
        {
            name = "PhysicalTraits",
            title = "Physical Traits",
            width = 200,
            minWidth = 140,
            resizable = true,
            stretchable = true,
            sortable = true,
            makeCell = () =>
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.paddingLeft = 8;

                var statusLabel = new Label { name = "TraitsStatusLabel" };
                statusLabel.style.flexGrow = 1;

                statusLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

                var viewBtn = new Button { name = "TraitsViewBtn", text = "View" };
                viewBtn.style.fontSize = 16;
                viewBtn.style.marginRight = 6;

                row.Add(statusLabel);
                row.Add(viewBtn);
                return row;
            },
            bindCell = (e, i) =>
            {
                if (!IndexValid(i, _filteredObjectNames)) return;
                string name = _filteredObjectNames[i];
                int count = _databaseManager.GetPhysicalTraitsCount(name);

                var statusLabel = e.Q<Label>("TraitsStatusLabel");
                var viewBtn = e.Q<Button>("TraitsViewBtn");

                statusLabel.text = count == 0 ? "No data" : $"{count} trait{(count == 1 ? "" : "s")}";
                viewBtn.SetEnabled(count > 0);

                viewBtn.clicked -= OnTraitsViewClicked;
                viewBtn.clicked += OnTraitsViewClicked;

                void OnTraitsViewClicked() => OpenTraitsPanel(name);
            }
        });

        _mainTable.schedule.Execute(() =>
        {
            _mainTable
                .Query<VisualElement>(className: "unity-multi-column-header__column__content-container")
                .ForEach(container =>
                {
                    container.style.flexGrow = 0;
                    container.style.flexShrink = 0;
                });
        }).StartingIn(0);
    }

    public void ApplySearchText(string text)
    {
        _searchField?.SetValueWithoutNotify(text);
        ApplyMainFilter(text);
    }

    void ApplyMainFilter(string query)
    {
        query = query?.Trim() ?? string.Empty;
        _filteredObjectNames.Clear();

        if (string.IsNullOrEmpty(query))
        {
            _filteredObjectNames.AddRange(_allObjectNames);
        }
        else
        {
            string lower = query.ToLowerInvariant();
            foreach (string name in _allObjectNames)
                if (name.ToLowerInvariant().Contains(lower))
                    _filteredObjectNames.Add(name);
        }

        ApplyMainSort();
        _mainTable?.RefreshItems();
    }

    void ApplyMainSort()
    {
        if (_filteredObjectNames.Count <= 1) return;
        if (_mainTable.sortColumnDescriptions == null || _mainTable.sortColumnDescriptions.Count == 0) return;
        _filteredObjectNames.Sort(CompareByMainSortDescriptions);
    }

    int CompareByMainSortDescriptions(string a, string b)
    {
        foreach (var desc in _mainTable.sortColumnDescriptions)
        {
            int result = desc.columnName switch
            {
                "ObjectName" => string.Compare(a, b, StringComparison.OrdinalIgnoreCase),
                "Ephemeris" => _databaseManager.GetEphemerisCount(a).CompareTo(_databaseManager.GetEphemerisCount(b)),
                "PhysicalTraits" => _databaseManager.GetPhysicalTraitsCount(a).CompareTo(_databaseManager.GetPhysicalTraitsCount(b)),
                _ => 0
            };

            if (result != 0)
                return desc.direction == SortDirection.Descending ? -result : result;
        }
        return 0;
    }

    void OnMainRowContextClick(PointerUpEvent evt, Label label)
    {
        bool isContextClick = evt.button == 1 || (evt.button == 0 && (evt.modifiers & EventModifiers.Control) != 0);
        if (!isContextClick) return;
        if (label.userData is not string objectName) return;

        var menu = new GenericDropdownMenu();

        menu.AddItem("Copy/Object Name", false, () =>
        {
            GUIUtility.systemCopyBuffer = objectName;
            UIMessage.Instance.NewFadingMessage(MessageType.Info, $"Copied '{objectName}' to clipboard.", 3f);
        });

        menu.AddSeparator("");

        menu.AddItem("Delete All Data", false, () =>
        {
            UIMessage.Instance.NewUIConfirmation(
                message: $"Delete ALL saved data for '{objectName}'? This cannot be undone.",
                title: "Confirm Delete",
                onYes: () => DeleteObjectAndRefresh(objectName),
                onNo: null);
        });

        menu.DropDown(new Rect(evt.position, Vector2.zero), label, DropdownMenuSizeMode.Content);
        evt.StopPropagation();
    }

    void DeleteObjectAndRefresh(string objectName)
    {
        if (!HorizonsResponseSaver.TryRemoveObject(objectName))
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, $"Failed to remove '{objectName}' from database.", "Delete Failed");
            return;
        }

        HorizonsResponseSaver.TrySaveToFile();

        if (_traitsObjectName == objectName) CloseTraitsPanel();
        if (_ephemerisObjectName == objectName) CloseEphemerisPanel();

        RefreshFromManager();
        UIMessage.Instance.NewFadingMessage(MessageType.Success, $"'{objectName}' deleted from database.", 3f);
    }

    void OpenTraitsPanel(string objectName)
    {
        if (_traitsPanel == null) return;

        _traitsObjectName = objectName;
        _allTraits.Clear();
        _mainPanel.style.display = DisplayStyle.None; // Ensure main panel is closed when opening traits panel
        _traitsPanel.style.overflow = Overflow.Visible;
        _traitsPanelObjectNameLabel.text = $"'{objectName}' Physical Traits";

        IReadOnlyDictionary<string, ObjectDataJSON> db = HorizonsResponseSaver.GetAllSavedObjectData();
        if (db.TryGetValue(objectName, out ObjectDataJSON obj)) _allTraits.AddRange(obj.PhysicalTraitsData);

        _traitsSearchField.SetValueWithoutNotify(string.Empty);
        ApplyTraitsFilter(string.Empty);
        _traitsPanel.style.display = DisplayStyle.Flex;
    }

    void CloseTraitsPanel()
    {
        if (_traitsPanel == null) return;
        _traitsPanel.style.display = DisplayStyle.None;
        _mainPanel.style.display = DisplayStyle.Flex;
        _traitsObjectName = string.Empty;
    }

    void ConfigureTraitsTable()
    {
        _traitsPanel.style.overflow = Overflow.Visible;
        _traitsPanel.style.flexDirection = FlexDirection.Column;
        _traitsPanel.style.flexShrink = 1;

        _traitsTable.style.flexGrow = 1;
        _traitsTable.style.flexShrink = 1;
        _traitsTable.style.minHeight = 0;
        _traitsTable.style.overflow = Overflow.Hidden;

        _traitsTable.itemsSource = _filteredTraits;
        _traitsTable.selectionType = SelectionType.None;
        _traitsTable.fixedItemHeight = 28f;
        _traitsTable.sortingMode = ColumnSortingMode.Default;

        _traitsTable.itemsSource = _filteredTraits;
        _traitsTable.selectionType = SelectionType.None;
        _traitsTable.fixedItemHeight = 28f;
        _traitsTable.sortingMode = ColumnSortingMode.Custom; // change from Default to Custom

        _traitsTable.columnSortingChanged += () =>
        {
            if (_traitsTable.sortColumnDescriptions == null ||
                _traitsTable.sortColumnDescriptions.Count == 0) return;

            var desc = _traitsTable.sortColumnDescriptions[0];

            if (desc.columnName == "Property")
            {
                _filteredTraits.Sort((a, b) =>
                    desc.direction == SortDirection.Descending
                        ? string.Compare(b.TraitName, a.TraitName, StringComparison.OrdinalIgnoreCase)
                        : string.Compare(a.TraitName, b.TraitName, StringComparison.OrdinalIgnoreCase));
            }

            _traitsTable.RefreshItems();
        };

        _traitsTable.sortColumnDescriptions.Clear();
        _traitsTable.sortColumnDescriptions.Add(new SortColumnDescription("Property", SortDirection.Ascending));

        BuildTraitsColumns();
        _traitsTable.Rebuild();
    }
    void BuildTraitsColumns()
    {
        _traitsTable.columns.Clear();
        _traitsTable.showAlternatingRowBackgrounds = AlternatingRowBackground.None;

        static Label MakeTraitsLabel(TextAnchor align)
        {
            var label = new Label();
            label.AddToClassList("db-cell");
            label.style.paddingLeft = 8;
            label.style.unityTextAlign = align;
            label.style.width = Length.Percent(100);
            label.style.height = Length.Percent(100);
            label.pickingMode = PickingMode.Position;
            return label;
        }

        _traitsTable.columns.Add(new Column
        {
            name = "Property",
            title = "Property",
            width = 280,
            minWidth = 180,
            resizable = true,
            stretchable = true,
            sortable = true,
            makeCell = () => MakeTraitsLabel(TextAnchor.MiddleLeft),
            bindCell = (e, i) =>
            {
                if (!IndexValid(i, _filteredTraits)) return;

                var trait = _filteredTraits[i];
                var label = (Label)e;
                label.text = trait.TraitName;

                BindTraitsRowContextToCell(label, trait);
            }
        });

        _traitsTable.columns.Add(new Column
        {
            name = "Value",
            title = "Value",
            width = 300,
            minWidth = 160,
            resizable = true,
            stretchable = true,
            sortable = false,
            makeCell = () => MakeTraitsLabel(TextAnchor.MiddleLeft),
            bindCell = (e, i) =>
            {
                if (!IndexValid(i, _filteredTraits)) return;

                var trait = _filteredTraits[i];
                string display = trait.HasNumericValue
                    ? FormatNumeric(trait.NumericValue, trait.Unit)
                    : trait.StringValue ?? "-";

                var label = (Label)e;
                label.text = display;

                BindTraitsRowContextToCell(label, trait);
            }
        });

        _traitsTable.schedule.Execute(() =>
        {
            _traitsTable
                .Query<VisualElement>(
                    className: "unity-multi-column-header__column__content-container")
                .ForEach(container =>
                {
                    container.style.flexGrow = 0;
                    container.style.flexShrink = 0;
                });
        }).StartingIn(0);
    }

    void BindTraitsRowContextToCell(VisualElement cellRoot, PhysicalTraitsEntryJSON trait)
    {
        cellRoot.userData = trait;
        cellRoot.pickingMode = PickingMode.Position;

        cellRoot.UnregisterCallback<PointerUpEvent>(OnTraitsRowPointerUp);
        cellRoot.RegisterCallback<PointerUpEvent>(OnTraitsRowPointerUp);
    }

    void OnTraitsRowPointerUp(PointerUpEvent evt)
    {
        bool isContextClick =
            evt.button == 1 ||
            (evt.button == 0 && (evt.modifiers & EventModifiers.Control) != 0);

        if (!isContextClick)
            return;

        if (evt.currentTarget is not VisualElement cell)
            return;

        if (cell.userData is not PhysicalTraitsEntryJSON trait)
            return;

        var menu = new GenericDropdownMenu();

        menu.AddItem("Copy/Property Value", false, () =>
        {
            GUIUtility.systemCopyBuffer = trait.HasNumericValue ? FormatNumeric(trait.NumericValue, trait.Unit) : trait.StringValue ?? "-";
            UIMessage.Instance.NewFadingMessage(MessageType.Info, $"Copied '{(trait.HasNumericValue ? trait.NumericValue : trait.StringValue)}' to clipboard.", 3f);
        });

        menu.AddItem("Delete Property", false, () =>
        {
            UIMessage.Instance.NewUIConfirmation(
                message: $"Delete trait '{trait.TraitName}' for '{_traitsObjectName}'? This cannot be undone.",
                title: "Confirm Delete",
                onYes: () =>
                {
                    if (!HorizonsResponseSaver.TryRemovePhysicalTraitsEntry(
                        _traitsObjectName,
                        trait.TraitName))
                    {
                        UIMessage.Instance.NewUIMessage(
                            MessageType.Error,
                            $"Failed to remove trait entry for '{_traitsObjectName}'.",
                            "Delete Failed");
                        return;
                    }

                    HorizonsResponseSaver.TrySaveToFile();

                    _allTraits.Clear();
                    IReadOnlyDictionary<string, ObjectDataJSON> db =
                        HorizonsResponseSaver.GetAllSavedObjectData();

                    if (db.TryGetValue(_traitsObjectName, out ObjectDataJSON obj))
                        _allTraits.AddRange(obj.PhysicalTraitsData);

                    ApplyTraitsFilter(_traitsSearchField?.value ?? string.Empty);

                    UIMessage.Instance.NewFadingMessage(
                        MessageType.Success,
                        $"Trait '{trait.TraitName}' deleted for '{_traitsObjectName}'.",
                        3f);
                },
                onNo: null);
        });

        menu.DropDown(
            new Rect(evt.position, Vector2.zero),
            cell,
            DropdownMenuSizeMode.Content);

        evt.StopPropagation();
    }

    void OpenEphemerisPanel(string objectName)
    {
        if (_ephemerisPanel == null) return;

        _ephemerisObjectName = objectName;
        _allEphemeris.Clear();

        IReadOnlyDictionary<string, ObjectDataJSON> db = HorizonsResponseSaver.GetAllSavedObjectData();
        if (db.TryGetValue(objectName, out ObjectDataJSON obj))
            _allEphemeris.AddRange(obj.EphemerisData);

        _ephemerisPanelObjectNameLabel.text = $"'{objectName}' Ephemeris";
        _mainPanel.style.display = DisplayStyle.None;
        _ephemerisSearchField.SetValueWithoutNotify(string.Empty);
        ApplyEphemerisFilter(string.Empty);
        _ephemerisPanel.style.display = DisplayStyle.Flex;
    }

    void CloseEphemerisPanel()
    {
        if (_ephemerisPanel == null) return;
        _ephemerisPanel.style.display = DisplayStyle.None;
        _mainPanel.style.display = DisplayStyle.Flex;
        _ephemerisObjectName = string.Empty;
    }

    void ConfigureEphemerisTable()
    {
        _ephemerisPanel.style.overflow = Overflow.Visible;
        _ephemerisPanel.style.flexDirection = FlexDirection.Column;
        _ephemerisPanel.style.flexShrink = 1;

        _ephemerisTable.style.flexGrow = 1;
        _ephemerisTable.style.flexShrink = 1;
        _ephemerisTable.style.minHeight = 0;
        _ephemerisTable.style.overflow = Overflow.Hidden;

        _ephemerisTable.itemsSource = _groupedEphemerisRows;
        _ephemerisTable.selectionType = SelectionType.None;
        _ephemerisTable.fixedItemHeight = 52f;
        _ephemerisTable.sortingMode = ColumnSortingMode.Default;

        _ephemerisTable.sortingMode = ColumnSortingMode.Custom;
        _ephemerisTable.columnSortingChanged += () =>
        {
            if (_ephemerisTable.sortColumnDescriptions == null ||
                _ephemerisTable.sortColumnDescriptions.Count == 0) return;

            var desc = _ephemerisTable.sortColumnDescriptions[0];

            if (desc.columnName == "Date")
            {
                _groupedEphemerisRows.Sort((a, b) =>
                    desc.direction == SortDirection.Descending
                        ? b.DateTimeTicks.CompareTo(a.DateTimeTicks)
                        : a.DateTimeTicks.CompareTo(b.DateTimeTicks));
            }

            _ephemerisTable.RefreshItems();
        };

        // Set default sort direction
        _ephemerisTable.sortColumnDescriptions.Clear();
        _ephemerisTable.sortColumnDescriptions.Add(
            new SortColumnDescription("Date", SortDirection.Ascending));

        BuildEphemerisColumns();
        _ephemerisTable.Rebuild();
    }

    void OnEphemerisRowPointerUp(PointerUpEvent evt)
    {
        bool isContextClick =
            evt.button == 1 ||
            (evt.button == 0 && (evt.modifiers & EventModifiers.Control) != 0);

        if (!isContextClick)
            return;

        if (evt.currentTarget is not VisualElement cell)
            return;

        if (cell.userData is not EphemerisRowGroup group)
            return;

        string dateText = group.DateTime.ToString("yyyy-MM-dd HH:mm:ss");

        EphemerisEntryJSON positionEntry = null;
        EphemerisEntryJSON velocityEntry = null;

        foreach (var entry in group.Entries)
        {
            if (positionEntry == null && entry.HasPosition)
                positionEntry = entry;

            if (velocityEntry == null && entry.HasVelocity)
                velocityEntry = entry;

            if (positionEntry != null && velocityEntry != null)
                break;
        }

        bool hasPosition = positionEntry != null;
        bool hasVelocity = velocityEntry != null;

        var menu = new GenericDropdownMenu();

        menu.AddItem("Copy/Date", false, () =>
        {
            GUIUtility.systemCopyBuffer = dateText;
            UIMessage.Instance.NewFadingMessage(
                MessageType.Info,
                $"Copied '{dateText}' to clipboard.",
                3f);
        });

        menu.AddSeparator("");

        if (hasPosition)
        {
            menu.AddItem($"Apply Position", false, () =>
            {
                double3 pos = new(positionEntry.PosX, positionEntry.PosY, positionEntry.PosZ);
                ApplyVectorManager.Instance.OpenPanel(ApplyVectorManager.ApplyVectorMode.Position, pos);
            });
        }
        else
        {
            menu.AddDisabledItem($"Apply Position", false);
        }

        if (hasVelocity)
        {
            menu.AddItem($"Apply Velocity", false, () =>
            {
                double3 vel = new(velocityEntry.VelX, velocityEntry.VelY, velocityEntry.VelZ);
                ApplyVectorManager.Instance.OpenPanel(ApplyVectorManager.ApplyVectorMode.Velocity, vel);
            });
        }
        else
        {
            menu.AddDisabledItem($"Apply Velocity", false);
        }

        menu.AddSeparator("");

        menu.AddItem("Delete Ephemeris", false, () =>
        {
            UIMessage.Instance.NewUIConfirmation(
                message: $"Delete ephemeris entry for '{_ephemerisObjectName}' at {dateText}? This cannot be undone.",
                title: "Confirm Delete",
                onYes: () => DeleteEphemerisGroupAndRefresh(group),
                onNo: null);
        });

        menu.DropDown(
            new Rect(evt.position, Vector2.zero),
            cell,
            DropdownMenuSizeMode.Content);

        evt.StopPropagation();
    }

    void BuildEphemerisColumns()
    {
        _ephemerisTable.columns.Clear();
        _ephemerisTable.showAlternatingRowBackgrounds = AlternatingRowBackground.None;

        _ephemerisTable.columns.Add(new Column
        {
            name = "Date",
            title = "Date",
            width = 200,
            minWidth = 160,
            resizable = true,
            stretchable = false,
            sortable = true,
            makeCell = () =>
            {
                var label = MakeEphemerisCellLabel(TextAnchor.MiddleLeft);
                label.pickingMode = PickingMode.Position; // required for pointer events
                return label;
            },
            bindCell = (e, i) =>
            {
                if (!IndexValid(i, _groupedEphemerisRows)) return;

                var label = (Label)e;
                EphemerisRowGroup group = _groupedEphemerisRows[i];

                label.text = group.DateTime.ToString("yyyy-MM-dd HH:mm:ss");

                BindRowContextToCell(label, group);
            }
        });

        (string col, string title)[] xyzCols = { ("X", "X"), ("Y", "Y"), ("Z", "Z") };

        foreach (var (col, title) in xyzCols)
        {
            string capturedCol = col;

            _ephemerisTable.columns.Add(new Column
            {
                name = capturedCol,
                title = title,
                width = 220,
                minWidth = 140,
                resizable = true,
                stretchable = true,
                sortable = false,
                makeCell = () =>
                {
                    var container = new VisualElement();
                    container.style.flexDirection = FlexDirection.Column;
                    container.style.justifyContent = Justify.Center;
                    container.style.paddingLeft = 8;
                    container.style.height = Length.Percent(100);

                    for (int l = 0; l < 2; l++)
                    {
                        var line = new Label { name = $"{capturedCol}Line{l}" };
                        line.style.unityTextAlign = TextAnchor.MiddleLeft;
                        line.style.fontSize = 13;
                        container.Add(line);
                    }
                    return container;
                },
                bindCell = (e, i) =>
                {
                    if (!IndexValid(i, _groupedEphemerisRows)) return;

                    EphemerisRowGroup group = _groupedEphemerisRows[i];
                    var line0 = e.Q<Label>($"{capturedCol}Line0");
                    var line1 = e.Q<Label>($"{capturedCol}Line1");

                    line0.text = string.Empty;
                    line1.text = string.Empty;

                    foreach (EphemerisEntryJSON entry in group.Entries)
                    {
                        if (entry.HasPosition && string.IsNullOrEmpty(line0.text))
                        {
                            double posVal = capturedCol switch
                            {
                                "X" => entry.PosX,
                                "Y" => entry.PosY,
                                _ => entry.PosZ
                            };
                            line0.text = $"p{capturedCol}: {FormatEphemerisValue(posVal)}";
                        }

                        if (entry.HasVelocity && string.IsNullOrEmpty(line1.text))
                        {
                            double velVal = capturedCol switch
                            {
                                "X" => entry.VelX,
                                "Y" => entry.VelY,
                                _ => entry.VelZ
                            };
                            line1.text = $"v{capturedCol}: {FormatEphemerisValue(velVal)}";
                        }
                    }

                    BindRowContextToCell(e, group);
                }
            });
        }

        _ephemerisTable.schedule.Execute(() =>
        {
            _ephemerisTable
                .Query<VisualElement>(className: "unity-multi-column-header__column__content-container")
                .ForEach(container =>
                {
                    container.style.flexGrow = 0;
                    container.style.flexShrink = 0;
                });
        }).StartingIn(0);
    }

    void BindRowContextToCell(VisualElement cellRoot, EphemerisRowGroup group)
    {
        cellRoot.userData = group;
        cellRoot.pickingMode = PickingMode.Position;

        cellRoot.UnregisterCallback<PointerUpEvent>(OnEphemerisRowPointerUp);
        cellRoot.RegisterCallback<PointerUpEvent>(OnEphemerisRowPointerUp);
    }

    void DeleteEphemerisGroupAndRefresh(EphemerisRowGroup group)
    {
        bool anyRemoved = false;

        foreach (EphemerisEntryJSON entry in group.Entries)
        {
            if (HorizonsResponseSaver.TryRemoveEphemerisEntry(_ephemerisObjectName, entry.DedupKey))
                anyRemoved = true;
        }

        if (!anyRemoved)
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error,
                $"Failed to remove ephemeris entry for '{_ephemerisObjectName}'.", "Delete Failed");
            return;
        }

        HorizonsResponseSaver.TrySaveToFile();

        // Resync '_allEphemeris' from saved data
        _allEphemeris.Clear();
        IReadOnlyDictionary<string, ObjectDataJSON> db = HorizonsResponseSaver.GetAllSavedObjectData();
        if (db.TryGetValue(_ephemerisObjectName, out ObjectDataJSON obj))
            _allEphemeris.AddRange(obj.EphemerisData);

        ApplyEphemerisFilter(_ephemerisSearchField?.value ?? string.Empty);

        UIMessage.Instance.NewFadingMessage(MessageType.Success,
            $"Ephemeris entry deleted for '{_ephemerisObjectName}'.", 3f);
    }

    void ApplyEphemerisFilter(string query)
    {
        query = query?.Trim().ToLowerInvariant() ?? string.Empty;

        // Group all entries by DateTimeTicks
        var grouped = new Dictionary<long, EphemerisRowGroup>();
        foreach (EphemerisEntryJSON entry in _allEphemeris)
        {
            if (!grouped.TryGetValue(entry.DateTimeTicks, out EphemerisRowGroup group))
            {
                group = new EphemerisRowGroup { DateTimeTicks = entry.DateTimeTicks };
                grouped[entry.DateTimeTicks] = group;
            }
            group.Entries.Add(entry);
        }

        // Sort groups by date ascending
        var sortedGroups = new List<EphemerisRowGroup>(grouped.Values);
        sortedGroups.Sort((a, b) => a.DateTimeTicks.CompareTo(b.DateTimeTicks));

        // Apply search filter against the date string
        _groupedEphemerisRows.Clear();
        foreach (EphemerisRowGroup group in sortedGroups)
        {
            if (string.IsNullOrEmpty(query) ||
                group.DateTime.ToString("yyyy-MM-dd HH:mm:ss").Contains(query))
            {
                _groupedEphemerisRows.Add(group);
            }
        }

        bool descending = _ephemerisTable.sortColumnDescriptions?.Count > 0 &&
                    _ephemerisTable.sortColumnDescriptions[0].direction == SortDirection.Descending;

        sortedGroups.Sort((a, b) => descending
            ? b.DateTimeTicks.CompareTo(a.DateTimeTicks)
            : a.DateTimeTicks.CompareTo(b.DateTimeTicks));

        _ephemerisTable?.RefreshItems();
    }

    static Label MakeEphemerisCellLabel(TextAnchor align)
    {
        var label = new Label();
        label.AddToClassList("db-cell");
        label.style.paddingLeft = 8;
        label.style.unityTextAlign = align;
        label.style.width = Length.Percent(100);
        label.style.height = Length.Percent(100);
        return label;
    }

    static string FormatEphemerisValue(double value)
    {
        return Math.Abs(value) >= 1e6 || (Math.Abs(value) < 1e-3 && value != 0)
            ? value.ToString("G6")
            : value.ToString("G10");
    }

    void ApplyTraitsFilter(string query)
    {
        query = query?.Trim().ToLowerInvariant() ?? string.Empty;
        _filteredTraits.Clear();

        foreach (var trait in _allTraits)
        {
            if (string.IsNullOrEmpty(query) ||
                trait.TraitName.ToLowerInvariant().Contains(query))
            {
                _filteredTraits.Add(trait);
            }
        }

        bool descending =
            _traitsTable.sortColumnDescriptions?.Count > 0 &&
            _traitsTable.sortColumnDescriptions[0].direction == SortDirection.Descending;

        _filteredTraits.Sort((a, b) =>
            descending
                ? string.Compare(b.TraitName, a.TraitName, StringComparison.OrdinalIgnoreCase)
                : string.Compare(a.TraitName, b.TraitName, StringComparison.OrdinalIgnoreCase));

        _traitsTable?.RefreshItems();
    }

    static Label MakeMainCellLabel(TextAnchor align)
    {
        var label = new Label();
        label.AddToClassList("db-cell");
        label.style.paddingLeft = 8;
        label.style.unityTextAlign = align;
        label.style.width = Length.Percent(100);
        label.style.height = Length.Percent(100);
        label.pickingMode = PickingMode.Position;
        return label;
    }

    static string FormatNumeric(double value, string unitName)
    {
        Enum.TryParse(unitName, out UnitMeasurements unit);
        string unitStr = HorizonsParser.UnitMeasurementsToString(unit);
        string numStr = Math.Abs(value) >= 1e6 || (Math.Abs(value) < 1e-3 && value != 0)
            ? value.ToString("G6")
            : value.ToString("G10");
        return unitStr == unitName ? numStr : $"{numStr} {unitStr}";
    }

    static bool IndexValid<T>(int i, List<T> list) => i >= 0 && i < list.Count;

    void SetSortingOrder(int order) { if (_panelSettings != null) _panelSettings.sortingOrder = order; }
}

public class EphemerisRowGroup
{
    public long DateTimeTicks;
    public DateTime DateTime => new(DateTimeTicks, DateTimeKind.Utc);
    public List<EphemerisEntryJSON> Entries = new();
}