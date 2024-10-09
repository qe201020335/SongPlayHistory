using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using SongPlayHistory.Configuration;
using SongPlayHistory.Model;
using SongPlayHistory.VoteTracker;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Object;

namespace SongPlayHistory
{
    [HarmonyPatch(typeof(LevelListTableCell), nameof(LevelListTableCell.SetDataFromLevelAsync))]
    internal class SetDataFromLevelAsync
    {
        private static Sprite? _thumbsUp;
        private static Sprite? _thumbsDown;

        private static Color _upColor = new Color(0.455f, 0.824f, 0.455f, 0.8f);
        private static Color _downColor = new Color(0.824f, 0.498f, 0.455f, 0.8f);
        public static bool Prepare()
        {
            if (Plugin.Instance.BeatSaverVotingInstalled) return false;  // let BeatSaverVoting do the job
            _thumbsUp ??= LoadSpriteFromResource(@"SongPlayHistory.Assets.ThumbsUp.png");
            _thumbsDown ??= LoadSpriteFromResource(@"SongPlayHistory.Assets.ThumbsDown.png");

            return _thumbsUp != null && _thumbsDown != null;
        }

        [HarmonyAfter("com.kyle1413.BeatSaber.SongCore")]
        public static void Postfix(LevelListTableCell __instance, BeatmapLevel? beatmapLevel, bool isFavorite, 
            Image ____favoritesBadgeImage, TextMeshProUGUI? ____songBpmText)
        {
            if (!PluginConfig.Instance.ShowVotes) return;
            if (beatmapLevel == null) return;
            if (____songBpmText != null)
            {
                if (float.TryParse(____songBpmText.text, out float bpm))
                {
                    ____songBpmText.text = bpm.ToString("0");
                }
            }

            Image? voteIcon = null;
            foreach (var image in __instance.GetComponentsInChildren<Image>())
            {
                // For performance reason, avoid using Linq.
                if (image.name == "Vote")
                {
                    voteIcon = image;
                    break;
                }
            }
            if (voteIcon == null)
            {
                voteIcon = Instantiate(____favoritesBadgeImage, __instance.transform);
                voteIcon.name = "Vote";
                voteIcon.rectTransform.sizeDelta = new Vector2(2.5f, 2.5f);
            }

            if (!isFavorite && InMenuVoteTrackingHelper.Instance?.TryGetVote(beatmapLevel, out var vote) == true)
            {
                if (vote == VoteType.Upvote)
                {
                    voteIcon.sprite = _thumbsUp;
                    voteIcon.color = _upColor;
                }
                else
                {
                    voteIcon.sprite = _thumbsDown;
                    voteIcon.color = _downColor;
                }
                
                voteIcon.enabled = true;
            }
            else
            {
                voteIcon.enabled = false;
            }
        }

        public static void OnUnpatch()
        {
            foreach (var image in Resources.FindObjectsOfTypeAll<Image>())
            {
                if (image.name == "Vote")
                {
                    Destroy(image.gameObject);
                }
            }
        }

        private static Sprite? LoadSpriteFromResource(string resourcePath)
        {
            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath);
                using var ms = new MemoryStream();
                stream!.CopyTo(ms);
                
                var texture = new Texture2D(2, 2);
                texture.LoadImage(ms.ToArray());

                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
                return sprite;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error("Error while loading a resource.\n" + ex.ToString());
                return null;
            }
        }
    }
}
