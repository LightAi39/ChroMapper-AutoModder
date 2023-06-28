using Beatmap.Base;
using Beatmap.Enums;
using ChroMapper_LightModding.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChroMapper_LightModding.Helpers
{
    internal class OutlineHelper
    {
        private Plugin plugin;

        public HashSet<BaseObject> selectionCache;

        public OutlineHelper(Plugin plugin)
        {
            this.plugin = plugin;
        }
        public void UpdateSelectionCache(BaseObject baseObject)
        {
            selectionCache.Add(baseObject);
        }

        public void ManageSelectionCacheAndOutlines()
        {
            foreach (var item in selectionCache.ToList())
            {
                if (!SelectionController.SelectedObjects.Contains(item))
                {
                    selectionCache.Remove(item);
                    SetOutlineIfInReview(item);
                }
            }
        }

        public void SetOutlineIfInReview(BaseObject baseObject)
        {
            if (!plugin.showOutlines)
            {
                return;
            }

            SelectedObject spawnedObject = null;

            if (baseObject is BaseNote note)
            {
                spawnedObject = new()
                {
                    Beat = note.JsonTime,
                    PosX = note.PosX,
                    PosY = note.PosY,
                    ObjectType = note.ObjectType,
                    Color = note.Color
                };
            }

            if (baseObject is BaseObstacle wall)
            {
                spawnedObject = new()
                {
                    Beat = wall.JsonTime,
                    PosX = wall.PosX,
                    PosY = wall.PosY,
                    ObjectType = wall.ObjectType,
                    Color = 0
                };
            }

            if (baseObject is BaseSlider slider)
            {
                spawnedObject = new()
                {
                    Beat = slider.JsonTime,
                    PosX = slider.PosX,
                    PosY = slider.PosY,
                    ObjectType = slider.ObjectType,
                    Color = slider.Color
                };
            }

            if (baseObject is BaseBpmEvent bpm)
            {
                spawnedObject = new()
                {
                    Beat = bpm.JsonTime,
                    PosX = 0,
                    PosY = 0,
                    ObjectType = bpm.ObjectType,
                    Color = 0
                };
            }

            try
            {
                if (plugin.currentReview.Comments.Any(c => c.Objects.Any(o => JsonConvert.SerializeObject(o) == JsonConvert.SerializeObject(spawnedObject))))
                {
                    Comment comment = plugin.currentReview.Comments.Where(c => c.Objects.Any(o => JsonConvert.SerializeObject(o) == JsonConvert.SerializeObject(spawnedObject))).FirstOrDefault();
                    SelectedObject selectedObject = comment.Objects.Where(o => JsonConvert.SerializeObject(o) == JsonConvert.SerializeObject(spawnedObject)).FirstOrDefault();

                    if (comment.MarkAsSuppressed)
                    {
                        SetOutlineColor(selectedObject, Color.gray);
                    }
                    else
                    {
                        SetOutlineColor(selectedObject, ChooseOutlineColor(comment.Type));
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }

        }

        public Color ChooseOutlineColor(CommentTypesEnum type)
        {
            switch (type)
            {
                case CommentTypesEnum.Suggestion:
                    return Color.green;
                case CommentTypesEnum.Unsure:
                    return Color.yellow;
                case CommentTypesEnum.Issue:
                    return Color.red;
                default:
                    return Color.clear;
            }
        }

        public void SetOutlineColor(SelectedObject mapObject, Color color)
        {
            try
            {
                var collection = BeatmapObjectContainerCollection.GetCollectionForType(mapObject.ObjectType);

                if (mapObject.ObjectType == ObjectType.Note)
                {
                    var container = collection.LoadedContainers.Where((item) =>
                    {
                        if (item.Key is BaseNote note)
                        {
                            if (note.JsonTime == mapObject.Beat && note.PosX == mapObject.PosX && note.PosY == mapObject.PosY && note.Color == mapObject.Color)
                            {
                                return true;
                            }
                        }
                        return false;
                    }).First().Value;
                    container.SetOutlineColor(color);
                }
                else if (mapObject.ObjectType == ObjectType.Obstacle)
                {
                    var container = collection.LoadedContainers.Where((item) =>
                    {
                        if (item.Key is BaseGrid gridItem)
                        {
                            if (gridItem.JsonTime == mapObject.Beat && gridItem.PosX == mapObject.PosX && gridItem.PosY == mapObject.PosY)
                            {
                                return true;
                            }
                        }
                        return false;
                    }).First().Value;
                    container.SetOutlineColor(color);
                }
                else if (mapObject.ObjectType == ObjectType.Arc || mapObject.ObjectType == ObjectType.Chain)
                {
                    var container = collection.LoadedContainers.Where((item) =>
                    {
                        if (item.Key is BaseSlider slider)
                        {
                            if (slider.JsonTime == mapObject.Beat && slider.PosX == mapObject.PosX && slider.PosY == mapObject.PosY && slider.Color == mapObject.Color)
                            {
                                return true;
                            }
                        }
                        return false;
                    }).First().Value;
                    container.SetOutlineColor(color);
                }
                else if (mapObject.ObjectType == ObjectType.BpmChange)
                {
                    var container = collection.LoadedContainers.Where((item) =>
                    {
                        if (item.Key is BaseBpmEvent bpmEvent)
                        {
                            if (bpmEvent.JsonTime == mapObject.Beat)
                            {
                                return true;
                            }
                        }
                        return false;
                    }).First().Value;
                    container.SetOutlineColor(color);
                }
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message != "Sequence contains no elements")
                {
                    throw;
                }
                // dont need to do anything, objects just not inside the loaded range.
            }

        }

        public void SetOutlineColor(List<SelectedObject> mapObjects, Color color)
        {
            foreach (var mapObject in mapObjects)
            {
                SetOutlineColor(mapObject, color);
            }
        }

        public void ClearOutlineColor(SelectedObject mapObject)
        {
            SetOutlineColor(mapObject, Color.clear);
        }

        public void ClearOutlineColor(List<SelectedObject> mapObjects)
        {
            foreach (var mapObject in mapObjects)
            {
                ClearOutlineColor(mapObject);
            }
        }

        internal void RefreshOutlines()
        {
            plugin.NoteGridContainer.RefreshPool(true);
            plugin.ObstacleGridContainer.RefreshPool(true);
            plugin.EventGridContainer.RefreshPool(true);
            plugin.ArcGridContainer.RefreshPool(true);
            plugin.ChainGridContainer.RefreshPool(true);
            plugin.BPMChangeGridContainer.RefreshPool(true);
        }
    }
}
