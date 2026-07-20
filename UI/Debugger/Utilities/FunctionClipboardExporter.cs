using Avalonia.Controls;
using Avalonia.Input.Platform;
using DataBoxControl;
using Mesen.Config;
using Mesen.Debugger.ViewModels;
using Mesen.Interop;
using Mesen.Localization;
using Mesen.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Mesen.Interop 与 Mesen.Debugger.ViewModels 都暴露了 CallerCalleeEntry，
// 本文件只使用调试器 ViewModel 的包装类，故在此消歧。
using CallerCalleeEntry = Mesen.Debugger.ViewModels.CallerCalleeEntry;

namespace Mesen.Debugger.Utilities
{
	// 把"函数列表 / 调用关系 / 断点记录 / 反汇编函数"复制为分块可读文本的逻辑集中于此。
	// - FunctionList：复制选中/全部函数行（+ 可选访问记录 + 汇编）。
	// - Caller/Callee：以树状分支展开 caller / callee，地址去重，depth 控制层数。
	// - 断点（反向）记录：复制反向面板行（+ caller/callee 树 + 可选访问 + 汇编）。
	// - 反汇编函数：复制光标所在单个函数的记录（+ 可选访问 + 汇编）。
	//
	// 复制首行统一为 "复制了{目标}的{记录/访问/汇编…}信息:" 便于粘贴后辨识来源。
	// 屏蔽过滤：
	// - 函数列表用其自身的"显示屏蔽"(ShowBlocked)；
	// - caller/callee 与断点记录用面板的"显示屏蔽"(ShowBlockedRanges)，
	//   未勾选时同时跳过被屏蔽函数与被屏蔽访问区间，做到"只复制看到的"。
	public static class FunctionClipboardExporter
	{
		public static void CopyFunctionList(FunctionListViewModel model, IEnumerable<FunctionNode> nodes, bool includeAccess, bool includeAssembly)
		{
			if(nodes == null) {
				return;
			}
			bool showBlocked = model.ShowBlocked;
			// 把当前实时（可能尚未保存）的屏蔽状态同步进缓存，使复制与界面显示一致。
			model.Debugger.SyncRangeMetaToCache();
			var list = nodes.Where(n => showBlocked || !n.IsBlocked).ToList();
			if(list.Count == 0) {
				return;
			}

			StringBuilder sb = new StringBuilder();
			string context = list.Count == 1 ? list[0].FunctionName : ResourceHelper.GetMessage("lblCopyCtxFunctions");
			AppendCopyHeader(sb, context, BuildParts(includeAccess, includeAssembly));
			if(model.Grid != null) {
				sb.Append(model.Grid.ConvertToText(list));
			}
			if(includeAccess || includeAssembly) {
				using var dv = new DisassemblyViewModel(model.Debugger, ConfigManager.Config.Debug, model.CpuType);
				var printedDetail = new HashSet<AddressInfo>();
				foreach(var node in list) {
					AppendFunctionDetail(sb, model.Debugger, model.CpuType, dv, node.FuncAbsAddr, node.FuncRelAddr, node.FunctionLength, node.AbsAddressDisplay, node.FunctionName, includeAccess, includeAssembly, "  ", false, printedDetail);
				}
			}
			Copy(sb.ToString());
		}

		// caller/callee 记录复制：每个 root 打印自身行，再以树状分支展开
		// Callers / Callees 子树（与调用关系图一致的排版），可选追加访问 / 汇编。
		public static void CopyCallerCallee(CallerCalleeViewModel model, IEnumerable<CallerCalleeEntry> roots, int depth, bool includeAccess, bool includeAssembly)
		{
			if(roots == null) {
				return;
			}
			bool showBlocked = model.ShowBlockedRanges;
			bool excludeBlockedRanges = !model.ShowBlockedRanges;
			// 把当前实时（可能尚未保存）的屏蔽状态同步进缓存，使复制与界面显示一致。
			model.Debugger.SyncRangeMetaToCache();
			var rootList = roots.Where(r => r.FuncAbsAddr.Address >= 0 && (showBlocked || !r.IsBlocked)).ToList();
			if(rootList.Count == 0) {
				return;
			}

			DataBox? grid = model.CallersGrid ?? model.CalleesGrid;
			StringBuilder sb = new StringBuilder();
			AppendCopyHeader(sb, model.SelectedFunctionName, BuildParts(includeAccess, includeAssembly));
			if(grid != null) {
				sb.AppendLine(grid.GetHeader());
			}

			using var dv = new DisassemblyViewModel(model.Debugger, ConfigManager.Config.Debug, model.CpuType);
			var printedDetail = new HashSet<AddressInfo>();
			foreach(var root in rootList) {
				sb.AppendLine(FormatCallerCalleeRow(grid, root));
				AppendFunctionDetail(sb, model.Debugger, model.CpuType, dv, root.FuncAbsAddr, root.FuncRelAddr, root.Node.FunctionLength, root.AbsAddressDisplay, root.FunctionName, includeAccess, includeAssembly, "  ", excludeBlockedRanges, printedDetail);
				AppendRecordSubtrees(sb, model, dv, grid, root.FuncAbsAddr, depth, showBlocked, includeAccess, includeAssembly, excludeBlockedRanges, printedDetail);
			}

			Copy(sb.ToString());
		}

