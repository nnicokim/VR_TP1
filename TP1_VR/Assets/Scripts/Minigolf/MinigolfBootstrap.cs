using UnityEngine;
using UnityEngine.SceneManagement;

namespace Minigolf
{
    /// <summary>
    /// Wires the prison minigolf prototype at runtime from existing scene objects.
    /// Player stays fixed (XRRig). Aim with head, hold VR BOX button / touch to charge, release to shoot.
    /// Target: the sideways Vase at the far end of the corridor.
    /// </summary>
    public class MinigolfBootstrap : MonoBehaviour
    {
        static bool _listeningForSceneLoads;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            _listeningForSceneLoads = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBootstrap()
        {
            if (!_listeningForSceneLoads)
            {
                SceneManager.sceneLoaded += HandleSceneLoaded;
                _listeningForSceneLoads = true;
            }

            TryBootstrapCurrentScene();
        }

        static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryBootstrapCurrentScene();
        }

        static void TryBootstrapCurrentScene()
        {
            if (FindFirstObjectByType<GameManager>() != null)
                return;

            var ballGo = GameObject.Find("Golf Ball");
            if (ballGo == null)
                return;

            var systems = new GameObject("MinigolfSystems");
            systems.AddComponent<MinigolfBootstrap>().Setup(ballGo);
        }

        void Setup(GameObject ballGo)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("[Minigolf] No Main Camera found.");
                Destroy(gameObject);
                return;
            }

            DisablePutterColliders();
            ConfigureGreenFriction();

            var ball = ConfigureBall(ballGo);
            var putter = ConfigurePutter();
            var hole = ConfigureHole();

            var aimGo = new GameObject("AimIndicator");
            aimGo.transform.SetParent(transform, false);
            var aim = aimGo.AddComponent<AimIndicator>();
            aim.Initialize(ball.transform);

            var shot = gameObject.AddComponent<ShotController>();
            shot.Initialize(ball, aim, putter, camera);

            var manager = gameObject.AddComponent<GameManager>();
            manager.Initialize(ball, shot, putter, camera);

            if (hole != null)
                hole.OnBallEntered += manager.NotifyHole;

            CreateOutOfBounds();
            Debug.Log("[Minigolf] Prototype ready. Hold screen to charge, release to putt into the vase.");
        }

        static BallController ConfigureBall(GameObject ballGo)
        {
            ballGo.tag = "Ball";

            var rb = ballGo.GetComponent<Rigidbody>();
            if (rb == null)
                rb = ballGo.AddComponent<Rigidbody>();

            rb.mass = 0.045f;
            rb.linearDamping = 0.35f;
            rb.angularDamping = 1.2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.None;

            var col = ballGo.GetComponent<SphereCollider>();
            if (col != null)
            {
                var mat = new PhysicsMaterial("GolfBallMat")
                {
                    dynamicFriction = 0.35f,
                    staticFriction = 0.4f,
                    bounciness = 0.15f,
                    frictionCombine = PhysicsMaterialCombine.Average,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };
                col.sharedMaterial = mat;
            }

            var ball = ballGo.GetComponent<BallController>();
            if (ball == null)
                ball = ballGo.AddComponent<BallController>();
            ball.CaptureTee();
            return ball;
        }

        static PutterAnimator ConfigurePutter()
        {
            var putterGo = GameObject.Find("Putter");
            if (putterGo == null)
                return null;

            var anim = putterGo.GetComponent<PutterAnimator>();
            if (anim == null)
                anim = putterGo.AddComponent<PutterAnimator>();
            return anim;
        }

        static void DisablePutterColliders()
        {
            var putterGo = GameObject.Find("Putter");
            if (putterGo == null)
                return;

            foreach (var col in putterGo.GetComponentsInChildren<Collider>())
                col.enabled = false;
        }

        static HoleTrigger ConfigureHole()
        {
            var vase = GameObject.Find("Vase");
            if (vase == null)
            {
                Debug.LogWarning("[Minigolf] Vase not found — creating fallback hole.");
                return CreateFallbackHole(new Vector3(8.86f, 0.12f, 3.591f));
            }

            // Prefer the vase mesh child collider as the cup trigger.
            Collider cup = null;
            foreach (var col in vase.GetComponentsInChildren<Collider>())
            {
                cup = col;
                break;
            }

            if (cup == null)
            {
                var triggerGo = new GameObject("HoleTrigger");
                triggerGo.transform.SetParent(vase.transform, false);
                triggerGo.transform.localPosition = Vector3.zero;
                var sphere = triggerGo.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 0.35f;
                cup = sphere;
            }
            else
            {
                cup.isTrigger = true;
                // Slightly enlarge the cup so hole-in-one is achievable for a prototype.
                if (cup is CapsuleCollider capsule)
                {
                    capsule.radius = Mathf.Max(capsule.radius, 0.65f);
                    capsule.height = Mathf.Max(capsule.height, 2.2f);
                }
                else if (cup is SphereCollider sphere)
                {
                    sphere.radius = Mathf.Max(sphere.radius, 0.35f);
                }
            }

            var hole = cup.GetComponent<HoleTrigger>();
            if (hole == null)
                hole = cup.gameObject.AddComponent<HoleTrigger>();
            return hole;
        }

        static HoleTrigger CreateFallbackHole(Vector3 worldPos)
        {
            var go = new GameObject("Hole");
            go.transform.position = worldPos;
            var sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.28f;

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "HoleMarker";
            marker.transform.SetParent(go.transform, false);
            marker.transform.localScale = new Vector3(0.45f, 0.01f, 0.45f);
            Object.Destroy(marker.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat != null)
            {
                mat.color = new Color(0.05f, 0.05f, 0.05f);
                marker.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            return go.AddComponent<HoleTrigger>();
        }

        static void ConfigureGreenFriction()
        {
            var passage = GameObject.Find("Passage");
            if (passage == null)
                return;

            var col = passage.GetComponent<Collider>();
            if (col == null)
                return;

            col.sharedMaterial = new PhysicsMaterial("GreenMat")
            {
                dynamicFriction = 0.55f,
                staticFriction = 0.6f,
                bounciness = 0.05f,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
        }

        void CreateOutOfBounds()
        {
            var go = new GameObject("OutOfBounds");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, -1.5f, 4.5f);
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(40f, 1f, 40f);
            go.AddComponent<OutOfBoundsTrigger>();
        }
    }

    public class OutOfBoundsTrigger : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            var ball = other.GetComponentInParent<BallController>();
            ball?.ForceOutOfBounds();
        }
    }
}
