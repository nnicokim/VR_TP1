using UnityEngine;

namespace Minigolf
{
    public class AimIndicator : MonoBehaviour
    {
        [SerializeField] float maxLength = 2.2f;
        [SerializeField] float height = 0.03f;

        LineRenderer _line;
        Transform _ball;
        bool _visible;

        public void Initialize(Transform ball)
        {
            _ball = ball;
            _line = gameObject.AddComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.startWidth = 0.025f;
            _line.endWidth = 0.008f;
            _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.startColor = new Color(1f, 0.92f, 0.2f, 0.95f);
            _line.endColor = new Color(1f, 0.55f, 0.1f, 0.15f);
            _line.enabled = false;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (_line != null)
                _line.enabled = visible;
        }

        public void UpdateAim(Vector3 flatDirection, float power01)
        {
            if (_line == null || _ball == null)
                return;

            Vector3 origin = _ball.position + Vector3.up * height;
            Vector3 dir = flatDirection.sqrMagnitude > 0.0001f
                ? flatDirection.normalized
                : Vector3.forward;
            float length = Mathf.Lerp(0.35f, maxLength, power01);

            _line.SetPosition(0, origin);
            _line.SetPosition(1, origin + dir * length);
            _line.enabled = _visible;
        }
    }
}
