namespace Clashura.CharacterSelection.SaveSystem
{
    /// <summary>
    /// Contract for saving and loading player character selections.
    /// Allows easy swapping between local storage, PlayerPrefs, or future network backends.
    /// </summary>
    public interface ISelectionSaveSystem
    {
        void SaveSelection(PlayerSelectionData data);
        PlayerSelectionData LoadSelection(string playerId);
        bool HasSavedSelection(string playerId);
        void DeleteSelection(string playerId);
    }
}
