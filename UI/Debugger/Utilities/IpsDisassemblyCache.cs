using Mesen.Interop;
using Mesen.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Mesen.Debugger.Utilities
{
	/// <summary>
	/// JSON persistence for IPS patch disassembly text, keyed by (MemoryType, TargetOffset).
	/// Stored as &lt;ROM_PATH&gt;_ips.json alongside the ROM file.
	/// </summary>
	public class IpsDisassemblyCache
	{
		public List<IpsDisassemblyEntry> Records { get; set; } = new();

		/// <summary>Load cache from disk, or return an empty cache if not found.</summary>
		public static IpsDisassemblyCache Load(string romPath)
		{
			if(string.IsNullOrEmpty(romPath)) {
				return new IpsDisassemblyCache();
			}

			string jsonPath = romPath + "_ips.json";
			if(!File.Exists(jsonPath)) {
				return new IpsDisassemblyCache();
			}

			try {
				string json = File.ReadAllText(jsonPath);
				return JsonSerializer.Deserialize(json, typeof(IpsDisassemblyCache), MesenSerializerContext.Default) as IpsDisassemblyCache
					?? new IpsDisassemblyCache();
			} catch {
				return new IpsDisassemblyCache();
			}
		}

		/// <summary>Save cache to &lt;ROM_PATH&gt;_ips.json.</summary>
		public void Save(string romPath)
		{
			if(string.IsNullOrEmpty(romPath)) {
				return;
			}

			string jsonPath = romPath + "_ips.json";
			try {
				string json = JsonSerializer.Serialize(this, typeof(IpsDisassemblyCache), MesenSerializerContext.Default);
				File.WriteAllText(jsonPath, json);
			} catch {
				// best-effort persistence; swallow errors
			}
		}

		/// <summary>Get cached disassembly text for a record, or null if not cached.</summary>
		public string? Get(MemoryType memType, int offset)
		{
			foreach(var entry in Records) {
				if(entry.TargetMemory == memType && entry.TargetOffset == offset) {
					return entry.AssemblyText;
				}
			}
			return null;
		}

		/// <summary>Set (add or replace) cached disassembly text for a record.</summary>
		public void Set(MemoryType memType, int offset, string text)
		{
			for(int i = 0; i < Records.Count; i++) {
				if(Records[i].TargetMemory == memType && Records[i].TargetOffset == offset) {
					Records[i].AssemblyText = text;
					return;
				}
			}
			Records.Add(new IpsDisassemblyEntry {
				TargetMemory = memType,
				TargetOffset = offset,
				AssemblyText = text
			});
		}
	}

	public class IpsDisassemblyEntry
	{
		public MemoryType TargetMemory { get; set; }
		public int TargetOffset { get; set; }
		public string AssemblyText { get; set; } = "";
	}
}
