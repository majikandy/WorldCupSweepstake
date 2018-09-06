using System;
using FluentAssertions;
using NSubstitute;
using Stratis.SmartContracts;
using WorldCupSweepstake.Tests.TestTools;
using Xunit;

namespace WorldCupSweepstake.Tests
{
    public class SweepStakeTests
    {
        private readonly Address contractOwnerAddress = new Address("contract_owner_address");
        private readonly Address contractAddress = new Address("ContractAddress");
        private readonly Address punter1Address = new Address("Punter1 address");
        private readonly Address punter2Address = new Address("Punter2 address");
        private readonly Address punter3Address = new Address("Punter3 address");
        private uint entryFeeStrats = 5;
        private const ulong SatoshiMuliplier = 100000000; //note via swagger when calling methods it is strats. eg amount:"1"  means 1 strat (1*10^8 satoshis)

        private TestSmartContractState smartContractState;
        private IInternalTransactionExecutor transactionExecutor;
        private IPersistentState persistentState;

        public SweepStakeTests()
        {
            CreateSmartContractState();
        }

        private Sweepstake SetupValidSweepstake()
        {
            var message = ((TestMessage)smartContractState.Message);
            message.Sender = contractOwnerAddress;
            message.Value =
                entryFeeStrats *
                SatoshiMuliplier; 

            var teams = "Germany,Brazil,England,Argentina";

            var totalPrizeFund = entryFeeStrats * 4;
            uint firstPrize = (uint)(totalPrizeFund * 0.7);
            uint secondPrize = (uint)(totalPrizeFund * 0.2);
            uint thirdPrize = (uint)(totalPrizeFund * 0.1);

            var contract = new Sweepstake(smartContractState, teams, entryFeeStrats, firstPrize, secondPrize,
                thirdPrize);

            return contract;
        }

        [Fact]
        public void Create_sweepstake_providing_start_block_list_of_teams_prize_allocation_and_end_date()
        {
            var contract = SetupValidSweepstake();

            persistentState.GetAddress("Owner").Should().Be(contractOwnerAddress);
            persistentState.GetString("TeamsCsv").Should().Be("germany,brazil,england,argentina");
            persistentState.GetUInt64("FirstPrizeSatoshis").Should().Be(14ul * SatoshiMuliplier);
            persistentState.GetUInt64("SecondPrizeSatoshis").Should().Be(4ul * SatoshiMuliplier);
            persistentState.GetUInt64("ThirdPrizeSatoshis").Should().Be(2ul * SatoshiMuliplier);
        }

        [Fact]
        public void Assign_teams_psuedo_randomly_after_final_player_joins_the_game()
        {
            var message = ((TestMessage)smartContractState.Message);

            var contract = SetupValidSweepstake();
            all_players_join_and_get_assigned_teams(message, contract);
        }

        [Fact]
        public void
            StartGameNow_shortcuts_the_join_game_process_and_assigns_all_remaining_teams_to_current_players_and_starts_the_game()
        {
            var message = ((TestMessage)smartContractState.Message);
            message.Sender = contractOwnerAddress;

            var contract = SetupValidSweepstake();
            message.Sender = contractOwnerAddress;
            contract.JoinGame("owner");
            message.Sender = punter1Address;
            contract.JoinGame("p1");

            message.Sender = contractOwnerAddress;
            contract.StartGameNow();

            var persistedPlayers = persistentState.GetAddressList("Players");
            persistedPlayers.Count.Should().Be((uint)persistentState.GetString("TeamsCsv").Split(",").Length);

            var persistedAssignedTeams = persistentState.GetStringList("AssignedTeams");

            persistedAssignedTeams.GetValue(0).Should().Be("england");
            persistedPlayers.GetValue(0).Should().Be(contractOwnerAddress);
            persistedAssignedTeams.GetValue(1).Should().Be("argentina");
            persistedPlayers.GetValue(1).Should().Be(punter1Address);
            persistedAssignedTeams.GetValue(2).Should().Be("germany");
            persistedPlayers.GetValue(2).Should().Be(contractOwnerAddress);
            persistedAssignedTeams.GetValue(3).Should().Be("brazil");
            persistedPlayers.GetValue(3).Should().Be(punter1Address);
        }

        [Fact]
        public void Only_contract_owner_can_start_game_now()
        {
            var message = ((TestMessage)smartContractState.Message);

            var contract = SetupValidSweepstake();
            message.Sender = punter1Address;
            contract.JoinGame("p1");

            Action startGameNow = () => contract.StartGameNow();
            startGameNow.Should().Throw<Exception>().And.Message.Should()
                .Be($"Someone other than owner attempted to declare result: {punter1Address}.");
        }

