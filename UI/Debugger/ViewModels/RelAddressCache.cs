using Mesen.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Mesen.Debugger.ViewModels
{
	public class RelAddressCacheData
	{
		// NES mapper-level PRG page size (0x2000/0x4000 etc.), -1 for non-NES.
		// Omitted from JSON when -1 (non-NES).
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public int PrgPageSize { get; set; } = -1;

		// NES mapper-level CHR page size (0x2000/0x1000 etc.), -1 for non-NES or no CHR-ROM.
		// Omitted from JSON when -1.
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public int ChrPageSize { get; set; } = -1;

		public Dictionary<CpuType, List<RelAddressCacheEntry>> CacheByCpu { get; set; } = new();
	}

	[Flags]
	public enum RwFlags : byte
	{
		None = 0,
		Read = 1,
		Write = 2,
		Execute = 4,
		ReadWrite = Read | Write,
		ReadWriteExec = Read | Write | Execute
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

		// ROM 区间的相对地址拆分（page + 相对地址），与 RelAddressCacheEntry 同构。
		// -1 表示无页，通过 *Ser shadow prop 省略写入。
		private int _relPage = -1;
		[JsonIgnore]
		public int RelPage { get => _relPage; set => _relPage = value; }
		[JsonPropertyName("RelPage")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public int? RelPageSer { get => _relPage < 0 ? null : _relPage; set => _relPage = value ?? -1; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public int? RelAddress { get; set; }

		// Range color/block state — persisted in JSON so it survives window
		// close/reopen, same as FuncMeta for functions.
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? RangeColor { get; set; }
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public bool Blocked { get; set; }

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
				.Select(r => (MemType: r.MemType, Start: r.Start, End: r.End, Interval: r.Interval, Flags: r.Flags, RelPage: r.RelPage, RelAddress: r.RelAddress, RangeColor: r.RangeColor, Blocked: r.Blocked))
				.OrderBy(x => (int)x.MemType)
				.ThenBy(x => x.Start)
				.ToList();

			const int maxSpans = 4096; // hard bound on cache size (pathological disjoint access)
			var merged = new List<AccessRange>();
			var cur = norm[0];
			for(int i = 1; i < norm.Count; i++) {
				var n = norm[i];
				bool sameSpan = n.MemType == cur.MemType && n.Interval == cur.Interval && n.Start <= cur.End + 1;
				if(sameSpan) {
					// Contiguous/overlapping within the same type & stride: extend
					// the span, OR the R/W flags, and carry the color/block state
					// (blocked if any part is blocked; first non-null color wins).
					cur.End = Math.Max(cur.End, n.End);
					cur.Flags |= n.Flags;
					cur.Blocked |= n.Blocked;
					cur.RangeColor ??= n.RangeColor;
				} else {
					merged.Add(MakeSpan(cur));
					cur = n;
				}
			}
			merged.Add(MakeSpan(cur));

			if(merged.Count > maxSpans) {
				merged = merged.GetRange(0, maxSpans);
			}
			result.Ranges = merged;
			return result;
		}

		private static AccessRange MakeSpan((MemoryType MemType, uint Start, uint End, uint Interval, RwFlags Flags, int RelPage, int? RelAddress, string? RangeColor, bool Blocked) s)
		{
			// Persisted union is always a contiguous span (stride collapsed to 1).
			// RelPage/RelAddress are carried forward from the span's start so the
			// ROM display survives the merge without recomputing on reload.
			// Color/block state is preserved so the cached blocking survives merges.
			return new AccessRange {
				Start = s.Start,
				Length = s.End - s.Start + 1,
				Interval = 1,
				MemType = s.MemType,
				Flags = s.Flags,
				RelPage = s.RelPage,
				RelAddress = s.RelAddress,
				RangeColor = s.RangeColor,
				Blocked = s.Blocked
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

		// NES 专属：函数被调用时刻的 PRG bank→page 映射快照集合（增量去重）。
		// 每个元素是一个 int 列表：索引=bank slot，值=PRG ROM page index。
		// PrgPageSize 决定了 slot 数量 = 0x8000 / PrgPageSize。
		// 非 NES 时 null，JSON 省略。
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public List<List<int>>? PrgMapSnapshots { get; set; }

		// NES 专属：函数被调用时刻的 CHR bank→page 映射快照集合（增量去重）。
		// 仅在卡带含 CHR-ROM 时记录。
		// 每个元素是一个 int 列表：索引=bank slot，值=CHR ROM page index。
		// ChrPageSize 决定了 slot 数量 = 0x2000 / ChrPageSize。
		// 非 NES 或无 CHR-ROM 时 null，JSON 省略。
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public List<List<int>>? ChrMapSnapshots { get; set; }

		// 调用关系（仅结构，不含次数）：调用本函数的函数 / 本函数调用的函数。
		// 次数来自运行时 profiler 采样，随每次运行变化，缓存无意义，故不持久化。
		// 双向都存（每条边出现两次），读取直接、逻辑简单。
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public List<CallerCalleeRef>? Callers { get; set; }
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public List<CallerCalleeRef>? Callees { get; set; }
	}

	/// <summary>调用关系中的一个函数引用：仅缓存绝对地址与 page（不缓存次数）。</summary>
	public class CallerCalleeRef
	{
		public int Address { get; set; }
		public MemoryType Type { get; set; }
		// 函数的 rel page；-1 时省略以精简序列化。
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public int Page { get; set; } = -1;
	}

	/// <summary>反向（断点记录）内存访问的一条持久化记录：某连续地址区间，及其访问它的函数列表。区间合并自断点的 Length，函数列表嵌套避免逐函数膨胀。</summary>
	public class ReverseAccessEntry
	{
		public int StartAddr { get; set; }
		public int EndAddr { get; set; }
		public MemoryType MemType { get; set; }
		// 访问该区间的函数集合（嵌套），取代逐函数平铺，大幅精简 JSON。
		public List<ReverseAccessFunc> Functions { get; set; } = new();
	}

	/// <summary>区间内的一个访问函数及其 r/w/e 标记。</summary>
	public class ReverseAccessFunc
	{
		public int FuncAddress { get; set; }
		public MemoryType FuncType { get; set; }
		// 读写执行标记：默认 0 时省略以精简序列化（记录项恒 >= 1）。
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public RwFlags Flags { get; set; }
	}

	/// <summary>反向内存访问的 JSON 缓存（按 CpuType 分桶，镜像 RelAddressCacheData）。</summary>
	public class ReverseAccessCacheData
	{
		public Dictionary<CpuType, List<ReverseAccessEntry>> EntriesByCpu { get; set; } = new();
	}
}
