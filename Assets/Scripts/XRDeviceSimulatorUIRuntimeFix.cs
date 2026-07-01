using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the legacy XR Device Simulator overlay visible inside the Game view.
/// In some Unity 6 layouts the simulator UI canvas is spawned with a shifted
/// root RectTransform, which clips the left side of the help panel.
/// </summary>
public class XRDeviceSimulatorUIRuntimeFix : MonoBehaviour
{
    [SerializeField] private Vector2 panelTopLeftOffset = new Vector2(20f, -20f);
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private bool keepFixingDuringPlay = true;

    private Canvas fixedCanvas;
    private RectTransform fixedPanel;

    private void OnEnable()
    {
        StartCoroutine(FixWhenReady());
    }

    private IEnumerator FixWhenReady()
    {
        for (var i = 0; i < 120; i++)
        {
            if (TryFix())
            {
                yield break;
            }

            yield return null;
        }
    }

    private void LateUpdate()
    {
        if (keepFixingDuringPlay)
        {
            TryFix();
        }
    }

    private bool TryFix()
    {
        if (fixedCanvas == null)
        {
            fixedCanvas = FindSimulatorCanvas();
        }

        if (fixedCanvas == null)
        {
            return false;
        }

        FixRootCanvas(fixedCanvas);
        FixPanel(fixedCanvas);
        return true;
    }

    private static Canvas FindSimulatorCanvas()
    {
        var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (var canvas in canvases)
        {
            if (!canvas.gameObject.scene.IsValid())
            {
                continue;
            }

            if (canvas.gameObject.name.Contains("XR Device Simulator UI"))
            {
                return canvas;
            }
        }

        return null;
    }

    private void FixRootCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var rectTransform = canvas.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return;
        }

        var width = Mathf.Max(Screen.width, 1);
        var height = Mathf.Max(Screen.height, 1);

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(width, height);
        rectTransform.anchoredPosition = new Vector2(width * 0.5f, height * 0.5f);
        rectTransform.localScale = Vector3.one;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    private void FixPanel(Canvas canvas)
    {
        if (fixedPanel == null)
        {
            fixedPanel = FindPanel(canvas.transform);
        }

        if (fixedPanel == null)
        {
            return;
        }

        fixedPanel.anchorMin = new Vector2(0f, 1f);
        fixedPanel.anchorMax = new Vector2(0f, 1f);
        fixedPanel.pivot = new Vector2(0f, 1f);
        fixedPanel.anchoredPosition = panelTopLeftOffset;
        fixedPanel.localScale = Vector3.one;
    }

    private static RectTransform FindPanel(Transform root)
    {
        var direct = root.Find("SimulatorUIPanel");
        if (direct != null && direct.TryGetComponent(out RectTransform directRect))
        {
            return directRect;
        }

        var rectTransforms = root.GetComponentsInChildren<RectTransform>(true);
        foreach (var rectTransform in rectTransforms)
        {
            if (rectTransform.name.Contains("SimulatorUIPanel"))
            {
                return rectTransform;
            }
        }

        return null;
    }
}