		// ----- 断点记录（反向）复制 -----
		// 每个反向记录（函数）打印自身行，再树状展开 Callers / Callees 子树（depth 层），
		// 可选追加访问 / 汇编。
		public static void CopyReverseRecords(CallerCalleeViewModel model, IEnumerable<MemoryAccessFunctionEntry> nodes, int depth, bool includeAccess, bool includeAssembly)
		{
			if(nodes == null) {
				return;
			}
			bool showBlocked = model.ShowBlockedRanges;
			bool excludeBlockedRanges = !model.ShowBlockedRanges;
			// 把当前实时（可能尚未保存）的屏蔽状态同步进缓存，使复制与界面显示一致。
			model.Debugger.SyncRangeMetaToCache();
			var list = nodes.Where(e => showBlocked || !e.IsBlocked).ToList();
			if(list.Count == 0) {
				return;
			}

			DataBox? grid = model.ReverseGrid;
			DataBox? childGrid = model.CallersGrid ?? model.CalleesGrid;
			StringBuilder sb = new StringBuilder();
			AppendCopyHeader(sb, ReverseContext(model), BuildParts(includeAccess, includeAssembly));
			if(grid != null) {
				sb.AppendLine(grid.GetHeader());
			}
			using var dv = new DisassemblyViewModel(model.Debugger, ConfigManager.Config.Debug, model.CpuType);
			var printedDetail = new HashSet<AddressInfo>();
			foreach(var e in list) {
				string rowText = grid != null ? grid.FormatRow(e) : "";
				if(string.IsNullOrEmpty(rowText)) {
					rowText = BuildReverseRow(e);
				}
				sb.AppendLine(rowText);
				AppendFunctionDetail(sb, model.Debugger, model.CpuType, dv, e.FuncAbsAddr, e.FuncRelAddr, e.FunctionLength, e.AbsAddressDisplay, e.FunctionName, includeAccess, includeAssembly, "  ", excludeBlockedRanges, printedDetail);
				AppendRecordSubtrees(sb, model, dv, childGrid, e.FuncAbsAddr, depth, showBlocked, includeAccess, includeAssembly, excludeBlockedRanges, printedDetail);
			}
			Copy(sb.ToString());
		}

		// ----- 反汇编视图：复制光标所在函数 -----
		public static void CopyFunction(DebuggerWindowViewModel dbg, CpuType cpu, FunctionNode node, bool includeAccess, bool includeAssembly)
		{
			if(node.FuncAbsAddr.Address < 0) {
				return;
			}
			bool showBlocked = dbg.FunctionList?.ShowBlocked ?? ConfigManager.Config.Debug.Debugger.ShowBlockedFunctions;
			// 把当前实时（可能尚未保存）的屏蔽状态同步进缓存，使复制与界面显示一致。
			dbg.SyncRangeMetaToCache();
			if(!showBlocked && node.IsBlocked) {
				return;
			}

			StringBuilder sb = new StringBuilder();
			AppendCopyHeader(sb, node.FunctionName, BuildParts(includeAccess, includeAssembly));
			using var dv = new DisassemblyViewModel(dbg, ConfigManager.Config.Debug, cpu);
			sb.AppendLine(BuildCallerCalleeRow(new CallerCalleeEntry(node)));
			AppendFunctionDetail(sb, dbg, cpu, dv, node.FuncAbsAddr, node.FuncRelAddr, node.FunctionLength, node.AbsAddressDisplay, node.FunctionName, includeAccess, includeAssembly, "  ", false, new HashSet<AddressInfo>());
			Copy(sb.ToString());
		}

