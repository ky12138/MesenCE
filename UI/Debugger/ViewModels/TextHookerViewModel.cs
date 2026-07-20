using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Mesen.Config;
using Mesen.Interop;
using Mesen.Utilities;
using Mesen.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Mesen.Debugger.ViewModels
{
	public partial class TextHookerViewModel : DisposableViewModel
	{
		[ObservableProperty] public partial DynamicBitmap NametableBitmap { get; private set; }
		[ObservableProperty] public partial DynamicBitmap ChrBitmap { get; private set; }
		[ObservableProperty] public partial string ExtractedText { get; set; } = "";

		[ObservableProperty] public partial int ChrSelectionIndex { get; set; }

		public TextHookerConfig Config { get; }

		public ObservableCollection<string> ChrBankOptions { get; set; } = new();
		public ObservableCollection<TextHookerMappingRowViewModel> MappingRows { get; } = new();
		public ObservableCollection<string> ColumnHeaders { get; } = new();

		private object _updateLock = new();
		private bool _refreshPending = false;

		private byte[] _ppuMemory = Array.Empty<byte>();
		private byte[] _prevPpuMemory = Array.Empty<byte>();
		private string? _prevText;
		private int _prevXScroll = -1;
		private int _prevYScroll = -1;
		private NesPpuState _ppuState;
		private NesMirroringType _mirroring;
		private int _xScroll, _yScroll;

		private ConcurrentDictionary<string, string> _charMappings = new();
		private static Dictionary<string, string> _defaultCharMappings = new();
		private static bool _defaultMappingsLoaded = false;

		private byte[][] _tileData = new byte[16][];
		private byte[] _chrData = new byte[16 * 16 * 16];
		private byte[] _prevChrData = new byte[16 * 16 * 16];
		private DynamicBitmap[] _tileBitmaps = new DynamicBitmap[256];
		private bool _chrSizeDirty = true;
		private int _chrMaxBank = 0;
		private bool _hasChrRom = false;

		private static readonly uint[] HexLookup = InitializeHexLookup();

		[Obsolete("For designer only")]
		public TextHookerViewModel() : this(new TextHookerConfig())
		{
		}

		public TextHookerViewModel(TextHookerConfig config)
		{
			Config = config.Clone();
			NametableBitmap = new DynamicBitmap(new PixelSize(512, 480), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
			ChrBitmap = new DynamicBitmap(new PixelSize(128, 128), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

			for(int i = 0; i < 16; i++) {
				_tileData[i] = new byte[16];
			}

			// Pre-allocate the 256 tile bitmaps once and reuse them on every refresh to avoid GC pressure
			for(int i = 0; i < 256; i++) {
				_tileBitmaps[i] = new DynamicBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
			}

			LoadDefaultMappings();

			// Pre-populate CHR bank options so ComboBox shows text immediately
			ChrBankOptions.Add("PPU: $0000");
			ChrBankOptions.Add("PPU: $1000");

			// Column headers (0-F) for the 16x16 mapping grid
			for(int c = 0; c < 16; c++) {
				ColumnHeaders.Add(c.ToString("X"));
			}

			// Create 16 mapping rows
			for(int row = 0; row < 16; row++) {
				MappingRows.Add(new TextHookerMappingRowViewModel(this, row));
			}

			LoadSavedMappings();
		}

		partial void OnChrSelectionIndexChanged(int value)
		{
			if(!_refreshPending && value >= 0) {
				RefreshChrData();
			}
		}

		private void LoadDefaultMappings()
		{
			// Load from embedded resource once (static)
			if(!_defaultMappingsLoaded) {
				_defaultMappingsLoaded = true;
				try {
					var assembly = System.Reflection.Assembly.GetExecutingAssembly();
					using(var stream = assembly.GetManifestResourceStream("Mesen.Debugger.Utilities.CharacterMappings.txt")) {
						if(stream != null) {
							using(var reader = new System.IO.StreamReader(stream)) {
								string content = reader.ReadToEnd();
								char[] separator = new char[] { ',' };
								foreach(string mappingRow in content.Replace("\r", "").Split('\n')) {
									string[] parts = mappingRow.Split(separator, 2);
									if(parts.Length == 2) {
										_defaultCharMappings[parts[0]] = parts[1];
									}
								}
							}
						}
					}
				} catch { }
			}

			// Always copy defaults into per-instance dictionary
			_charMappings.Clear();
			foreach(var kvp in _defaultCharMappings) {
				_charMappings[kvp.Key] = kvp.Value;
			}
		}

		public void LoadSavedMappings()
		{
			// Apply user's saved custom mappings on top of defaults
			foreach(var entry in Config.SavedCharMappings) {
				if(!string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value)) {
					_charMappings[entry.Key] = entry.Value;
				}
			}
		}

		public void SaveMappingsToConfig()
		{
			Config.SavedCharMappings.Clear();

			foreach(var kvp in _charMappings) {
				if(!string.IsNullOrWhiteSpace(kvp.Value)) {
					// Skip entries that match default mappings
#pragma warning disable CS8600
					if(_defaultCharMappings.TryGetValue(kvp.Key, out string defaultVal) && defaultVal == kvp.Value) {
						continue;
					}
#pragma warning restore CS8600

					Config.SavedCharMappings.Add(new CharMappingEntry() { Key = kvp.Key, Value = kvp.Value });
				}
			}
		}

		public void SetCharMapping(string key, string value)
		{
			if(string.IsNullOrWhiteSpace(value)) {
				_charMappings.TryRemove(key, out _);
			} else {
				_charMappings[key] = value;
			}
		}

		public string GetCharMapping(string key)
		{
#pragma warning disable CS8600
			if(_charMappings.TryGetValue(key, out string value)) {
				return value;
			}
#pragma warning restore CS8600
			return " ";
		}

		public string GetCharMappingForTile(byte[] tileBytes)
		{
			if(tileBytes == null || tileBytes.Length < 16) return "";
			string key = GetColorIndependentKey(tileBytes);
#pragma warning disable CS8600
			return _charMappings.TryGetValue(key, out string value) ? value : "";
#pragma warning restore CS8600
		}

		public void SetCharMappingForTile(byte[] tileBytes, string value)
		{
			if(tileBytes == null || tileBytes.Length < 16) return;
			string key = GetColorIndependentKey(tileBytes);
			SetCharMapping(key, value);
		}

		private void UpdateChrBankDropdown(NesCartridgeState cartridge)
		{
			int oldMax = _chrMaxBank;
			_chrMaxBank = 2;
			if(cartridge.ChrRomSize > 0 || cartridge.ChrRamSize > 0) {
				int chrSize = (int)(cartridge.ChrRomSize == 0 ? cartridge.ChrRamSize : cartridge.ChrRomSize);
				_chrMaxBank = 2 + (chrSize / 0x1000);
			}

			if(oldMax != _chrMaxBank || ChrBankOptions.Count == 0) {
				// Reset index before clearing to avoid ComboBox out-of-bounds
				ChrSelectionIndex = -1;
				ChrBankOptions.Clear();
				ChrBankOptions.Add("PPU: $0000");
				ChrBankOptions.Add("PPU: $1000");
				for(int i = 2; i < _chrMaxBank; i++) {
					ChrBankOptions.Add($"CHR: ${(i - 2) * 0x1000:X4}");
				}

				ChrSelectionIndex = 0;
			}

			_chrSizeDirty = false;
		}

		public void OnGameLoaded()
		{
			_ppuMemory = Array.Empty<byte>();
			_prevPpuMemory = Array.Empty<byte>();
			_prevText = null;
			_prevXScroll = -1;
			_prevYScroll = -1;
			_chrData = new byte[16 * 16 * 16];
			_prevChrData = new byte[16 * 16 * 16];
			_chrSizeDirty = true;
			_refreshPending = false;
		}

		public void Refresh(TilemapViewerTabKind kind)
		{
			if(_refreshPending) return;
			_refreshPending = true;

			Dispatcher.UIThread.Post(() => {
				try {
					if(kind == TilemapViewerTabKind.CharacterMappings) {
						RefreshCharacterMappings();
					} else {
						RefreshTextHooker();
					}
				} finally {
					_refreshPending = false;
				}
			});
		}

		#region Text Hooker

		private void RefreshTextHooker()
		{
			lock(_updateLock) {
				try {
					_ppuState = DebugApi.GetPpuState<NesPpuState>(CpuType.Nes);
					var nesState = DebugApi.GetConsoleState<NesState>(ConsoleType.Nes);
					_mirroring = nesState.Cartridge.Mirroring;
					_hasChrRom = nesState.Cartridge.ChrRomSize > 0;
					DebugApi.GetMemoryState(MemoryType.NesPpuMemory, ref _ppuMemory);
				} catch {
					return;
				}

				int coarseX = _ppuState.TmpVideoRamAddr & 0x1F;
				int fineX = _ppuState.ScrollX;
				int coarseY = (_ppuState.TmpVideoRamAddr >> 5) & 0x1F;
				int fineY = (_ppuState.TmpVideoRamAddr >> 12) & 0x07;
				_xScroll = coarseX * 8 + fineX;
				_yScroll = coarseY * 8 + fineY;
				_xScroll &= 0xFFF8;
				_yScroll &= 0xFFF8;
			}

			// Skip the expensive text extraction when neither the PPU memory (which contains the nametable
			// and pattern data) nor the scroll position has changed. This avoids re-running ExtractText on
			// every tilemap refresh while paused or when nothing on screen changed.
			bool memChanged = false;
			if(_prevPpuMemory.Length != _ppuMemory.Length) {
				_prevPpuMemory = new byte[_ppuMemory.Length];
				memChanged = _ppuMemory.Length > 0;
			} else if(_ppuMemory.Length > 0 && !_ppuMemory.AsSpan().SequenceEqual(_prevPpuMemory)) {
				memChanged = true;
			}

			bool dataChanged = memChanged || _xScroll != _prevXScroll || _yScroll != _prevYScroll;

			if(!dataChanged && _prevText != null) {
				_prevXScroll = _xScroll;
				_prevYScroll = _yScroll;
				return;
			}

			if(memChanged) {
				Array.Copy(_ppuMemory, _prevPpuMemory, _ppuMemory.Length);
			}

			_prevXScroll = _xScroll;
			_prevYScroll = _yScroll;

			string extractedText = ExtractText();

			if(dataChanged || _prevText == null || _prevText != extractedText) {
				_prevText = extractedText;
				Dispatcher.UIThread.Post(() => {
					ExtractedText = extractedText;

					if(Config.AutoCopyToClipboard && !string.IsNullOrWhiteSpace(extractedText)) {
						try {
							ApplicationHelper.GetMainWindow()?.Clipboard?.SetTextAsync(extractedText);
						} catch { }
					}
				});
			}
		}

		private string ExtractText()
		{
			if(_ppuMemory.Length < 0x3000) return "";

			StringBuilder output = new StringBuilder();
			DakutenType[] previousLineDakutenType = new DakutenType[32];

			for(int nt = 0; nt < 4; nt++) {
				for(int y = 0; y < 30; y++) {
					StringBuilder lineOutput = new StringBuilder();
					for(int x = 0; x < 32; x++) {
						string value = GetCharacter(nt, y, x);

						DakutenType dakutenType = GetDakutenType(value);
						if(dakutenType == DakutenType.None) {
							bool isKana = value.Length > 0 && (
								(value[0] >= '\x3041' && value[0] <= '\x3096') ||
								(value[0] >= '\x30A1' && value[0] <= '\x30FA')
							);

							DakutenType effectiveDakuten = DakutenType.None;
							if(previousLineDakutenType[x] != DakutenType.None) {
								effectiveDakuten = previousLineDakutenType[x];
							} else if(isKana) {
								effectiveDakuten = GetDakutenType(GetCharacter(nt, y, x + 1));
								if(effectiveDakuten != DakutenType.None && x < 31) {
									previousLineDakutenType[x + 1] = DakutenType.None;
									x++;
								}
							}

							if(isKana && effectiveDakuten == DakutenType.Dakuten) {
								lineOutput.Append((char)(value[0] + 1));
							} else if(isKana && effectiveDakuten == DakutenType.Handakuten) {
								lineOutput.Append((char)(value[0] + 2));
							} else {
								lineOutput.Append(value);
							}
						}
						previousLineDakutenType[x] = dakutenType;
					}

					string rowString = lineOutput.ToString().Trim();
					if(rowString.Length > 0) {
						output.AppendLine(rowString);
					}
				}
			}

			return output.ToString();
		}

		private string GetCharacter(int nt, int y, int x)
		{
			int outNt, outY, outX;
			GetIndexes(nt, y, x, out outNt, out outY, out outX);

			if(IgnoreTile(outNt)) {
				return " ";
			}

			string key = GetTileKey(outNt, (outY << 5) + outX);
			return GetCharMapping(key);
		}

		private void GetIndexes(int inNt, int inY, int inX, out int outNt, out int outY, out int outX)
		{
			outX = inX;
			outY = inY;
			outNt = inNt & 0x03;

			if(Config.AdjustViewportScrolling) {
				outY += _yScroll / 8;
				outX += _xScroll / 8;
			}

			while(outX < 0) { outX += 32; outNt ^= 1; }
			while(outX >= 32) { outX -= 32; outNt ^= 1; }
			while(outY >= 30) { outY -= 30; outNt ^= 2; }
			while(outY < 0) { outY += 30; outNt ^= 2; }

			outNt &= 0x03;
		}

		private bool IgnoreTile(int nametableIndex)
		{
			if(!Config.IgnoreMirroredNametables) return false;

			switch(_mirroring) {
				case NesMirroringType.ScreenAOnly:
				case NesMirroringType.ScreenBOnly:
					return nametableIndex > 0;
				case NesMirroringType.Horizontal:
					return (nametableIndex & 0x01) == 0x01;
				case NesMirroringType.Vertical:
					return (nametableIndex & 0x02) == 0x02;
				default:
					return false;
			}
		}

		private string GetTileKey(int nametableIndex, int index)
		{
			if(_ppuMemory.Length < 0x3000) return "";

			int ntBaseAddr = 0x2000 + nametableIndex * 0x400;
			byte tileIndex = _ppuMemory[ntBaseAddr + index];

			int patternAddr = (int)_ppuState.Control.BackgroundPatternAddr;
			int tileBaseAddr = patternAddr + tileIndex * 16;

			if(tileBaseAddr + 16 > _ppuMemory.Length) return "";

			byte[] tileBytes = _tileData[0];
			for(int i = 0; i < 16; i++) {
				tileBytes[i] = _ppuMemory[tileBaseAddr + i];
			}

			return GetColorIndependentKey(tileBytes);
		}

		private DakutenType GetDakutenType(string value)
		{
			if(value == "daku" || value == "ﾞ") return DakutenType.Dakuten;
			if(value == "han" || value == "ﾟ") return DakutenType.Handakuten;
			return DakutenType.None;
		}

		#endregion

		#region Character Mappings

		private void RefreshCharacterMappings()
		{
			lock(_updateLock) {
				try {
					var nesState = DebugApi.GetConsoleState<NesState>(ConsoleType.Nes);
					var cartridgeState = nesState.Cartridge;
					_mirroring = cartridgeState.Mirroring;
					_hasChrRom = cartridgeState.ChrRomSize > 0;
					DebugApi.GetMemoryState(MemoryType.NesPpuMemory, ref _ppuMemory);

					if(_chrSizeDirty) {
						UpdateChrBankDropdown(cartridgeState);
					}
				} catch {
					return;
				}
			}

			Dispatcher.UIThread.Post(() => RefreshChrData());
		}

		public void RefreshChrData()
		{
			int chrSelection = ChrSelectionIndex;

			lock(_updateLock) {
				if(chrSelection < 2) {
					if(_ppuMemory.Length < (chrSelection + 1) * 0x1000) return;
					int startIndex = chrSelection * 0x1000;
					Array.Copy(_ppuMemory, startIndex, _chrData, 0, 0x1000);
				} else {
					int bankIndex = chrSelection - 2;
					int startIndex = bankIndex * 0x1000;
					MemoryType memType = _hasChrRom ? MemoryType.NesChrRom : MemoryType.NesChrRam;

					int maxSize = DebugApi.GetMemorySize(memType);
					if(startIndex >= maxSize) {
						startIndex = 0;
					}

					byte[] chrMem = DebugApi.GetMemoryValues(memType, (uint)startIndex, (uint)(startIndex + 0x1000 - 1));
					Array.Copy(chrMem, 0, _chrData, 0, Math.Min(chrMem.Length, 0x1000));
				}
			}

			// Skip all rendering work when the CHR data hasn't actually changed (e.g. mid-frame refresh
			// while paused, or when the ROM isn't actively modifying the selected CHR bank). This avoids
			// re-rendering 256 tile bitmaps on every tilemap refresh.
			bool dataChanged = false;
			for(int i = 0; i < _chrData.Length; i++) {
				if(_chrData[i] != _prevChrData[i]) {
					dataChanged = true;
					break;
				}
			}

			if(!dataChanged) {
				return;
			}

			Array.Copy(_chrData, _prevChrData, _chrData.Length);

			RenderChrTiles();
			UpdateMappingRows();
		}

		private void RenderChrTiles()
		{
			DebugPaletteInfo paletteInfo = DebugApi.GetPaletteInfo(CpuType.Nes);
			uint[] palette = paletteInfo.GetRgbPalette();

			using(var fb = ChrBitmap.Lock()) {
				GetTileViewOptions options = new GetTileViewOptions {
					MemType = MemoryType.NesPpuMemory,
					Format = TileFormat.NesBpp2,
					Layout = TileLayout.Normal,
					Width = 16,
					Height = 16,
					StartAddress = 0,
					Palette = 0,
					Background = TileBackground.Magenta,
				};

				DebugApi.GetTileView(CpuType.Nes, options, _chrData, _chrData.Length, palette, fb.FrameBuffer.Address);
			}
		}

		private void UpdateMappingRows()
		{
			uint[] palette = DebugApi.GetPaletteInfo(CpuType.Nes).GetRgbPalette();
			byte[] tileBytes = _tileData[0]; // reuse a scratch buffer for the current tile

			for(int row = 0; row < 16 && row < MappingRows.Count; row++) {
				var mappingRow = MappingRows[row];
				mappingRow.SetParentReference(this);
				var tiles = mappingRow.Tiles;

				for(int col = 0; col < 16 && col < tiles.Count; col++) {
					int tileIndex = row * 16 + col;
					Array.Copy(_chrData, tileIndex * 16, tileBytes, 0, 16);

					// Render this tile as its own 16x16 bitmap (2x scale), transparent background.
					// The bitmap is cached and reused on every refresh to avoid allocating 256 objects per frame.
					DynamicBitmap bmp = _tileBitmaps[tileIndex];
					using(var fb = bmp.Lock()) {
						unsafe {
							uint* buf = (uint*)fb.FrameBuffer.Address.ToPointer();
							for(int i = 0; i < 16 * 16; i++) buf[i] = 0;

							for(int y = 0; y < 8; y++) {
								byte lowByte = tileBytes[y];
								byte highByte = tileBytes[y + 8];
								for(int x = 0; x < 8; x++) {
									byte colorIdx = (byte)(((lowByte >> (7 - x)) & 0x01) | (((highByte >> (7 - x)) & 0x01) << 1));
									uint color = palette.Length > colorIdx ? palette[colorIdx] : 0xFF000000;
									for(int dy = 0; dy < 2; dy++)
										for(int dx = 0; dx < 2; dx++)
											buf[(y * 2 + dy) * 16 + (x * 2 + dx)] = color;
								}
							}
						}
					}

					var cell = tiles[col];
					cell.TileImage = bmp;

					string key = GetColorIndependentKey(tileBytes);
#pragma warning disable CS8600
					if(_charMappings.TryGetValue(key, out string mapping)) {
						cell.SetTileText(mapping);
					} else {
						cell.SetTileText("");
					}
#pragma warning restore CS8600
				}
			}
		}

		public string GenerateTblContent()
		{
			//Generate a TBL file based on the current CHR bank's tile indices (0-255).
			//Only tiles that have a mapping are exported.
			byte[] chrData;
			lock(_updateLock) {
				chrData = new byte[_chrData.Length];
				Array.Copy(_chrData, chrData, _chrData.Length);
			}

			StringBuilder sb = new StringBuilder();
			for(int tileIndex = 0; tileIndex < 256; tileIndex++) {
				byte[] tileBytes = new byte[16];
				int offset = tileIndex * 16;
				if(offset + 16 > chrData.Length) {
					break;
				}
				Array.Copy(chrData, offset, tileBytes, 0, 16);

				string key = GetColorIndependentKey(tileBytes);
				string mapping = GetCharMapping(key);
				if(!string.IsNullOrWhiteSpace(mapping)) {
					sb.AppendLine(tileIndex.ToString("X2") + "=" + mapping);
				}
			}
			return sb.ToString();
		}

		public void OnMappingTextChanged(int row, int col, string text)
		{
			if(row >= 16 || col >= 16) return;

			byte[] tileBytes = new byte[16];
			lock(_updateLock) {
				Array.Copy(_chrData, row * 256 + col * 16, tileBytes, 0, 16);
			}

			string key = GetColorIndependentKey(tileBytes);

			if(string.IsNullOrWhiteSpace(text)) {
				string? old;
				_charMappings.TryRemove(key, out old);
			} else {
				_charMappings[key] = text;
			}
		}

		#endregion

		#region Static Methods

		private static uint[] InitializeHexLookup()
		{
			var result = new uint[256];
			for(int i = 0; i < 256; i++) {
				string s = i.ToString("X2");
				result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
			}
			return result;
		}

		private static string ToHexString(ReadOnlySpan<byte> bytes)
		{
			char[] result = new char[bytes.Length * 2];
			for(int i = 0; i < bytes.Length; i++) {
				var val = HexLookup[bytes[i]];
				result[2 * i] = (char)val;
				result[2 * i + 1] = (char)(val >> 16);
			}
			return new string(result);
		}

		public static string GetColorIndependentKey(byte[] tileData)
		{
			sbyte nextColor = 0;
			Span<byte> colorKey = stackalloc byte[16];
			Span<sbyte> lookupTable = stackalloc sbyte[4] { -1, -1, -1, -1 };
			for(int y = 0; y < 8; y++) {
				byte lowByte = tileData[y];
				byte highByte = tileData[y + 8];

				for(int x = 0; x < 8; x++) {
					byte color = (byte)((lowByte & 0x01) | ((highByte << 1) & 0x02));
					lowByte >>= 1;
					highByte >>= 1;
					if(lookupTable[color] == -1) {
						lookupTable[color] = nextColor;
						nextColor++;
					}

					colorKey[(y << 1) + x / 4] |= (byte)(lookupTable[color] << ((x & 0x03) << 1));
				}
			}

			return ToHexString(colorKey);
		}

		#endregion
	}

	public partial class MappingCellViewModel : ViewModelBase
	{
		private TextHookerViewModel? _parent;
		public int Row { get; }
		public int Col { get; }

		private bool _updatingText = false;

		[ObservableProperty] public partial IImage? TileImage { get; set; }

		[ObservableProperty] public partial string CharText { get; set; } = "";

		public MappingCellViewModel(TextHookerViewModel parent, int row, int col)
		{
			_parent = parent;
			Row = row;
			Col = col;
		}

		[Obsolete("For designer only")]
		public MappingCellViewModel() { }

		public void SetTileText(string text)
		{
			_updatingText = true;
			CharText = text;
			_updatingText = false;
		}

		partial void OnCharTextChanged(string value)
		{
			if(_updatingText || _parent == null) return;
			_parent.OnMappingTextChanged(Row, Col, value);
		}
	}

	public partial class TextHookerMappingRowViewModel : ViewModelBase
	{
		private TextHookerViewModel? _parent;
		private int _rowIndex;

		public string RowLabel { get; } = "";

		public ObservableCollection<MappingCellViewModel> Tiles { get; } = new();

		public TextHookerMappingRowViewModel(TextHookerViewModel parent, int rowIndex)
		{
			_parent = parent;
			_rowIndex = rowIndex;
			RowLabel = rowIndex.ToString("X");

			for(int col = 0; col < 16; col++) {
				Tiles.Add(new MappingCellViewModel(parent, rowIndex, col));
			}
		}

		/// <summary>For designer only</summary>
		[Obsolete]
		public TextHookerMappingRowViewModel() { }

		public void SetParentReference(TextHookerViewModel parent)
		{
			_parent = parent;
		}
	}

	public enum TilemapViewerTabKind
	{
		Tilemap = 0,
		CharacterMappings = 1
	}

	enum DakutenType
	{
		None = 0,
		Dakuten = 1,
		Handakuten = 2
	}
}
