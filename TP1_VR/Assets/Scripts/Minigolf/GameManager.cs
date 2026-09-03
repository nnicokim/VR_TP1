using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Minigolf
{
    public enum MinigolfState
    {
        Ready,
        Rolling,
        Won,
        Lost
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        BallController _ball;
        ShotController _shot;
        PutterAnimator _putter;
        Text _statusText;
        GameObject _hudRoot;
        MinigolfState _state = MinigolfState.Ready;
        bool _ended;

        public MinigolfState State => _state;

        public void Initialize(BallController ball, ShotController shot, PutterAnimator putter, Camera cam)
        {
            Instance = this;
            _ball = ball;
            _shot = shot;
            _putter = putter;

            _ball.OnStopped += HandleBallStopped;
            _ball.OnFellOut += HandleBallFell;

            BuildHud(cam);
            EnterReady();
        }

        void OnDestroy()
        {
            if (_ball != null)
            {
                _ball.OnStopped -= HandleBallStopped;
                _ball.OnFellOut -= HandleBallFell;
            }

            if (Instance == this)
                Instance = null;
        }

        public void NotifyShotTaken()
        {
            if (_state != MinigolfState.Ready)
                return;

            _state = MinigolfState.Rolling;
            SetStatus("...");
        }

        public void NotifyHole(BallController ball)
        {
            if (_ended || ball != _ball)
                return;

            _ended = true;
            _state = MinigolfState.Won;
            _ball.FreezeInHole();
            _shot.SetCanShoot(false);
            SetStatus("HOLE IN ONE!");
            StartCoroutine(RestartAfterDelay(2.5f));
        }

        void HandleBallStopped()
        {
            if (_ended || _state != MinigolfState.Rolling)
                return;

            _ended = true;
            _state = MinigolfState.Lost;
            _shot.SetCanShoot(false);
            SetStatus("Missed — press to retry");
        }

        void HandleBallFell()
        {
            if (_ended || _state == MinigolfState.Won)
                return;

            _ended = true;
            _state = MinigolfState.Lost;
            _shot.SetCanShoot(false);
            SetStatus("Out of bounds — press to retry");
        }

        void Update()
        {
            if (_state == MinigolfState.Lost && VrShotInput.WasPressedThisFrame())
                RestartRound();
        }

        void EnterReady()
        {
            _ended = false;
            _state = MinigolfState.Ready;
            _putter?.ResetPose();
            SetStatus("Look to aim\nHold screen to set power");
            _shot.SetCanShoot(true);
        }

        void RestartRound()
        {
            StopAllCoroutines();
            _ball.ResetToTee();
            EnterReady();
        }

        IEnumerator RestartAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            RestartRound();
        }

        void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
        }

        void BuildHud(Camera cam)
        {
            _hudRoot = new GameObject("MinigolfHUD");
            var canvas = _hudRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            canvas.sortingOrder = 100;

            var scaler = _hudRoot.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            _hudRoot.AddComponent<GraphicRaycaster>();

            var rt = _hudRoot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(800f, 220f);

            // Parent to camera so both Cardboard eyes see the HUD.
            Transform parent = cam != null ? cam.transform : null;
            if (parent != null)
            {
                _hudRoot.transform.SetParent(parent, false);
                _hudRoot.transform.localPosition = new Vector3(0f, 0.15f, 1.35f);
                _hudRoot.transform.localRotation = Quaternion.identity;
                _hudRoot.transform.localScale = Vector3.one * 0.00115f;
            }

            var statusGo = new GameObject("Status", typeof(RectTransform));
            statusGo.transform.SetParent(_hudRoot.transform, false);
            var statusRt = statusGo.GetComponent<RectTransform>();
            statusRt.anchorMin = Vector2.zero;
            statusRt.anchorMax = Vector2.one;
            statusRt.offsetMin = new Vector2(20f, 20f);
            statusRt.offsetMax = new Vector2(-20f, -20f);

            _statusText = statusGo.AddComponent<Text>();
            // Prefer OS fonts on iOS; LegacyRuntime often fails in mobile VR builds.
            _statusText.font = Font.CreateDynamicFontFromOSFont(
                new[] { "Helvetica Neue", "Helvetica", "Arial", "Roboto" }, 48);
            if (_statusText.font == null)
                _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.color = Color.white;
            _statusText.fontSize = 48;
            _statusText.fontStyle = FontStyle.Bold;
            _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _statusText.verticalOverflow = VerticalWrapMode.Overflow;
            _statusText.resizeTextForBestFit = true;
            _statusText.resizeTextMinSize = 28;
            _statusText.resizeTextMaxSize = 56;

            var outline = statusGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
        }
    }
}
