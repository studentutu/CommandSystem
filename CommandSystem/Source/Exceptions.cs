﻿using System;
using System.Text;
using System.Reflection;

namespace SickDev.CommandSystem {
    public class CommandSystemException : Exception {
        public CommandSystemException() { }
        public CommandSystemException(Exception innerException):base(string.Empty, innerException) { }
    }

    public class InvalidCommandTypeConstructorException : CommandSystemException {
        Type type;

        public InvalidCommandTypeConstructorException(Type type) {
            this.type = type;
        }

        public override string Message {
            get { return type.Name + " does not have a valid constructor. Please, review the docs on how to create new Command types";  }
        }
    }

    public class UnsupportedCommandDeclarationException : CommandSystemException {
        MethodInfo method;

        string methodPath { get { return method.DeclaringType + "." + method.Name; } }

        string header { get { return "The command "+methodPath; } }
        string tail { get { return "Please, review the docs on how to create new Commands"; } }

        public UnsupportedCommandDeclarationException(MethodInfo method) {
            this.method = method;
        }

        public override string Message {
            get {
                if (!method.IsStatic)
                    return header+" is not static. Only static commands are supported. " + tail;
                else if (method.IsGenericMethod || method.IsGenericMethodDefinition)
                    return header + " is generic, which is not yet supported. " + tail;
                else
                    return header + " declaration is unsupported. " +tail;
            }
        }
    }

    public class InvalidArgumentFormatException<T> : CommandSystemException {
        string argument;

        public InvalidArgumentFormatException(string argument) {
            this.argument = argument;
        }

        public override string Message {
            get {return "Argument \"" + argument + "\" cannot be parsed into type " + typeof(T).Name + " because it is not in the correct format";}
        }
    }

    public class NoValidParserFoundException : CommandSystemException {
        Type type;

        public NoValidParserFoundException(Type type) {
            this.type = type;
        }

        public override string Message {
            get {return "No valid Parser method found for type " + type.Name+". Please, review the docs on how to create new Parser methods";}
        }
    }

    public class AmbiguousCommandCallException : CommandSystemException {
        string rawCall;
        Command[] matches;

        public AmbiguousCommandCallException(string rawCall, Command[] matches) {
            this.rawCall = rawCall;
            this.matches = matches;
        }

        public override string Message {
            get {
                StringBuilder builder = new StringBuilder();
                for(int i = 0; i < matches.Length; i++) {
                    builder.Append(string.Format("{0}", matches[i].signature.raw));
                    if(i < matches.Length - 1)
                        builder.AppendLine();
                }
                return "The command call \"" + rawCall + "\" is ambiguous between the following commands:\n"+builder.ToString();
            }
        }
    }

    public class CommandNotFoundException : CommandSystemException {
        ParsedCommand command;

        public CommandNotFoundException(ParsedCommand command) {
            this.command = command;
        }

        public override string Message {
            get {return "No command found with name '" + command.command + "'";}
        }
    }

    public class MatchNotFoundException : CommandSystemException {
        ParsedCommand command;

        public MatchNotFoundException(ParsedCommand command) {
            this.command = command;
        }

        public override string Message {
            get {return "No match found for command " + command.raw;}
        }
    }

    public class DuplicatedParserException : CommandSystemException {
        ParserAttribute parser;

        public DuplicatedParserException(ParserAttribute parser) {
            this.parser = parser;
        }

        public override string Message {
            get {return "More than one Parser was specified for type " + parser.type + ".Please, note that most common types already have a built-in Parser"; }
        }
    }

    public class CommandBuildingException : CommandSystemException {
        Type type;
        MemberInfo member;

        public CommandBuildingException(Type type, MemberInfo member, Exception innerException):base(innerException) {
            this.type = type;
            this.member = member;
        }

        public override string Message {
            get {
                return "There was an error creating a command for "+member.MemberType+" "+ member.Name + " of type " + type.Name + ". Skipping command.\n" +
                    "Error Message: " + InnerException.Message + "\nStackTrace" + InnerException.StackTrace;
            }
        }
    }
}
