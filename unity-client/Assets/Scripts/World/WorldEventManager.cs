using System;
using System.Collections.Generic;
using UnityEngine;

namespace EternalKingdoms.World
{
    // -------------------------------------------------------------------------
    // Event types (Phase 8+ implementation, infrastructure only in Phase 3)
    // -------------------------------------------------------------------------

    public enum WorldEventType
    {
        ShrineCapture,      // Phase 6: alliance captures a shrine
        CongressSession,    // Phase 6: Congress session opens
        SeasonEvent,        // Phase 7: seasonal event begins
        BossSpawn,          // Phase 7: world boss appears
        CrystalRush,        // Phase 7: crystal nodes double-yield window
        AllianceWar,        // Phase 6: war declared
        RealmBattle,        // Phase 8: cross-world battle
    }

    public class WorldEvent
    {
        public int            Id;
        public WorldEventType EventType;
        public string         Title;
        public string         Description;
        public DateTime       StartsAt;
        public DateTime       EndsAt;
        public bool           IsActive => DateTime.UtcNow >= StartsAt && DateTime.UtcNow < EndsAt;
        public bool           IsUpcoming => DateTime.UtcNow < StartsAt;
    }

    /// <summary>
    /// WorldEventManager — infrastructure foundation for world events.
    ///
    /// Phase 3 scope: data model + event registry only.
    ///   - No backend polling yet (endpoints arrive in Phase 6).
    ///   - Exposes RegisterEvent / GetActiveEvents / OnEventStarted / OnEventEnded.
    ///   - WorldSimulationManager will tick this every second (Phase 6+).
    ///
    /// Phase 6: Shrine capture events, Congress session countdowns.
    /// Phase 7: Boss spawn events, Crystal Rush, Season Events.
    /// Phase 8: Realm Battle notifications.
    ///
    /// Event UI:
    ///   - WorldBottomBar "Events" button (already exists) will open EventListPanel.
    ///   - Active events appear as pulsing icons on the world map near their tile.
    ///   - Toast notifications via NotificationManager when events start/end.
    ///
    /// DontDestroyOnLoad — persists across Kingdom ↔ World transitions.
    /// </summary>
    public class WorldEventManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        public event Action<WorldEvent> OnEventStarted;
        public event Action<WorldEvent> OnEventEnded;
        public event Action<WorldEvent> OnEventRegistered;

        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------

        private static WorldEventManager _instance;
        public  static WorldEventManager Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private readonly Dictionary<int, WorldEvent>      _events         = new();
        private readonly HashSet<int>                     _firedStarted   = new();
        private readonly HashSet<int>                     _firedEnded     = new();

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// Register an event (from backend poll or admin trigger).
        public void RegisterEvent(WorldEvent worldEvent)
        {
            _events[worldEvent.Id] = worldEvent;
            OnEventRegistered?.Invoke(worldEvent);
        }

        /// Returns all currently active events (StartsAt ≤ now < EndsAt).
        public IReadOnlyList<WorldEvent> GetActiveEvents()
        {
            var now    = DateTime.UtcNow;
            var result = new List<WorldEvent>();
            foreach (var e in _events.Values)
                if (e.IsActive) result.Add(e);
            return result;
        }

        /// Returns events starting within the next N minutes.
        public IReadOnlyList<WorldEvent> GetUpcomingEvents(double withinMinutes = 60)
        {
            var now    = DateTime.UtcNow;
            var cutoff = now.AddMinutes(withinMinutes);
            var result = new List<WorldEvent>();
            foreach (var e in _events.Values)
                if (e.IsUpcoming && e.StartsAt <= cutoff) result.Add(e);
            return result;
        }

        /// Seconds until the next event starts (or 0 if one is active).
        public double SecondsUntilNextEvent()
        {
            var now = DateTime.UtcNow;
            double min = double.MaxValue;
            foreach (var e in _events.Values)
            {
                if (e.IsActive) return 0;
                if (e.IsUpcoming)
                {
                    double secs = (e.StartsAt - now).TotalSeconds;
                    if (secs < min) min = secs;
                }
            }
            return min == double.MaxValue ? -1 : min;
        }

        // -------------------------------------------------------------------------
        // Tick — called by WorldSimulationManager every second
        // -------------------------------------------------------------------------

        public void Tick(DateTime now)
        {
            foreach (var e in _events.Values)
            {
                // Fire started
                if (e.IsActive && !_firedStarted.Contains(e.Id))
                {
                    _firedStarted.Add(e.Id);
                    OnEventStarted?.Invoke(e);
                }

                // Fire ended
                if (!e.IsActive && !e.IsUpcoming && !_firedEnded.Contains(e.Id))
                {
                    _firedEnded.Add(e.Id);
                    OnEventEnded?.Invoke(e);
                }
            }
        }

        // -------------------------------------------------------------------------
        // Reset
        // -------------------------------------------------------------------------

        public void ClearEvents()
        {
            _events.Clear();
            _firedStarted.Clear();
            _firedEnded.Clear();
        }
    }
}
