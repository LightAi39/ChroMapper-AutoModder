using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ChroMapper_LightModding.Models;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

namespace ChroMapper_LightModding
{

    [Plugin("LightModding")]
    public class Plugin
    {
        static public BeatSaberSongContainer _beatSaberSongContainer = null!;
        private NoteGridContainer _noteGridContainer = null!;
        private ObstacleGridContainer _obstacleGridContainer = null!;

        private Scene currentScene;
        private bool inEditorScene;

        private DifficultyReview currentReview = new DifficultyReview();


        string text;

        [Init]
        private void Init()
        {
            SceneManager.sceneLoaded += SceneLoaded;

            // register a button in the side tab menu
            ExtensionButton button = ExtensionButtons.AddButton(LoadSprite("ChroMapper_LightModding.Assets.Icon.png"), "LightModding", ShowMainUI);

        }

        [Exit]
        private void Exit()
        {

        }

        #region Event Handlers

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            currentScene = scene;

            if (scene.buildIndex == 3) // the editor scene
            {
                inEditorScene = true;
                _noteGridContainer = UnityEngine.Object.FindObjectOfType<NoteGridContainer>();
                _obstacleGridContainer = UnityEngine.Object.FindObjectOfType<ObstacleGridContainer>();
                _beatSaberSongContainer = UnityEngine.Object.FindObjectOfType<BeatSaberSongContainer>();

                // check in the map folder for any existing review files, then either set it if it exists or create a new file

            }
            else
            {
                // save a backup of the review file
                inEditorScene = false;
                currentReview = new DifficultyReview();
            }
        }

        #endregion Event Handlers

        #region UI

        private void ShowCreateUI(float beat, int posX, int posY)
        {
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Add comment to note");
            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"Beat {beat} - row {posY} - lane {posX}");

            dialog.AddComponent<TextBoxComponent>()
                .WithLabel("Comment")
                .OnChanged((string s) => { text = s; });

            dialog.AddComponent<DropdownComponent>()
                .WithLabel("Type")
                .WithOptions<CommentTypesEnum>();

            dialog.AddFooterButton(null, "Cancel");
            dialog.AddFooterButton(HandleCreateComment, "Create");

            dialog.Open();
        }

        // soontm supported
        private void ShowCreateUIMultiple(float beat1, int posX1, int posY1, float beat2, int posX2, int posY2)
        {
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Add comment to multiple notes");
            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"First note at Beat {beat1} - row {posY1} - lane {posX1}");
            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"Last note at Beat {beat2} - row {posY2} - lane {posX2}");

            dialog.AddComponent<TextBoxComponent>()
                .WithLabel("Comment")
                .OnChanged((string s) => { text = s; });

            dialog.AddComponent<DropdownComponent>()
                .WithLabel("Type")
                .WithOptions<CommentTypesEnum>();

            dialog.AddFooterButton(null, "Cancel");
            dialog.AddFooterButton(HandleCreateComment, "Create");

            dialog.Open();
        }

        private void ShowMainUI()
        {
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Main UI");
            dialog.AddFooterButton(null, "Close");
            dialog.Open();
        }

        #endregion UI

        #region Comment Creation

        private void HandleCreateComment()
        {

        }

        #endregion Comment Creation

        #region Other

        public static Sprite LoadSprite(string asset) // taken from Moizac's Extended LightIDs code because i didn't want to figure it out myself
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(asset);
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);

            Texture2D texture2D = new Texture2D(256, 256);
            texture2D.LoadRawTextureData(data);
            texture2D.Apply();

            return Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0, 0), 100.0f);
        }

        #endregion Other
    }
}
