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
    
    // [ARENA DATA]
    public int elo = 0;
    public int wins = 0;
    public int losses = 0;
    public int totalGames = 0;
    public int rank = 0;
    public string displayName = "";

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
public class CardSnapshot
{
    public int id;
    public int star;
    public int top;
    public int right;
    public int bottom;
    public int left;
}

[Serializable]
public class DeckSaveData
{
    public CardSnapshot[] snapshots = new CardSnapshot[PlayFabConstants.MAX_DECK_SIZE];
}

[Serializable]
public class PublicProfileSaveData
{
    public string displayName;
    public int level;
    public long exp;
    public string avatarId;
    public string frameId;
    public int totalPower;
    public int arenaRank; // Hiện hạng cho người khác soi
    public DeckSaveData deck;
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
