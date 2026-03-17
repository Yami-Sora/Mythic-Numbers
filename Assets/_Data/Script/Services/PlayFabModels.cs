using System;
using System.Collections.Generic;

namespace PlayFabModels
{
    [Serializable]
    public class LoginWithCustomIdRequest
    {
        public string CustomId;
        public bool CreateAccount = true;
    }

    [Serializable]
    public class LoginResponse
    {
        public int code;
        public string status;
        public LoginData data;
    }

    [Serializable]
    public class LoginData
    {
        public string SessionTicket;
        public string PlayFabId;
        public bool NewlyCreated;
    }

    [Serializable]
    public class GetPlayerStatisticsRequest
    {
        public List<string> StatisticNames;
    }

    [Serializable]
    public class GetPlayerStatisticsResponse
    {
        public int code;
        public string status;
        public string error;
        public StatisticsData data;
    }

    [Serializable]
    public class StatisticsData
    {
        public List<PlayerStatistic> Statistics;
    }

    [Serializable]
    public class PlayerStatistic
    {
        public string StatisticName;
        public int Value;
        public int Version;
    }

    [Serializable]
    public class UpdatePlayerStatisticsRequest
    {
        public List<StatisticUpdate> Statistics;
    }

    [Serializable]
    public class StatisticUpdate
    {
        public string StatisticName;
        public int Value;
    }

    [Serializable]
    public class GetLeaderboardRequest
    {
        public string StatisticName = "elo";
        public int StartPosition = 0;
        public int MaxResultsCount = 10;
    }

    [Serializable]
    public class GetLeaderboardResponse
    {
        public int code;
        public string status;
        public string error;
        public LeaderboardData data;
    }

    [Serializable]
    public class LeaderboardData
    {
        public List<PlayerLeaderboardEntry> Leaderboard;
        public int Version;
    }

    [Serializable]
    public class PlayerLeaderboardEntry
    {
        public string PlayFabId;
        public string DisplayName;
        public int StatValue;
        public int Position;
        public string Profile;
    }
}
