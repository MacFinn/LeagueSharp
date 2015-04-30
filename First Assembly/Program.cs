#region Using
using Color = System.Drawing.Color;
using System.Linq;
using SharpDX;
using LeagueSharp;
using LeagueSharp.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TwistedFate;
#endregion

namespace First_Assembly
{
    class Program
    {

        #region Variables
        private static Menu Config;
        private static Obj_AI_Hero Player;
        private static Obj_AI_Hero Target;
        private static Orbwalking.Orbwalker Orbwalker;
        private static float Qangle = 28 * (float)Math.PI / 180;
        private static Spell Q;
        private static Spell W;
        private static Spell E;
        private static Spell R;
        private static SpellSlot IgniteSlot;
        private static string LastCast;
        private static float LastFlashTime;
        private static int WallCastT;
        private static Vector2 YasuoWallCastedPos;
        private static GameObject YasuoWall;
        #endregion

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (Player.ChampionName != "Twisted Fate") return;

            Q = new Spell(SpellSlot.Q, 1450);
            W = new Spell(SpellSlot.W, 525);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 5500);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");

            GameOptions Options = new GameOptions();
            Options.CreateMenu();

            Game.OnUpdate += Game_OnUpdate;
            Orbwalking.AfterAttack += OrbwalkingAfterAttack;
        }

        private static void Game_OnUpdate(EventArgs args){
            Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            Killsteal();
            if (GetActive("AutoPoke"))
            {
                AutoPoke();
            }

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Mixed();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    Freeze();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                    break;
                case Orbwalking.OrbwalkingMode.None:
                    return;
            }
        }

        static void OrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit enemy)
        {
            Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            Killsteal();
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Mixed();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    Freeze();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                    break;
                case Orbwalking.OrbwalkingMode.None:
                    return;
            }
        }

        private static void Freeze()
        {
            var allMinions = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (allMinions.Count < 1) return;
            foreach (var minion in allMinions)
            {
                if (Damage.GetAutoAttackDamage(Player, Target, false) > minion.Health && Orbwalker.InAutoAttackRange(minion))
                {
                    Orbwalker.ForceTarget(minion);
                }
            }
        }


        private static void Mixed()
        {
            if (Target == null || !detectCollision(Target)) return;
            if (IsInvul(Target))
            {
                UseCard(Cards.Yellow);
            }
            var allMinions = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (allMinions.Count < 1) return;
            if (Orbwalker.InAutoAttackRange(Target))
            {
                Orbwalker.ForceTarget(Target);
            }
            else
            {
                foreach (var minion in allMinions)
                {
                    if (Damage.GetAutoAttackDamage(Player, Target, true) > minion.Health && Orbwalker.InAutoAttackRange(minion))
                    {
                        Orbwalker.ForceTarget(minion);
                    }
                }
            }
        }

        private static void LaneClear()
        {
            Orbwalker.ActiveMode = Orbwalking.OrbwalkingMode.LaneClear;
            var allMinions = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (allMinions.Count < 1) return;
            foreach (var minion in allMinions)
            {
                
                if (Q.IsReady() && Player.ManaPercent < 40)
                {
                    Q.CastOnUnit(minion);
                } 
                else if(Player.ManaPercent > 40 && W.IsInRange(minion))
                {
                    CardSelector.StartSelecting(Cards.Blue);
                    while (CardSelector.Status == SelectStatus.Selecting)
                    {
                        if (CardSelector.Status == SelectStatus.Ready)
                        {
                            W.CastOnUnit(minion);
                        }
                    }
                }
                else
                {
                    Orbwalker.ForceTarget(minion);
                }
            }
        }

        private static void AutoPoke()
        {
            if (!GetBool("AutoPoke")) return;
            if (Q.IsReady() && Q.IsInRange(Target) && Player.ManaPercent > 30)
            {
                Q.SetSkillshot(Q.Delay, Q.Width, Q.Speed, false, SkillshotType.SkillshotLine, Q.From, Q.RangeCheckFrom);
                Q.CastIfHitchanceEquals(Target, HitChance.High);
            }
        }

        private static void Killsteal()
        {
            
            if (Orbwalker.InAutoAttackRange(Target) && Target.Health <= Damage.GetAutoAttackDamage(Player, Target, true) && Orbwalker.InAutoAttackRange(Target))
            {
                Orbwalker.SetAttack(true);
            }
            else if (Q.IsKillable(Target) && Q.IsReady())
            {
                Q.SetSkillshot(Q.Delay, Q.Width, Q.Speed, false, SkillshotType.SkillshotLine, Q.From, Q.RangeCheckFrom);
                Q.CastOnUnit(Target);
            }
            else if (W.IsKillable(Target) && W.IsReady() && W.IsInRange(Target))
            {
                Q.Cast();
                Q.CastOnUnit(Target);
            }
        }

        private static void Combo()
        {
            if (Target == null || !detectCollision(Target)) return;
            if (IsInvul(Target) && W.IsInRange(Target))
            {
                UseCard(Cards.Yellow);
            }
            if (GetBool("UseIgnite") && CanIgnite() && GetDistance(Target) <= 600 && GetComboDamage(Target) >= (double)Target.Health)
            {
                Player.Spellbook.CastSpell(IgniteSlot, Target);
            }

            if (Q.IsReady())
            {
                Q.SetSkillshot(Q.Delay, Q.Width, Q.Speed, false, SkillshotType.SkillshotLine, Q.From, Q.RangeCheckFrom);
                Q.CastOnUnit(Target);
            } 
            else if (W.IsInRange(Target) && W.IsReady())
            {
                UseCard(Cards.Yellow);
            }
            else
            {
                Orbwalker.ForceTarget(Target);
            }
        }

        private static void UseCard(Cards card)
        {
            CardSelector.StartSelecting(card);
            while (CardSelector.Status == SelectStatus.Selecting)
            {
                if (CardSelector.Status == SelectStatus.Selected)
                {
                    W.Cast(Target, true, false);
                }
            }
        }

        private static bool IsInvul(Obj_AI_Hero target)
        {
            if (target.HasBuff("JudicatorIntervention") || target.HasBuff("Undying Rage"))
                return true;
            return false;
        }

        private static bool GetActive(string s)
        {
            return Config.Item(s).GetValue<KeyBind>().Active;
        }

        private static bool GetBool(string s)
        {
            return Config.Item(s).GetValue<bool>();
        }

        private static int GetSelected(string s)
        {
            return Config.Item(s).GetValue<StringList>().SelectedIndex;
        }

        private static bool detectCollision(Obj_AI_Hero target)
        {
            if (YasuoWall == null || !GetBool("YasuoWall"))
                return true;
            else
            {
                var level = YasuoWall.Name.Substring(YasuoWall.Name.Length - 6, 1);
                var wallWidth = (300 + 50 * Convert.ToInt32(level));
                var wallDirection = (YasuoWall.Position.To2D() - YasuoWallCastedPos).Normalized().Perpendicular();
                var wallStart = YasuoWall.Position.To2D() + wallWidth / 2 * wallDirection;
                var wallEnd = wallStart - wallWidth * wallDirection;
                var intersection = Geometry.Intersection(wallStart, wallEnd, Player.Position.To2D(), target.Position.To2D());
                var intersections = new List<Vector2>();
                if (intersection.Point.IsValid() && Environment.TickCount + Game.Ping - WallCastT < 4000)
                    return false;
                else
                    return true;
            }
        }

        private static float GetDistance(AttackableUnit target)
        {
            return Vector3.Distance(Player.Position, target.Position);
        }

        private static bool CanIgnite()
        {
            return (IgniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready);
        }

        private static double GetComboDamage(Obj_AI_Base target)
        {
            double dmg = 0;
            if (Q.IsReady() && GetDistance(target) <= Q.Range)
                dmg += Player.GetSpellDamage(target, SpellSlot.Q) * 2;
            if (W.IsReady() && GetDistance(target) <= W.Range)
                dmg += Player.GetSpellDamage(target, SpellSlot.W);
            if (E.IsReady() && GetDistance(target) <= E.Range)
                dmg += Player.GetSpellDamage(target, SpellSlot.E);
            if (CanIgnite() && GetDistance(target) <= 600)
                dmg += Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            return dmg;
        }
    }
}
