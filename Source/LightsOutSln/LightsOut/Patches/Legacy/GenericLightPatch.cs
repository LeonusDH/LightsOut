﻿//************************************************
// The most general way I can disable glowers
// for custom classes that mods implement poorly
//************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using HarmonyLib;
using System.Reflection;
using LightsOut.Boilerplate;
using LightsOut.Patches.Lights;
using LightsOut.Utility;

namespace LightsOut.Patches
{
    [StaticConstructorOnStartup]
    public static class GenericLightPatch
    {
        static GenericLightPatch()
        {
            Assembly rimworld  = Assembly.GetAssembly(typeof(Pawn));
            Assembly lightsOut = Assembly.GetExecutingAssembly();
            Assembly mscorlib  = Assembly.GetAssembly(typeof(int));

            // get all types that contain "glower" and come from outside rimworld
            List<Type> types = new List<Type>();
            try
            {
                types = (from asm in AppDomain.CurrentDomain.GetAssemblies()
                         where asm != rimworld && asm != lightsOut && asm != mscorlib
                         from type in asm.GetTypes()
                         where type.IsClass && type.Name.ToLower().Contains("glower")
                         select type).ToList();
            }
            catch (ReflectionTypeLoadException) { }

            foreach(Type type in types)
            {
                try
                {
                    // if this is a thingcomp, then we should be able to patch it
                    if (type.IsSubclassOf(typeof(ThingComp)))
                    {
                        var original = type.GetProperty("shouldBeLitNow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (original is null)
                            original = type.GetProperty("ShouldBeLitNow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                        if (!(original is null))
                        {
                            // this looks to be a CompGlower-like thing, so keep track of that
                            ModResources.CompGlowers.Add(type);
                            var postfix = typeof(DisableLightGlowPatch).GetMethod("Postfix", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                            if (postfix != null)
                            {
                                Log.Message($"[LightsOut] patching \"{type.Namespace} - {type.Name}\" to count as a glower");
                                HarmonyPatches.Harmony.Patch(original.GetMethod, null, new HarmonyMethod(postfix));
                            }
                        }
                    }
                }
                catch (Exception) { }
            }
        }
    }
}