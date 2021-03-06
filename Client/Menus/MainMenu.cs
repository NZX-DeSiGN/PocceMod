﻿using CitizenFX.Core;
using MenuAPI;
using PocceMod.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    public class MainMenu : Menu
    {
        private static readonly int MenuKey;
        private static readonly int GamepadMenuKey;
        private readonly Dictionary<Type, Menu> _submenus = new Dictionary<Type, Menu>();
        private readonly Dictionary<int, Func<Task>> _menuItemActions = new Dictionary<int, Func<Task>>();
        private readonly Dictionary<int, List<Func<Task>>> _menuListItemActions = new Dictionary<int, List<Func<Task>>>();

        static MainMenu()
        {
            MenuKey = Config.GetConfigInt("MenuKey");
            if (MenuKey <= 0)
            {
                Common.Notification("No PocceMod menu key configured", true, true);
                MenuKey = -1;
            }

            GamepadMenuKey = Config.GetConfigInt("GamepadMenuKey");

            MenuController.MenuToggleKey = (Control)MenuKey;
            MenuController.ControllerMenuToggleKey = (Control)GamepadMenuKey;
            MenuController.EnableMenuToggleKeyOnController = GamepadMenuKey > 0;
            MenuController.DontOpenAnyMenu = true;
            MenuController.MenuAlignment = Config.GetConfigBool("MenuRightAlign") ? MenuController.MenuAlignmentOption.Right : MenuController.MenuAlignmentOption.Left;
        }

        public MainMenu() : base("PocceMod", "menu")
        {
            MenuController.AddMenu(this);

            var submenus = Assembly.GetExecutingAssembly().GetExportedTypes().Where(type => type.IsDefined(typeof(MainMenuIncludeAttribute), false));
            foreach (var submenu in submenus)
            {
                var instance = (Menu)Activator.CreateInstance(submenu);
                _submenus.Add(submenu, instance);
                MenuController.AddSubmenu(this, instance);
            }

            OnItemSelect += async (menu, item, index) =>
            {
                if (_menuItemActions.TryGetValue(index, out Func<Task> action))
                    await action();
            };

            OnListItemSelect += async (menu, listItem, listIndex, itemIndex) =>
            {
                if (_menuListItemActions.TryGetValue(itemIndex, out List<Func<Task>> actions))
                    await actions[listIndex]();
            };
        }

        public static bool IsOpen
        {
            get { return MenuController.IsAnyMenuOpen(); }
        }

        public T Submenu<T>() where T : Menu
        {
            _submenus.TryGetValue(typeof(T), out Menu value);
            return (T)value;
        }

        public void AddMenuItemAsync(string item, Func<Task> onSelect)
        {
            MenuController.DontOpenAnyMenu = false;

            var menuItem = new MenuItem(item);
            AddMenuItem(menuItem);
            _menuItemActions.Add(menuItem.Index, onSelect);
        }

        public void AddMenuItem(string item, Action onSelect)
        {
            AddMenuItemAsync(item, () => { onSelect(); return Task.FromResult(0); });
        }

        public void AddMenuListItemAsync(string item, string subitem, Func<Task> onSelect)
        {
            MenuController.DontOpenAnyMenu = false;

            foreach (var menuItem in GetMenuItems())
            {
                if (menuItem is MenuListItem && menuItem.Text == item)
                {
                    var subitems = ((MenuListItem)menuItem).ListItems;
                    subitems.Add(subitem);
                    _menuListItemActions[menuItem.Index].Add(onSelect);
                    return;
                }
            }

            var menuListItem = new MenuListItem(item, new List<string> { subitem }, 0);
            AddMenuItem(menuListItem);
            _menuListItemActions.Add(menuListItem.Index, new List<Func<Task>> { onSelect });
        }

        public void AddMenuListItem(string item, string subitem, Action onSelect)
        {
            AddMenuListItemAsync(item, subitem, () => { onSelect(); return Task.FromResult(0); });
        }

    }
}
