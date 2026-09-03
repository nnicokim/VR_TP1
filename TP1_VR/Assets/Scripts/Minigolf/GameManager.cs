using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
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

        public void Initialize(BallController ball, ShotController shot, PutterAnimator putter)
        {
            Instance = this;
            _ball = ball;
            _shot = shot;
            _putter = putter;

            _ball.OnStopped += HandleBallStopped;
            _ball.OnFellOut += HandleBallFell;

            BuildHud();
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
            SetStatus("Missed - tap to retry");
        }

        void HandleBallFell()
        {
            if (_ended || _state == MinigolfState.Won)
                return;

            _ended = true;
            _state = MinigolfState.Lost;
            _shot.SetCanShoot(false);
            SetStatus("Out of bounds - tap to retry");
        }

        void Update()
        {
            if (_state == MinigolfState.Lost && WasConfirmPressed())
                RestartRound();
        }

        void EnterReady()
        {
            _ended = false;
            _state = MinigolfState.Ready;
            _putter?.ResetPose();
            SetStatus("Look to aim\nHold to set power");
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

        void BuildHud()
        {
            _hudRoot = new GameObject("MinigolfHUD");
            var canvas = _hudRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            _hudRoot.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _hudRoot.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
            _hudRoot.AddComponent<GraphicRaycaster>();

            var statusGo = new GameObject("Status", typeof(RectTransform));
            statusGo.transform.SetParent(_hudRoot.transform, false);
            var statusRt = statusGo.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.1f, 0.78f);
            statusRt.anchorMax = new Vector2(0.9f, 0.95f);
            statusRt.offsetMin = Vector2.zero;
            statusRt.offsetMax = Vector2.zero;
            _statusText = statusGo.AddComponent<Text>();
            _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_statusText.font == null)
                _statusText.font = Font.CreateDynamicFontFromOSFont("Helvetica", 42);
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.color = Color.white;
            _statusText.fontSize = 42;
            _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _statusText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        static bool WasConfirmPressed()
        {
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
                return true;
            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
        }
    }
}
