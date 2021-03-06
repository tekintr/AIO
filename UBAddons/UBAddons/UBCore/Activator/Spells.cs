﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK.Spells;
using EloBuddy.SDK;
using UBAddons.Libs;

namespace UBAddons.UBCore.Activator
{
    class Spells
    {
        private static bool Initialized { get; set; }
        private static Spell.Active Barrier = SummonerSpells.Barrier;
        internal static Spell.Active Cleanse = SummonerSpells.Cleanse;
        private static Spell.Targeted Exhaust = SummonerSpells.Exhaust;
        private static Spell.Active Heal = SummonerSpells.Heal;
        private static Spell.Targeted Ignite = SummonerSpells.Ignite;
        private static Spell.Skillshot Mark = SummonerSpells.Mark;
        private static Spell.Targeted Smite = SummonerSpells.Smite;

        private static readonly Dictionary<SummonerSpellsEnum, Spell.SpellBase> SummonerList = new Dictionary<SummonerSpellsEnum, Spell.SpellBase>()
        {
            { SummonerSpellsEnum.Barrier, Barrier },
            { SummonerSpellsEnum.Exhaust, Exhaust },
            { SummonerSpellsEnum.Heal, Heal },
            { SummonerSpellsEnum.Ignite, Ignite },
            { SummonerSpellsEnum.Mark, Mark },
            { SummonerSpellsEnum.Smite, Smite },
        };
        static Spells()
        {
            var keys = SummonerList.Where(x => x.Value.Slot == SpellSlot.Unknown).Select(x => x.Key).ToList();
            foreach (var key in keys)
            {
                SummonerList.Remove(key);
            }
            if (SummonerSpells.PlayerHas(SummonerSpellsEnum.Barrier))
            {
                GameObject.OnCreate += GameObject_OnCreate;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            }
        }
        public static void OnLoad()
        {
            if (Initialized)
            {
                return;
            }
            Initialized = true;
        }
        internal static void OnTick()
        {
            foreach (var spells in SummonerList.Where(x => x.Value.IsReady()))
            {
                UseSummoner(spells.Key);
            }
        }
        private static void UseSummoner(SummonerSpellsEnum summoner)
        {
            switch (summoner)
            {
                case SummonerSpellsEnum.Exhaust:
                    {
                        if (!Exhaust.IsReady() || !Main.SpellsMenu.VChecked("Exhaust.Enabled") || (Player.HasBuffOfType(BuffType.Invisibility) && Main.SpellsMenu.VChecked("Exhaust.Stealth"))
                            || (Main.SpellsMenu.VChecked("Exhaust.Combo") && !Orbwalker.ActiveModes.Combo.IsOrb()))
                        {
                            break;
                        }
                        var target = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(Exhaust.Range)
                        && (Main.SpellsMenu.VSliderValue("Exhaust.EnemyHP") <= x.HealthPercent
                        || EntityManager.Heroes.Allies.Any(ally => ally.IsValidTarget(800) && Main.SpellsMenu.VSliderValue("Exhaust.AllyHP") <= ally.HealthPercent)));
                        switch (Main.SpellsMenu.VComboValue("Exhaust.Target"))
                        {
                            case 0:
                                {
                                    target.OrderByDescending(x => TargetSelector.GetPriority(x));
                                }
                                break;
                            case 1:
                                {
                                    target.OrderBy(x => x.Health);
                                }
                                break;
                            case 2:
                                {
                                    target.OrderByDescending(x => x.TotalAttackDamage);
                                }
                                break;
                            case 3:
                                {
                                    target.OrderByDescending(x => x.TotalMagicalDamage);
                                }
                                break;
                            case 4:
                                {
                                    target.OrderByDescending(x => x.MoveSpeed);
                                }
                                break;
                            case 5:
                                {
                                    target.OrderByDescending(x => x.PercentAttackSpeedMod);
                                }
                                break;
                        }
                        if (target.Any())
                        {
                            Exhaust.Cast(target.First());
                        }
                    }
                    break;
                case SummonerSpellsEnum.Heal:
                    {
                        if (!Heal.IsReady() || !Main.SpellsMenu.VChecked("Heal.Enabled") || (Player.HasBuffOfType(BuffType.Invisibility) && Main.SpellsMenu.VChecked("Heal.Stealth"))
                            || (Main.SpellsMenu.VChecked("Heal.Combo") && !Orbwalker.ActiveModes.Combo.IsOrb()))
                        {
                            break;
                        }
                        var allies = EntityManager.Heroes.Allies.Where(x => x.IsValidTarget(Heal.Range) && x.HealthPercent <= (x.IsMe ? Main.SpellsMenu.VSliderValue("Heal.MyHP") : Main.SpellsMenu.VSliderValue("Heal.AllyHP")));
                        if (!allies.Any()) break;
                        var dic = new Dictionary<Obj_AI_Base, int>();
                        foreach (var ally in allies)
                        {
                            dic.Add(ally, 500);
                        }
                        if (!Main.SpellsMenu.VChecked("Heal.Smart") || Prediction.Health.GetPrediction(dic).Any(x => x.Value <= 0))
                        {
                            Heal.Cast();
                        }
                    }
                    break;
                case SummonerSpellsEnum.Ignite:
                    {
                        if (!Ignite.IsReady() || !Main.SpellsMenu.VChecked("Ignite.Enabled")
                            || (Main.SpellsMenu.VChecked("Ignite.Combo") && !Orbwalker.ActiveModes.Combo.IsOrb()))
                        {
                            break;
                        }
                        var target = EntityManager.Heroes.Enemies.Where(t =>
                                t.IsValidTarget(Ignite.Range) &&
                                t.Health <= Player.Instance.GetSummonerSpellDamage(t, DamageLibrary.SummonerSpells.Ignite)).FirstOrDefault();

                        if (target == null)
                        {
                            break;
                        }
                        Ignite.Cast(target);
                    }
                    break;
                case SummonerSpellsEnum.Mark:
                    {
                        if (!Mark.IsReady() || !Main.SpellsMenu.VChecked("Mark.Enabled")
                            || !Orbwalker.ActiveModes.Combo.IsOrb())
                        {
                            break;
                        }
                        var target = Mark.GetTarget();
                        if (target == null) break;
                        var pred = Mark.GetPrediction(target);
                        if (pred.CanNext(Mark, Main.SpellsMenu.VSliderValue("Mark.Percent"), true))
                        {
                            Mark.Cast(pred.CastPosition);
                        }
                    }
                    break;
                case SummonerSpellsEnum.Smite:
                    {
                        if (!Smite.IsReady() || !Main.SpellsMenu.VChecked("Smite.Enabled"))
                        {
                            break;
                        }
                        var target = EntityManager.Heroes.Enemies.Where(t =>
                                t.IsValidTarget(Smite.Range) &&
                                t.Health <= Player.Instance.GetSummonerSpellDamage(t, DamageLibrary.SummonerSpells.Smite)).FirstOrDefault();
                        var minion = ObjectManager.Get<Obj_AI_Minion>()
                            .FirstOrDefault(m => m.Health < Player.Instance.GetSummonerSpellDamage(m, DamageLibrary.SummonerSpells.Smite)
                            && m != null && m.IsMonster && m.IsImportant() && Smite.IsInRange(m));
                        if (minion != null)
                        {
                            Smite.Cast(minion);
                        }
                        if (target != null)
                        {
                            Smite.Cast(target);
                        }
                    }
                    break;
                case SummonerSpellsEnum.Barrier:
                    {
                        if (!Barrier.IsReady() || !Main.SpellsMenu.VChecked("Barrier.Enabled") || (Player.HasBuffOfType(BuffType.Invisibility) && Main.SpellsMenu.VChecked("Barrier.Stealth"))
                            || (Main.SpellsMenu.VChecked("Barrier.Combo") && !Orbwalker.ActiveModes.Combo.IsOrb()) || Player.Instance.HealthPercent > Main.SpellsMenu.VSliderValue("Barrier.MyHP"))
                        {
                            break;
                        }
                        if (!Main.SpellsMenu.VChecked("Barrier.Smart") || Prediction.Health.GetPrediction(Player.Instance, 800) <= 0)
                        {
                            Barrier.Cast();
                        }
                    }
                    break;
                default:
                    {
                        break;
                    }
            }
        }
        internal static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!Barrier.IsReady() || !sender.IsEnemy || args.Target == null || !args.Target.IsMe || !Main.SpellsMenu.VChecked("Barrier.Enabled")
                || (Main.SpellsMenu.VChecked("Barrier.Combo") && !Orbwalker.ActiveModes.Combo.IsOrb()) || (Player.HasBuffOfType(BuffType.Invisibility) && Main.SpellsMenu.VChecked("Barrier.Stealth"))) return;
            var caster = sender as AIHeroClient;
            var Target = args.Target as AIHeroClient;
            if (caster == null || Target == null) return;
            if (caster.GetSpellDamage(Target, args.Slot) >= Main.SpellsMenu.VSliderValue("Barrier.Value") * 50)
            {
                Barrier.Cast();
            }
        }
        internal static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (!Barrier.IsReady() || sender == null || (Main.SpellsMenu.VChecked("Barrier.Combo") && !Orbwalker.ActiveModes.Combo.IsOrb()) 
                || (Player.HasBuffOfType(BuffType.Invisibility) && Main.SpellsMenu.VChecked("Barrier.Stealth"))) return;
            var misile = sender as MissileClient;
            if (misile == null || misile.Target == null || !misile.SpellCaster.IsEnemy || !misile.Target.IsMe) return;
            if (misile.GetDamage() >= Main.SpellsMenu.VSliderValue("Barrier.Value") * 50)
            {
                Barrier.Cast();
            }
        }
    }
}
