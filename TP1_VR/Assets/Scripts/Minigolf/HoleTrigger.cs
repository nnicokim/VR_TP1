using System;
using UnityEngine;

namespace Minigolf
{
    public class HoleTrigger : MonoBehaviour
    {
        public event Action<BallController> OnBallEntered;

        void OnTriggerEnter(Collider other)
        {
            var ball = other.GetComponentInParent<BallController>();
            if (ball != null)
                OnBallEntered?.Invoke(ball);
        }
    }
}
