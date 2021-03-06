using GameLovers.Services;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

namespace GameLoversEditor.Services.Tests
{
	[TestFixture]
	public class CommandServiceTest
	{
		private CommandService<IGameLogicMockup> _commandService;
		private IGameLogicMockup _gameLogicMockup;
		private ICommandNetworkService _networkService;

		// ReSharper disable once MemberCanBePrivate.Global
		public interface IGameLogicMockup
		{
			void CallMockup(int payload);
		}

		private struct CommandMockup : IGameCommand<IGameLogicMockup>
		{
			public int Payload;
			
			public void Execute(IGameLogicMockup gameLogic)
			{
				gameLogic.CallMockup(Payload);
			}
		}

		[SetUp]
		public void Init()
		{
			_gameLogicMockup = Substitute.For<IGameLogicMockup>();
			_networkService = Substitute.For<ICommandNetworkService>();
			_commandService = new CommandService<IGameLogicMockup>(_gameLogicMockup, _networkService);
		}

		[Test]
		public void ExecuteCommand_Successfully()
		{
			var payload = 1;
			var command = new CommandMockup { Payload = payload };
			
			_commandService.ExecuteCommand(command);
			
			_gameLogicMockup.Received().CallMockup(Arg.Is(payload));
			_networkService.Received().SendCommand(Arg.Is(command));
		}
	}
}