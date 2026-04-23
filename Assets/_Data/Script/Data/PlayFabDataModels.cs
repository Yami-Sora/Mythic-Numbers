using System;
using System.Collections.Generic;

[Serializable]
public class PlayFabSaveData
{
    public List<ItemSaveData> inventory = new List<ItemSaveData>();
    public List<CardSaveData> cards = new List<CardSaveData>();
    // [PLAYER STATS]
    public int level = 1;
    public long exp = 0;

    // [DUNGEON DATA]
    public int goldDungeonStage = 1;
    public int lnDungeonStage = 1;
    public int gemMineStage = 1;
    public int goldDungeonEntries = 0;
    public int lnDungeonEntries = 0;
    public int gemMineEntries = 0;
    public string lastDungeonDate = ""; // Format: yyyy-MM-dd
}

[Serializable]
public class ItemSaveData 
{ 
    public string id; 
    public int amt; 
}

[Serializable]
public class CardSaveData
{
    public int id;
    public int lvl;
    public int star;
    public int shards;
    public string[] gems = new string[PlayFabConstants.MAX_GEMS];
}

[Serializable]
public class DeckSaveData
{
    public int[] deckCardIDs = new int[PlayFabConstants.MAX_DECK_SIZE] { -1, -1, -1, -1, -1 };
}

[Serializable]
public class DungeonSaveData
{
    public int goldStage;
    public int lnStage;
    public int gemMineStage;
    public int goldEntries;
    public int lnEntries;
    public int gemMineEntries;
    public string lastDate;
}
