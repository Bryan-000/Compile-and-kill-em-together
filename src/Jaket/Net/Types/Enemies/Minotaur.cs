namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.World;

/// <summary> Representation of Minotaur. </summary>
public class Minotaur : Enemy
{
    MinotaurChase minotaur;

    /// <summary> Id of the current attack. </summary>
    public byte Attack = 0xFF, LastAttack = 0xFF;

    private void Awake()
    {
        Init(_ => EntityType.Minotaur_Chase);
        TryGetComponent(out minotaur);

        Owner = LobbyController.LastOwner;
        World.Minotaur = this;
    }

    private void Start() => Boss(true, 80f, 1);

    private void Update() => Stats.MTE(() =>
    {
        if (IsOwner || Dead) return;
        transform.position = new(x.Get(LastUpdate), y.Target, z.Get(LastUpdate));

        if (LastAttack != Attack)
            switch (LastAttack = Attack)
            {
                case 0: Call("HammerSwing", minotaur); break;
                case 1: Call("MeatThrow", minotaur); break;
                case 2: Call("HandSwing", minotaur); break;
            }
    });

    #region entity

    public override void Write(Writer w)
    {
        UpdatesCount++;

        w.Vector(transform.position);
        w.Byte(Attack);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        x.Read(r); y.Read(r); z.Read(r);
        Attack = r.Byte();
    }

    #endregion
}
