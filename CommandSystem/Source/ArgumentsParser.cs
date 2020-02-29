﻿using System;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;

namespace SickDev.CommandSystem 
{
    internal class ArgumentsParser 
    {
        ReflectionFinder finder;
        NotificationsHandler notificationsHandler;
        //Dictionary for linking a given type with its respective Parser method
        Dictionary<Type, MethodInfo> parsers = new Dictionary<Type, MethodInfo>();

        public bool dataLoaded { get; private set; }

        public ArgumentsParser(ReflectionFinder finder, Configuration configuration, NotificationsHandler notificationsHandler) 
        {
            this.finder = finder;
            this.notificationsHandler = notificationsHandler;
            if(configuration.allowThreading)
                new Thread(Load).Start();
            else
                Load();
        }

        //Finds every Parser method and adds it to the array
        void Load() 
        {
            Type[] allTypes = finder.userClassesAndStructs;
            for (int i = 0; i < allTypes.Length; i++) 
            { 
                MethodInfo[] methods = allTypes[i].GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int j = 0; j < methods.Length; j++) 
                { 
                    object[] attributes = methods[j].GetCustomAttributes(typeof(ParserAttribute), false);
                    if (attributes.Length > 0) 
                    {
                        ParserAttribute parser = (ParserAttribute)attributes[0];
                        if (!parsers.ContainsKey(parser.type))
                            parsers.Add(parser.type, methods[j]);
                        else
                            notificationsHandler.NotifyException(new DuplicatedParserException(parser));
                    }
                }
            }
            dataLoaded = true;
            notificationsHandler.NotifyMessage("Loaded " + parsers.Count + " parsers:\n" + string.Join("\n", parsers.ToList().ConvertAll(x => x.Key.Namespace+"."+SignatureBuilder.TypeToString(x.Key)).ToArray()));
        }

        public object Parse(ParsedArgument argument, Type type) 
        {
            if(argument.type != null && argument.type != type)
                throw new ExplicitCastMismatchException(argument.type, type);
            return Parse(argument.argument, type);
        }

        //Given a type, looks for a corresponding Parser method
        object Parse(string value, Type type) 
        {
            if(type.IsEnum)
                return Enum.Parse(type, value);
            if(type.IsArray)
                return HandleArrayType(value, type);
            else if(HasParserForType(type))
                return CallParser(type, value);
            throw new NoValidParserFoundException(type);
        }

        object HandleArrayType(string value, Type type) 
        {
            ParsedCommand parsedArray = new ParsedCommand("command " + value);
            Array array = (Array)Activator.CreateInstance(type, parsedArray.args.Length);
            for(int i = 0; i < parsedArray.args.Length; i++)
                array.SetValue(Parse(parsedArray.args[i], type.GetElementType()), i);
            return array;
        }

        bool HasParserForType(Type type) => parsers.ContainsKey(type);
        object CallParser(Type type, string value) => parsers[type].Invoke(null, new object[] { value });
    }
}