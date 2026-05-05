#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor automation that scaffolds <see cref="PowerUpSO"/> assets for every
/// <see cref="PowerUpSO.PowerUpKey"/> enum value. Mirrors the HUD / Audio
/// setup pattern: idempotent — only creates missing assets, never overwrites
/// hand-tuned ones (skip-if-exists). Pickled defaults (tint, duration, mutex)
/// are sensible starting points; effect behaviour still lives in
/// <c>PowerUpFactory</c>.
/// </summary>
public static class PowerUpSetupAutomation
{
    private const string Folder = "Assets/_Project/ScriptableObjects/Powerups";

    private struct Defaults
    {
        public string                   displayName;
        public string                   description;
        public Color                    tint;
        public float                    duration;
        public PowerUpSO.MutexGroup     mutex;
        public bool                     stackable;
    }

    public static void RunAll()
    {
        EditorAssetUtils.EnsureFolder(Folder);

        int created = 0, skipped = 0;
        foreach (PowerUpSO.PowerUpKey key in Enum.GetValues(typeof(PowerUpSO.PowerUpKey)))
        {
            if (key == PowerUpSO.PowerUpKey.None) continue;

            string path = $"{Folder}/PowerUp_{key}.asset";
            if (AssetDatabase.LoadAssetAtPath<PowerUpSO>(path) != null)
            {
                skipped++;
                continue;
            }

            var so = ScriptableObject.CreateInstance<PowerUpSO>();
            ApplyDefaults(so, key);
            AssetDatabase.CreateAsset(so, path);
            EditorUtility.SetDirty(so);
            created++;
        }

        EditorAssetUtils.Refresh();
        Debug.Log($"[PowerUpSetup] Created {created} new PowerUpSO asset(s); skipped {skipped} existing.");
    }

    private static void ApplyDefaults(PowerUpSO so, PowerUpSO.PowerUpKey key)
    {
        var d = ResolveDefaults(key);
        so.key         = key;
        so.displayName = d.displayName;
        so.description = d.description;
        so.tint        = d.tint;
        so.durationSec = d.duration;
        so.mutex       = d.mutex;
        so.stackable   = d.stackable;
        // activateCue / pickupCue keep their SO field defaults from the class.
    }

    private static Defaults ResolveDefaults(PowerUpSO.PowerUpKey key) => key switch
    {
        PowerUpSO.PowerUpKey.Caffeinated      => new Defaults {
            displayName = "Caffeinated",
            description = "Move + dash speed boost.",
            tint        = new Color(1.00f, 0.92f, 0.40f),
            duration    = 10f,
            mutex       = PowerUpSO.MutexGroup.None,
            stackable   = true,
        },
        PowerUpSO.PowerUpKey.DashThroughWalls => new Defaults {
            displayName = "Dash Through Walls",
            description = "Dash phases through walls for the duration.",
            tint        = new Color(0.66f, 0.50f, 0.96f),
            duration    = 8f,
            mutex       = PowerUpSO.MutexGroup.None,
            stackable   = false,
        },
        PowerUpSO.PowerUpKey.Teleport         => new Defaults {
            displayName = "Teleport",
            description = "One-shot teleport to aim direction.",
            tint        = new Color(0.40f, 0.90f, 1.00f),
            duration    = 5f,
            mutex       = PowerUpSO.MutexGroup.None,
            stackable   = false,
        },
        PowerUpSO.PowerUpKey.Explosive        => new Defaults {
            displayName = "Explosive",
            description = "Boomerang explodes on hit, AoE damage.",
            tint        = new Color(1.00f, 0.55f, 0.20f),
            duration    = 12f,
            mutex       = PowerUpSO.MutexGroup.None,
            stackable   = true,
        },
        PowerUpSO.PowerUpKey.Multi            => new Defaults {
            displayName = "Multi-Shot",
            description = "Throw spawns 3 boomerangs in a fan.",
            tint        = new Color(0.85f, 0.60f, 1.00f),
            duration    = 12f,
            mutex       = PowerUpSO.MutexGroup.None,
            stackable   = true,
        },
        PowerUpSO.PowerUpKey.Extra            => new Defaults {
            displayName = "Extra Boomerang",
            description = "Extra boomerang charge in flight.",
            tint        = new Color(1.00f, 0.84f, 0.40f),
            duration    = 12f,
            mutex       = PowerUpSO.MutexGroup.None,
            stackable   = true,
        },
        PowerUpSO.PowerUpKey.Fire             => new Defaults {
            displayName = "Fire",
            description = "Boomerang ignites victim — lethal seketika.",
            tint        = new Color(1.00f, 0.40f, 0.10f),
            duration    = 12f,
            mutex       = PowerUpSO.MutexGroup.FireIce,
            stackable   = true,
        },
        PowerUpSO.PowerUpKey.Ice              => new Defaults {
            displayName = "Ice",
            description = "Boomerang freezes victim briefly.",
            tint        = new Color(0.55f, 0.90f, 1.00f),
            duration    = 12f,
            mutex       = PowerUpSO.MutexGroup.FireIce,
            stackable   = true,
        },
        PowerUpSO.PowerUpKey.Disguise         => new Defaults {
            displayName = "Disguise",
            description = "Look like a wall / pickup briefly.",
            tint        = new Color(0.65f, 0.65f, 0.70f),
            duration    = 8f,
            mutex       = PowerUpSO.MutexGroup.None,
            stackable   = false,
        },
        PowerUpSO.PowerUpKey.Shield           => new Defaults {
            displayName = "Shield",
            description = "Absorbs the next hit, then breaks.",
            tint        = new Color(1.00f, 0.95f, 0.40f),
            duration    = 12f,
            mutex       = PowerUpSO.MutexGroup.None,
            stackable   = false,
        },
        PowerUpSO.PowerUpKey.Telekinesis      => new Defaults {
            displayName = "Telekinesis",
            description = "Steer thrown boomerang mid-flight.",
            tint        = new Color(0.80f, 0.70f, 0.95f),
            duration    = 10f,
            mutex       = PowerUpSO.MutexGroup.None,
            stackable   = false,
        },
        PowerUpSO.PowerUpKey.Decoy            => new Defaults {
            displayName = "Decoy",
            description = "Spawn a clone that pulls aggro.",
            tint        = new Color(0.96f, 0.78f, 0.86f),
            duration    = 10f,
            mutex       = PowerUpSO.MutexGroup.None,
            stackable   = false,
        },
        _ => new Defaults {
            displayName = key.ToString(),
            description = "",
            tint        = Color.white,
            duration    = 10f,
            mutex       = PowerUpSO.MutexGroup.None,
            stackable   = true,
        },
    };
}
#endif