		// ----- ASCII 函数调用关系图 -----
		// 以选中函数为根，分别向下展开 Callers / Callees 两棵子树，使用制表符
		// 绘制分支。地址去重以避免环；depth 控制展开层数（复用 CopyDepth）。

		public static void CopyAsciiGraph(CallerCalleeViewModel model, IEnumerable<CallerCalleeEntry> roots, int depth)
		{
			bool showBlocked = model.ShowBlockedRanges;
			var rootList = roots.Where(r => r.FuncAbsAddr.Address >= 0 && (showBlocked || !r.IsBlocked)).ToList();
			if(rootList.Count == 0) {
				return;
			}

			StringBuilder sb = new StringBuilder();
			AppendCopyHeader(sb, model.SelectedFunctionName, ResourceHelper.GetMessage("lblCopyPartGraph"));
			sb.AppendLine(ResourceHelper.GetMessage("lblCopyGraphHeader"));
			// Callers / Callees 分别维护去重集合：两者是不同的子图，互不挤占深度，
			// 避免某一方向的深层展开被另一方向已访问的节点截断（例如 Callees 已
			// 展开 5 层后，Callers 的深层节点全部被标记为已访问而得不到展开）。
			var callersVisited = new HashSet<AddressInfo>();
			var calleesVisited = new HashSet<AddressInfo>();
			foreach(var root in rootList) {
				// 用 CallCountValue（数值）而非 CallCount（字符串）：缓存根节点 CallCount
				// 为空，但 CallCountValue 为 0，确保列头 count 列显示 0 而非空白。
				AppendAsciiGraphForRoot(sb, model, root.FuncAbsAddr, root.FunctionName, root.RelAddressDisplay, root.AbsAddressDisplay, root.CallCountValue.ToString(), depth, showBlocked, callersVisited, calleesVisited);
			}

			Copy(sb.ToString());
		}

		// 断点记录（反向）调用关系图：对每个访问过目标地址的函数展开 Callers / Callees 树。
		public static void CopyReverseAsciiGraph(CallerCalleeViewModel model, IEnumerable<MemoryAccessFunctionEntry> nodes, int depth)
		{
			bool showBlocked = model.ShowBlockedRanges;
			var list = nodes.Where(e => e.FuncAbsAddr.Address >= 0 && (showBlocked || !e.IsBlocked)).ToList();
			if(list.Count == 0) {
				return;
			}

			StringBuilder sb = new StringBuilder();
			AppendCopyHeader(sb, ReverseContext(model), ResourceHelper.GetMessage("lblCopyPartGraph"));
			sb.AppendLine(ResourceHelper.GetMessage("lblCopyGraphHeader"));
			// Callers / Callees 分别维护去重集合：两者是不同的子图，互不挤占深度。
			var callersVisited = new HashSet<AddressInfo>();
			var calleesVisited = new HashSet<AddressInfo>();
			foreach(var e in list) {
				AppendAsciiGraphForRoot(sb, model, e.FuncAbsAddr, e.FunctionName, e.RelAddressDisplay, e.AbsAddressDisplay, e.AccessCount.ToString(), depth, showBlocked, callersVisited, calleesVisited);
			}

			Copy(sb.ToString());
		}

		private static void AppendAsciiGraphForRoot(StringBuilder sb, CallerCalleeViewModel model, AddressInfo rootAddr, string name, string relDisplay, string absDisplay, string countDisplay, int depth, bool showBlocked, HashSet<AddressInfo> callersVisited, HashSet<AddressInfo> calleesVisited)
		{
			if(rootAddr.Address < 0) {
				return;
			}
			// 列格式：标签,cpuAddr,romAddr,count（与列头一致）。缓存函数 count 为 "0"。
			string rootLabel = $"{name},{relDisplay},{absDisplay},{countDisplay}";
			sb.AppendLine(rootLabel);

			// 把 root 自身加入两个集合：既避免环，也保证某函数若已作为其他 root
			// 的子树展开过，则此处只打印标签、不再重复展开其子树。Callers / Callees
			// 分别记录，使两个方向的展开深度互不挤占。
			callersVisited.Add(rootAddr);
			calleesVisited.Add(rootAddr);
			var callers = GetGraphChildren(model, rootAddr, true, showBlocked);
			if(callers.Count > 0) {
				sb.AppendLine("  Callers:");
				PrintGraph(sb, model, callers, "    ", depth, callersVisited, true, showBlocked);
			}
			var callees = GetGraphChildren(model, rootAddr, false, showBlocked);
			if(callees.Count > 0) {
				sb.AppendLine("  Callees:");
				PrintGraph(sb, model, callees, "    ", depth, calleesVisited, false, showBlocked);
			}
			sb.AppendLine();
		}

