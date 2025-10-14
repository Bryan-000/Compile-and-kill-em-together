/*
namespace Jaket.Content;

using System.Collections.Generic;
using UnityEngine;

using Jaket.IO;
using Jaket.Net;
using Jaket.Net.Types;

/// <summary> List of all bullets in the game and some useful methods. </summary>
public class Bullets
{
    /// <summary> List of prefabs of all bullets. </summary>
    public static List<GameObject> Prefabs = new();

    /// <summary> Damage markers needed to prevent synchronized bullets from dealing extra damage. </summary>
    public static GameObject Fake, NetDmg;
    /// <summary> List of all synchronized damage types. </summary>
    public static string[] Types = new[]
    {
        /* hitscan * "revolver", "railcannon", "coin",
        /* shotgun * "shotgun", "shotgunzone", "chainsaw", "chainsawzone", "chainsawbounce", "chainsawprojectile", "hammer",
        /* other * "nail", "sawblade", "cannonball",
        /* drill * "harpoon", "drill", "drillpunch",
        /* environmental * "explosion" /*???*, "aftershock", "zapper",
        /* melee * "punch", "heavypunch", "ground slam", "hook",
        /* parry * "projectile", "enemy"
    };

    /// <summary> Loads all bullets for future use. </summary>
    public static void Load()
    {
        void Add(GameObject bullet, string name)
        {
            if (bullet == null || Prefabs.Contains(bullet)) return;
            Prefabs.Add(bullet);
            bullet.name = name;
        }
        int rv = 0, ng = 0, rc = 0;
        foreach (var weapon in Weapons.Prefabs)
        {
            if (weapon.TryGetComponent<Revolver>(out var revolver))
            {
                Add(revolver.revolverBeam, $"RV{++rv} PRI");
                Add(revolver.revolverBeamSuper, $"RV{rv} ALT");
                Add(revolver.coin, "Coin"); // a coin is also a bullet, honestly-honestly
            }
            else
            if (weapon.TryGetComponent<Shotgun>(out var shotgun))
            {
                Add(shotgun.bullet, $"SG PRI");
                Add(shotgun.grenade, $"SG ALT");
                Add(shotgun.explosion, $"SG EXT");
            }
            else
            if (weapon.TryGetComponent<ShotgunHammer>(out var hammer))
            {
                Add(Get("overPumpExplosion", hammer) as GameObject, "SH"); // thank you, developers
            }
            else
            if (weapon.TryGetComponent<Railcannon>(out var railcannon))
            {
                Add(railcannon.beam, $"RC{++rc}");
            }
            else
            if (weapon.TryGetComponent<RocketLauncher>(out var launcher))
            {
                Add(launcher.rocket, $"RL PRI");
                Add((Get("napalmProjectile", launcher) as Rigidbody)?.gameObject, $"RL EXT");
            }
        }

        Fake = Create("Fake").gameObject;
        NetDmg = Create("Network Damage");
    }

    /// <summary> Finds the bullet type by object name. </summary>
    public static byte CType(string name)
    {
        name = name.Contains("(") ? name.Substring(0, name.IndexOf("(")) : name;
        return (byte)Prefabs.FindIndex(prefab => prefab.name == name);
    }
    public static EntityType EType(string name) => name switch
    {
        "Coin(Clone)" => EntityType.Coin,
        "RL PRI(Clone)" => EntityType.Rocket,
        _ => EntityType.None
    };

    /// <summary> Spawns a bullet with the given type or other data. </summary>
    public static void CInstantiate(Reader r)
    {
        var obj = Entities.Mark(Prefabs[r.Byte()]);
        obj.transform.position = r.Vector();
        obj.transform.eulerAngles = r.Vector();

        // TODO get the size as it would be unpleasant to write additional bytes
        if (r.Length == 27) Coins.PaintBeam(obj, r.Enum<Team>());
        if (r.Length == 38) obj.GetComponent<Rigidbody>().velocity = r.Vector();
    }
    public static Entity EInstantiate(EntityType type) => Entities.Mark(Prefabs[type switch
    {
        EntityType.Coin => 4,
        EntityType.Rocket => 21,
        _ => -1
    }]).AddComponent(type == EntityType.Coin ? typeof(TeamCoin) : typeof(Bullet)) as Entity;

    /// <summary> Synchronizes the bullet between network members. </summary>
    public static void Sync(GameObject bullet, bool hasRigidbody, bool applyOffset, byte team = byte.MaxValue)
    {
        if (LobbyController.Offline || bullet == null || bullet.name == "Net") return;

        if (bullet.name != "Coin(Clone)" && bullet.name != "RL PRI(Clone)" && bullet.name != "RL ALT(Clone)")
        {
            var type = CType(bullet.name);
            if (type == 0xFF) return; // how? these are probably enemy projectiles

            Networking.Send(PacketType.SpawnBullet, hasRigidbody ? 37 : 26, w =>
            {
                w.Byte(type);
                w.Vector(bullet.transform.position + (applyOffset ? bullet.transform.forward * 2f : Vector3.zero));
                w.Vector(bullet.transform.eulerAngles);

                if (type == 0 && team != byte.MaxValue) w.Byte(team);
                if (hasRigidbody) w.Vector(bullet.GetComponent<Rigidbody>().velocity);
            });
        }
        else bullet.AddComponent(bullet.name == "Coin(Clone)" ? typeof(TeamCoin) : typeof(Bullet));
    }

    /// <summary> Synchronizes the bullet or marks it as fake if it was downloaded from the network. </summary>
    public static void Sync(GameObject bullet, ref GameObject sourceWeapon, bool hasRigidbody, bool applyOffset, byte team = byte.MaxValue)
    {
        if (sourceWeapon == null && bullet.name == "Net")
            sourceWeapon = Fake;
        else
            Sync(bullet, hasRigidbody, applyOffset, team);
    }

    /// <summary> Synchronizes the "death" of the bullet. </summary>
    public static void SyncDeath(GameObject bullet, bool harmless = true, bool big = false)
    {
        if (bullet.TryGetComponent<Entity>(out var entity) && entity.IsOwner)
        {
            Networking.Send(PacketType.Death, harmless ? 4 : 5, w =>
            {
                w.Id(entity.Id);
                if (!harmless) w.Bool(big);
            });
        }
    }

    #region damage

    /// <summary> Synchronizes damage dealt to the enemy. </summary>
    public static void SyncDamage(uint enemyId, string hitter, float damage, float crit)
    {
        var type = (byte)Types.IndexOf(hitter);
        if (type != 0xFF) Networking.Send(PacketType.Damage, 11, w =>
        {
            w.Id(enemyId);
            w.Enum(Networking.LocalPlayer.Team);
            w.Byte(type);

            w.Float(damage);
            w.Bool(crit == 0f);
        });
    }

    /// <summary> Deals bullet damage to the enemy. </summary>
    public static void DealDamage(EnemyIdentifier enemyId, Reader r)
    {
        r.Team(); // skip team because enemies don't have a team
        enemyId.hitter = Types[r.Byte()];
        enemyId.DeliverDamage(enemyId.gameObject, Vector3.zero, enemyId.transform.position, r.Float(), false, r.Bool() ? 0f : 1f, NetDmg);
    }

    #endregion
}
*/
