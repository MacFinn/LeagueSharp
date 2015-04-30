using LeagueSharp;
using LeagueSharp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace First_Assembly
{
    class GameOptions
    {
        private static Menu Config;
        private static Orbwalking.Orbwalker Orbwalker;

        public Menu CreateMenu()
        {
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

            Game.PrintChat("Mac's TF Loaded");

            return (Config);
        }
    }
}
