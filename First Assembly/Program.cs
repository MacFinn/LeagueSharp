﻿#region Using
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
        private static int EStacks;

        #endregion

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (Player.ChampionName != "TwistedFate") return;

            Q = new Spell(SpellSlot.Q, 1450);
            W = new Spell(SpellSlot.W, 525);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 5500);

            Q.SetSkillshot(0.5f, 60f, Q.Speed, false, SkillshotType.SkillshotLine);
            Q.MinHitChance = HitChance.Medium;

            IgniteSlot = Player.GetSpellSlot("SummonerDot");


            Config = new Menu("Mac's TF", "Mac's TF", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseAA", "Use Auto Attacks").SetValue(true));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("AutoPoke", "Auto Harass Q").SetValue(new KeyBind("P".ToCharArray()[0], KeyBindType.Toggle)));
            Config.SubMenu("Harass").AddItem(new MenuItem("ManaH", "Auto Harass if % MP >").SetValue(new Slider(30, 1, 100)));

            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("FQ", "Use Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("FW", "Use W").SetValue(true));

            Config.AddSubMenu(new Menu("KillSteal", "KillSteal"));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("KQ", "Use Q").SetValue(true));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("KW", "Use W").SetValue(true));

            Config.AddSubMenu(new Menu("Extra", "Extra"));
            Config.SubMenu("Extra").AddItem(new MenuItem("UseSeraphs", "Use Seraphs Embrace").SetValue(true));
            Config.SubMenu("Extra").AddItem(new MenuItem("HP", "SE when % HP <=").SetValue(new Slider(20, 100, 0)));

            Config.AddToMainMenu();


            Notifications.AddNotification("Mac's TF Loaded!", 5);
            
            
            

            Game.OnUpdate += Game_OnUpdate;
            Orbwalking.AfterAttack += OrbwalkingAfterAttack;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            //Draw Q Range
            if(Q.IsReady())
                Render.Circle.DrawCircle(Player.Position, Q.Range, Color.Aqua, 5);
            //Draw W Range
            if(W.IsReady())
                Render.Circle.DrawCircle(Player.Position, W.Range, Color.Aqua, 5);
            //Draw Ult Range
            if(R.IsReady())
                Render.Circle.DrawCircle(Player.Position, R.Range, Color.Aqua, 5);
            //Draw number of stacks on E
            //Drawing.DrawText(Drawing.WorldToScreen(Game.CursorPos)[0] - 50, Drawing.WorldToScreen(Game.CursorPos)[1] - 50, Color.AliceBlue, "E Stacks: " + EStacks);
        }

        private static void Game_OnUpdate(EventArgs args){

            if (Player.IsDead)
            {
                EStacks = 0;

            }
            if(Q.IsReady())
            {
                Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);


            }
            else if(W.IsReady())
            {
                Target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);

            }

            //if (GetActive("AutoPoke"))
            //{
            //    AutoPoke();
            

            Orbwalker.ActiveMode = Orbwalking.OrbwalkingMode.None;

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
            if (E.Level > 0)
            {
                Console.WriteLine("E READY");
                if (EStacks < 4)
                {
                    EStacks++;
                }
                else
                {
                    EStacks = 0;
                }

            }
            else
            {
                Console.WriteLine("E Not ready");
            }
        }

        private static void Freeze()
        {
            Killsteal();
            var allMinions = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (allMinions.Count < 1) return;
            foreach (var minion in allMinions)
            {
                if (Damage.GetAutoAttackDamage(Player, minion, false) > minion.Health && Orbwalker.InAutoAttackRange(minion))
                {
                    Orbwalker.ForceTarget(minion);
                }
            }
        }


        private static void Mixed()
        {
            
            Killsteal();
            if (CardSelector.Status == SelectStatus.Selected)
            {
                Orbwalker.ForceTarget(Target);
            }
            if (Target == null || !W.IsInRange(Target))
            {
                var allMinions = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                if (allMinions.Count < 1) return;
                foreach (var minion in allMinions)
                {
                    if (Damage.GetAutoAttackDamage(Player, Target, true) > minion.Health && Orbwalker.InAutoAttackRange(minion))
                    {
                        Orbwalker.ForceTarget(minion);
                    }
                }
            }
            else
            {
                if (IsInvul(Target))
                {
                    CardSelector.StartSelecting(Cards.Yellow);
                }
                if (Q.IsReady() && Player.Mana >= 160) {
                    Q.CastOnBestTarget(0f, false, true);
                }
                Orbwalker.ForceTarget(Target);
            }
        }

        private static void LaneClear()
        {
            Killsteal();

            Orbwalker.ActiveMode = Orbwalking.OrbwalkingMode.LaneClear;
            var allMinions = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            var lowHealtMinis = ObjectManager.Get<Obj_AI_Base>().Where(mini => mini.IsMinion && mini.IsEnemy && mini.Health < Q.GetDamage(mini));
            var bestPosition = Q.GetLineFarmLocation(lowHealtMinis.ToList()); //Get the location of the highest hit
            

            if (allMinions.Count < 1) return;
            
            foreach (var minion in allMinions)
            {
                if (CardSelector.Status == SelectStatus.Selected)
                {
                    Orbwalker.ForceTarget(minion);
                }
                if (Q.IsReady() && Player.Mana >= 160)
                {
                    if (bestPosition.Position.IsValid())
                    {
                        if (bestPosition.MinionsHit >= 2)
                        {
                            Q.Cast(bestPosition.Position, false);
                        }
                    }
                }
                else if(Player.Mana > 100 && W.IsInRange(minion) && W.IsReady())
                {
                    Console.WriteLine("Red card minion");
                    CardSelector.StartSelecting(Cards.Red);
                }
                else if (Player.Mana <= 100 && W.IsInRange(minion) && W.IsReady())
                {
                    Console.WriteLine("Blue card minion");
                    CardSelector.StartSelecting(Cards.Blue);
                }
                else
                {
                    Orbwalker.ForceTarget(minion);
                }
            }
        }

        private static void AutoPoke()
        {
            if (!GetBool("AutoPoke"))
            {
                Console.WriteLine("Poke disabled");
                return;
            }
            if (Target == null) return;

                
            if (Q.IsReady() && Q.IsInRange(Target) && Player.ManaPercent > 30)
            {
                Console.WriteLine("trying to poke");
                Q.CastOnBestTarget(0f, false, true);

            }
        }

        private static void Killsteal()
        {
            if (Target == null) return;
            if(CardSelector.Status == SelectStatus.Selected && W.IsKillable(Target)){
                Orbwalker.ForceTarget(Target);
            }
            if (Orbwalker.InAutoAttackRange(Target) && Target.Health <= Damage.GetAutoAttackDamage(Player, Target, true) && Orbwalker.InAutoAttackRange(Target))
            {
                Console.WriteLine("KS");
                Orbwalker.SetAttack(true);
            }
            else if (Q.IsKillable(Target) && Q.IsReady())
            {
                Console.WriteLine("KS");
                Q.SetSkillshot(Q.Delay, Q.Width, Q.Speed, false, SkillshotType.SkillshotLine, Q.From, Q.RangeCheckFrom);
                Q.CastOnBestTarget(0f, false, true);

            }
            else if (W.IsKillable(Target) && W.IsReady() && W.IsInRange(Target))
            {
                CardSelector.StartSelecting(Cards.Blue);
            }
        }

        private static void Combo()
        {
            if (Target == null || !detectCollision(Target))
            {
                Console.WriteLine("Noone in range");
                return;
            }

            Killsteal();

            if (CardSelector.Status == SelectStatus.Selected)
            {
                Orbwalker.ForceTarget(Target);
            }
            if (IsInvul(Target) && W.IsInRange(Target))
            {
                Console.WriteLine("use yellow");
                CardSelector.StartSelecting(Cards.Yellow);
            }
            if (GetBool("UseIgnite") && CanIgnite() && GetDistance(Target) <= 300 && GetComboDamage(Target) >= (double)Target.Health)
            {
                Player.Spellbook.CastSpell(IgniteSlot, Target);
            }

            if (Q.IsReady() && Q.CastIfHitchanceEquals(Target, Q.MinHitChance))
            {
                Console.WriteLine("throw Q");
                Q.CastOnBestTarget(0f, false, true;
            }
            else if (W.IsInRange(Target) && W.IsReady())
            {
                Console.WriteLine("use yellow");
                CardSelector.StartSelecting(Cards.Yellow);
            }
            else
            {
                Console.WriteLine("AA");

                Orbwalker.ForceTarget(Target);
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
