using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartScreenController : MonoBehaviour
{
    [SerializeField] Texture2D mainImage;
    [SerializeField] string gameSceneName = "GameScene";
    [SerializeField] Camera targetCamera;
    [SerializeField] float distanceFromCamera = 2f;
    [SerializeField] Vector2 canvasReferenceSize = new Vector2(1000f, 700f);
    [SerializeField] float canvasWorldScale = 0.002f;

    bool loading;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        BuildWorldSpaceCanvas();
    }

    void Update()
    {
        if (loading || !WasStartPressedThisFrame())
            return;

        loading = true;
        SceneManager.LoadScene(gameSceneName);
    }

    void BuildWorldSpaceCanvas()
    {
        if (targetCamera == null)
        {
            Debug.LogError("[StartScreen] No camera found for the start screen.");
            return;
        }

        var canvasGo = new GameObject("StartScreenCanvas", typeof(RectTransform));
        canvasGo.transform.position = targetCamera.transform.position + targetCamera.transform.forward * distanceFromCamera;
        canvasGo.transform.rotation = targetCamera.transform.rotation;
        canvasGo.transform.localScale = Vector3.one * canvasWorldScale;

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = targetCamera;
        canvas.sortingOrder = 100;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 2f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var canvasRt = canvasGo.GetComponent<RectTransform>();
        canvasRt.sizeDelta = canvasReferenceSize;

        CreateImage(canvasRt);
        CreatePrompt(canvasRt);
    }

    void CreateImage(RectTransform parent)
    {
        var imageGo = new GameObject("PuttForPuddingImage", typeof(RectTransform));
        imageGo.transform.SetParent(parent, false);

        var rt = imageGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var image = imageGo.AddComponent<RawImage>();
        image.texture = mainImage;
        image.color = Color.white;

        if (mainImage != null)
        {
            var fitter = imageGo.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)mainImage.width / mainImage.height;
        }
        else
        {
            Debug.LogWarning("[StartScreen] Main image is not assigned.");
        }
    }

    void CreatePrompt(RectTransform parent)
    {
        var textGo = new GameObject("PressScreenToPlayText", typeof(RectTransform));
        textGo.transform.SetParent(parent, false);

        var rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 45f);
        rt.sizeDelta = new Vector2(-160f, 130f);

        var text = textGo.AddComponent<Text>();
        text.text = "Press screen to play";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 48;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.resizeTextForBestFit = false;

        var outline = textGo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);
    }

    static bool WasStartPressedThisFrame()
    {
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            return true;

#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
#endif

        return false;
    }
}
