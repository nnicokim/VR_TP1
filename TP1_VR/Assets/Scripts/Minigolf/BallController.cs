using System;
using System.Collections;
using UnityEngine;

namespace Minigolf
{
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour
    {
        [SerializeField] float stopSpeed = 0.08f;
        [SerializeField] float stopHoldTime = 0.6f;
        [SerializeField] float fallY = -1f;

        Rigidbody _rb;
        Vector3 _teePosition;
        Quaternion _teeRotation;
        bool _isRolling;
        float _stoppedTimer;

        public bool IsRolling => _isRolling;
        public event Action OnStopped;
        public event Action OnFellOut;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _teePosition = transform.position;
            _teeRotation = transform.rotation;
        }

        public void CaptureTee()
        {
            _teePosition = transform.position;
            _teeRotation = transform.rotation;
        }

        public void Shoot(Vector3 impulse)
        {
            StopAllCoroutines();
            _rb.isKinematic = false;
            _rb.WakeUp();
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.AddForce(impulse, ForceMode.Impulse);
            _isRolling = true;
            _stoppedTimer = 0f;
        }

        public void ResetToTee()
        {
            StopAllCoroutines();
            _isRolling = false;
            _stoppedTimer = 0f;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
            transform.SetPositionAndRotation(_teePosition, _teeRotation);
            StartCoroutine(ReleaseKinematicNextFixed());
        }

        IEnumerator ReleaseKinematicNextFixed()
        {
            yield return new WaitForFixedUpdate();
            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        public void FreezeInHole()
        {
            _isRolling = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        public void ForceOutOfBounds()
        {
            if (!_isRolling)
                return;
            _isRolling = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            OnFellOut?.Invoke();
        }

        void FixedUpdate()
        {
            if (!_isRolling || _rb.isKinematic)
                return;

            if (transform.position.y < fallY)
            {
                _isRolling = false;
                OnFellOut?.Invoke();
                return;
            }

            float speed = _rb.linearVelocity.magnitude;
            if (speed < stopSpeed)
            {
                _stoppedTimer += Time.fixedDeltaTime;
                if (_stoppedTimer >= stopHoldTime)
                {
                    _isRolling = false;
                    _rb.linearVelocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                    OnStopped?.Invoke();
                }
            }
            else
            {
                _stoppedTimer = 0f;
            }
        }
    }
}
