using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class NAIFDatabaseUIController : MonoBehaviour
{
    // UXML element names
    const string SearchFieldName = "SearchField";
    const string DatabaseTableName = "DatabaseTable";

    [Header("References")]
    [SerializeField] NAIFCatalogManager _NAIFCatalogDBManager;

    // UI Toolkit refs
    UIDocument _uiDocument;
    TextField _searchField;
    MultiColumnListView _databaseTable;

    // Data
    readonly List<BodyCatalog> _filteredCatalogDB = new();
    List<BodyCatalog> _runtimeCatalogDB = new();

    // For unregistering cleanly
    EventCallback<ChangeEvent<string>> _onSearchChangedCallback;

    void Awake()
    {
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

    void ConfigureDatabaseTable()
    {
        // Source
        _databaseTable.itemsSource = _filteredCatalogDB;

        // Appearance / behavior
        _databaseTable.selectionType = SelectionType.None;
        _databaseTable.fixedItemHeight = 28f;

        // Built-in zebra striping
        _databaseTable.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;

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

            label.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                if (label.userData is not int naifId) return;

                evt.menu.AppendAction("Request Horizon", _ => { }, DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Check Ephemeris Database", _ => { }, DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Copy NAIFID", _ => GUIUtility.systemCopyBuffer = naifId.ToString(),
                    DropdownMenuAction.Status.Normal);
            }));

            return label;
        }

        void BindCell(VisualElement e, int rowIndex, Func<BodyCatalog, string> getText)
        {
            if (rowIndex < 0 || rowIndex >= _filteredCatalogDB.Count)
                return;

            var entry = _filteredCatalogDB[rowIndex];

            var label = (Label)e;
            label.text = getText(entry);
            label.userData = entry.NAIFID; // used by right-click menu
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
                header.style.paddingLeft = 5;
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
}
