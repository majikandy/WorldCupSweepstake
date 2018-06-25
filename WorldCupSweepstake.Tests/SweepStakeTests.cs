using System;
using FluentAssertions;
using NSubstitute;
using Stratis.SmartContracts;
using WorldCupSweepstake.Tests.TestTools;
using Xunit;

namespace WorldCupSweepstake.Tests
{
    /// <summary>
    /// DISCLAIMER: This contract is an experimental bit of fun for the world cup and NOT reference code. 
    /// It is not guranteed to work and is my own work, not the work of the company.
    /// </summary>
    public class SweepStakeTests
    {
        private readonly Address contractOwnerAddress = new Address("contract_owner_address");
        private readonly Address contractAddress = new Address("ContractAddress");
        private readonly Address punter1Address = new Address("Punter1 address");
        private readonly Address punter2Address = new Address("Punter2 address");
        private readonly Address punter3Address = new Address("Punter3 address");
        private uint entryFeeStrats = 5;
        private const ulong SatoshiMuliplier = 100000000;

        private readonly TestSmartContractState smartContractState;
        private readonly IInternalTransactionExecutor transactionExecutor;

        public SweepStakeTests()
        {
            var block = new TestBlock
            {
                Coinbase = contractOwnerAddress,
                Number = 2
            };

            var message = new TestMessage
            {
                ContractAddress = contractAddress,
                GasLimit = (Gas)(ulong)10000,
                Sender = punter1Address,
                Value = 0
            };

            this.transactionExecutor = Substitute.For<IInternalTransactionExecutor>();

            this.smartContractState = new TestSmartContractState(
                block,
                message,
                new InMemoryState(),
                null,
                transactionExecutor,
                () => (ulong)0,
                new TestInternalHashHelper()
            );
        }

        private Sweepstake SetupValidSweepstake()
        {
            var message = ((TestMessage)smartContractState.Message);
            message.Sender = contractOwnerAddress;
            message.Value = entryFeeStrats * SatoshiMuliplier; //note via swagger when calling methods it is strats. eg amount:"1"  means 1 strat (1*10^8 satoshis)

            var teams = "Germany,Brazil,England,Argentina";

            var totalPrizeFund = entryFeeStrats * 4;
            uint firstPrize = (uint)(totalPrizeFund * 0.7);
            uint secondPrize = (uint)(totalPrizeFund * 0.2);
            uint thirdPrize = (uint)(totalPrizeFund * 0.1);

            var contract = new Sweepstake(smartContractState, teams, entryFeeStrats, firstPrize, secondPrize, thirdPrize);

            return contract;
        }

        [Fact]
        public void Create_sweepstake_providing_start_block_list_of_teams_prize_allocation_and_end_date()
        {
            var contract = SetupValidSweepstake();

            contract.Owner.Should().Be(contractOwnerAddress);
            contract.TeamsCsv.Should().Be("germany,brazil,england,argentina");
            contract.FirstPrizeSatoshis.Should().Be(14ul * SatoshiMuliplier);
            contract.SecondPrizeSatoshis.Should().Be(4ul * SatoshiMuliplier);
            contract.ThirdPrizeSatoshis.Should().Be(2ul * SatoshiMuliplier);
        }

        [Fact]
        public void Assign_teams_psuedo_randomly_after_final_player_joins_the_game()
        {
            var message = ((TestMessage)smartContractState.Message);

            var contract = SetupValidSweepstake();
            all_players_join_and_get_assigned_teams(message, contract);
        }

        [Fact]
        public void StartGameNow_shortcuts_the_join_game_process_and_assigns_all_remaining_teams_to_current_players_and_starts_the_game()
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

            contract.Players.Count.Should().Be((uint)contract.TeamsCsv.Split(",").Length);

            contract.AssignedTeams.GetValue(0).Should().Be("england");
            contract.Players.GetValue(0).Should().Be(contractOwnerAddress);
            contract.AssignedTeams.GetValue(1).Should().Be("argentina");
            contract.Players.GetValue(1).Should().Be(punter1Address);
            contract.AssignedTeams.GetValue(2).Should().Be("germany");
            contract.Players.GetValue(2).Should().Be(contractOwnerAddress);
            contract.AssignedTeams.GetValue(3).Should().Be("brazil");
            contract.Players.GetValue(3).Should().Be(punter1Address);

        }

