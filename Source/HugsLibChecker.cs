﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HugsLibChecker {
	[StaticConstructorOnStartup]
	public static class HugsLibChecker {
		private const string TokenObjectName = "HugsLibCheckerToken";
		private const string LibraryModName = "HugsLib";
		private const string MissingLibraryMessage = "<b>{0}</b> requires the <b>HugsLib library</b> to work properly.\nWould you like to download it now?";
		private const string OutdatedLibraryMessage = "<b>{0}</b> requires version <b>{1}</b> of the <b>HugsLib library</b> to work properly.\nWould you like to update it now?";
		
		// entry point
		static HugsLibChecker() {
			try {
				if (ChecksAlreadyPerformed()) return;
				var relatedMods = EnumerateLibraryRelatedMods();
				if (!LibraryIsLoaded(relatedMods)) {
					ScheduleDialog(String.Format(MissingLibraryMessage, GetFirstLibraryConsumerName(relatedMods)));
				} else if (!LibraryIsUpToDate(relatedMods)) {
					string consumerName;
					var requiredVersion = GetHighestRequiredLibraryVersion(relatedMods, out consumerName);
					ScheduleDialog(String.Format(OutdatedLibraryMessage, consumerName, requiredVersion));
				}
			} catch (Exception e) {
				Log.Error("An exception was caused by the HugsLibChecker assembly. Exception was: "+e);
			}
		}

		private static bool ChecksAlreadyPerformed() {
			var tokenObject = GameObject.Find(TokenObjectName);
			if (tokenObject != null) return true;
			new GameObject(TokenObjectName);
			return false;
		}

		private static List<LibaryRelatedMod> EnumerateLibraryRelatedMods() {
			var checkerAssemblyName = typeof (HugsLibChecker).Assembly.GetName().Name;
			var relatedMods = new List<LibaryRelatedMod>();
			foreach (var modContentPack in LoadedModManager.RunningMods) {
				var versionFile = VersionFile.TryParseVersionFile(modContentPack);
				var hasCheckerAssembly = false;
				foreach (var assembly in modContentPack.assemblies.loadedAssemblies) {
					if (assembly.GetName().Name == checkerAssemblyName) {
						hasCheckerAssembly = true;
						break;
					}
				}
				if (hasCheckerAssembly || versionFile != null) {
					relatedMods.Add(new LibaryRelatedMod(modContentPack.Name, versionFile));	
				}
			}
			return relatedMods;
		}

		private static bool LibraryIsLoaded(List<LibaryRelatedMod> relatedMods) {
			for (int i = 0; i < relatedMods.Count; i++) {
				var mod = relatedMods[i];
				if (mod.isLibrary) return true;
			}
			return false;
		}

		private static bool LibraryIsUpToDate(List<LibaryRelatedMod> relatedMods) {
			string consumerName;
			var libraryVersion = TryGetLibraryVersion(relatedMods);
			var requiredVersion = GetHighestRequiredLibraryVersion(relatedMods, out consumerName);
			return libraryVersion != null && libraryVersion >= requiredVersion;
		}

		private static string GetFirstLibraryConsumerName(List<LibaryRelatedMod> relatedMods) {
			for (int i = 0; i < relatedMods.Count; i++) {
				if (!relatedMods[i].isLibrary) return relatedMods[i].name;
			}
			return "";
		}

		private static Version GetHighestRequiredLibraryVersion(List<LibaryRelatedMod> relatedMods, out string consumerName) {
			var maxRequiredVersion = new Version();
			consumerName = "";
			for (int i = 0; i < relatedMods.Count; i++) {
				var mod = relatedMods[i];
				if (mod.isLibrary) continue;
				if (mod.file == null || mod.file.RequiredLibraryVersion == null) continue;
				if (maxRequiredVersion < mod.file.RequiredLibraryVersion) {
					consumerName = mod.name;
					maxRequiredVersion = mod.file.RequiredLibraryVersion;
				}
			}
			return maxRequiredVersion;
		}

		private static Version TryGetLibraryVersion(List<LibaryRelatedMod> relatedMods) {
			Version libraryVersion = null;
			for (int i = 0; i < relatedMods.Count; i++) {
				var mod = relatedMods[i];
				if (!mod.isLibrary) continue;
				if (mod.file == null) throw new Exception("Library is missing Version file");
				libraryVersion = mod.file.OverrideVersion;
			}
			return libraryVersion;
		}

		private static void ScheduleDialog(string message) {
			LongEventHandler.QueueLongEvent(() => {
				Find.WindowStack.Add(new Dialog_LibraryError(message));
			}, null, false, null);
		}
		
		private class LibaryRelatedMod {
			public readonly string name;
			public readonly VersionFile file;
			public readonly bool isLibrary;

			public LibaryRelatedMod(string name, VersionFile file) {
				this.name = name;
				this.file = file;
				isLibrary = name == LibraryModName;
			}
		}
	}
}