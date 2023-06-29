using ChroMapper_LightModding.BeatmapScanner.Data.Criteria;
using ChroMapper_LightModding.Export;
using ChroMapper_LightModding.Helpers;
using ChroMapper_LightModding.Models;
using Newtonsoft.Json;
using SFB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ChroMapper_LightModding.BeatmapScanner.Data.Criteria.InfoCrit;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;
using Transform = UnityEngine.Transform;

namespace ChroMapper_LightModding.UI
{
    internal class SongInfoUI
    {
        private Plugin plugin;
        private FileHelper fileHelper;
        private Exporter exporter;
        private AutocheckHelper autocheckHelper;

        private GameObject _menuButton;
        private GameObject _loadMenu;
        private GameObject _infoMenu;
        private GameObject _diffMenu;

        private Transform _header;
        private Transform _infoSave;
        private Transform _diffSave;
        public bool enabled = false;

        public SongInfoUI(Plugin plugin, FileHelper fileHelper, Exporter exporter, AutocheckHelper autocheckHelper)
        {
            this.plugin = plugin;
            this.fileHelper = fileHelper;
            this.exporter = exporter;
            this.autocheckHelper = autocheckHelper;
        }

        public void Enable(Transform header, Transform infoSave, Transform diffSave)
        {
            if (enabled) { return; }
            enabled = true;
            _header = header;
            _infoSave = infoSave;
            _diffSave = diffSave;
            AddLoadMenu(_infoSave);
            AddMenuButton(_header);
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
                    ToggleInfoMenu();
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
                ToggleInfoMenu(false);
            });
            UIHelper.AddButton(_loadMenu.transform, "AutoSelectAMFile", "Autoselect file", new Vector2(0, -31), () =>
            {
                if (fileHelper.MapsetReviewLoader())
                {
                    _loadMenu.SetActive(false);
                    ToggleInfoMenu(false);
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
                    ToggleInfoMenu(false);
                } else
                {
                    UIHelper.AddErrorLabel(_loadMenu.transform, "AutoSelectNotFound", "File not loaded!", new Vector2(72, -45));
                }
            });
        }

        public void ToggleInfoMenu(bool destroyIfExists = true)
        {
            GameObject infoMenu = GameObject.Find("Automodder Info Menu");
            if (infoMenu != null)
            {
                if (destroyIfExists) RemoveInfoMenu();
            } else
            {
                CreateInfoMenu();
            }
        }

        public void RefreshInfoMenu()
        {
            RemoveInfoMenu();
            CreateInfoMenu();
        }

        private void RemoveInfoMenu()
        {
            GameObject infoMenu = GameObject.Find("Automodder Info Menu");
            GameObject diffMenu = GameObject.Find("Automodder Difficulty Menu Overlay");
            Object.Destroy(infoMenu);
            Object.Destroy(diffMenu);
        }

        private void CreateInfoMenu()
        {
            fileHelper.CheckDifficultyReviewsExist();
            AddInfoMenu(_infoSave);
            AddDifficultyMenu(_diffSave);
            _infoMenu.SetActive(true);
            _diffMenu.SetActive(true);
        }

        public void AddInfoMenu(Transform parent)
        {
            if (plugin.currentMapsetReview != null) if (plugin.currentMapsetReview.Creator == "Pink") Debug.Log("Pink cute"); // you saw nothing
            _infoMenu = new GameObject("Automodder Info Menu");
            _infoMenu.transform.parent = parent;
            _infoMenu.SetActive(false);

            UIHelper.AttachTransform(_infoMenu, 400, 200, 1, 10.5f, 0, 0, 1, 1);

            Image image = _infoMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.24f, 0.24f, 0.24f);

            #region Top left buttons
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
                    GameObject gameObjectToDelete = GameObject.Find("Automodder Info Menu");
                    Object.Destroy(gameObjectToDelete);
                });
            });
            #endregion

            #region Top right buttons
            UIHelper.AddButton(_infoMenu.transform, "AutoCheckInfo", "Auto Check Song Info", new Vector2(36, -18), () =>
            {
                autocheckHelper.RunAutoCheckOnInfo();
                RefreshInfoMenu();
            }, 64, 25, 10);

            UIHelper.AddButton(_infoMenu.transform, "ExportAMDiscordBeatOrder", "Export (Beat Ordered)", new Vector2(100, -18), () =>
            {
                exporter.ExportToDiscordMDByBeats(plugin.currentMapsetReview);
            }, 64, 25, 10);

            UIHelper.AddButton(_infoMenu.transform, "ExportAMDiscordSeverityOrder", "Export (Severity Ordered)", new Vector2(164, -18), () =>
            {
                exporter.ExportToDiscordMDByImportance(plugin.currentMapsetReview);
            }, 64, 25, 10);

            UIHelper.AddButton(_infoMenu.transform, "ViewSongInfoComments", "View Song Info Comments", new Vector2(164, -45), () =>
            {
                ShowAllSongInfoCommentsMainUI();
            }, 64, 25, 10);
            #endregion

            #region Criteria
            InfoCrit criteria = plugin.currentMapsetReview.Criteria;
            float startPosY = -65, posY, offsetX = 10;
            string name;

            // this is extremely ugly. i tried minimizing the amount of manual changing i had to do but i ended up making this super messy because we still need the same values later when they are already changed.
            #region please collapse this
            posY = startPosY;
            name = "Song Name";
            UIHelper.AddLabel(_infoMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.SongName, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_infoMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.SongName = IncrementSeverity(criteria.SongName);
                posY = startPosY;
                offsetX = 10;
                name = "Song Name";
                CreateCriteriaStatusElement(criteria.SongName, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 1;
            name = "Song Sub Name";
            UIHelper.AddLabel(_infoMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.SubName, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_infoMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.SubName = IncrementSeverity(criteria.SubName);
                posY = startPosY - 26 * 1;
                offsetX = 10;
                name = "Song Sub Name";
                CreateCriteriaStatusElement(criteria.SubName, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 2;
            name = "Song Author";
            UIHelper.AddLabel(_infoMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.SongAuthor, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_infoMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.SongAuthor = IncrementSeverity(criteria.SongAuthor);
                posY = startPosY - 26 * 2;
                offsetX = 10;
                name = "Song Author";
                CreateCriteriaStatusElement(criteria.SongAuthor, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 3;
            name = "Creator";
            UIHelper.AddLabel(_infoMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Creator, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_infoMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Creator = IncrementSeverity(criteria.Creator);
                posY = startPosY - 26 * 3;
                offsetX = 10;
                name = "Creator";
                CreateCriteriaStatusElement(criteria.Creator, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 4;
            name = "Offset";
            UIHelper.AddLabel(_infoMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Offset, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_infoMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Offset = IncrementSeverity(criteria.Offset);
                posY = startPosY - 26 * 4;
                offsetX = 10;
                name = "Offset";
                CreateCriteriaStatusElement(criteria.Offset, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            // 1 column to the right now
            offsetX = 200;
            posY = startPosY - 26 * 1;
            name = "BPM";
            UIHelper.AddLabel(_infoMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.BPM, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_infoMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.BPM = IncrementSeverity(criteria.BPM);
                posY = startPosY - 26 * 1;
                offsetX = 200;
                name = "BPM";
                CreateCriteriaStatusElement(criteria.BPM, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 2;
            name = "Difficulty Ordering";
            UIHelper.AddLabel(_infoMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.DifficultyOrdering, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_infoMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.DifficultyOrdering = IncrementSeverity(criteria.DifficultyOrdering);
                posY = startPosY - 26 * 2;
                offsetX = 200;
                name = "Difficulty Ordering";
                CreateCriteriaStatusElement(criteria.DifficultyOrdering, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 3;
            name = "Requirements";
            UIHelper.AddLabel(_infoMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Requirement, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_infoMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Requirement = IncrementSeverity(criteria.Requirement);
                posY = startPosY - 26 * 3;
                offsetX = 200;
                name = "Requirements";
                CreateCriteriaStatusElement(criteria.Requirement, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 4;
            name = "Preview Time";
            UIHelper.AddLabel(_infoMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Preview, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_infoMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Preview = IncrementSeverity(criteria.Preview);
                posY = startPosY - 26 * 4;
                offsetX = 200;
                name = "Preview Time";
                CreateCriteriaStatusElement(criteria.Preview, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            #endregion
            #endregion

        }

        public void AddDifficultyMenu(Transform parent)
        {
            _diffMenu = new GameObject("Automodder Difficulty Menu Overlay");
            _diffMenu.transform.parent = parent;
            _diffMenu.SetActive(false);

            UIHelper.AttachTransform(_diffMenu, 400, 200, 1, 10.5f, 0, 0, 1, 1);

            //Image image = _diffMenu.AddComponent<Image>();
            //image.sprite = PersistentUI.Instance.Sprites.Background;
            //image.type = Image.Type.Sliced;
            //image.color = new Color(0.24f, 0.24f, 0.24f);

            #region Autocheck buttons
            List<DifficultyReview> reviews = plugin.currentMapsetReview.DifficultyReviews;

            // hard coded mess until i figure out how to get which tab the user is on.
            if (plugin.currentMapsetReview != null)
            {
                if (plugin.currentMapsetReview.DifficultyReviews.Any(x => x.DifficultyRank == 9 && x.DifficultyCharacteristic == "Standard"))
                {
                    Severity severity = reviews.Where(x => x.DifficultyRank == 9 && x.DifficultyCharacteristic == "Standard").FirstOrDefault().Critera.HighestSeverityCheck();
                    CreateCriteriaStatusElement(severity, "AutoCheckEx+Status", new Vector2(84, -75), _diffMenu.transform);
                    UIHelper.AddButton(_diffMenu.transform, "AutoCheckEx+", "Auto Check", new Vector2(116, -75), () =>
                    {
                        autocheckHelper.RunAutoCheckOnDiff("Standard", 9, "ExpertPlus");
                    }, 50, 25);
                }
                if (plugin.currentMapsetReview.DifficultyReviews.Any(x => x.DifficultyRank == 7 && x.DifficultyCharacteristic == "Standard"))
                {
                    Severity severity = reviews.Where(x => x.DifficultyRank == 7 && x.DifficultyCharacteristic == "Standard").FirstOrDefault().Critera.HighestSeverityCheck();
                    CreateCriteriaStatusElement(severity, "AutoCheckExStatus", new Vector2(84, -100.33f), _diffMenu.transform);
                    UIHelper.AddButton(_diffMenu.transform, "AutoCheckEx", "Auto Check", new Vector2(116, -100.33f), () =>
                    {
                        autocheckHelper.RunAutoCheckOnDiff("Standard", 7, "Expert");
                    }, 50, 25);
                }
                if (plugin.currentMapsetReview.DifficultyReviews.Any(x => x.DifficultyRank == 5 && x.DifficultyCharacteristic == "Standard"))
                {
                    Severity severity = reviews.Where(x => x.DifficultyRank == 5 && x.DifficultyCharacteristic == "Standard").FirstOrDefault().Critera.HighestSeverityCheck();
                    CreateCriteriaStatusElement(severity, "AutoCheckHStatus", new Vector2(84, -125.66f), _diffMenu.transform);
                    UIHelper.AddButton(_diffMenu.transform, "AutoCheckH", "Auto Check", new Vector2(116, -125.66f), () =>
                    {
                        autocheckHelper.RunAutoCheckOnDiff("Standard", 5, "Hard");
                    }, 50, 25);
                }
                if (plugin.currentMapsetReview.DifficultyReviews.Any(x => x.DifficultyRank == 3 && x.DifficultyCharacteristic == "Standard"))
                {
                    Severity severity = reviews.Where(x => x.DifficultyRank == 3 && x.DifficultyCharacteristic == "Standard").FirstOrDefault().Critera.HighestSeverityCheck();
                    CreateCriteriaStatusElement(severity, "AutoCheckNStatus", new Vector2(84, -151f), _diffMenu.transform);
                    UIHelper.AddButton(_diffMenu.transform, "AutoCheckN", "Auto Check", new Vector2(116, -151f), () =>
                    {
                        autocheckHelper.RunAutoCheckOnDiff("Standard", 3, "Normal");
                    }, 50, 25);
                }
                if (plugin.currentMapsetReview.DifficultyReviews.Any(x => x.DifficultyRank == 1 && x.DifficultyCharacteristic == "Standard"))
                {
                    Severity severity = reviews.Where(x => x.DifficultyRank == 1 && x.DifficultyCharacteristic == "Standard").FirstOrDefault().Critera.HighestSeverityCheck();
                    CreateCriteriaStatusElement(severity, "AutoCheckEStatus", new Vector2(84, -176.33f), _diffMenu.transform);
                    UIHelper.AddButton(_diffMenu.transform, "AutoCheckE", "Auto Check", new Vector2(116, -176.33f), () =>
                    {
                        autocheckHelper.RunAutoCheckOnDiff("Standard", 1, "Easy");
                    }, 50, 25);
                }
            }

            #endregion
        }

        public void ShowAllSongInfoCommentsMainUI()
        {
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("All Comments");
            List<Comment> comments = plugin.currentMapsetReview.Comments.Take(5).ToList();

            foreach (var comment in comments)
            {
                string read = "";
                if (comment.MarkAsSuppressed)
                {
                    read = " - Marked As Suppressed";
                }
                dialog.AddComponent<ButtonComponent>()
                    .WithLabel($"{comment.Type}: {comment.Message}{read}")
                    .OnClick(() => { ShowReviewCommentUI(comment.Id); });
            }

            if (plugin.currentMapsetReview.Comments.Count == 0)
            {
                dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"No comments found!");
            }

            dialog.AddFooterButton(ShowAllSongInfoCommentsMainUI, "<-");
            dialog.AddFooterButton(null, "Close");
            if (plugin.currentMapsetReview.Comments.Count > 5)
            {
                dialog.AddFooterButton(() =>
                {
                    ShowAllSongInfoCommentsMoreUI(5);
                }, "->");
            }
            else
            {
                dialog.AddFooterButton(ShowAllSongInfoCommentsMainUI, "->");
            }


            dialog.Open();
        }

        public void ShowAllSongInfoCommentsMoreUI(int startIndex)
        {
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("All Comments");
            int count = 5;
            bool lastTab = false;
            if (plugin.currentMapsetReview.Comments.Count < startIndex + count)
            {
                count = plugin.currentMapsetReview.Comments.Count - startIndex;
                lastTab = true;
            }
            List<Comment> comments = plugin.currentMapsetReview.Comments.GetRange(startIndex, count).ToList();

            foreach (var comment in comments)
            {
                string read = "";
                if (comment.MarkAsSuppressed)
                {
                    read = " - Marked As Suppressed";
                }
                dialog.AddComponent<ButtonComponent>()
                    .WithLabel($"{comment.Type}: {comment.Message}{read}")
                    .OnClick(() => { ShowReviewCommentUI(comment.Id); });
            }

            if (startIndex == 5)
            {
                dialog.AddFooterButton(ShowAllSongInfoCommentsMainUI, "<-");
            }
            else
            {
                dialog.AddFooterButton(() =>
                {
                    ShowAllSongInfoCommentsMoreUI(startIndex - 5);
                }, "<-");
            }

            dialog.AddFooterButton(null, "Close");
            if (lastTab)
            {
                dialog.AddFooterButton(() => ShowAllSongInfoCommentsMoreUI(startIndex), "->");
            }
            else
            {
                dialog.AddFooterButton(() =>
                {
                    ShowAllSongInfoCommentsMoreUI(startIndex + 5);
                }, "->");
            }

            dialog.Open();
        }

        public void ShowReviewCommentUI(string id)
        {
            Comment comment = plugin.currentMapsetReview.Comments.Where(x => x.Id == id).First();
            string message = comment.Response;
            bool read = comment.MarkAsSuppressed;

            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("View comment");

            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"Type: {comment.Type}");

            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"Comment: {comment.Message}");

            dialog.AddComponent<TextBoxComponent>()
                .WithLabel("Response:")
                .WithInitialValue(message)
                .OnChanged((string s) => { message = s; });

            dialog.AddComponent<ToggleComponent>()
                .WithLabel("Mark as suppressed")
                .WithInitialValue(read)
                .OnChanged((bool o) => { read = o; });

            dialog.AddFooterButton(null, "Close");
            dialog.AddFooterButton(() =>
            {
                comment.Response = message;
                comment.MarkAsSuppressed = read;
                plugin.HandleUpdateSongInfoComment(comment);
            }, "Update reply");

            dialog.Open();
        }

        private void CreateCriteriaStatusElement(Severity severity, string name, Vector2 pos, Transform parent = null)
        {
            if (parent == null) parent = _infoMenu.transform;
            GameObject critStatusObj = GameObject.Find($"Crit_{name}_status");
            if (critStatusObj != null) Object.Destroy(critStatusObj);

            Color color;
            switch (severity)
            {
                case Severity.Success:
                    color = Color.green;
                    break;
                case Severity.Warning:
                    color = Color.yellow;
                    break;
                case Severity.Fail:
                    color = Color.red;
                    break;
                default:
                    color = Color.gray;
                    break;
            }
            UIHelper.AddLabel(parent, $"Crit_{name}_status", "●", pos, new Vector2(25, 24), null, color, 12);
        }

        private Severity IncrementSeverity(Severity severity)
        {
            Severity[] enumValues = (Severity[])Enum.GetValues(typeof(Severity));
            int currentIndex = Array.IndexOf(enumValues, severity);
            int nextIndex = (currentIndex + 1) % enumValues.Length;
            return enumValues[nextIndex];
        }

    }
}
