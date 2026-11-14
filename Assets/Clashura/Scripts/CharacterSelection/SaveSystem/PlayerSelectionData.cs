using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clashura.CharacterSelection.SaveSystem
{
    /// <summary>
    /// Persistent data structure representing a player's selected team of characters.
    /// Modular and decoupled for future local or network multiplayer.
    /// </summary>
    [Serializable]
    public class PlayerSelectionData
    {
        [SerializeField] private string playerId = "Player_Local";
        [SerializeField] private List<string> selectedCharacterIds = new();
        [SerializeField] private long saveTimestamp;

        public string PlayerId
        {
            get => playerId;
            set => playerId = value;
        }

        public List<string> SelectedCharacterIds
        {
            get => selectedCharacterIds;
            set => selectedCharacterIds = value ?? new List<string>();
        }

        public long SaveTimestamp
        {
            get => saveTimestamp;
            set => saveTimestamp = value;
        }

        public PlayerSelectionData()
        {
            playerId = "Player_Local";
            selectedCharacterIds = new List<string>();
            saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public PlayerSelectionData(string id, IEnumerable<string> characterIds)
        {
            playerId = string.IsNullOrWhiteSpace(id) ? "Player_Local" : id;
            selectedCharacterIds = characterIds != null ? new List<string>(characterIds) : new List<string>();
            saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
