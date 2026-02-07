using System;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class NAIFDatabaseUIController : MonoBehaviour
{
    public static NAIFDatabaseUIController Instance { get; private set; }
    // UXML element names
    const string SearchFieldName = "SearchField";
    const string DatabaseTableName = "DatabaseTable";
    const string AddEntryButtonName = "AddButton";
    const string DisplayPanelName = "Panel";
    const string ClosePanelButtonName = "ClosePanel";
    const string TooltipHoverName = "TooltipHover";
    const string TooltipMessageName = "TooltipMessage";

    // Add Entry Panel
    const string TryAddButtonName = "TryAddButton";
    const string AddPanelName = "AddPanel";
    const string CloseAddPanelButtonName = "CloseAddPanel";
    const string AddNAIFIDInputName = "NAIFIDInput";
    const string AddNameInputName = "NameInput";
    const string AddDesignationInputName = "DesignationInput";
    const string AddAliasesInputName = "AliasesInput";
    const string NAIFIDInputName = "NAIFIDInput";

    [Header("References")]
    [SerializeField] NAIFCatalogManager _NAIFCatalogDBManager;
    [SerializeField] TextMeshProUGUI _naifDatabaseTabText;

    // UI Toolkit refs
    UIDocument _uiDocument;
    TextField _searchField;
    MultiColumnListView _databaseTable;
    VisualElement _displayPanel;
    VisualElement _addEntryPanel;
    TextField _addNAIFIDInput;
    TextField _addNameInput;
    TextField _addDesignationInput;
    TextField _addAliasesInput;


    // Data
    readonly List<BodyCatalog> _filteredCatalogDB = new();
    List<BodyCatalog> _runtimeCatalogDB = new();

    // For unregistering cleanly
    EventCallback<ChangeEvent<string>> _onSearchChangedCallback;

    // Tooltip 
    VisualElement _tooltip;
    Label _tooltipMessage;
    TextField _naifIDInput;
    bool _isHovering;
    bool _tooltipVisible;
    float _lastMoveTimeUnscaled;
    Vector2 _lastPointerPanelLocalPos; // event position is in panel space
    string _pendingMessage;
    readonly float _moveEpsilonSqr = 0;
    readonly float _hoverDelaySeconds = 1.25f;
    Coroutine _idleWatcherCoroutine;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;

        if (_NAIFCatalogDBManager == null)
        {
            Debug.LogError("NAIFDatabaseUIController: _NAIFCatalogDBManager is not assigned.");
            enabled = false;
            return;
        }

        if (!TryGetComponent(out _uiDocument))
        {
            Debug.LogError("NAIFDatabaseUIController: Could not find UIDocument on this GameObject.");
            enabled = false;
            return;
        }

        VisualElement root = _uiDocument.rootVisualElement;

        _searchField = root.Q<TextField>(SearchFieldName);
        _databaseTable = root.Q<MultiColumnListView>(DatabaseTableName);
        _tooltip = root.Q<VisualElement>(TooltipHoverName);
        _tooltipMessage = root.Q<Label>(TooltipMessageName);
        _tooltip.style.position = Position.Absolute;
        _tooltip.pickingMode = PickingMode.Ignore;
        _tooltip.style.display = DisplayStyle.None;

        _addEntryPanel = root.Q<VisualElement>(AddPanelName);
        _addEntryPanel.style.display = DisplayStyle.None;
        _addNAIFIDInput = root.Q<TextField>(AddNAIFIDInputName);
        _addNameInput = root.Q<TextField>(AddNameInputName);
        _addDesignationInput = root.Q<TextField>(AddDesignationInputName);
        _addAliasesInput = root.Q<TextField>(AddAliasesInputName);

        _displayPanel = root.Q<VisualElement>(DisplayPanelName);
        _displayPanel.style.display = DisplayStyle.None;

        if (_searchField == null || _databaseTable == null)
        {
            Debug.LogError(
                $"NAIFDatabaseUIController: UI elements not found.\n" +
                $"- TextField name expected: '{SearchFieldName}'\n" +
                $"- MultiColumnListView name expected: '{DatabaseTableName}'\n" +
                $"Check your UXML element names in UI Builder."
            );
            enabled = false;
            return;
        }

        _naifIDInput = root.Q<TextField>(NAIFIDInputName);
        _naifIDInput.RegisterCallback<PointerEnterEvent>(OnNAIFIDInputEnter);
        _naifIDInput.RegisterCallback<PointerMoveEvent>(OnNAIFIDInputMove);
        _naifIDInput.RegisterCallback<PointerLeaveEvent>(OnNAIFIDInputLeave);
        _naifIDInput.RegisterCallback<PointerDownEvent>(_ => ResetHoverTimerAndHide());
        _naifIDInput.RegisterCallback<WheelEvent>(_ => ResetHoverTimerAndHide());

        var _closePanelButton = root.Q<Button>(ClosePanelButtonName);
        var _closeAddPanelButton = root.Q<Button>(CloseAddPanelButtonName);
        var _tryAddEntryButton = root.Q<Button>(TryAddButtonName);
        var _addEntryButton = root.Q<Button>(AddEntryButtonName);

        _closePanelButton.clicked += ClosePanel;
        _closeAddPanelButton.clicked += CloseAddEntryPanel;
        _addEntryButton.clicked += OnAddClicked;
        _tryAddEntryButton.clicked += TryAddEntry;

        // Optional: remove built-in label spacing
        _searchField.label = string.Empty;

        ConfigureDatabaseTable();

        _onSearchChangedCallback = evt => ApplyFilter(evt.newValue);
        _searchField.RegisterValueChangedCallback(_onSearchChangedCallback);
    }

    void Start()
    {
        UpdateUICatalogDB();
    }

    void OnDisable()
    {
        if (_searchField != null && _onSearchChangedCallback != null)
            _searchField.UnregisterValueChangedCallback(_onSearchChangedCallback);
    }

    void OnNAIFIDInputEnter(PointerEnterEvent evt)
    {
        _isHovering = true;

        _pendingMessage = "(*) Required Field. Must be a unique integer NAIF ID.";

        _lastPointerPanelLocalPos = PanelSpaceToPanelLocal(evt.position);

        _lastMoveTimeUnscaled = Time.unscaledTime;

        HideTooltip();      // clean start
        StartIdleWatcher(); // begins “stillness” countdown
    }

    void OnNAIFIDInputMove(PointerMoveEvent evt)
    {
        if (!_isHovering) return;

        Vector2 newLocal = PanelSpaceToPanelLocal(evt.position);

        if ((newLocal - _lastPointerPanelLocalPos).sqrMagnitude <= _moveEpsilonSqr)
            return;

        _lastPointerPanelLocalPos = newLocal;
        _lastMoveTimeUnscaled = Time.unscaledTime;

        HideTooltip();
    }

    void OnNAIFIDInputLeave(PointerLeaveEvent evt)
    {
        _isHovering = false;
        StopIdleWatcher();
        HideTooltip();
    }

    void ResetHoverTimerAndHide()
    {
        if (!_isHovering) return;
        _lastMoveTimeUnscaled = Time.unscaledTime;
        HideTooltip();
    }

    void StartIdleWatcher()
    {
        if (_idleWatcherCoroutine != null)
            StopCoroutine(_idleWatcherCoroutine);

        _idleWatcherCoroutine = StartCoroutine(IdleWatcher());
    }

    void StopIdleWatcher()
    {
        if (_idleWatcherCoroutine == null) return;

        StopCoroutine(_idleWatcherCoroutine);
        _idleWatcherCoroutine = null;
    }

    IEnumerator IdleWatcher()
    {
        while (_isHovering)
        {
            if (!_tooltipVisible)
            {
                float idleSeconds = Time.unscaledTime - _lastMoveTimeUnscaled;
                if (idleSeconds >= _hoverDelaySeconds)
                    ShowTooltipAtCursor(_pendingMessage, _lastPointerPanelLocalPos);
            }

            yield return null;
        }
    }

    void ShowTooltipAtCursor(string message, Vector2 cursorPanelLocalPos)
    {
        _tooltipMessage.text = message;

        Vector2 p = cursorPanelLocalPos;

        _tooltip.style.left = p.x;
        _tooltip.style.top = p.y;
        _tooltip.style.display = DisplayStyle.Flex;

        _tooltipVisible = true;
    }

    void HideTooltip()
    {
        if (_tooltip == null) return;

        _tooltipMessage.text = string.Empty;
        _tooltip.style.display = DisplayStyle.None;
        _tooltipVisible = false;
    }

    Vector2 PanelSpaceToPanelLocal(Vector2 panelSpacePos) => panelSpacePos - _displayPanel.worldBound.position;

    void OnAddClicked()
    {
        if (_addEntryPanel.style.display == DisplayStyle.None) _addEntryPanel.style.display = DisplayStyle.Flex;
        else _addEntryPanel.style.display = DisplayStyle.None;
    }

    void TryAddEntry()
    {
        int naifID = -1;
        string name = string.Empty;
        string designation = string.Empty;
        string aliases = string.Empty;

        if (_addNAIFIDInput.value == null || _addNAIFIDInput.value == string.Empty)
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, "NAIF ID field cannot be empty.", "Invalid NAIF ID");
            return;
        }
        if (_addNAIFIDInput.value != null)
        {
            string naifIDStr = _addNAIFIDInput.value.Trim();
            if (!int.TryParse(naifIDStr, out naifID))
            {
                UIMessage.Instance.NewUIMessage(MessageType.Error, "NAIF ID must be an integer.", "Invalid NAIF ID");
                return;
            }
        }

        if (_addNameInput.value != null) name = _addNameInput.value.Trim();

        if (_addDesignationInput.value != null) designation = _addDesignationInput.value.Trim();

        if (_addAliasesInput.value != null) aliases = _addAliasesInput.value.Trim();


        BodyCatalog newEntry = new()
        {
            NAIFID = naifID,
            Name = name,
            Designation = designation,
            Aliases = aliases
        };

        if (_NAIFCatalogDBManager.TryAddUserCatalogEntry(newEntry))
        {
            ClearAddInputFields();
            CloseAddEntryPanel();
        }
    }

    void ClearAddInputFields()
    {
        _addNAIFIDInput.value = string.Empty;
        _addNameInput.value = string.Empty;
        _addDesignationInput.value = string.Empty;
        _addAliasesInput.value = string.Empty;
    }

    void CloseAddEntryPanel()
    {
        if (_addEntryPanel.style.display == DisplayStyle.Flex) _addEntryPanel.style.display = DisplayStyle.None;
        else return;
    }


    void ConfigureDatabaseTable()
    {
        _databaseTable.itemsSource = _filteredCatalogDB;
        _databaseTable.selectionType = SelectionType.None;
        _databaseTable.fixedItemHeight = 28f;

        BuildColumns();

        // Sorting setup
        _databaseTable.sortingMode = ColumnSortingMode.Custom;
        _databaseTable.columnSortingChanged += OnColumnSortingChanged;
        // Default sort: NAIFID descending (initial state)
        _databaseTable.sortColumnDescriptions.Clear();
        _databaseTable.sortColumnDescriptions.Add(
            new SortColumnDescription("NAIFID", SortDirection.Descending)
        );

        _databaseTable.Rebuild();

        // Fix header column flex "inline" issues
        _databaseTable.schedule.Execute(() =>
        {
            _databaseTable
                .Query<VisualElement>(className: "unity-multi-column-header__column__content-container")
                .ForEach(container =>
                {
                    container.style.flexGrow = 0;
                    container.style.flexShrink = 0;
                });
        }).StartingIn(0);
    }

    void OnColumnSortingChanged()
    {
        ApplyCurrentSort();
        _databaseTable.RefreshItems();
    }

    void ApplyCurrentSort()
    {
        // No data yet
        if (_filteredCatalogDB == null || _filteredCatalogDB.Count <= 1)
            return;

        // If user hasn't selected a sort column, keep your default ordering
        if (_databaseTable.sortColumnDescriptions == null || _databaseTable.sortColumnDescriptions.Count == 0)
            return;

        // MultiColumnListView supports multi-sort (Shift+Click).
        // We'll apply sort descriptions in order.
        _filteredCatalogDB.Sort(CompareBySortDescriptions);
    }

    int CompareBySortDescriptions(BodyCatalog a, BodyCatalog b)
    {
        foreach (var desc in _databaseTable.sortColumnDescriptions)
        {
            int result = CompareByColumn(desc.columnName, a, b);

            if (result != 0)
            {
                // Flip result if descending
                if (desc.direction == SortDirection.Descending)
                    result = -result;

                return result;
            }
        }

        return 0;
    }

    int CompareByColumn(string columnName, BodyCatalog a, BodyCatalog b)
    {
        return columnName switch
        {
            "NAIFID" => a.NAIFID.CompareTo(b.NAIFID),
            "Name" => CompareStrings(a.Name, b.Name),
            "Designation" => CompareStrings(a.Designation, b.Designation),
            "Aliases" => CompareStrings(a.Aliases, b.Aliases),
            _ => 0,
        };
    }

    static int CompareStrings(string left, string right)
    {
        left ??= string.Empty;
        right ??= string.Empty;

        // Case-insensitive alphabetical sort
        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    void BuildColumns()
    {
        _databaseTable.columns.Clear();
        _databaseTable.showAlternatingRowBackgrounds = AlternatingRowBackground.None;

        // Helper functions so column setup stays clean.
        Label MakeCellLabel(int paddingLeft, TextAnchor align)
        {
            var label = new Label();
            label.AddToClassList("db-cell");
            label.style.paddingLeft = paddingLeft;
            label.style.unityTextAlign = align;
            label.pickingMode = PickingMode.Position;
            label.style.width = Length.Percent(100);
            label.style.height = Length.Percent(100);

            label.RegisterCallback<PointerUpEvent>(evt =>
            {
                // Right mouse on Windows/Linux.
                // (Optional) treat Ctrl+LeftClick as context click on macOS.
                bool isContextClick = evt.button == 1 || (evt.button == 0 && (evt.modifiers & EventModifiers.Control) != 0);

                if (!isContextClick) return;

                if (label.userData is not BodyCatalog entry) return;

                var menu = new GenericDropdownMenu();

                // Copy submenu
                menu.AddItem("Copy/NAIF ID", false, () =>
                {
                    GUIUtility.systemCopyBuffer = entry.NAIFID.ToString();
                    UIMessage.Instance.NewFadingMessage(MessageType.Info, $"Copied NAIF ID '{entry.NAIFID}' to clipboard.", 3f);
                });
                menu.AddItem("Copy/Name", false, () =>
                {
                    GUIUtility.systemCopyBuffer = entry.Name ?? string.Empty;
                    UIMessage.Instance.NewFadingMessage(MessageType.Info, $"Copied Name '{entry.Name}' to clipboard.", 3f);
                });
                menu.AddItem("Copy/Designation", false, () =>
                {
                    GUIUtility.systemCopyBuffer = entry.Designation ?? string.Empty;
                    UIMessage.Instance.NewFadingMessage(MessageType.Info, $"Copied Designation '{entry.Designation}' to clipboard.", 3f);
                });
                menu.AddItem("Copy/Aliases", false, () =>
                {
                    GUIUtility.systemCopyBuffer = entry.Aliases ?? string.Empty;
                    UIMessage.Instance.NewFadingMessage(MessageType.Info, $"Copied Aliases '{entry.Aliases}' to clipboard.", 3f);
                });

                menu.AddSeparator("");

                // Not implemented yet
                menu.AddItem("Request Horizons API", false, () => Debug.Log("Request Horizon (not implemented)"));
                menu.AddItem("Check Ephemeris Database", false, () => Debug.Log("Check Ephemeris Database (not implemented)"));

                menu.AddSeparator("");

                // Remove entry
                menu.AddItem("Remove Entry", false, () =>
                {
                    int naifId = entry.NAIFID;

                    _NAIFCatalogDBManager.TryRemoveCatalogEntry(naifId, onComplete =>
                    {
                        if (onComplete) Debug.Log($"NAIFDatabaseUIController: Removed NAIF ID '{naifId}'.");
                        else Debug.LogWarning($"NAIFDatabaseUIController: Failed to remove NAIF ID '{naifId}'.");
                    });
                });

                menu.DropDown(new Rect(evt.position, Vector2.zero), label, DropdownMenuSizeMode.Content);

                evt.StopPropagation();
            });

            return label;
        }

        void BindCell(VisualElement e, int rowIndex, Func<BodyCatalog, string> getText)
        {
            if (rowIndex < 0 || rowIndex >= _filteredCatalogDB.Count)
                return;

            var entry = _filteredCatalogDB[rowIndex];
            var label = (Label)e;

            label.text = getText(entry);

            // Store full entry for context menu actions (Copy fields, Remove, etc.)
            label.userData = entry;
        }


        // Column: NAIF ID
        _databaseTable.columns.Add(new Column
        {
            name = "NAIFID",
            title = "NAIF ID",
            width = 90,
            minWidth = 70,
            resizable = true,
            stretchable = false,
            sortable = true,

            makeHeader = () =>
            {
                var header = new Label("NAIF ID");
                header.style.color = Color.white;
                header.style.unityTextAlign = TextAnchor.MiddleCenter;
                header.style.paddingLeft = 3;
                header.style.paddingTop = 6;

                return header;
            },
            makeCell = () =>
            {
                var label = MakeCellLabel(8, TextAnchor.MiddleCenter);
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                return label;
            },
            bindCell = (e, i) => BindCell(e, i, entry => entry.NAIFID.ToString())
        });

        // Column: Name
        _databaseTable.columns.Add(new Column
        {
            name = "Name",
            title = "Name",
            width = 320,
            minWidth = 160,
            resizable = true,
            stretchable = true,
            sortable = true,

            makeCell = () => MakeCellLabel(8, TextAnchor.MiddleLeft),
            bindCell = (e, i) => BindCell(e, i, entry =>
                string.IsNullOrWhiteSpace(entry.Name) ? "-" : entry.Name)
        });

        // Column: Designation
        _databaseTable.columns.Add(new Column
        {
            name = "Designation",
            title = "Designation",
            width = 240,
            minWidth = 140,
            resizable = true,
            stretchable = true,
            sortable = true,

            makeCell = () => MakeCellLabel(8, TextAnchor.MiddleLeft),
            bindCell = (e, i) => BindCell(e, i, entry =>
                string.IsNullOrWhiteSpace(entry.Designation) ? "-" : entry.Designation)
        });

        // Column: Aliases
        _databaseTable.columns.Add(new Column
        {
            name = "Aliases",
            title = "Aliases",
            width = 320,
            minWidth = 160,
            resizable = true,
            stretchable = true,
            sortable = true,

            makeCell = () => MakeCellLabel(8, TextAnchor.MiddleLeft),
            bindCell = (e, i) => BindCell(e, i, entry =>
                string.IsNullOrWhiteSpace(entry.Aliases) ? "-" : entry.Aliases)
        });
    }

    /// <summary>
    /// Pulls latest runtime DB from manager and refreshes the UI view.
    /// Called whenever NAIFCatalogManager updates its RuntimeCatalogDB.
    /// </summary>
    public void UpdateUICatalogDB()
    {
        _runtimeCatalogDB = new List<BodyCatalog>(_NAIFCatalogDBManager.RuntimeCatalogDB);

        _filteredCatalogDB.Clear();
        _filteredCatalogDB.AddRange(_runtimeCatalogDB);

        _databaseTable.RefreshItems();
    }

    void ApplyFilter(string query)
    {
        query = query?.Trim();
        _filteredCatalogDB.Clear();

        if (_runtimeCatalogDB == null || _runtimeCatalogDB.Count == 0)
        {
            _databaseTable.RefreshItems();
            return;
        }

        // Empty query = show all
        if (string.IsNullOrEmpty(query))
        {
            _filteredCatalogDB.AddRange(_runtimeCatalogDB);
            _databaseTable.RefreshItems();
            return;
        }

        // Token-based AND search:
        // "earth 399" requires BOTH tokens to match somewhere in the entry.
        string[] tokens = query.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var entry in _runtimeCatalogDB)
        {
            string haystack =
                $"{entry.NAIFID} {entry.Name} {entry.Designation} {entry.Aliases}"
                .ToLowerInvariant();

            bool matchesAll = true;

            for (int t = 0; t < tokens.Length; t++)
            {
                if (!haystack.Contains(tokens[t]))
                {
                    matchesAll = false;
                    break;
                }
            }

            if (matchesAll)
                _filteredCatalogDB.Add(entry);
        }

        ApplyCurrentSort();
        _databaseTable.RefreshItems();
    }

    public void ClosePanel()
    {
        if (_displayPanel.style.display == DisplayStyle.Flex)
        {
            _naifDatabaseTabText.fontStyle = FontStyles.Normal;
            _displayPanel.style.display = DisplayStyle.None;
        }
        else return;
    }
    void NAIFDatabaseSortOrder(int sortOrder) => _uiDocument.sortingOrder = sortOrder;

    public void OpenPanel(int sortOrder = -1)
    {
        if (_displayPanel.style.display == DisplayStyle.None)
        {
            if (sortOrder != -1) NAIFDatabaseSortOrder(sortOrder);

            _displayPanel.style.display = DisplayStyle.Flex;
            _naifDatabaseTabText.fontStyle = FontStyles.Underline;
            _databaseTable.RefreshItems();
        }
        else return;
    }

    public void OpenClosePanel()
    {
        if (_displayPanel.style.display == DisplayStyle.None)
        {
            OpenPanel();
        }
        else if (_displayPanel.style.display == DisplayStyle.Flex)
        {
            ClosePanel();
        }
    }


}