        [Fact]
        public void start_game_now_errors_if_no_players()
        {
            var contract = SetupValidSweepstake();
            Action startGameNow = () => contract.StartGameNow();
            startGameNow.Should().Throw<Exception>().And.Message.Should().Be("There are no players yet.");
        }

        private void all_players_join_and_get_assigned_teams(TestMessage message, Sweepstake contract)
        {
            message.Sender = contractOwnerAddress;
            contract.JoinGame("owner");
            message.Sender = punter1Address;
            contract.JoinGame("p1");
            message.Sender = punter2Address;
            contract.JoinGame("p2");
            message.Sender = punter3Address;
            contract.JoinGame("p3");

            var persistedAssignedTeams = persistentState.GetStringList("AssignedTeams");

            persistedAssignedTeams.GetValue(0).Should().Be("england");
            persistedAssignedTeams.GetValue(1).Should().Be("argentina");
            persistedAssignedTeams.GetValue(2).Should().Be("germany");
            persistedAssignedTeams.GetValue(3).Should().Be("brazil");
        }

        [Fact]
        public void AnnounceResult_and_payout()
        {
            var message = ((TestMessage)smartContractState.Message);

            var contract = SetupValidSweepstake();
            all_players_join_and_get_assigned_teams(message, contract);

            var persistedAssignedTeams = persistentState.GetStringList("AssignedTeams");
            var persistedPlayers = persistentState.GetAddressList("Players");

            message.Sender = contractOwnerAddress;

            contract.DeclareResult(
                persistedAssignedTeams.GetValue(3),
                persistedAssignedTeams.GetValue(0),
                persistedAssignedTeams.GetValue(2));

            var result = persistentState.GetString("Result");
            var persistedPlayersNickNames = persistentState.GetString("PlayersNickNames");

            var persistedFirstPrizeStrats = persistentState.GetUInt64("FirstPrizeSatoshis") / SatoshiMuliplier;
            var persistedSecondPrizeSatoshis = persistentState.GetUInt64("SecondPrizeSatoshis") / SatoshiMuliplier;
            var persistedThirdPrizeSatoshis = persistentState.GetUInt64("ThirdPrizeSatoshis") / SatoshiMuliplier;

            var persistedNickNames = persistedPlayersNickNames.Split(",");

            result.Should().Be(
$@"{persistedNickNames[3]}({persistedPlayers.GetValue(3)}) : {persistedAssignedTeams.GetValue(3)} : {persistedFirstPrizeStrats} SC-TSTRAT
{persistedNickNames[0]}({persistedPlayers.GetValue(0)}) : {persistedAssignedTeams.GetValue(0)} : {persistedSecondPrizeSatoshis} SC-TSTRAT
{persistedNickNames[2]}({persistedPlayers.GetValue(2)}) : {persistedAssignedTeams.GetValue(2)} : {persistedThirdPrizeSatoshis} SC-TSTRAT");

            this.transactionExecutor.Received().TransferFunds(smartContractState, persistedPlayers.GetValue(3),
                14ul * SatoshiMuliplier, null);

            this.transactionExecutor.Received().TransferFunds(smartContractState, persistedPlayers.GetValue(0),
                4ul * SatoshiMuliplier, null);

            this.transactionExecutor.Received().TransferFunds(smartContractState, persistedPlayers.GetValue(2),
                2ul * SatoshiMuliplier, null);
        }

        [Fact]
        public void Error_if_entry_fee_too_high()
        {
            var contract = SetupValidSweepstake();
            var message = ((TestMessage)smartContractState.Message);
            message.Value = entryFeeStrats + 1;

            Action joinGame = () => contract.JoinGame("alice");

            joinGame.Should().Throw<Exception>()
                .WithMessage("Amount must equal entryFee: 5, but was: 6.");
        }

        [Fact]
        public void Error_if_entry_fee_too_low()
        {
            var contract = SetupValidSweepstake();
            var message = ((TestMessage)smartContractState.Message);
            message.Value = entryFeeStrats - 1;

            Action joinGame = () => contract.JoinGame("alice");

            joinGame.Should().Throw<Exception>()
                .WithMessage("Amount must equal entryFee: 5, but was: 4.");
        }

        [Fact]
        public void Error_when_no_teams_left()
        {
            var contract = SetupValidSweepstake();

            Action joinGame = () => contract.JoinGame("alice");

            joinGame();
            joinGame();
            joinGame();
            joinGame();

            joinGame.Should().Throw<Exception>()
                .WithMessage("The game is already full. No more players can join.");
        }

        [Fact]
        public void Error_when_name_contains_comma()
        {
            var contract = SetupValidSweepstake();

            Action joinGame = () => contract.JoinGame(",");

            joinGame.Should().Throw<SmartContractAssertException>()
                .WithMessage("Cannot have comma in nickname.");
        }

        [Fact]
        public void Error_when_name_contains_whitespace()
        {
            var contract = SetupValidSweepstake();

            Action joinGame = () => contract.JoinGame(" ");

            joinGame.Should().Throw<SmartContractAssertException>()
                .WithMessage("Cannot have whitespace in nickname.");
        }

        [Fact]
        public void Only_contract_owner_can_announce_results()
        {
            var contract = SetupValidSweepstake();

            Action declareResult = () => contract.DeclareResult("Brazil", "England", "Germany");

            var message = ((TestMessage)smartContractState.Message);
            message.Sender = new Address("not_owner");

            declareResult.Should().Throw<Exception>().And.Message.Should()
                .Be("Someone other than owner attempted to declare result: not_owner.");
        }

        [Fact]
        public void Error_when_prizes_dont_add_up_to_number_of_players()
        {
            var message = ((TestMessage)smartContractState.Message);
            message.Sender = contractOwnerAddress;
            message.Value = entryFeeStrats;

            uint firstPrize = 100;
            uint secondPrize = 40;
            uint thirdPrize = 20;

            Action construct = () => new Sweepstake(smartContractState, "Germany,Brazil,England,Argentina",
                entryFeeStrats, firstPrize, secondPrize, thirdPrize);

            construct.Should().Throw<SmartContractAssertException>()
                .And.Message.Should().Be("Sum of the prizes must equal the teams * entryFee.");
        }

        [Fact]
        public void When_declaring_result_check_all_teams_are_different_and_exist()
        {
            var message = ((TestMessage)smartContractState.Message);

            var contract = SetupValidSweepstake();
            all_players_join_and_get_assigned_teams(message, contract);

            message.Sender = contractOwnerAddress;

            VerifyAssertFail(() => contract.DeclareResult("Brazil", "Brazil  ", "Germany"),
                "First place and second place were same team: brazil.");

            VerifyAssertFail(() => contract.DeclareResult("England", "Brazil", "Brazil"),
               "Third place and second place were same team: brazil.");

            VerifyAssertFail(() => contract.DeclareResult("England", "Brazil", "England"),
                "First place and third place were same team: england.");

            VerifyAssertFail(() => contract.DeclareResult("E", "Brazil", "Germany"), 
                "Winning team not present in team list: e.");

            VerifyAssertFail(() => contract.DeclareResult("England", "B", "Germany"), 
                "Second place team not present in team list: b.");

            VerifyAssertFail(() => contract.DeclareResult("England", "Brazil", "G"), 
                "Third place team not present in team list: g.");
        }

        [Fact]
        public void Cancel_and_refund()
        {
            var message = ((TestMessage)smartContractState.Message);

            var contract = SetupValidSweepstake();
            message.Sender = contractOwnerAddress;
            contract.JoinGame("owner");
            message.Sender = punter1Address;
            contract.JoinGame("p1");
            message.Sender = punter2Address;
            contract.JoinGame("p2");
            message.Sender = punter3Address;
            contract.JoinGame("p3");

            message.Sender = contractOwnerAddress;

            contract.CancelAndRefund();

            this.transactionExecutor.Received()
                .TransferFunds(smartContractState, contractOwnerAddress, 5ul * SatoshiMuliplier, null);

            this.transactionExecutor.Received()
                .TransferFunds(smartContractState, punter1Address, 5ul * SatoshiMuliplier, null);

            this.transactionExecutor.Received()
                .TransferFunds(smartContractState, punter2Address, 5ul * SatoshiMuliplier, null);

            this.transactionExecutor.Received()
                .TransferFunds(smartContractState, punter3Address, 5ul * SatoshiMuliplier, null);

            persistentState.GetString("Result").Should()
                .Be("Cancelled by owner at block: " + smartContractState.Block.Number + ". Refunds issued.");
        }

        private void CreateSmartContractState()
        {
            var block = new TestBlock
            {
                Coinbase = contractOwnerAddress,
                Number = 2
            };

            var message = new TestMessage
            {
                ContractAddress = contractAddress,
                GasLimit = (Gas) (ulong) 10000,
                Sender = punter1Address,
                Value = 0
            };

            this.transactionExecutor = Substitute.For<IInternalTransactionExecutor>();
            persistentState = new InMemoryState();

            this.smartContractState = new TestSmartContractState(
                block,
                message,
                persistentState,
                null,
                transactionExecutor,
                () => (ulong) 0,
                new TestInternalHashHelper()
            );
        }

        private static void VerifyAssertFail(Action action, string assertFailMessage)
        {
            action.Should().Throw<SmartContractAssertException>()
                .And.Message.Should().Be(assertFailMessage);
        }
    }
}