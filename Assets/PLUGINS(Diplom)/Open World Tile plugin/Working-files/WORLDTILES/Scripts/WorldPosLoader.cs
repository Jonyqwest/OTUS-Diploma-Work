using UnityEngine;

public static class WorldPosLoader
{
    private const string SaveKey = "LastWorldPosition";

    public static void Save(WorldPosition pos)
    {
        string saveData = pos.ToSaveString();
        PlayerPrefs.SetString(SaveKey, saveData);
        PlayerPrefs.Save();
        Debug.Log($"[Save] WorldPosition saved: {saveData}");
    }

    public static bool TryLoad(out WorldPosition pos)
    {
        if (PlayerPrefs.HasKey(SaveKey))
        {
            string data = PlayerPrefs.GetString(SaveKey);
            pos = WorldPosition.FromSaveString(data);
            Debug.Log($"[Load] WorldPosition loaded: {data}");
            return true;
        }

        pos = default;
        return false;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(SaveKey);
    }
}
