namespace Tribulation.Config
{
    public static class GameConfigService
    {
        public static GameConfig Config => new()
        {
            selectedCharacterId = ConfigCenter.GetSelectedCharacter().id,
            selectedMapId = ConfigCenter.GetSelectedMap().id,
            language = ConfigCenter.Language,
            characters = ConfigCenter.Characters,
            enemies = ConfigCenter.Enemies,
            maps = ConfigCenter.Maps,
            level = ConfigCenter.Level,
            weapon = ConfigCenter.Weapon,
            pickup = ConfigCenter.Pickup,
            upgradeOptions = ConfigCenter.Upgrades
        };

        public static void Reload()
        {
            ConfigCenter.Reload();
        }
    }
}
