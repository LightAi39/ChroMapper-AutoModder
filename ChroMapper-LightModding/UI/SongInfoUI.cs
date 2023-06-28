using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ChroMapper_LightModding.UI
{
    internal class SongInfoUI
    {
        private Plugin plugin;

        private GameObject _menuButton;
        private GameObject _loadMenu;
        private GameObject _infoMenu;
        public bool enabled = false;

        public SongInfoUI(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void Enable(Transform parent)
        {
            if (enabled) { return; }
            enabled = true;
            AddLoadMenu();
            AddInfoMenu();
            AddMenuButton(parent);
        }

        public void Disable()
        {
            if (!enabled) { return; }
            enabled = false;
        }

        public void AddMenuButton(Transform parent)
        {
            _menuButton = new GameObject("Automodder Menu Button");
            _menuButton.transform.parent = parent;

            UIHelper.AttachTransform(_menuButton, 23, 23, 0.752f, 0.90f, 0, 0, 1, 1);

            Image image = _menuButton.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.35f, 0.35f, 0.35f);


            UIHelper.AddImageButton(_menuButton.transform, "OpenAM", Plugin.LoadSprite("ChroMapper_LightModding.Assets.Icon.png"), new Vector2(0, -11.4f), () =>
            {
                if (plugin.currentMapsetReview == null)
                {
                    _loadMenu.SetActive(!_loadMenu.activeSelf);
                }
                else
                {
                    _infoMenu.SetActive(!_loadMenu.activeSelf);
                }
            });

            return;
            UIHelper.AddButton(_menuButton.transform, "OpenAM", "AutoMapper", new Vector2(-6, -11), () =>
            {
                if (plugin.currentMapsetReview == null)
                {
                    _loadMenu.SetActive(!_loadMenu.activeSelf);
                } else
                {
                    _infoMenu.SetActive(!_loadMenu.activeSelf);
                }
            });
        }

        public void AddLoadMenu()
        {
            _loadMenu = new GameObject("Automodder Load Menu");

            UIHelper.AttachTransform(_loadMenu, 300, 200, 1, 1, 0, 0, 1, 1);

            Image image = _loadMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.24f, 0.24f, 0.24f);
        }

        public void AddInfoMenu()
        {
            _infoMenu = new GameObject("Automodder Info Menu");

            UIHelper.AttachTransform(_infoMenu, 100, 100, 1, 1, 0, 0, 1, 1);

            Image image = _infoMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.24f, 0.24f, 0.24f);
        }

    }
}
