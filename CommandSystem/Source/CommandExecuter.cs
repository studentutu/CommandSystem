﻿using System;
using System.Reflection;
using System.Collections.Generic;

namespace SickDev.CommandSystem
{
	public class CommandExecuter
	{
		readonly List<Command> commands;
		readonly ParsedCommand parsedCommand;
		readonly NotificationsHandler notificationsHandler;
		List<Command> overloads = new List<Command>();
		List<Match> matches = new List<Match>();

		//TODO this can be dettermined in the constructor
		public bool isValidCommand => overloads.Count >= 1;
		public bool canBeExecuted => matches.Count == 1;
		public bool hasReturnValue => canBeExecuted && matches[0].command.hasReturnValue;

		internal CommandExecuter(List<Command> commands, ParsedCommand parsedCommand, ArgumentsParser parser, NotificationsHandler notificationsHandler)
		{
			this.commands = commands;
			this.parsedCommand = parsedCommand;
			this.notificationsHandler = notificationsHandler;

			FilterOverloads();
			FilterMatches(parser);
		}

		void FilterOverloads()
		{
			for (int i = 0; i < commands.Count; i++)
				if (commands[i].IsOverloadOf(parsedCommand))
					overloads.Add(commands[i]);
		}

		void FilterMatches(ArgumentsParser parser)
		{
			for (int i = 0; i < overloads.Count; i++)
			{
				try
				{
					if (overloads[i].signature.Matches(parsedCommand.args))
					{
						object[] arguments = overloads[i].signature.Convert(parsedCommand.args, parser);
						matches.Add(new Match(overloads[i], arguments));
					}
				}
				catch (TargetInvocationException) { }
				catch (CommandSystemException) { }
				//TODO no more catches?
			}
		}

		public object Execute()
		{
			try
			{
				Match match = GetMatch();
				return match.command.Execute(match.parameters);
			}
			catch (Exception exception)
			{
				notificationsHandler.NotifyException(exception);
				return null;
			}
		}

		public Command[] GetOverloads() => overloads.ToArray();

		public Match GetMatch()
		{
			if (!isValidCommand)
				throw new CommandNotFound(parsedCommand);
			if (matches.Count == 0)
				throw new MatchNotFound(parsedCommand, overloads.ToArray());
			if (matches.Count > 1)
				throw new AmbiguousCommandCall(parsedCommand, matches.ConvertAll(x => x.command).ToArray());
			return matches[0];
		}

		public struct Match
		{
			public readonly Command command;
			public readonly object[] parameters;

			public Match(Command command, object[] parameters)
			{
				this.command = command;
				this.parameters = parameters;
			}
		}
	}
}
