using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class NAIFDatabaseUIController : MonoBehaviour
{
    // UXML element names
    const string SearchFieldName = "SearchField";
    const string DatabaseListName = "DatabaseList";

    [Header("References")]
    [SerializeField] NAIFCatalogManager _NAIFCatalogDBManager;

    // UI Toolkit refs
    UIDocument _uiDocument;
    TextField _searchField;
    ListView _databaseListView;

    // Data
    readonly List<BodyCatalog> _filteredCatalogDB = new();
    List<BodyCatalog> _runtimeCatalogDB;

    // For unregistering cleanly
    EventCallback<ChangeEvent<string>> _onSearchChangedCallback;

    // Row refs stored per recycled row (avoids repeated Q() lookups in bindItem)
    sealed class RowRefs
    {
        public Label id;
        public Label name;
        public Label designation;
        public Label aliases;
        public int naifId;
    }

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
        _databaseListView = root.Q<ListView>(DatabaseListName);

        if (_searchField == null || _databaseListView == null)
        {
            Debug.LogError(
                $"NAIFDatabaseUIController: UI elements not found.\n" +
                $"- TextField name expected: '{SearchFieldName}'\n" +
                $"- ListView name expected: '{DatabaseListName}'\n" +
                $"Check your UXML element names in UI Builder."
            );
            enabled = false;
            return;
        }

        ConfigureListView();

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

    void ConfigureListView()
    {
        _databaseListView.itemsSource = _filteredCatalogDB;

        _databaseListView.fixedItemHeight = 28f; // adjust to taste

        _databaseListView.selectionType = SelectionType.None;

        _databaseListView.makeItem = MakeRow;
        _databaseListView.bindItem = BindRow;
    }

    VisualElement MakeRow()
    {
        // This creates ONE row visual. ListView will recycle these as you scroll.
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.paddingLeft = 6;
        row.style.paddingRight = 6;
        row.style.unityTextAlign = TextAnchor.MiddleLeft;

        // Create columns
        var id = new Label { name = "col_id" };
        var name = new Label { name = "col_name" };
        var designation = new Label { name = "col_designation" };
        var aliases = new Label { name = "col_aliases" };

        // Column "weights"
        id.style.flexGrow = 1;
        name.style.flexGrow = 2;
        designation.style.flexGrow = 2;
        aliases.style.flexGrow = 3;

        // Prevent text from looking cramped
        id.style.paddingRight = 8;
        name.style.paddingRight = 8;
        designation.style.paddingRight = 8;

        row.Add(id);
        row.Add(name);
        row.Add(designation);
        row.Add(aliases);
        row.AddToClassList("db-row");

        id.AddToClassList("db-col");
        name.AddToClassList("db-col");
        designation.AddToClassList("db-col");
        aliases.AddToClassList("db-col");

        // Cache refs so bindItem doesn't have to Q() each time
        row.userData = new RowRefs
        {
            id = id,
            name = name,
            designation = designation,
            aliases = aliases
        };

        row.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            if (row.userData is not RowRefs refs) return;

            evt.menu.AppendAction(
                "Request Horizon",
                _ => Debug.Log("Request Horizon (not implemented yet)"),
                DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction(
                "Check Ephemeris Database",
                _ => Debug.Log("Check Ephemeris Database (not implemented yet)"),
                DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction(
                "Copy NAIFID",
                _ => GUIUtility.systemCopyBuffer = refs.naifId.ToString(),
                DropdownMenuAction.Status.Normal);
        }));

        return row;
    }

    void BindRow(VisualElement row, int index)
    {
        // Called whenever a recycled row should represent a different entry
        if (index < 0 || index >= _filteredCatalogDB.Count)
            return;

        if (row.userData is not RowRefs refs)
            return;

        BodyCatalog entry = _filteredCatalogDB[index];

        row.EnableInClassList("odd", (index & 1) == 1);

        refs.id.text = entry.NAIFID.ToString();
        refs.name.text = string.IsNullOrWhiteSpace(entry.Name) ? "-" : entry.Name;
        refs.designation.text = string.IsNullOrWhiteSpace(entry.Designation) ? "-" : entry.Designation;
        refs.aliases.text = string.IsNullOrWhiteSpace(entry.Aliases) ? "-" : entry.Aliases;
        refs.naifId = entry.NAIFID;
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

        _databaseListView.RefreshItems();
    }

    void ApplyFilter(string query)
    {
        query = query?.Trim();

        _filteredCatalogDB.Clear();

        // Empty query = show all
        if (string.IsNullOrEmpty(query))
        {
            _filteredCatalogDB.AddRange(_runtimeCatalogDB);
            _databaseListView.RefreshItems();
            return;
        }

        // Token-based AND search:
        // "earth 399" requires BOTH tokens to match somewhere in the entry.
        string[] tokens = query.ToLowerInvariant()
                               .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var entry in _runtimeCatalogDB)
        {
            // Build one searchable string for this entry
            // (fast + simple for your dataset size)
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

        _databaseListView.RefreshItems();
    }
}
