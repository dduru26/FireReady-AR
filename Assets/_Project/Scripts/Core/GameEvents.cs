using UnityEngine;

namespace FireReady.Core
{
    // ---- Simulation flow -------------------------------------------------

    public readonly struct SimulationStateChanged : IGameEvent
    {
        public readonly SimulationStateId Previous;
        public readonly SimulationStateId Current;

        public SimulationStateChanged(SimulationStateId previous, SimulationStateId current)
        {
            Previous = previous;
            Current = current;
        }
    }

    // ---- AR setup --------------------------------------------------------

    /// <summary>The first usable plane has been detected.</summary>
    public readonly struct PlaneDetected : IGameEvent { }

    /// <summary>What the placement reticle is currently aimed at.</summary>
    public readonly struct PlacementTargetChanged : IGameEvent
    {
        public readonly bool HasTarget;
        public readonly bool IsLargeEnough;
        public readonly Vector2 AreaSize;

        public PlacementTargetChanged(bool hasTarget, bool isLargeEnough, Vector2 areaSize)
        {
            HasTarget = hasTarget;
            IsLargeEnough = isLargeEnough;
            AreaSize = areaSize;
        }
    }

    /// <summary>The trainee pressed "Place here".</summary>
    public readonly struct PlaceRequested : IGameEvent { }

    /// <summary>The training scene now sits on an anchor.</summary>
    public readonly struct ScenePlaced : IGameEvent
    {
        public readonly Transform SceneRoot;
        public readonly bool IsReposition;

        public ScenePlaced(Transform sceneRoot, bool isReposition)
        {
            SceneRoot = sceneRoot;
            IsReposition = isReposition;
        }
    }

    /// <summary>The trainee accepted the placement.</summary>
    public readonly struct PlacementConfirmed : IGameEvent { }

    /// <summary>The trainee wants to move the scene (Move button or double-tap).</summary>
    public readonly struct RepositionRequested : IGameEvent { }

    // ---- Tracking --------------------------------------------------------

    public readonly struct TrackingLost : IGameEvent
    {
        public readonly string Reason;
        public TrackingLost(string reason) => Reason = reason;
    }

    public readonly struct TrackingRestored : IGameEvent { }

    /// <summary>The trainee pressed Resume on the Paused screen.</summary>
    public readonly struct ResumeRequested : IGameEvent { }
}
