using System;
using System.Collections.Generic;


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

[Serializable]
public class PlayerStatsSaveData
{
    public int level = 1;
    public long exp = 0;
    public int elo = 0;
    public int wins = 0;
    public int losses = 0;
    public int totalGames = 0;
    public int rank = 0;
    public string displayName = "";
}

[Serializable]
public class PlayerInventorySaveData
{
    public List<ItemSaveData> inventory = new List<ItemSaveData>();
}

[Serializable]
public class PlayerCardListSaveData
{
    public List<CardSaveData> cards = new List<CardSaveData>();
}

