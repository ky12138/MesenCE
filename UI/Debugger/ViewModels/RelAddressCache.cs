using Mesen.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Mesen.Debugger.ViewModels
{
	public class RelAddressCacheData
	{
		public Dictionary<CpuType, List<RelAddressCacheEntry>> CacheByCpu { get; set; } = new();
	}

	[Flags]
	public enum RwFlags : byte
	{
		None = 0,
		Read = 1,
		Write = 2,
		ReadWrite = Read | Write
	}

	/// <summary>Stable identity of an access range, independent of live counters.</summary>
	public readonly record struct RangeIdentity(MemoryType MemType, uint Start, uint Length, RwFlags Flags, uint Interval);

	/// <summary>连续地址区间 + 读写标记 + 内存类型（合并去重后的结果）</summary>
	public class AccessRange
	{
		public UInt32 Start { get; set; }

		// Length / Interval both default to 1 (a single, contiguous address). To
		// keep the persisted JSON compact the redundant "Length":1 / "Interval":1
		// are omitted on write (via the *Ser shadow props) and restored to 1 on
		// read (a missing key leaves the backing field at its default of 1).
		private UInt32 _length = 1;
		[JsonIgnore]
		public UInt32 Length { get => _length; set => _length = value; }
		[JsonPropertyName("Length")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public UInt32? LengthSer { get => _length == 1 ? null : _length; set => _length = value ?? 1; }

		private UInt32 _interval = 1;
		// Stride between consecutive addresses of a merged run. 1 = contiguous.
		// Only set by live tracking; the persisted union flattens to contiguous
		// spans (Interval = 1) so cached data stays compact.
		[JsonIgnore]
		public UInt32 Interval { get => _interval; set => _interval = value; }
		[JsonPropertyName("Interval")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public UInt32? IntervalSer { get => _interval == 1 ? null : _interval; set => _interval = value ?? 1; }

		public RwFlags Flags { get; set; }
		public MemoryType MemType { get; set; }

		// ROM 区间的相对地址拆分（page + 相对地址），与 RelAddressCacheEntry 同构：
		// 只存两个整数，显示串由 MemoryHelper.FormatRelDisplay 在展示时重建，不再
		// 把格式化字符串写入 JSON。-1 / null 时分别按 WhenWritingDefault /
		// WhenWritingNull 省略以精简序列化。
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public int RelPage { get; set; } = -1;
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public int? RelAddress { get; set; }

		// Access counters — only populated by live tracking while a function is
		// marked. Deliberately NOT serialized into the JSON cache.
		[JsonIgnore]
		public UInt32 ReadCount { get; set; }
		[JsonIgnore]
		public UInt32 WriteCount { get; set; }
		[JsonIgnore]
		public UInt32 AccessCount { get; set; }

		// ---- Span geometry (computed; single source of truth) ----
		// Previously duplicated in AccessRangeViewModel and Union. All [JsonIgnore].

		// Stride between addresses; a 0 stride is treated as contiguous (1).
		[JsonIgnore]
		public UInt32 EffectiveInterval => Interval > 0 ? Interval : 1;

		// Last covered address: Start + (Length - 1) * stride (not Start + Length - 1).
		[JsonIgnore]
		public UInt32 End => Start + (Length - 1) * EffectiveInterval;

		// Address count of [Start, End]; 0 for the empty range (Length == 0),
		// which also serves as the breakpoint size when there is no span.
		[JsonIgnore]
		public UInt32 SpanLength => Length == 0 ? 0 : End - Start + 1;

		// Stable identity (independent of live counts) used as the key that maps a
		// range to its view-model instance so refreshes preserve drill-down state.
		[JsonIgnore]
		public RangeIdentity Identity => new(MemType, Start, Length, Flags, Interval);
	}

	/// <summary>函数对内存/ROM 的读写访问记录（合并后）</summary>
	public class FuncMemoryAccess
	{
		public List<AccessRange> Ranges { get; set; } = new();
		// True only for live (C++-sourced) snapshots; the persisted union is false.
		// Omitted on write when false so cached data stays compact.
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public bool Sampled { get; set; }

		// Merge two access snapshots into a cumulative union of every address
		// touched by either side. Counts are intentionally dropped — the JSON
		// cache stores spans (Start/Length/MemType/Flags) only, not frequencies,
		// so a cached entry always reports 0 counts. This makes the cache a
		// full record of all addresses a function has ever read/written across
		// sessions, while staying compact (contiguous/overlapping spans merge).
		public static FuncMemoryAccess Union(FuncMemoryAccess? a, FuncMemoryAccess? b)
		{
			var result = new FuncMemoryAccess();
			var spans = new List<AccessRange>();
			if(a != null) spans.AddRange(a.Ranges);
			if(b != null) spans.AddRange(b.Ranges);
			if(spans.Count == 0) {
				return result;
			}

			// Normalize to [Start, End] (End accounts for any stride) and sort by
			// (MemType, Start) so spans of the same memory type are adjacent.
			var norm = spans
				.Select(r => (MemType: r.MemType, Start: r.Start, End: r.End, Interval: r.Interval, Flags: r.Flags, RelPage: r.RelPage, RelAddress: r.RelAddress))
				.OrderBy(x => (int)x.MemType)
				.ThenBy(x => x.Start)
				.ToList();

			const int MaxSpans = 4096; // hard bound on cache size (pathological disjoint access)
			var merged = new List<AccessRange>();
			var cur = norm[0];
			for(int i = 1; i < norm.Count; i++) {
				var n = norm[i];
				bool sameSpan = n.MemType == cur.MemType && n.Interval == cur.Interval && n.Start <= cur.End + 1;
				if(sameSpan) {
					// Contiguous/overlapping within the same type & stride: extend
					// the span and OR the R/W flags.
					cur.End = Math.Max(cur.End, n.End);
					cur.Flags |= n.Flags;
				} else {
					merged.Add(MakeSpan(cur));
					cur = n;
				}
			}
			merged.Add(MakeSpan(cur));

			if(merged.Count > MaxSpans) {
				merged = merged.GetRange(0, MaxSpans);
			}
			result.Ranges = merged;
			return result;
		}

		private static AccessRange MakeSpan((MemoryType MemType, uint Start, uint End, uint Interval, RwFlags Flags, int RelPage, int? RelAddress) s)
		{
			// Persisted union is always a contiguous span (stride collapsed to 1).
			// RelPage/RelAddress are carried forward from the span's start so the
			// ROM display survives the merge without recomputing on reload.
			return new AccessRange {
				Start = s.Start,
				Length = s.End - s.Start + 1,
				Interval = 1,
				MemType = s.MemType,
				Flags = s.Flags,
				RelPage = s.RelPage,
				RelAddress = s.RelAddress
			};
		}
	}

	public class RelAddressCacheEntry
	{
		public int Address { get; set; }
		public MemoryType Type { get; set; }

		// 函数的 page，由 absAddr 强绑定算出（与函数身份一致，随 ROM 失效）。
		// 写入 JSON 缓存，供跨会话稳定复用；-1 时省略以精简序列化。
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public int RelPage { get; set; } = -1;

		// 函数的相对地址（缓存值）。null 表示该函数无相对地址。用可空类型而非
		// -1 哨兵，是因为 0 本身是一个合法的相对地址：若用 int + WhenWritingDefault
		// 省略，反序列化回落到默认 0 会被误判为有效地址。null 时按 WhenWritingNull
		// 省略以精简序列化。显示串（"page:$relAddr"）已由 RelPage + RelAddress 在
		// 读取/展示时重建，不再把格式化字符串写入 JSON。
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public int? RelAddress { get; set; }

		// 函数标记元数据（并入同一份 JSON 缓存）
		// Null/default fields are omitted on write to keep the JSON compact;
		// they deserialize back to null/false as appropriate.
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? FunctionColor { get; set; }   // 固定色名 或 "#RRGGBB"
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public bool Blocked { get; set; }             // 是否 block 屏蔽
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public bool Marked { get; set; }              // 是否特殊标记（监视）
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public FuncMemoryAccess? MemoryAccess { get; set; } // 标记后采样的读写记录
	}
}
