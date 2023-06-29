using ChroMapper_LightModding.Export;
using ChroMapper_LightModding.Helpers;
using ChroMapper_LightModding.Models;
using Newtonsoft.Json;
using SFB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ChroMapper_LightModding.UI
{
    internal class SongInfoUI
    {
        private Plugin plugin;
        private FileHelper fileHelper;
        private Exporter exporter;

        private GameObject _menuButton;
        private GameObject _loadMenu;
        private GameObject _infoMenu;
        public bool enabled = false;

        public SongInfoUI(Plugin plugin, FileHelper fileHelper, Exporter exporter)
        {
            this.plugin = plugin;
            this.fileHelper = fileHelper;
            this.exporter = exporter;
        }

        public void Enable(Transform header, Transform save)
        {
            if (enabled) { return; }
            enabled = true;
            AddLoadMenu(save);
            AddInfoMenu(save);
            AddMenuButton(header);
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
                    _infoMenu.SetActive(!_infoMenu.activeSelf);
                }
            });
        }

        public void AddLoadMenu(Transform parent)
        {
            
            _loadMenu = new GameObject("Automodder Load Menu");
            _loadMenu.transform.parent = parent;
            _loadMenu.SetActive(false);

            UIHelper.AttachTransform(_loadMenu, 400, 50, 1, 10.5f, 0, 0, 1, 1);

            Image image = _loadMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.24f, 0.24f, 0.24f);

            UIHelper.AddLabel(_loadMenu.transform, "Automodder", "Automodder", new Vector2(0, -12));

            UIHelper.AddButton(_loadMenu.transform, "CreateAMFile", "Create new", new Vector2(-72, -31), () =>
            {
                fileHelper.MapsetReviewCreator();
                _loadMenu.SetActive(false);
                _infoMenu.SetActive(true);
            });
            UIHelper.AddButton(_loadMenu.transform, "AutoSelectAMFile", "Autoselect file", new Vector2(0, -31), () =>
            {
                if (fileHelper.MapsetReviewLoader())
                {
                    _loadMenu.SetActive(false);
                    _infoMenu.SetActive(true);
                } else
                {
                    UIHelper.AddErrorLabel(_loadMenu.transform, "AutoSelectNotFound", "None found!", new Vector2(0, -45));
                }
            }, 80);
            UIHelper.AddButton(_loadMenu.transform, "SelectAMFile", "Select file", new Vector2(72, -31), () =>
            {
                StandaloneFileBrowser.OpenFilePanelAsync("Open Review file", Directory.GetCurrentDirectory(), "lreview", false, fileHelper.OnSelectReviewFile);
                if (plugin.currentMapsetReview != null)
                {
                    _loadMenu.SetActive(false);
                    _infoMenu.SetActive(true);
                } else
                {
                    UIHelper.AddErrorLabel(_loadMenu.transform, "AutoSelectNotFound", "File not loaded!", new Vector2(72, -45));
                }
            });
        }

        public void AddInfoMenu(Transform parent)
        {
            _infoMenu = new GameObject("Automodder Info Menu");
            _infoMenu.transform.parent = parent;
            _infoMenu.SetActive(false);

            UIHelper.AttachTransform(_infoMenu, 400, 200, 1, 10.5f, 0, 0, 1, 1);

            Image image = _infoMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.24f, 0.24f, 0.24f);

            UIHelper.AddButton(_infoMenu.transform, "SaveAMFile", "Save File", new Vector2(-166, -18), () =>
            {
                fileHelper.MapsetReviewSaver();
            });

            UIHelper.AddButton(_infoMenu.transform, "DeleteAMFile", "Delete File", new Vector2(-104, -18), () =>
            {
                UIHelper.AddButton(_infoMenu.transform, "CancelDeleteAMFile", "Cancel", new Vector2(-104, -18), () =>
                {
                    GameObject gameObjectToDelete = GameObject.Find("CancelDeleteAMFile");
                    Object.Destroy(gameObjectToDelete);
                    gameObjectToDelete = GameObject.Find("ReallyDeleteAMFile");
                    Object.Destroy(gameObjectToDelete);
                });
                UIHelper.AddButton(_infoMenu.transform, "ReallyDeleteAMFile", "Confirm Delete", new Vector2(-104, -45), () =>
                {
                    fileHelper.MapsetReviewRemover();
                    GameObject gameObjectToDelete = GameObject.Find("CancelDeleteAMFile");
                    Object.Destroy(gameObjectToDelete);
                    gameObjectToDelete = GameObject.Find("ReallyDeleteAMFile");
                    Object.Destroy(gameObjectToDelete);
                    _infoMenu.SetActive(false);
                });
            });


            UIHelper.AddButton(_infoMenu.transform, "ExportAMDiscordSeverityOrder", "Export (Severity Ordered)", new Vector2(164, -18), () =>
            {
                exporter.ExportToDiscordMDByImportance(plugin.currentMapsetReview);
            }, 64, 25, 10);

            UIHelper.AddButton(_infoMenu.transform, "ExportAMDiscordBeatOrder", "Export (Beat Ordered)", new Vector2(100, -18), () =>
            {
                exporter.ExportToDiscordMDByBeats(plugin.currentMapsetReview);
            }, 64, 25, 10);
        }

    }
}
