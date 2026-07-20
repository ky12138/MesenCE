using Mesen.Config;
using Mesen.Debugger;
using Mesen.Debugger.Labels;
using Mesen.Debugger.Utilities;
using Mesen.Debugger.ViewModels;
using Mesen.Utilities;
using Mesen.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mesen.Utilities
{
	public static class JsonHelper
	{
		public static T Clone<T>(T obj) where T : notnull
		{
			using MemoryStream stream = new MemoryStream();
			byte[] jsonData = JsonSerializer.SerializeToUtf8Bytes(obj, obj.GetType(), MesenSerializerContext.Default);
			T? clone = (T?)JsonSerializer.Deserialize(jsonData.AsSpan<byte>(), obj.GetType(), MesenSerializerContext.Default);
			if(clone == null) {
				throw new Exception("Invalid object");
			}
			return clone;
		}
	}
}

[JsonSerializable(typeof(Configuration))]
[JsonSerializable(typeof(Breakpoint))]
[JsonSerializable(typeof(CodeLabel))]
[JsonSerializable(typeof(GameDipSwitches))]
[JsonSerializable(typeof(CheatCodes))]
[JsonSerializable(typeof(GameConfig))]
[JsonSerializable(typeof(DebugWorkspace))]
[JsonSerializable(typeof(UpdateInfo))]
[JsonSerializable(typeof(RelAddressCacheData))]
[JsonSerializable(typeof(FuncMemoryAccess))]
[JsonSerializable(typeof(AccessRange))]
[JsonSerializable(typeof(RwFlags))]
[JsonSerializable(typeof(ReverseAccessCacheData))]
[JsonSerializable(typeof(ReverseAccessEntry))]
[JsonSerializable(typeof(ReverseAccessFunc))]
[JsonSourceGenerationOptions(
	WriteIndented = true,
	IgnoreReadOnlyProperties = true,
	UseStringEnumConverter = true
)]
public partial class MesenSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(DocEntryViewModel[]))]
[JsonSerializable(typeof(DocFileFormat))]
[JsonSerializable(typeof(CheatDatabase))]
[JsonSourceGenerationOptions(
	WriteIndented = true,
	IgnoreReadOnlyProperties = true,
	UseStringEnumConverter = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
public partial class MesenCamelCaseSerializerContext : JsonSerializerContext { }