		private static List<(AddressInfo Addr, UInt64 CallCount)> GetGraphChildren(CallerCalleeViewModel model, AddressInfo addr, bool isCaller, bool showBlocked)
		{
			var record = DebugApi.GetCallerCallee(model.CpuType, addr);
			int count = isCaller ? (int)Math.Min(record.CallerCount, 64) : (int)Math.Min(record.CalleeCount, 64);
			var list = new List<(AddressInfo, UInt64)>();
			var seen = new HashSet<(int, MemoryType)>();
			for(int i = 0; i < count; i++) {
				var c = isCaller ? record.Callers[i] : record.Callees[i];
				if(c.Address.Address >= 0) {
					var node = model.GetFunctionNode(c.Address);
					if(showBlocked || !node.IsBlocked) {
						list.Add((c.Address, c.CallCount));
						seen.Add((c.Address.Address, c.Address.Type));
					}
				}
			}
			// 合并缓存（受 ShowCached 控制）：纯缓存节点或实时数据不全的节点，从
			// CallerCalleeCache 取结构继续按 depth 向下展开（次数为 0）。这样即使没有
			// 实时采样，也能从 FunctionList 刷新时批量快照的缓存中梳理出多层调用图。
			if(model.ShowCached && model.Debugger.CallerCalleeCache.TryGetValue(addr, out var cached)) {
				var refs = isCaller ? cached.Callers : cached.Callees;
				foreach(var r in refs) {
					var key = (r.Address, r.Type);
					if(seen.Contains(key)) {
						continue;
					}
					var a = new AddressInfo { Address = r.Address, Type = r.Type };
					var node = model.GetFunctionNode(a);
					if(showBlocked || !node.IsBlocked) {
						list.Add((a, 0));
						seen.Add(key);
					}
				}
			}
			return list;
		}

		private static void PrintGraph(StringBuilder sb, CallerCalleeViewModel model, List<(AddressInfo Addr, UInt64 CallCount)> nodes, string prefix, int depthLeft, HashSet<AddressInfo> visited, bool isCaller, bool showBlocked)
		{
			for(int i = 0; i < nodes.Count; i++) {
				bool isLast = i == nodes.Count - 1;
				string branch = isLast ? "└── " : "├── ";
				var (addr, callCount) = nodes[i];
				var node = model.GetFunctionNode(addr);
				// 列格式：标签,cpuAddr,romAddr,count（与列头一致）。缓存函数 count 为 0，
				// 不再因 callCount>0 守卫而省略，确保纯缓存调用图也能看到计数 0。
				string label = $"{node.FunctionName},{node.RelAddressDisplay},{node.AbsAddressDisplay},{callCount}";
				// 该函数已在别处展开过（跨 root 或同树内），此处不再重复展开其子树，
				// 用 ↩ 标记表示其调用关系已在上方完整呈现，避免调用图冗余。
				bool isNew = visited.Add(addr);
				if(!isNew && depthLeft > 1) {
					label += " ↩";
				}
				sb.AppendLine(prefix + branch + label);

				if(depthLeft > 1 && isNew) {
					var children = GetGraphChildren(model, addr, isCaller, showBlocked);
					string childPrefix = prefix + (isLast ? "    " : "│   ");
					PrintGraph(sb, model, children, childPrefix, depthLeft - 1, visited, isCaller, showBlocked);
				}
			}
		}