        [Fact]
        public void Only_contract_owner_can_start_game_now()
        {
            var message = ((TestMessage)smartContractState.Message);
            
            var contract = SetupValidSweepstake();
            message.Sender = punter1Address;
            contract.JoinGame("p1");

            Action startGameNow = () => contract.StartGameNow();
            startGameNow.Should().Throw<Exception>().And.Message.Should().Be($"Someone other than owner attempted to declare result: {punter1Address}.");
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
             
            contract.AssignedTeams.GetValue(0).Should().Be("england");
            contract.AssignedTeams.GetValue(1).Should().Be("argentina");
            contract.AssignedTeams.GetValue(2).Should().Be("germany");
            contract.AssignedTeams.GetValue(3).Should().Be("brazil");
        }

        [Fact]
        public void AnnounceResult_and_payout()
        {
            var message = ((TestMessage)smartContractState.Message);

            var contract = SetupValidSweepstake();
            all_players_join_and_get_assigned_teams(message, contract);

            message.Sender = contractOwnerAddress;
            contract.DeclareResult(contract.AssignedTeams.GetValue(3), contract.AssignedTeams.GetValue(0), contract.AssignedTeams.GetValue(2));

            contract.Result.Should().Be(
                $@"{contract.PlayersNickNames.Split(",")[3]}({contract.Players.GetValue(3)}) : {contract.AssignedTeams.GetValue(3)} : {contract.FirstPrizeSatoshis/SatoshiMuliplier} SC-TSTRAT
{contract.PlayersNickNames.Split(",")[0]}({contract.Players.GetValue(0)}) : {contract.AssignedTeams.GetValue(0)} : {contract.SecondPrizeSatoshis/SatoshiMuliplier} SC-TSTRAT
{contract.PlayersNickNames.Split(",")[2]}({contract.Players.GetValue(2)}) : {contract.AssignedTeams.GetValue(2)} : {contract.ThirdPrizeSatoshis/SatoshiMuliplier} SC-TSTRAT");

            this.transactionExecutor.Received().TransferFunds(smartContractState, contract.Players.GetValue(3), 14ul * SatoshiMuliplier, null);
            this.transactionExecutor.Received().TransferFunds(smartContractState, contract.Players.GetValue(0), 4ul * SatoshiMuliplier, null);
            this.transactionExecutor.Received().TransferFunds(smartContractState, contract.Players.GetValue(2), 2ul * SatoshiMuliplier, null);
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

            joinGame.Should().Throw<Exception>()
                .WithMessage("Cannot have comma in nickname.");
        }

        [Fact]
        public void Only_contract_owner_can_announce_results()
        {
            var contract = SetupValidSweepstake();

            Action declareResult = () => contract.DeclareResult("Brazil", "England", "Germany");

            var message = ((TestMessage)smartContractState.Message);
            message.Sender = new Address("not_owner");

            declareResult.Should().Throw<Exception>().And.Message.Should().Be("Someone other than owner attempted to declare result: not_owner.");
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

            Action construct = () => new Sweepstake(smartContractState, "Germany,Brazil,England,Argentina", entryFeeStrats, firstPrize, secondPrize, thirdPrize);

            construct.Should().Throw<Exception>();
        }

        [Fact]
        public void When_declaring_result_check_all_teams_are_different_and_exist()
        {
            var message = ((TestMessage)smartContractState.Message);

            var contract = SetupValidSweepstake();
            all_players_join_and_get_assigned_teams(message, contract);

            message.Sender = contractOwnerAddress;
            Action declareResult = () => contract.DeclareResult("Brazil", "Brazil  ", "Germany");
            declareResult.Should().Throw<Exception>();
            declareResult = () => contract.DeclareResult("England", "Brazil", "Brazil");
            declareResult.Should().Throw<Exception>();
            declareResult = () => contract.DeclareResult("England", "Brazil", "England");
            declareResult.Should().Throw<Exception>();
            declareResult = () => contract.DeclareResult("E", "Brazil", "Germany");
            declareResult.Should().Throw<Exception>();
            declareResult = () => contract.DeclareResult("England", "B", "Germany");
            declareResult.Should().Throw<Exception>();
            declareResult = () => contract.DeclareResult("England", "Brazil", "G");
            declareResult.Should().Throw<Exception>();
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

            this.transactionExecutor.Received().TransferFunds(smartContractState, contractOwnerAddress, 5ul * SatoshiMuliplier, null);
            this.transactionExecutor.Received().TransferFunds(smartContractState, punter1Address, 5ul * SatoshiMuliplier, null);
            this.transactionExecutor.Received().TransferFunds(smartContractState, punter2Address, 5ul * SatoshiMuliplier, null);
            this.transactionExecutor.Received().TransferFunds(smartContractState, punter3Address, 5ul * SatoshiMuliplier, null);

            contract.Result.Should().Be("Cancelled by owner at block: " + smartContractState.Block.Number + ". Refunds issued.");
        }
    }
}