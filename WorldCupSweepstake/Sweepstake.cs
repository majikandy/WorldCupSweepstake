using Stratis.SmartContracts;

/// <summary>
/// DISCLAIMER: This contract is an experimental bit of fun for the world cup and NOT reference code. 
/// It is not guaranteed to work and is my own work, not the work of the company.
/// </summary>
public class Sweepstake : SmartContract
{
    private const ulong SatoshiMuliplier = 100000000;

    private Address Owner
    {
        get
        {
            return PersistentState.GetAddress("Owner");
        }
        set
        {
            PersistentState.SetAddress("Owner", value);
        }
    }

    private ISmartContractList<Address> Players
    {
        get { return PersistentState.GetAddressList("Players"); }
    }

    private string PlayersCsv
    {
        get
        {
            return PersistentState.GetString("PlayersCsv");
        }
        set
        {
            PersistentState.SetString("PlayersCsv", value);
        }
    }

    private string PlayersNickNames
    {
        get
        {
            return PersistentState.GetString("PlayersNickNames");
        }
        set
        {
            PersistentState.SetString("PlayersNickNames", value);
        }
    }

    private ISmartContractList<string> AssignedTeams
    {
        get { return PersistentState.GetStringList("AssignedTeams"); }
    }

    private string AssignedTeamsCsv
    {
        get
        {
            return PersistentState.GetString("AssignedTeamsCsv");
        }
        set
        {
            PersistentState.SetString("AssignedTeamsCsv", value);
        }
    }

    private string Result
    {
        get
        {
            return PersistentState.GetString("Result");
        }
        set
        {
            PersistentState.SetString("Result", value);
        }
    }

    private ulong EntryFeeSatoshis
    {
        get
        {
            return PersistentState.GetUInt64("EntryFeeSatoshis");
        }
        set
        {
            PersistentState.SetUInt64("EntryFeeSatoshis", value);
        }
    }

    private ulong FirstPrizeSatoshis
    {
        get
        {
            return PersistentState.GetUInt64("FirstPrizeSatoshis");
        }
        set
        {
            PersistentState.SetUInt64("FirstPrizeSatoshis", value);
        }
    }

    private ulong SecondPrizeSatoshis
    {
        get
        {
            return PersistentState.GetUInt64("SecondPrizeSatoshis");
        }
        set
        {
            PersistentState.SetUInt64("SecondPrizeSatoshis", value);
        }
    }

    private ulong ThirdPrizeSatoshis
    {
        get
        {
            return PersistentState.GetUInt64("ThirdPrizeSatoshis");
        }
        set
        {
            PersistentState.SetUInt64("ThirdPrizeSatoshis", value);
        }
    }

    private string TeamsCsv
    {
        get
        {
            return PersistentState.GetString("TeamsCsv");
        }
        set
        {
            PersistentState.SetString("TeamsCsv", value);
        }
    }

    private string Log
    {
        get
        {
            return PersistentState.GetString("Log");
        }
        set
        {
            PersistentState.SetString("Log", value);
        }
    }

    private bool TeamsAssigned {
        get
        {
            return PersistentState.GetBool("TeamsAssigned");
        }
        set
        {
            PersistentState.SetBool("TeamsAssigned", value);
        }
    }

    public Sweepstake(ISmartContractState smartContractState, string teams, uint entryFeeStrats, uint firstPrizeStrats, uint secondPrizeStrats, uint thirdPrizeStrats)
        : base(smartContractState)
    {
        teams = teams.Trim().Trim(',').ToLower();
        Assert((ulong)teams.Split(",").Length * entryFeeStrats == (firstPrizeStrats + secondPrizeStrats + thirdPrizeStrats),
            "Sum of the prizes must equal the teams * entryFee.");

        Owner = Message.Sender;
        
        TeamsCsv = teams;
        EntryFeeSatoshis = entryFeeStrats * SatoshiMuliplier;
        FirstPrizeSatoshis = firstPrizeStrats * SatoshiMuliplier;
        SecondPrizeSatoshis = secondPrizeStrats * SatoshiMuliplier;
        ThirdPrizeSatoshis = thirdPrizeStrats * SatoshiMuliplier;
    }

    public void JoinGame(string nickname)
    {
        Assert(!nickname.Contains(","), "Cannot have comma in nickname.");
        Assert(!GameIsFull(), "The game is already full. No more players can join.");
        Assert(Message.Value == this.EntryFeeSatoshis, "Amount must equal entryFee: " + this.EntryFeeSatoshis/SatoshiMuliplier +", but was: " + Message.Value + ".");

        AddPlayer(nickname, Message.Sender);

        if (GameIsFull())
        {
            AssignTeams();
        }
    }

    private void AddPlayer(string nickname, Address sender)
    {
        this.Players.Add(sender);
        this.PlayersCsv = (PlayersCsv + "," + sender).Trim(',').Trim();
        this.PlayersNickNames = (PlayersNickNames + "," + nickname).Trim(',').Trim();
    }

    private void AssignTeams()
    {
        var shiftNumberLowerThanLastCommaIndex = GetDeterministicRandomishNumber() % (TeamsCsv.LastIndexOf(",") - 1);

        var randomCommaIndex = TeamsCsv.IndexOf(',', shiftNumberLowerThanLastCommaIndex);
        var stringBeforeComma = TeamsCsv.Substring(0, randomCommaIndex);
        var stringAfterComma = TeamsCsv.Substring(randomCommaIndex + 1);

        var reorderedTeams = stringAfterComma + "," + stringBeforeComma;

        this.AssignedTeamsCsv = reorderedTeams;

        foreach (var team in reorderedTeams.Split(","))
        {
            AssignedTeams.Add(team.Trim());
        }
    }

