using SongPlayHistory.UI;
using Zenject;

namespace SongPlayHistory.Installers
{
    public class MenuInstaller: Installer<MenuInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<SPHUI>().AsSingle();
        }
    }
}