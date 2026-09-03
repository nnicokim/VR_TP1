using UnityEngine;

namespace Minigolf
{
    public class ShotController : MonoBehaviour
    {
        [SerializeField] float chargeSeconds = 1.35f;
        [SerializeField] float minImpulse = 0.12f;
        [SerializeField] float maxImpulse = 0.85f;

        BallController _ball;
        AimIndicator _aim;
        PutterAnimator _putter;
        Camera _camera;
        bool _canShoot;
        bool _charging;
        float _power;

        public float Power01 => _power;
        public bool IsCharging => _charging;

        public void Initialize(BallController ball, AimIndicator aim, PutterAnimator putter, Camera cam)
        {
            _ball = ball;
            _aim = aim;
            _putter = putter;
            _camera = cam;
        }

        public void SetCanShoot(bool canShoot)
        {
            _canShoot = canShoot;
            if (!canShoot)
            {
                _charging = false;
                _power = 0f;
                _aim?.SetVisible(false);
            }
            else
            {
                _aim?.SetVisible(true);
                _aim?.UpdateAim(GetAimDirection(), 0f);
            }
        }

        void Update()
        {
            if (!_canShoot || _ball == null || _camera == null)
                return;

            Vector3 aim = GetAimDirection();
            bool pressed = VrShotInput.WasPressedThisFrame();
            bool held = VrShotInput.IsPressed();
            bool released = VrShotInput.WasReleasedThisFrame();

            if (pressed && !_charging)
            {
                _charging = true;
                _power = 0f;
            }

            if (_charging && held)
            {
                _power = Mathf.Clamp01(_power + Time.deltaTime / chargeSeconds);
                _aim?.UpdateAim(aim, _power);
            }
            else if (!_charging)
            {
                _aim?.UpdateAim(aim, 0.15f);
            }

            if (_charging && released)
            {
                Fire(aim, _power);
                _charging = false;
                _power = 0f;
            }
        }

        void Fire(Vector3 aim, float power01)
        {
            float impulse = Mathf.Lerp(minImpulse, maxImpulse, Mathf.Clamp01(power01));
            if (power01 < 0.05f)
                impulse = minImpulse * 0.65f;

            _aim?.SetVisible(false);
            _putter?.PlaySwing();
            _ball.Shoot(aim * impulse);
            _canShoot = false;
            GameManager.Instance?.NotifyShotTaken();
        }

        Vector3 GetAimDirection()
        {
            Vector3 forward = _camera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = _camera.transform.right;
            return forward.normalized;
        }
    }
}
