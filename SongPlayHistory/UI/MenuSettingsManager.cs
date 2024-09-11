using BeatSaberMarkupLanguage.Settings;
using Zenject;

namespace SongPlayHistory.UI;

internal class MenuSettingsManager: IInitializable
{
    private readonly SettingsController _settingsController;
    private readonly BSMLSettings _bsmlSettings;
    
    public MenuSettingsManager(SettingsController settingsController, BSMLSettings bsmlSettings)
    {
        _settingsController = settingsController;
        _bsmlSettings = bsmlSettings;
    }
    
    public void Initialize()
    {
        _bsmlSettings.AddSettingsMenu("Song Play History", "SongPlayHistory.UI.Settings.bsml", _settingsController);
    }
}