// Real configuration persistence and preset UI classes; Unity/BSML are explicit doubles.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using AlternativePlay.Models;
using AlternativePlay.UI;
using BeatSaberMarkupLanguage.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityEngine
{
    public class MonoBehaviour { }
    public static class Application { public static string dataPath; }
    public static class GUIUtility
    {
        public static int Reads;
        private static string clipboard;
        public static string systemCopyBuffer { get { Reads++; return clipboard; } set { clipboard = value; } }
    }
}
namespace Zenject { public class InjectAttribute : Attribute { } }
namespace BeatSaberMarkupLanguage.Attributes
{
    public class HotReloadAttribute : Attribute { }
    public class UIValueAttribute : Attribute { public UIValueAttribute(string name) { } }
    public class UIActionAttribute : Attribute { public UIActionAttribute(string name) { } }
    public class UIComponentAttribute : Attribute { public UIComponentAttribute(string name) { } }
}
namespace BeatSaberMarkupLanguage.ViewControllers
{
    public class BSMLAutomaticViewController
    {
        protected virtual void DidActivate(bool first, bool added, bool enabled) { }
    }
}
namespace HMUI
{
    public class TableView
    {
        public enum ScrollPositionType { End, Center }
        public int Reloads, Selected;
        public void ClearSelection() { }
        public void ReloadData() { Reloads++; }
        public void SelectCellWithIdx(int index) { Selected = index; }
        public void ScrollToCellWithIdx(int index, ScrollPositionType type, bool animated) { }
    }
    public class ModalView
    {
        public bool Visible;
        public void Show(bool animated) { Visible = true; }
        public void Hide(bool animated) { Visible = false; }
    }
}
namespace BeatSaberMarkupLanguage.Components
{
    public class CustomCellListTableData
    {
        public HMUI.TableView TableView = new HMUI.TableView();
        public List<object> Data = new List<object>();
    }
    public class KEYBOARD
    {
        public class KEY { }
        public class Text { public string text; public bool richText = true; }
        public Text KeyboardText = new Text();
        public string AddedLayout;
        public int AddCalls;
        public Dictionary<string, Action<KEY>> Actions = new Dictionary<string, Action<KEY>>();
        public KEYBOARD AddKeys(string layout) { AddedLayout = layout; AddCalls++; return this; }
        public void SetAction(string key, Action<KEY> action) { Actions[key] = action; }
    }
    public class ModalKeyboard
    {
        public HMUI.ModalView ModalView = new HMUI.ModalView();
        public KEYBOARD Keyboard = new KEYBOARD();
        public void SetText(string text) { Keyboard.KeyboardText.text = text; }
    }
}
namespace BeatSaberMarkupLanguage.GameplaySetup
{
    public enum MenuType { All }
    public class GameplaySetup
    {
        public static GameplaySetup Instance = new GameplaySetup();
        public void AddTab(string name, string resource, object owner, MenuType menu) { }
    }
}
namespace AlternativePlay.Models
{
    public class TrackerConfigData
    {
        public string Serial { get; set; }
        public static TrackerConfigData Clone(TrackerConfigData from) => new TrackerConfigData { Serial = from.Serial };
    }
}
namespace AlternativePlay.UI
{
    public class AlternativePlayMainFlowCoordinator
    {
        public PlayModeSettings Last; public int Index;
        public void ShowPlayModeSelect(PlayModeSettings settings, int index) { Last = settings; Index = index; }
    }
}

