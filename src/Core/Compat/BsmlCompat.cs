using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.FloatingScreen;
using HMUI;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ScoreSaber.Core.Compat {
    internal static class BsmlCompat {
        internal static void OnMainMenuInitializing(Action callback) {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1
            callback();
#else
            BeatSaberMarkupLanguage.Util.MainMenuAwaiter.MainMenuInitializing += callback;
#endif
        }

        internal static byte[] GetResource(Assembly assembly, string resource) {
#pragma warning disable CS0618
            return Utilities.GetResource(assembly, resource);
#pragma warning restore CS0618
        }

        internal static CurvedTextMeshPro CreateText(RectTransform parent, string text, Vector2 anchoredPosition) {
#pragma warning disable CS0618
            return (CurvedTextMeshPro)BeatSaberUI.CreateText(parent, text, anchoredPosition);
#pragma warning restore CS0618
        }

        internal static Sprite LoadSpriteRaw(byte[] image) {
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(image);
            return Utilities.LoadSpriteFromTexture(texture);
        }

        internal static void DestroySprite(Sprite sprite) {
            if (sprite == null) {
                return;
            }

            Texture2D texture = sprite.texture;
            UnityEngine.Object.Destroy(sprite);
            if (texture != null) {
                UnityEngine.Object.Destroy(texture);
            }
        }

#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1
        internal static BSMLParser Parser => BSMLParser.instance;

        internal static Image GetBackground(this Backgroundable backgroundable) => backgroundable.background;

        internal static System.Collections.IList GetData(this CustomCellListTableData tableData) => tableData.data;

        internal static void SetData(this CustomCellListTableData tableData, System.Collections.IList data) {
            var list = new List<object>();
            foreach (object item in data) {
                list.Add(item);
            }

            tableData.data = list;
        }

        internal static TableView GetTableView(this CustomCellListTableData tableData) => tableData.tableView;

        internal static TextSegmentedControl GetTextSegmentedControl(this TabSelector tabSelector) => tabSelector.textSegmentedControl;

        internal static Component GetComponent(this BSMLParser.ComponentTypeWithData componentType) => componentType.component;

        internal static Dictionary<string, string> GetData(this BSMLParser.ComponentTypeWithData componentType) => componentType.data;

        internal static GameObject GetHandle(this FloatingScreen screen) => screen.handle;
#else
        internal static BSMLParser Parser => BSMLParser.Instance;

        internal static Image GetBackground(this Backgroundable backgroundable) => backgroundable.Background;

        internal static System.Collections.IList GetData(this CustomCellListTableData tableData) => tableData.Data;

        internal static void SetData(this CustomCellListTableData tableData, System.Collections.IList data) => tableData.Data = data;

        internal static TableView GetTableView(this CustomCellListTableData tableData) => tableData.TableView;

        internal static TextSegmentedControl GetTextSegmentedControl(this TabSelector tabSelector) => tabSelector.TextSegmentedControl;

        internal static Component GetComponent(this BSMLParser.ComponentTypeWithData componentType) => componentType.Component;

        internal static Dictionary<string, string> GetData(this BSMLParser.ComponentTypeWithData componentType) => componentType.Data;

        internal static GameObject GetHandle(this FloatingScreen screen) => screen.Handle;
#endif
    }
}
