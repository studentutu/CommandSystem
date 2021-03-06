﻿using System;
using System.Reflection;
using System.Collections.Generic;

namespace SickDev.CommandSystem
{
	internal class CommandAttributeLoader
	{
		NotificationsHandler notificationsHandler;
		List<Command> commands = new List<Command>();
		Type[] types;

		public CommandAttributeLoader(ReflectionFinder finder, NotificationsHandler notificationsHandler)
		{
			this.notificationsHandler = notificationsHandler;
			types = finder.userClassesAndStructs;
		}

		public Command[] GetCommands()
		{
			for (int i = 0; i < types.Length; i++)
				commands.AddRange(GetCommandsInType(types[i]));
			return commands.ToArray();
		}

		Command[] GetCommandsInType(Type type)
		{
			List<Command> commands = new List<Command>();
			MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			for (int i = 0; i < methods.Length; i++)
			{
				CommandAttributeVerifier verifier = new CommandAttributeVerifier(methods[i]);
				if (!verifier.hasCommandAttribute)
					continue;

				if (!verifier.isDeclarationSupported)
					notificationsHandler.NotifyException(new UnsupportedCommandDeclaration(methods[i]));
				else
					commands.Add(verifier.ExtractCommand());
			}
			return commands.ToArray();
		}
	}
}