public static class PresetNameChecks
{
    private static int checks;
    private static void Check(bool ok, string description) { if (!ok) throw new Exception(description); checks++; }
    private static void Set(object obj, string field, object value) => obj.GetType().GetField(field,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);
    private static void Call(object obj, string method) => obj.GetType().GetMethod(method,
        BindingFlags.NonPublic | BindingFlags.Instance).Invoke(obj, null);
    private static string Gameplay(Configuration configuration)
    {
        var json = JObject.FromObject(configuration.ConfigurationData);
        foreach (var settings in json["PlayModeSettings"].Children<JObject>()) settings.Remove("PresetName");
        return json.ToString(Formatting.None);
    }
    public static string Run(string directory)
    {
        UnityEngine.Application.dataPath = Path.Combine(directory, "Beat Saber_Data");
        Directory.CreateDirectory(UnityEngine.Application.dataPath);
        Directory.CreateDirectory(Path.Combine(directory, "UserData"));
        var config = new Configuration();
        File.WriteAllText(Configuration.configurationFile,
            "{\"Selected\":1,\"FutureField\":true,\"PlayModeSettings\":[{\"PlayMode\":4,\"LeftFlailLength\":50},{\"PlayMode\":1,\"UseLeft\":true,\"ReverseMaulDirection\":true},{\"PlayMode\":3,\"ReverseNunchaku\":true,\"NunchakuLength\":10}]}");
        config.LoadConfiguration();
        Check(config.ConfigurationData.PlayModeSettings.Count == 3 && config.SelectedIndex == 1, "Old JSON preserves count/order/selection despite unknown field");
        var first = config.GetPlayModeSetting(0); var second = config.GetPlayModeSetting(1); var third = config.GetPlayModeSetting(2);
        Check(first.GetPresetDisplayName(0) == "Preset 1" && second.GetPresetDisplayName(1) == "Preset 2", "Missing names fall back by index");
        foreach (var blank in new[] { null, "", " \r\n\t", "\u2028\u2029" })
        {
            Check(config.RenamePlayModeSetting(0, first, blank) && first.PresetName == null && first.GetPresetDisplayName(0) == "Preset 1", "Blank/reset names use fallback");
        }
        // Rename must preserve every non-name serialized value, including selection.
        second.ReverseLeftSaber = true;
        string before = Gameplay(config);
        foreach (var name in new[] { "Flail 50cm", "右手ダスモ", "テスト", "Preset_日本語", "é 🥋 e\u0301", "<size=99>literal</size>" })
        {
            Check(config.RenamePlayModeSetting(0, first, name) && first.PresetName == name, "Rename supports literal Unicode and spaces");
            Check(Gameplay(config) == before, "Rename changes no gameplay setting, selection, tracker, or other preset");
            var roundtrip = JsonConvert.DeserializeObject<ConfigurationData>(File.ReadAllText(Configuration.configurationFile));
            Check(roundtrip.PlayModeSettings[0].PresetName == name && roundtrip.PlayModeSettings[1].ReverseLeftSaber,
                "Real file JSON roundtrip preserves name and preserves unrelated settings");
        }
        Check(PlayModeSettings.NormalizePresetName(" A\r\nB\tC\u2028D\u2029E\u0085F ") == "A  B C D E F", "Every line/control separator becomes a space");
        foreach (var unit in new[] { "a", "あ", "🥋", "e\u0301" })
        {
            string longName = string.Concat(Enumerable.Repeat(unit, 40));
            string expected = string.Concat(Enumerable.Repeat(unit, 32));
            Check(PlayModeSettings.NormalizePresetName(longName) == expected, "32 text elements, not bytes or half a surrogate/combining sequence");
            Check(new StringInfo(PlayModeSettings.NormalizePresetName(longName)).LengthInTextElements == 32, "Unicode length bound");
        }
        foreach(var value in new[]{"A",new string('あ',32)})
            Check(PlayModeSettings.NormalizePresetName(value)==value,"One-character/exact maximum names retained");
        var previousName=first.PresetName;
        var persisted=File.ReadAllText(Configuration.configurationFile);
        bool failed=false;
        using(var locked=new FileStream(Configuration.configurationFile,FileMode.Open,FileAccess.Read,FileShare.None))
        {
            try { config.RenamePlayModeSetting(0,first,"Cannot save"); }
            catch(IOException) { failed=true; }
        }
        Check(failed&&first.PresetName==previousName&&File.ReadAllText(Configuration.configurationFile)==persisted,
            "Save failure rolls back name and leaves persisted data unchanged");
        config.RenamePlayModeSetting(0, first, "Same"); config.RenamePlayModeSetting(2, third, "Same");
        Check(first.PresetName == third.PresetName && first.PlayMode != third.PlayMode, "Duplicate names do not merge presets");
        second.ReverseLeftSaber = false; // Restore fixture to the ordinary valid configuration.
        first.LeftFlailLength = 80; config.SaveConfiguration();
        var loaded = new Configuration(); loaded.LoadConfiguration();
        Check(loaded.GetPlayModeSetting(0).PresetName == "Same" && loaded.GetPlayModeSetting(0).LeftFlailLength == 80,
            "Existing settings save and game restart preserve custom names");
        Check(loaded.GetPlayModeSetting(1).UseLeft && loaded.GetPlayModeSetting(1).ReverseMaulDirection,
            "Existing Maul settings retain their values");
        Check(loaded.GetPlayModeSetting(2).ReverseNunchaku && loaded.GetPlayModeSetting(2).NunchakuLength == 10,
            "Existing Nunchaku settings survive metadata roundtrip");

        var view = new AlternativePlayView(); var tab = new PlayModeSelectTab();
        var list = new CustomCellListTableData(); var selector = new CustomCellListTableData();
        var flow = new AlternativePlayMainFlowCoordinator();
        Set(view, "configuration", config); Set(view, "mainFlowCoordinator", flow); Set(view, "SelectModeList", list);
        view.RenameKeyboard = new ModalKeyboard(); view.DeleteModal = new HMUI.ModalView();
        Set(tab, "configuration", config); Set(tab, "SelectModeList", selector); Call(tab, "Start");
        view.RefreshConfigurations(); tab.UpdatePlayModeSelectList();
        before = Gameplay(config);
        ((PlayModeSelectOption)list.Data[1]).OnRenameClicked();
        Check(view.RenameKeyboard.ModalView.Visible && UnityEngine.GUIUtility.Reads == 0, "Rename opens modal without reading clipboard or changing settings");
        foreach (var text in new[] { "A🥋", "Ae\u0301", "Aあ" })
        {
            view.RenameKeyboard.SetText(text); view.RenameKeyboard.Keyboard.Actions["⬅"](null);
            Check(view.RenameKeyboard.Keyboard.KeyboardText.text == "A", "Backspace removes a Unicode text element without splitting it");
        }
        UnityEngine.GUIUtility.systemCopyBuffer = "右手ダスモ\r\nONE";
        view.RenameKeyboard.Keyboard.Actions["PASTE"](null);
        Check(UnityEngine.GUIUtility.Reads == 1 && view.RenameKeyboard.Keyboard.KeyboardText.text == "右手ダスモ  ONE", "Explicit Paste accepts/sanitizes Unicode");
        Check(Gameplay(config) == before && second.PresetName == null, "Editing modal text does not save anything");
        view.OnRenameConfirmed(view.RenameKeyboard.Keyboard.KeyboardText.text);
        Check(second.PresetName == "右手ダスモ  ONE" && Gameplay(config) == before, "ENTER commits only the target name");
        Check(((PlayModeSelectOption)list.Data[1]).DisplayName == "2: 右手ダスモ  ONE"
            && ((PlayModeSelectOption)selector.Data[1]).DisplayName == "2: 右手ダスモ  ONE", "Both edit list and play selector update immediately");
        ((PlayModeSelectOption)list.Data[1]).OnRenameClicked();
        view.RenameKeyboard.Keyboard.Actions["CANCEL"](null); view.OnRenameConfirmed("must not save");
        Check(second.PresetName == "右手ダスモ  ONE" && view.RenameKeyboard.Keyboard.AddCalls == 1, "Cancel discards target; repeated opens do not duplicate keyboard buttons");
        view.OnModeClicked(null, (PlayModeSelectOption)list.Data[2]);
        Check(flow.Last == third && flow.Index == 2, "Edit uses index/object, never duplicate display name");
        tab.OnModeClicked(null, (PlayModeSelectOption)selector.Data[2]);
        Check(config.Current == third && config.SelectedIndex == 2, "Load uses duplicate-name preset's index");
        ((PlayModeSelectOption)list.Data[0]).OnRenameClicked();
        config.DeletePlayModeSetting(0);
        view.OnRenameConfirmed("wrong shifted preset");
        Check(config.GetPlayModeSetting(0) == second && second.PresetName == "右手ダスモ  ONE", "Stale rename index after deletion cannot affect the shifted preset");
        Check(config.GetPlayModeSetting(1) == third && third.PresetName == "Same", "Delete keeps each remaining name with its own settings object");
        config.DeletePlayModeSetting(1); config.DeletePlayModeSetting(0);
        Check(config.ConfigurationData.PlayModeSettings.Count == 1 && config.Current.PresetName == null
            && config.Current.GetPresetDisplayName(0) == "Preset 1", "Deleting final preset resets contents and name together");
        config.AddPlayModeSetting();
        Check(config.GetPlayModeSetting(1).PresetName == null && config.GetPlayModeSetting(1).GetPresetDisplayName(1) == "Preset 2", "Add creates unnamed independent preset");
        Check(!config.RenamePlayModeSetting(-1, null, "bad") && !config.RenamePlayModeSetting(99, first, "bad"), "Invalid rename indexes ignored");
        int reloads = selector.TableView.Reloads; Call(tab, "OnDestroy");
        config.RenamePlayModeSetting(0, config.Current, "After close");
        Check(selector.TableView.Reloads == reloads, "Scene teardown removes rename event subscription");
        // The old converter remains unchanged and creates optional, unnamed metadata.
        var old075 = new ConfigurationData075 { PlayMode = PlayMode075.Nunchaku, NunchakuLength = 20 };
        Check(old075.ToConfigurationData().PresetName == null && old075.ToConfigurationData().NunchakuLength == 20, "0.7.5 conversion retains mode data and defaults optional name");
        return checks + " real preset configuration/file/JSON/UI routing checks passed (Unity/BSML doubles; no game UserData accessed).";
    }
}
