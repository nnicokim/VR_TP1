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
        Lost,
        Finished
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        BallController _ball;
        ShotController _shot;
        PutterAnimator _putter;
        Text _rivalText;
        Text _statusText;
        Text _scoreText;
        GameObject _hudRoot;
        MinigolfState _state = MinigolfState.Ready;
        bool _ended;
        int _attemptsRemaining;
        int _playerScore;
        int _rivalScore;

        const int MaxAttempts = 10;

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
            StartNewMatch();
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

            _attemptsRemaining = Mathf.Max(0, _attemptsRemaining - 1);
            _state = MinigolfState.Rolling;
            UpdateScoreText();
            SetStatus("...");
        }

        public void NotifyHole(BallController ball)
        {
            if (_ended || ball != _ball)
                return;

            _ended = true;
            _state = MinigolfState.Won;
            _playerScore++;
            _ball.FreezeInHole();
            _shot.SetCanShoot(false);
            UpdateScoreText();
            SetStatus("HOLE IN ONE!");
            StartCoroutine(ContinueAfterDelay(1.5f));
        }

        void HandleBallStopped()
        {
            if (_ended || _state != MinigolfState.Rolling)
                return;

            _ended = true;
            _state = MinigolfState.Lost;
            _shot.SetCanShoot(false);
            SetStatus("Missed");
            StartCoroutine(ContinueAfterDelay(1.25f));
        }

        void HandleBallFell()
        {
            if (_ended || _state == MinigolfState.Won)
                return;

            _ended = true;
            _state = MinigolfState.Lost;
            _shot.SetCanShoot(false);
            SetStatus("Out of bounds");
            StartCoroutine(ContinueAfterDelay(1.25f));
        }

        void Update()
        {
            if (_state == MinigolfState.Finished && VrShotInput.WasPressedThisFrame())
                StartNewMatch();
        }

        void StartNewMatch()
        {
            StopAllCoroutines();
            _attemptsRemaining = MaxAttempts;
            _playerScore = 0;
            _rivalScore = Random.Range(1, 6);
            _ball.ResetToTee();
            EnterReady();
        }

        void EnterReady()
        {
            _ended = false;
            _state = MinigolfState.Ready;
            _putter?.ResetPose();
            UpdateScoreText();
            SetRivalText($"Your rival scored {_rivalScore} times");
            SetStatus("Look to aim\nHold screen to set power");
            _shot.SetCanShoot(true);
        }

        void RestartRound()
        {
            StopAllCoroutines();
            _ball.ResetToTee();
            EnterReady();
        }

        IEnumerator ContinueAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_attemptsRemaining <= 0)
                FinishMatch();
            else
                RestartRound();
        }

        void FinishMatch()
        {
            StopAllCoroutines();
            _ended = true;
            _state = MinigolfState.Finished;
            _shot.SetCanShoot(false);

            string result;
            if (_playerScore > _rivalScore)
                result = "Victory.\nEnjoy your pudding!!!";
            else if (_playerScore < _rivalScore)
                result = "You lost the bet.\nNo pudding for you";
            else
                result = "You tied.";

            UpdateScoreText();
            SetRivalText($"Your rival scored {_rivalScore} times");
            SetStatus($"{result}\n\nTouch the screen to play again.");
        }

        void SetRivalText(string message)
        {
            if (_rivalText != null)
                _rivalText.text = message;
        }

        void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
        }

        void UpdateScoreText()
        {
            if (_scoreText != null)
                _scoreText.text = $"Attempts: {_attemptsRemaining}\nScored: {_playerScore}";
        }

        void BuildHud(Camera cam)
        {
            _hudRoot = new GameObject("MinigolfHUD");
            var canvas = _hudRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            canvas.sortingOrder = 100;

            var scaler = _hudRoot.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 2f;
            _hudRoot.AddComponent<GraphicRaycaster>();

            var rt = _hudRoot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1200f, 520f);

            // Parent to camera so both Cardboard eyes see the HUD.
            Transform parent = cam != null ? cam.transform : null;
            if (parent != null)
            {
                _hudRoot.transform.SetParent(parent, false);
                _hudRoot.transform.localPosition = new Vector3(0f, 0.18f, 1.35f);
                _hudRoot.transform.localRotation = Quaternion.identity;
                _hudRoot.transform.localScale = Vector3.one * 0.00115f;
            }

            Font hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (hudFont == null)
                hudFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var rivalGo = new GameObject("RivalScore", typeof(RectTransform));
            rivalGo.transform.SetParent(_hudRoot.transform, false);
            var rivalRt = rivalGo.GetComponent<RectTransform>();
            rivalRt.anchorMin = new Vector2(0.12f, 0.59f);
            rivalRt.anchorMax = new Vector2(0.88f, 0.86f);
            rivalRt.offsetMin = Vector2.zero;
            rivalRt.offsetMax = Vector2.zero;

            _rivalText = rivalGo.AddComponent<Text>();
            ConfigureHudText(_rivalText, hudFont, TextAnchor.MiddleCenter, 36, Color.white);
            AddHudOutline(rivalGo);

            var statusGo = new GameObject("Status", typeof(RectTransform));
            statusGo.transform.SetParent(_hudRoot.transform, false);
            var statusRt = statusGo.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.16f, 0.14f);
            statusRt.anchorMax = new Vector2(0.84f, 0.6f);
            statusRt.offsetMin = Vector2.zero;
            statusRt.offsetMax = Vector2.zero;

            _statusText = statusGo.AddComponent<Text>();
            ConfigureHudText(_statusText, hudFont, TextAnchor.MiddleCenter, 34, Color.white);
            AddHudOutline(statusGo);

            var scoreGo = new GameObject("PuddingBetCounter", typeof(RectTransform));
            scoreGo.transform.SetParent(_hudRoot.transform, false);
            var scoreRt = scoreGo.GetComponent<RectTransform>();
            scoreRt.anchorMin = new Vector2(0f, 1f);
            scoreRt.anchorMax = new Vector2(0f, 1f);
            scoreRt.pivot = new Vector2(0f, 1f);
            scoreRt.anchoredPosition = new Vector2(8f, -8f);
            scoreRt.sizeDelta = new Vector2(320f, 100f);

            _scoreText = scoreGo.AddComponent<Text>();
            ConfigureHudText(_scoreText, hudFont, TextAnchor.UpperLeft, 28, Color.white);
            AddHudOutline(scoreGo);
        }

        void ConfigureHudText(Text text, Font font, TextAnchor alignment, int fontSize, Color color)
        {
            text.font = font;
            text.alignment = alignment;
            text.color = color;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = false;
        }

        void AddHudOutline(GameObject go)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
        }
    }
}
