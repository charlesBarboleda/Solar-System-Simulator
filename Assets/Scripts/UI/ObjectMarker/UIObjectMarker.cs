using System;
using System.Linq;
using TMPro;
using UnityEngine;

public sealed class UIObjectMarker : MonoBehaviour
{
    [Serializable]
    public class MarkerBinding
    {
        [Header("Wiring")]
        public Transform Target;
        public RectTransform MarkerRect;
        public TMP_Text MarkerText;

        [Header("Placement")]
        public Vector3 WorldOffset = Vector3.up;
        public Vector2 ScreenOffsetPx = Vector2.zero;

        [Header("Visibility")]
        public bool HideWhenOffscreen = true;
        public bool ClampToScreenEdge = false;
        [Range(0f, 0.2f)] public float EdgePaddingViewport = 0.03f;

        [Header("Optional: Occlusion")]
        public bool HideWhenOccluded = false;
        public LayerMask OcclusionMask;

        [Header("Optional: Label")]
        public bool UseTargetNameAsLabel = false;
        public string StaticLabel;
    }

    [Header("Canvas / Root")]
    [SerializeField] RectTransform safeAreaRoot;

    [Header("Camera")]
    [SerializeField] Camera worldCamera;

    [Header("_Markers")]
    [SerializeField] MarkerBinding[] _markers;

    [Header("Update")]
    [SerializeField] bool updateInLateUpdate = true;

    [Header("Object List")]
    [SerializeField] NBodyManager _nBodyManager;

    [Header("UI Marker Prefab")]
    [SerializeField] GameObject _markerPrefab;



    void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;

        if (safeAreaRoot == null)
            Debug.LogWarning($"{nameof(UIObjectMarker)}: SafeAreaRoot is not assigned.", this);

        if (_markers == null) return;

        for (int i = 0; i < _nBodyManager.SystemBodies.Length; i++)
        {
            AstronomicalObject obj = _nBodyManager.SystemBodies[i];
            if (obj.Data.Type == BodyType.Star || obj.Data.Type == BodyType.Planet || obj.Data.Type == BodyType.Moon)
            {
                // Create a marker for this object
                GameObject markerGO = Instantiate(_markerPrefab, safeAreaRoot);
                MarkerBinding binding = new()
                {
                    Target = obj.transform,
                    MarkerRect = markerGO.GetComponent<RectTransform>(),
                    MarkerText = markerGO.GetComponent<TMP_Text>(),
                    ClampToScreenEdge = true,
                    HideWhenOffscreen = false,
                    HideWhenOccluded = true,
                };

                binding.MarkerText.text = obj.Data.Name;
                _markers = _markers.Append(binding).ToArray();
            }
        }
    }

    void Update()
    {
        if (!updateInLateUpdate)
            Tick();
    }

    void LateUpdate()
    {
        if (updateInLateUpdate)
            Tick();
    }

    void Tick()
    {
        if (safeAreaRoot == null) return;
        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null) return;
        if (_markers == null) return;

        for (int i = 0; i < _markers.Length; i++)
        {
            UpdateMarker(_markers[i]);
        }
    }

    void UpdateMarker(MarkerBinding m)
    {
        if (m.MarkerRect == null)
            return;

        if (m.Target == null)
        {
            if (m.MarkerRect.gameObject.activeSelf)
                m.MarkerRect.gameObject.SetActive(false);
            return;
        }

        Vector3 worldPos = m.Target.position + m.WorldOffset;

        Vector3 vp = worldCamera.WorldToViewportPoint(worldPos);

        bool inFront = vp.z > 0f;
        bool onScreen = inFront && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;

        // Optional occlusion check
        if (m.HideWhenOccluded && inFront)
        {
            Vector3 camPos = worldCamera.transform.position;
            Vector3 dir = (worldPos - camPos);
            float dist = dir.magnitude;

            if (dist > 0.001f)
            {
                dir /= dist;

                if (Physics.Raycast(camPos, dir, dist, m.OcclusionMask, QueryTriggerInteraction.Ignore))
                    onScreen = false;
            }
        }

        if (!onScreen)
        {
            if (m.HideWhenOffscreen || !m.ClampToScreenEdge || !inFront)
            {
                if (m.MarkerRect.gameObject.activeSelf)
                    m.MarkerRect.gameObject.SetActive(false);
                return;
            }

            float pad = m.EdgePaddingViewport;
            vp.x = Mathf.Clamp(vp.x, pad, 1f - pad);
            vp.y = Mathf.Clamp(vp.y, pad, 1f - pad);
        }

        Vector3 screen = worldCamera.ViewportToScreenPoint(new Vector3(vp.x, vp.y, vp.z));
        screen.x += m.ScreenOffsetPx.x;
        screen.y += m.ScreenOffsetPx.y;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                safeAreaRoot, screen, null, out Vector2 localPoint))
        {
            if (!m.MarkerRect.gameObject.activeSelf)
                m.MarkerRect.gameObject.SetActive(true);

            m.MarkerRect.anchoredPosition = localPoint;
        }
        else
        {
            if (m.MarkerRect.gameObject.activeSelf)
                m.MarkerRect.gameObject.SetActive(false);
        }
    }

    void Reset()
    {
        worldCamera = Camera.main;
    }
}