		// 打印某 root 的 Callers / Callees 子树（记录模式，节点为 CSV 行 + 可选访问/汇编）。
		private static void AppendRecordSubtrees(StringBuilder sb, CallerCalleeViewModel model, DisassemblyViewModel dv, DataBox? grid, AddressInfo rootAddr, int depth, bool showBlocked, bool includeAccess, bool includeAssembly, bool excludeBlockedRanges, HashSet<AddressInfo> printedDetail)
		{
			if(depth <= 0) {
				return;
			}
			var visited = new HashSet<AddressInfo> { rootAddr };
			var callers = GetGraphChildren(model, rootAddr, true, showBlocked);
			if(callers.Count > 0) {
				sb.AppendLine("  Callers:");
				PrintRecordBranch(sb, model, dv, grid, callers, "    ", depth, visited, true, showBlocked, includeAccess, includeAssembly, excludeBlockedRanges, printedDetail);
			}
			var callees = GetGraphChildren(model, rootAddr, false, showBlocked);
			if(callees.Count > 0) {
				sb.AppendLine("  Callees:");
				PrintRecordBranch(sb, model, dv, grid, callees, "    ", depth, visited, false, showBlocked, includeAccess, includeAssembly, excludeBlockedRanges, printedDetail);
			}
		}

		private static void PrintRecordBranch(StringBuilder sb, CallerCalleeViewModel model, DisassemblyViewModel dv, DataBox? grid, List<(AddressInfo Addr, UInt64 CallCount)> nodes, string prefix, int depthLeft, HashSet<AddressInfo> visited, bool isCaller, bool showBlocked, bool includeAccess, bool includeAssembly, bool excludeBlockedRanges, HashSet<AddressInfo> printedDetail)
		{
			for(int i = 0; i < nodes.Count; i++) {
				bool isLast = i == nodes.Count - 1;
				string branch = isLast ? "└── " : "├── ";
				var (addr, callCount) = nodes[i];
				var node = model.GetFunctionNode(addr);
				var entry = new CallerCalleeEntry(node) { CallCount = callCount.ToString(), CallCountValue = callCount };
				sb.AppendLine(prefix + branch + FormatCallerCalleeRow(grid, entry));

				string childPrefix = prefix + (isLast ? "    " : "│   ");
				AppendFunctionDetail(sb, model.Debugger, model.CpuType, dv, node.FuncAbsAddr, node.FuncRelAddr, node.FunctionLength, node.AbsAddressDisplay, node.FunctionName, includeAccess, includeAssembly, childPrefix, excludeBlockedRanges, printedDetail);

				if(depthLeft > 1 && visited.Add(addr)) {
					var children = GetGraphChildren(model, addr, isCaller, showBlocked);
					PrintRecordBranch(sb, model, dv, grid, children, childPrefix, depthLeft - 1, visited, isCaller, showBlocked, includeAccess, includeAssembly, excludeBlockedRanges, printedDetail);
				}
			}
		}

