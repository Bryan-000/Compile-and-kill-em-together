namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Assets;
using Jaket.Content;
using Jaket.Input;
using Jaket.IO;
using Jaket.UI.Dialogs;

/// <summary>
/// There is a single instance of this entity, representing the player on whose machine the game is running.
/// The snapshot structure of this entity is identical to the remote player structure.
/// </summary>
public class LocalPlayer : Entity
{
    static NewMovement nm => NewMovement.Instance;
    static FistControl fc => FistControl.Instance;
    static GameObject cw => GunControl.Instance.currentWeapon;

    /// <summary> Team required for versus mechanics. </summary>
    public Team Team;
    /// <summary> Source playing the voice of the player. </summary>
    public AudioSource Voice = Create<AudioSource>("Sam");

    /// <summary> Whether the player just parried a projectile. </summary>
    public bool Parried;
    /// <summary> Grappling hook position, zero if the hook is not currently in use. </summary>
    public Vector3 Hook;
    /// <summary> Entity of the item the player is currently holding in their hands. </summary>
    public Item Holding;

    /// <summary> Identifier of the displayed weapon. </summary>
    private byte weapon;
    /// <summary> Whether the current level is 4-4. </summary>
    private bool pyramid;

    public LocalPlayer() : base(Tools.Id.AccountId, EntityType.Player)
    {
        Events.OnHandChange += () =>
        {
            SyncSuit();
            Recolor();
        };
        Events.OnTeamChange += () =>
        {
            var shine = nm.transform.Find("Point Light");
            if (shine) shine.GetComponent<Light>().color = LobbyController.Offline ? Color.white : Team.Color();
        };
        Create<Agent>("Agent").Patron = this;
        Owner = Id;
    }

    #region snapshot

    public override int BufferSize => 42;

    public override void Write(Writer w)
    {
        bool sliding = nm.sliding || (pyramid && nm.transform.position.y > 610f && nm.transform.position.y < 611f);

        w.Vector(nm.transform.position - Vector3.up * (sliding ? .3f : 1.5f));
        w.Vector(Hook);

        w.Float(nm.transform.eulerAngles.y);
        w.Float(135f - Mathf.Clamp(CameraController.Instance.rotationX, -40f, 80f));

        w.Byte((byte)nm.hp);
        w.Byte((byte)Mathf.Floor(WeaponCharges.Instance.raicharge * 2.5f));

        w.Player(Team, weapon, Emotes.Current, Emotes.Rps, Chat.Shown);
        w.Bools(
            nm.walking,
            sliding,
            !nm.gc.onGround,
            nm.gc.heavyFall,
            nm.boost && !sliding,
            nm.ridingRocket,
            Hook != Vector3.zero,
            fc.shopping);
    }

    public override void Read(Reader r) { }

    #endregion
    #region logic

    public override void Create() { }

    public override void Assign(Agent agent) { }

    public override void Update(float delta)
    {
        if (Holding == null || Holding.IsOwner) return;
        Holding = null;

        fc.currentPunch.ForceThrow();
        fc.currentPunch.PlaceHeldObject([], null);
    }

    public override void Damage(Reader r)
    {
        var team = r.Team();
        var drill = Bullets.Types[r.Byte()] == "drill";

        if (nm.dead || team.Ally()) return;

        nm.GetHurt(Mathf.CeilToInt(r.Float() * 4f), drill, 0f);
        if (nm.dead) LobbyController.Lobby?.SendChatString("#/s" + (byte)team);
    }

    public override void Killed(Reader r, int left) { }

    #endregion
    #region other

    /// <summary> Synchronizes the suit and custom weapon colors. </summary>
    public void SyncSuit()
    {
        Renderer renderer = null;
        bool custom = (cw?.GetComponentInChildren<GunColorGetter>()?.TryGetComponent(out renderer) ?? false) && renderer.material.name.Contains("Custom");

        Networking.Send(PacketType.Style, custom ? 25 : 13, w =>
        {
            w.Id(Id);
            w.Int(Shop.SelectedHat);
            w.Int(Shop.SelectedJacket);

            w.Bool(custom);
            if (custom) renderer.Properties(b =>
            {
                w.Color(b.GetColor("_CustomColor1"));
                w.Color(b.GetColor("_CustomColor2"));
                w.Color(b.GetColor("_CustomColor3"));
            });
        });
    }

    /// <summary> Recolors the hands seen in first person and caches some values. </summary>
    public void Recolor()
    {
        var main = cw?.transform.GetChild(0).Find("RightArm");
        if (main) main.GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture = ModAssets.HandTexture(0);

        var feed = fc?.transform.Find("Arm Blue(Clone)");
        if (feed) feed.GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture = ModAssets.HandTexture(1);

        var knkl = fc?.transform.Find("Arm Red(Clone)");
        if (knkl) knkl.GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture = ModAssets.HandTexture(2);

        weapon = Weapons.Type();
        pyramid = Scene == "Level 4-4";
    }

    #endregion
}
