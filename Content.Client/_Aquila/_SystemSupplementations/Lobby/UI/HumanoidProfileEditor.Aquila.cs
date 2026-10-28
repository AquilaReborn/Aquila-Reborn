using Content.Client._Aquila.OocNotes;
using Content.Client.Stylesheets;
using Content.Goobstation.Common.CCVar;
using Content.Shared._Aquila.CCVars;
using Content.Shared.Aquila.Settings;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Lobby.UI
{
    public sealed partial class HumanoidProfileEditor
    {
        private OocNotesEditor? _oocNotes;

        private void InitializeAquilaBarks(IConfigurationManager configurationManager)
        {
            if (configurationManager.GetCVar(GoobCVars.BarksEnabled))
            {
                BarksContainer.Visible = true;
                InitializeBarkVoice();
            }
        }

        private List<AqSettingPrototype> FilterDepartmentsBySetting(List<DepartmentPrototype> departments)
        {
            var activeSetting = _cfgManager.GetCVar(AquilaCVars.Setting);
            var filterSetting = _prototypeManager.HasIndex<AqSettingPrototype>(activeSetting);
            departments.RemoveAll(department => filterSetting && department.Setting != activeSetting);

            var settings = new List<AqSettingPrototype>();
            foreach (var setting in _prototypeManager.EnumeratePrototypes<AqSettingPrototype>())
            {
                if (filterSetting && setting.ID != activeSetting)
                    continue;

                settings.Add(setting);
            }

            settings.Sort(SettingUIComparer.Instance);

            return settings;
        }

        private void AddSettingCategories(List<AqSettingPrototype> settings)
        {
            foreach (var setting in settings)
            {
                var settingName = Loc.GetString(setting.Name);

                if (!_settingCategories.TryGetValue(setting.ID, out var settingCategory))
                {
                    settingCategory = new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        Name = setting.ID
                    };

                    settingCategory.AddChild(new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#464966") },
                        Children =
                        {
                            new Label
                            {
                                Text = settingName,
                                Margin = new Thickness(0, 12, 0, 0),
                                FontColorOverride = Color.Red,
                                StyleClasses = { StyleClass.LabelHeading }
                            }
                        }
                    });

                    _settingCategories[setting.ID] = settingCategory;
                    JobList.AddChild(settingCategory);
                }
            }
        }

        private void RefreshOocNotes()
        {
            if (_oocNotes != null)
                return;

            _oocNotes = new OocNotesEditor();
            TabContainer.AddChild(_oocNotes);
            TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString("ooc-notes-editor-tab"));
            _oocNotes.OnNotesChanged += OnOocNotesChange;
        }

        private void OnOocNotesChange(string content)
        {
            if (Profile is null)
                return;

            Profile = Profile.WithOocNotes(content);
            SetDirty();
        }

        private void UpdateOocNotesEdit()
        {
            _oocNotes?.SetNotes(Profile?.OocNotes ?? "");
        }

        private void OnSettingChanged(string setting)
        {
            RefreshJobs();
        }
    }
}