		private static void AppendFunctionDetail(StringBuilder sb, DebuggerWindowViewModel dbg, CpuType cpu, DisassemblyViewModel dv, AddressInfo absAddr, AddressInfo relAddr, uint length, string addrDisplay, string name, bool includeAccess, bool includeAssembly, string indent, bool excludeBlockedRanges, HashSet<AddressInfo> printedDetail)
		{
			// 同一函数可能作为多个 root / 子树的 caller、callee 反复出现，其访问/汇编
			// 信息完全相同。每个函数只输出一次详细块，避免复制结果大量重复。
			if(includeAccess || includeAssembly) {
				if(!printedDetail.Add(absAddr)) {
					return;
				}
			}

			if(includeAccess) {
				var access = DebugApi.GetFunctionMemoryAccess(cpu, absAddr);
				if(access != null) {
					// 实时 C++ 快照对每个区间都返回 Blocked=false；屏蔽/颜色状态的权威
					// 来源是 FuncMetaCache。先把它叠加到实时区间上，否则"显示屏蔽区间"
					// 关闭时复制仍会带上被屏蔽的区间。
					var cached = dbg.GetFuncMeta(absAddr)?.MemoryAccess;
					if(cached?.Ranges != null) {
						// 实时区间可能按 stride/标志拆成多段，而缓存是合并后的连续区间，
						// 故用地址重叠判定屏蔽，避免按 Start 精确匹配漏掉被拆出的子段。
						var blockedByType = cached.Ranges
							.Where(c => c.Blocked)
							.GroupBy(c => c.MemType)
							.ToDictionary(g => g.Key, g => g.Select(c => (c.Start, c.End)).ToList());
						foreach(var r in access.Ranges) {
							if(blockedByType.TryGetValue(r.MemType, out var spans)) {
								if(spans.Any(s => s.Start <= r.End && s.End >= r.Start)) {
									r.Blocked = true;
								}
							}
							var color = cached.Ranges.FirstOrDefault(c => c.MemType == r.MemType && c.Start == r.Start && c.RangeColor != null);
							if(color != null) {
								r.RangeColor = color.RangeColor;
							}
						}
					}
					var ranges = excludeBlockedRanges ? access.Ranges.Where(r => !r.Blocked).ToList() : access.Ranges;
					if(ranges.Count > 0) {
						sb.AppendLine($"{indent}=== Access: {name} ({addrDisplay}) ===");
						sb.AppendLine($"{indent}地址,R/W,类型,读,写,总");
						foreach(var r in ranges) {
							var vm = new AccessRangeViewModel(r, cpu, dbg, absAddr);
							sb.AppendLine($"{indent}  {vm.RangeDisplay},{vm.RwDisplay},{vm.MemTypeDisplay},{vm.ReadCountDisplay},{vm.WriteCountDisplay},{vm.AccessCountDisplay}");
						}
					}
				}
			}

			if(includeAssembly && length > 0) {
				// 汇编为空时不输出 "=== Assembly ===" 头，避免与上一行重复的信息冗余。
				List<string> lines;
				if(absAddr.Address >= 0) {
					// 始终从 ROM 绝对地址反汇编：相对地址反汇编依赖当前 bank 映射，
					// 若当前 bank 与函数实际所在 bank 不符（bank 切换后），会读到错误
					// 代码（例如函数实际在 06 但当前映射到 03，则复制出 03 处的垃圾指令）。
					// 绝对地址唯一确定了 ROM 中的代码，地址列用 bank:offset 风格。
					lines = BuildAbsoluteDisassembly(cpu, absAddr, length);
				} else if(relAddr.Address >= 0 && dv != null) {
					// 函数无绝对地址（极少数情况）才退回到相对地址反汇编。
					lines = dv.GetFunctionDisassembly(relAddr.Address, length).Split('\n').Where(l => !string.IsNullOrEmpty(l)).ToList();
				} else {
					lines = new List<string>();
				}
				if(lines.Count > 0) {
					sb.AppendLine($"{indent}=== Assembly: {name} ({addrDisplay}) ===");
					foreach(var line in lines) {
						sb.AppendLine($"{indent}  {line}");
					}
				}
			}
		}

		// grid 未渲染（例如面板隐藏，_rowsPresenter 为空）时 FormatRow 返回空，回退到手工拼行。
		// 函数无 CPU（相对）地址时，直接从 ROM 绝对地址区间反汇编。字节/助记符始终正确；
		// ROM 地址作为合成 PC 喂给反汇编引擎，故 PC 相对分支目标在 ROM 空间内自洽，与
		// 地址列一致。地址列用 bank:offset（RelAddressDisplay 风格），其 bank 来自 JSON
		// 缓存的页（MemoryHelper.GetPage），offset 为 ROM 地址。复制选项（地址/字节码/
		// 注释/块头）与正常复制路径一致。
		private static List<string> BuildAbsoluteDisassembly(CpuType cpu, AddressInfo absAddr, uint length)
		{
			DebuggerConfig cfg = ConfigManager.Config.Debug.Debugger;
			bool getAddresses = cfg.CopyAddresses;
			bool getByteCode = cfg.CopyByteCode;
			bool getComments = cfg.CopyComments;
			bool getHeaders = cfg.CopyBlockHeaders;

			// 单行最多 1 字节指令，故行数上界 = length；上限 8192 行以避免超大数组分配。
			uint rowCount = Math.Min(length, 8192u);
			InteropCodeLineData[] raw = DebugApi.GetDisassemblyOutputForAbsoluteRange(cpu, absAddr.Type, (uint)absAddr.Address, length, rowCount);
			if(raw.Length == 0) {
				return new List<string>();
			}

			// 预计算地址列文本与最大宽度，保证对齐。
			var addrTexts = new string[raw.Length];
			int maxAddrWidth = 0;
			for(int i = 0; i < raw.Length; i++) {
				CodeLineData lineData = new CodeLineData(raw[i]);
				if(getAddresses && lineData.AbsoluteAddress.Address >= 0) {
					int page = MemoryHelper.GetPage(lineData.AbsoluteAddress, cpu);
					addrTexts[i] = page >= 0
						? page.ToString("X2") + ":" + lineData.AbsoluteAddress.Address.ToString("X" + cpu.GetAddressSize())
						: lineData.AbsoluteAddress.Address.ToString("X" + cpu.GetAddressSize());
				} else {
					addrTexts[i] = "";
				}
				if(addrTexts[i].Length > maxAddrWidth) {
					maxAddrWidth = addrTexts[i].Length;
				}
			}

			var result = new List<string>(raw.Length);
			for(int i = 0; i < raw.Length; i++) {
				CodeLineData lineData = new CodeLineData(raw[i]);
				string codeString = lineData.Text.Trim();

				if(lineData.Flags.HasFlag(LineFlags.BlockEnd) || lineData.Flags.HasFlag(LineFlags.BlockStart)) {
					if(!getHeaders) {
						continue;
					}
					codeString = "--------" + codeString + "--------";
				}

				bool indentText = !(lineData.Flags.HasFlag(LineFlags.ShowAsData)
					|| lineData.Flags.HasFlag(LineFlags.BlockStart)
					|| lineData.Flags.HasFlag(LineFlags.BlockEnd)
					|| lineData.Flags.HasFlag(LineFlags.Label)
					|| (lineData.Flags.HasFlag(LineFlags.Comment) && lineData.Text.Length == 0));
				string line = (indentText ? "  " : "") + codeString;

				if(getByteCode) {
					line = lineData.ByteCodeStr.PadRight(13) + line;
				}
				if(getAddresses) {
					line = addrTexts[i].PadRight(maxAddrWidth) + "  " + line;
				}
				if(getComments && !string.IsNullOrWhiteSpace(lineData.Comment)) {
					line = line + lineData.Comment;
				}

				// 与正常复制一致：跳过跳转/子程序自动生成的 "$" 标签行，以及空行。
				bool skipLine = lineData.Flags.HasFlag(LineFlags.Label) && lineData.Text.StartsWith("$");
				string trimmed = line.TrimEnd();
				if(!skipLine && trimmed.Length > 0) {
					result.Add(trimmed);
				}
			}
			return result;
		}