    /// NOTE: Miner and last player could collaborate to manipulate team assignment
    private int GetDeterministicRandomishNumber()
    {
        var numberOfTeams = (uint)this.TeamsCsv.Split(",").Length;

        int sum = 0;

        // Use miner's address as source of pseudo randomness
        var coinbaseAddress = Block.Coinbase.Value;
        for (int i = 0; i < coinbaseAddress.Length; i++)
        {
            sum += coinbaseAddress[i];
        }

        // Also use last player's address as source of pseudo randomness
        var lastPlayer = Players[numberOfTeams - 1];

        for (int i = 0; i < lastPlayer.Value.Length; i++)
        {
            sum += lastPlayer.Value[i];
        }

        sum = sum + (int) Block.Number;
        return sum;
    }

    private void LogLine(string toLog)
    {
        this.Log = this.Log + "\r\n" + toLog;
    }

    public void DeclareResult(string winningTeam, string secondPlace, string thirdPlace)
    {
        EnsureOnlyOwnerCalled();

        winningTeam = winningTeam.Trim().Trim(',').ToLower();
        secondPlace = secondPlace.Trim().Trim(',').ToLower();
        thirdPlace = thirdPlace.Trim().Trim(',').ToLower();

        var assignedTeams = AssignedTeamsCsv.Split(",");
        var players = PlayersCsv.Split(",");

        var nickNames = PlayersNickNames.Split(",");

        CheckTeamsAreDifferentAndExist(winningTeam, secondPlace, thirdPlace);

        var winner = uint.MaxValue;
        var second = uint.MaxValue;
        var third = uint.MaxValue;

        LogLine("Loop through the assigned teams");
        for (uint i = 0; i < assignedTeams.Length; i++)
        {
            var team = assignedTeams[i];

            LogLine(i + ": if (team == winningTeam)");
            if (team == winningTeam)
            {
                LogLine("winner = i");
                winner = i;
            }

            LogLine("if (team == secondPlace)");
            if (team == secondPlace)
            {
                LogLine("second = i;");
                second = i;
            }

            LogLine("if (team == thirdPlace)");
            if (team == thirdPlace)
            {
                LogLine("third = i;");
                third = i;
            }
        }

        Result = 
            $"{nickNames[winner]}({players[winner]}) : {assignedTeams[winner]} : {(FirstPrizeSatoshis / SatoshiMuliplier)} {Currency(Message.ContractAddress)}\r\n" +
            $"{nickNames[second]}({players[second]}) : {assignedTeams[second]} : {(SecondPrizeSatoshis / SatoshiMuliplier)} {Currency(Message.ContractAddress)}\r\n" +
            $"{nickNames[third]}({players[third]}) : {assignedTeams[third]} : {(ThirdPrizeSatoshis / SatoshiMuliplier)} {Currency(Message.ContractAddress)}";

        TransferFunds(new Address(players[winner]), FirstPrizeSatoshis);
        TransferFunds(new Address(players[second]), SecondPrizeSatoshis);
        TransferFunds(new Address(players[third]), ThirdPrizeSatoshis);
    }

    public void CancelAndRefund()
    {
        EnsureOnlyOwnerCalled();

        for (uint i = 0; i < Players.Count; i++)
        {
            TransferFunds(Players[i], EntryFeeSatoshis);
        }

        Result = "Cancelled by owner at block: " + Block.Number + ". Refunds issued.";
    }

    private void EnsureOnlyOwnerCalled()
    {
        Assert(Message.Sender == Owner, $"Someone other than owner attempted to declare result: {Message.Sender}.");
    }

    private void CheckTeamsAreDifferentAndExist(string winningTeam, string secondPlace, string thirdPlace)
    {
        Assert(winningTeam != secondPlace, $"First place and second place were same team: {winningTeam}");
        Assert(winningTeam != thirdPlace, $"First place and third place were same team: {winningTeam}");
        Assert(secondPlace != thirdPlace, $"Third place and second place were same team: {secondPlace}");

        Assert(TeamsCsv.Contains(winningTeam), $"Winning team not present in team list: {winningTeam}");
        Assert(TeamsCsv.Contains(secondPlace), $"Second place team not present in team list: {secondPlace}");
        Assert(TeamsCsv.Contains(thirdPlace), $"Third place team not present in team list: {thirdPlace}");
    }

    private bool GameIsFull()
    {
        TeamsAssigned = this.Players.Count == this.TeamsCsv.Split(",").Length;

        return TeamsAssigned;
    }

    private string Currency(Address address)
    {
        return address.ToString().StartsWith("S") ? "STRAT" : "SC-TSTRAT";
    }


    public void StartGameNow()
    {
        EnsureOnlyOwnerCalled();
        Assert(this.Players.Count > 0, "There are no players yet.");

        int luckyPlayerIndex = 0;

        while (Players.Count < TeamsCsv.Split(",").Length)
        {
            AddPlayer(this.PlayersNickNames.Split(",")[luckyPlayerIndex], this.Players[(uint)luckyPlayerIndex]);
            luckyPlayerIndex = (luckyPlayerIndex+1) % (TeamsCsv.Length-1);
        } 

        AssignTeams();
    }
}