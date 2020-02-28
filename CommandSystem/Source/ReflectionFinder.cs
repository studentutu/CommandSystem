﻿using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace SickDev.CommandSystem 
{
    internal class ReflectionFinder 
    {
        Configuration configuration;
        Type[] userTypes;
        NotificationsHandler notificationsHandler;

        static Type[] _allTypes;

        public static Type[] allTypes
        {
            get 
            {
                if(_allTypes == null)
                    _allTypes = LoadAllTypes().ToArray();
                return _allTypes;
            }
        }

        public static Type[] enumTypes => allTypes.Where(x => x.IsEnum).ToArray();

        public ReflectionFinder(Configuration configuration, NotificationsHandler notificationsHandler) 
        {
            this.configuration = configuration;
            this.notificationsHandler = notificationsHandler;
            userTypes = LoadUserTypes().ToArray();
        }

        static List<Type> LoadAllTypes() 
        {
            List<Type> types = new List<Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();            
            for(int i = 0; i < assemblies.Length; i++)
                types.AddRange(assemblies[i].GetTypes());
            return types;
        }

        List<Type> LoadUserTypes() 
        {
            List<Type> types = new List<Type>();
            Assembly[] assemblies = GetAssembliesWithCommands();
            notificationsHandler.NotifyMessage("Loading CommandSystem data from: " +
                string.Join(", ", assemblies.ToList().ConvertAll(x => 
                {
                    AssemblyName name = x.GetName();
                    string path = name.CodeBase;
                    string extension = path.Substring(path.LastIndexOf('.'));
                    return name.Name + extension;
                }).ToArray()) + ".");
            for (int i = 0; i < assemblies.Length; i++)
                types.AddRange(assemblies[i].GetTypes());
            return types;
        }
        
        public Type[] GetUserClassesAndStructs() => userTypes.Where(x => x.IsClass || x.IsValueType && !x.IsEnum).ToArray();

        Assembly[] GetAssembliesWithCommands() 
        {
            List<Assembly> assemblies = new List<Assembly>();
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            string[] assembliesWithCommands = configuration.registeredAssemblies;

            for(int i = 0; i < assembliesWithCommands.Length; i++) 
            {
                bool loaded = false;
                for(int j = 0; j < loadedAssemblies.Length; j++) 
                {
                    if(loadedAssemblies[j].GetName().Name == assembliesWithCommands[i]) 
                    {
                        loaded = true;
                        assemblies.Add(loadedAssemblies[j]);
                        break;
                    }
                }
                if(!loaded) 
                {
                    try 
                    {
                        Assembly assembly = Assembly.Load(new AssemblyName(assembliesWithCommands[i]));
                        assemblies.Add(assembly);
                    }
                    catch 
                    {
                        notificationsHandler.NotifyMessage("Assembly with name '" + assembliesWithCommands[i] + "' could not be found. Please, make sure the assembly is properly loaded");
                    }
                }
            }
            return assemblies.ToArray();
        }
    }
}