		private static string FormatCallerCalleeRow(DataBox? grid, CallerCalleeEntry entry)
		{
			string row = grid != null ? grid.FormatRow(entry) : "";
			return string.IsNullOrEmpty(row) ? BuildCallerCalleeRow(entry) : row;
		}

		private static string BuildCallerCalleeRow(CallerCalleeEntry entry)
		{
			return $"{entry.FunctionName},{entry.RelAddressDisplay},{entry.AbsAddressDisplay},{entry.CallCount}";
		}

		private static string BuildReverseRow(MemoryAccessFunctionEntry entry)
		{
			return $"{entry.FunctionName},{entry.RelAddressDisplay},{entry.AbsAddressDisplay},{entry.AccessCount},{entry.RweDisplay}";
		}

		// 断点记录模式的上下文名：优先断点标题，否则选中函数名。
		private static string ReverseContext(CallerCalleeViewModel model)
		{
			return !string.IsNullOrEmpty(model.SelectedBreakpointTitle) ? model.SelectedBreakpointTitle : model.SelectedFunctionName;
		}

		// 复制内容各段名称（记录 / 访问 / 汇编），用于首行信息拼装。
		private static string[] BuildParts(bool includeAccess, bool includeAssembly)
		{
			var parts = new List<string> { ResourceHelper.GetMessage("lblCopyPartRecords") };
			if(includeAccess) {
				parts.Add(ResourceHelper.GetMessage("lblCopyPartAccess"));
			}
			if(includeAssembly) {
				parts.Add(ResourceHelper.GetMessage("lblCopyPartAssembly"));
			}
			return parts.ToArray();
		}

		// 复制首行："复制了{目标}的{记录/访问/汇编…}信息:"
		private static void AppendCopyHeader(StringBuilder sb, string? context, params string[] parts)
		{
			string ctx = string.IsNullOrEmpty(context) ? "-" : context;
			string joined = string.Join("/", parts.Where(p => !string.IsNullOrEmpty(p)));
			sb.AppendLine(ResourceHelper.GetMessage("msgCopyInfoHeader", ctx, joined));
		}

		private static void Copy(string text)
		{
			if(!string.IsNullOrEmpty(text)) {
				ApplicationHelper.GetMainWindow()?.Clipboard?.SetTextAsync(text);
			}
		}
	}
}
