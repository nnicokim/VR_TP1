using System.Collections;
using UnityEngine;

namespace Minigolf
{
    public class PutterAnimator : MonoBehaviour
    {
        Quaternion _restRotation;
        Coroutine _swing;

        void Awake()
        {
            _restRotation = transform.localRotation;
        }

        public void PlaySwing()
        {
            if (_swing != null)
                StopCoroutine(_swing);
            _swing = StartCoroutine(SwingRoutine());
        }

        public void ResetPose()
        {
            if (_swing != null)
                StopCoroutine(_swing);
            transform.localRotation = _restRotation;
        }

        IEnumerator SwingRoutine()
        {
            const float back = 0.12f;
            const float hit = 0.08f;
            const float recover = 0.2f;
            Quaternion backRot = _restRotation * Quaternion.Euler(0f, -35f, 0f);
            Quaternion hitRot = _restRotation * Quaternion.Euler(0f, 25f, 0f);

            float t = 0f;
            while (t < back)
            {
                t += Time.deltaTime;
                transform.localRotation = Quaternion.Slerp(_restRotation, backRot, t / back);
                yield return null;
            }

            t = 0f;
            while (t < hit)
            {
                t += Time.deltaTime;
                transform.localRotation = Quaternion.Slerp(backRot, hitRot, t / hit);
                yield return null;
            }

            t = 0f;
            while (t < recover)
            {
                t += Time.deltaTime;
                transform.localRotation = Quaternion.Slerp(hitRot, _restRotation, t / recover);
                yield return null;
            }

            transform.localRotation = _restRotation;
            _swing = null;
        }
    }
}
