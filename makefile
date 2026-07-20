#Welcome to what must be the most terrible makefile ever (but hey, it works)
#Both clang & gcc work fine - clang seems to output faster code
#.NET 6 (and its dev tools) must be installed to compile the UI.
#The emulation core also requires SDL2.
#Run "make" to build, "make run" to run

SHELL := /usr/bin/bash

MESENFLAGS=

ifeq ($(USE_GCC),true)
	CXX := g++
	CC := gcc
	PROFILE_GEN_FLAG := -fprofile-generate
	PROFILE_USE_FLAG := -fprofile-use
else
	CXX := clang++
	CC := clang
	PROFILE_GEN_FLAG := -fprofile-instr-generate=$(CURDIR)/PGOHelper/pgo.profraw
	PROFILE_USE_FLAG := -fprofile-instr-use=$(CURDIR)/PGOHelper/pgo.profdata
endif

# Use ccache automatically when available (big speedup for incremental/repeat builds).
# Set CCACHE=0 to disable, or CCACHE=1 to force (errors out if missing).
ifeq ($(CCACHE),1)
	CCACHE_CHECK := $(shell command -v ccache >/dev/null 2>&1 || (echo "ccache requested but not found" >&2; exit 1))
	CXX := ccache $(CXX)
	CC := ccache $(CC)
else ifneq ($(CCACHE),0)
	ifeq ($(shell command -v ccache >/dev/null 2>&1 && echo yes),yes)
		CXX := ccache $(CXX)
		CC := ccache $(CC)
	endif
endif

SDL2LIB := $(shell sdl2-config --libs)
SDL2INC := $(shell sdl2-config --cflags)

LINKCHECKUNRESOLVED := -Wl,-z,defs

LINKOPTIONS :=
MESENOS :=
UNAME_S := $(shell uname -s)

ifeq ($(UNAME_S),Linux)
	MESENOS := linux
	SHAREDLIB := MesenCore.so
endif

ifeq ($(UNAME_S),Darwin)
	MESENOS := osx
	SHAREDLIB := MesenCore.dylib
	LTO := false
	STATICLINK := false
	LINKCHECKUNRESOLVED :=
endif

ifneq ($(filter MSYS% MINGW% UCRT% CLANG%,$(UNAME_S)),)
	MESENOS := win
	SHAREDLIB := MesenCore.dll
	LTO := false
	LINKCHECKUNRESOLVED :=
endif

MESENFLAGS += -m64

MACHINE := $(shell uname -m)
ifeq ($(MACHINE),x86_64)
	MESENPLATFORM := $(MESENOS)-x64
endif
ifneq ($(filter %86,$(MACHINE)),)
	MESENPLATFORM := $(MESENOS)-x64
endif
# TODO: this returns `aarch64` on one of my machines...
ifneq ($(filter arm%,$(MACHINE)),)
	MESENPLATFORM := $(MESENOS)-arm64
endif
ifeq ($(MACHINE),aarch64)
	MESENPLATFORM := $(MESENOS)-arm64
	ifeq ($(USE_GCC),true)
		#don't set -m64 on arm64 for gcc (unrecognized option)
		MESENFLAGS=
	endif
endif

DEBUG ?= 0

ifeq ($(DEBUG),0)
	MESENFLAGS += -O3
	ifneq ($(LTO),false)
		MESENFLAGS += -DHAVE_LTO
		ifneq ($(USE_GCC),true)
			MESENFLAGS += -flto=thin
		else
			MESENFLAGS += -flto=auto
		endif
	endif
else
	MESENFLAGS += -O0
	# DWARF debug info is OFF by default to speed up debug builds.
	# Set DWARF=1 to emit it (e.g. when you need gdb/lldb).
	ifeq ($(DWARF),1)
		MESENFLAGS += -g -gsplit-dwarf
	endif
	# Note: if compiling with a sanitizer, you will likely need to `LD_PRELOAD` the library `libMesenCore.so` will be linked against.
	ifneq ($(SANITIZER),)
		ifeq ($(SANITIZER),address)
			# Currently, `-fsanitize=address` is not supported together with `-fsanitize=thread`
			MESENFLAGS += -fsanitize=address
		else ifeq ($(SANITIZER),thread)
			# Currently, `-fsanitize=address` is not supported together with `-fsanitize=thread`
			MESENFLAGS += -fsanitize=thread
		else
$(warning Unrecognised $$(SANITIZER) value: $(SANITIZER))
		endif
		# `-Wl,-z,defs` is incompatible with the sanitizers in a shared lib, unless the sanitizer libs are linked dynamically; hence `-shared-libsan` (not the default for Clang).
		# It seems impossible to link dynamically against two sanitizers at the same time, but that might be a Clang limitation.
		ifneq ($(USE_GCC),true)
			MESENFLAGS += -shared-libsan
		endif
	endif
endif

ifeq ($(PGO),profile)
	MESENFLAGS += ${PROFILE_GEN_FLAG}
endif

ifeq ($(PGO),optimize)
	MESENFLAGS += ${PROFILE_USE_FLAG}
endif

ifneq ($(STATICLINK),false)
	ifneq ($(MESENOS),win)
		LINKOPTIONS += -static-libgcc -static-libstdc++ -Wl,--export-dynamic,--exclude-libs=libstdc++.a
	else
		LINKOPTIONS += -static-libgcc -static-libstdc++
	endif
endif

ifeq ($(MESENOS),osx)
	LINKOPTIONS += -framework Foundation -framework Cocoa -framework GameController -framework CoreHaptics -Wl,-rpath,/opt/local/lib
endif

ifeq ($(MESENOS),win)
	LINKOPTIONS += -lws2_32 -lwinmm -ld3d11 -ld3dcompiler -ldxguid -ldsound -ldxgi -ldinput8 -lxinput -lole32 -loleaut32 -luuid
	MESENFLAGS += -fuse-ld=lld
endif

CXXFLAGS = -fPIC -w --std=c++17 $(MESENFLAGS) $(SDL2INC) -I $(realpath ./) -I $(realpath ./Core) -I $(realpath ./Utilities) -I $(realpath ./Sdl) -pipe
OBJCXXFLAGS = $(CXXFLAGS)
CFLAGS = -fPIC -w $(MESENFLAGS) -pipe

ifeq ($(MESENOS),linux)
	CXXFLAGS += -I $(realpath ./Linux)
endif
ifeq ($(MESENOS),osx)
	CXXFLAGS += -I $(realpath ./MacOS)
endif
ifeq ($(MESENOS),win)
	SDL2_PREFIX := $(patsubst %/include/SDL2,%,$(patsubst -I%,%,$(firstword $(SDL2INC))))
	CXXFLAGS += -I $(SDL2_PREFIX)/include/directxmath -I $(realpath ./Windows)
endif

OBJFOLDER := obj.$(MESENPLATFORM)
DEBUGFOLDER := bin/$(MESENPLATFORM)/Debug
RELEASEFOLDER := bin/$(MESENPLATFORM)/Release
ifeq ($(DEBUG), 0)
	OUTFOLDER = $(RELEASEFOLDER)
	BUILD_TYPE := Release
	OPTIMIZEUI := -p:OptimizeUi=true
else
	OUTFOLDER = $(DEBUGFOLDER)
	BUILD_TYPE := Debug
	OPTIMIZEUI :=
endif


ifeq ($(USE_AOT),true)
	PUBLISHFLAGS ?=  -r $(MESENPLATFORM) -p:PublishSingleFile=false -p:PublishAot=true -p:SelfContained=true
else
	PUBLISHFLAGS ?=  -r $(MESENPLATFORM) --no-self-contained -p:PublishSingleFile=true
endif


CORESRC := $(shell find Core -name '*.cpp')
COREOBJ := $(CORESRC:.cpp=.o)

UTILSRC := $(shell find Utilities -name '*.cpp' -o -name '*.c')
UTILOBJ := $(addsuffix .o,$(basename $(UTILSRC)))

SDLSRC := $(shell find Sdl -name '*.cpp')
SDLOBJ := $(SDLSRC:.cpp=.o)

SEVENZIPSRC := $(shell find SevenZip -name '*.c')
SEVENZIPOBJ := $(SEVENZIPSRC:.c=.o)

ifeq ($(MESENOS),win)
	LUASRC := $(shell find Lua -name '*.c' ! -name 'serial.c' ! -name 'unix.c' ! -name 'unixstream.c' ! -name 'unixdgram.c')
else
	LUASRC := $(shell find Lua -name '*.c')
endif
LUAOBJ := $(LUASRC:.c=.o)

ifeq ($(MESENOS),linux)
	LINUXSRC := $(shell find Linux -name '*.cpp')
else
	LINUXSRC :=
endif
LINUXOBJ := $(LINUXSRC:.cpp=.o)

ifeq ($(MESENOS),win)
	WINSSRC := $(shell find Windows -name '*.cpp')
else
	WINSSRC :=
endif
WINSOBJ := $(WINSSRC:.cpp=.o)

ifeq ($(MESENOS),osx)
	MACOSSRC := $(shell find MacOS -name '*.mm')
else
	MACOSSRC :=
endif
MACOSOBJ := $(MACOSSRC:.mm=.o)

DLLSRC := $(shell find InteropDLL -name '*.cpp')
DLLOBJ := $(DLLSRC:.cpp=.o)

ifeq ($(MESENOS),win)
	LIBEVDEVLIB :=
	LIBEVDEVOBJ :=
	LIBEVDEVINC :=
else ifeq ($(SYSTEM_LIBEVDEV), true)
	LIBEVDEVLIB := $(shell pkg-config --libs libevdev)
	LIBEVDEVINC := $(shell pkg-config --cflags libevdev)
else
	LIBEVDEVSRC := $(shell find Linux/libevdev -name '*.c')
	LIBEVDEVOBJ := $(LIBEVDEVSRC:.c=.o)
	LIBEVDEVINC := -I../
endif

ifeq ($(MESENOS),linux)
	X11LIB := -lX11
else
	X11LIB :=
endif

FSLIB := -lstdc++fs

ifeq ($(MESENOS),win)
	LIBEVDEVOBJ :=
	LIBEVDEVINC :=
	LIBEVDEVSRC :=
	FSLIB :=
endif

ifeq ($(MESENOS),osx)
	LIBEVDEVOBJ := 
	LIBEVDEVINC := 
	LIBEVDEVSRC := 
	FSLIB := 
	ifeq ($(USE_AOT),true)
		PUBLISHFLAGS := -t:BundleApp -p:UseAppHost=true -p:RuntimeIdentifier=$(MESENPLATFORM) -p:PublishSingleFile=false -p:PublishAot=true -p:SelfContained=true
	else
		PUBLISHFLAGS := -t:BundleApp -p:UseAppHost=true -p:RuntimeIdentifier=$(MESENPLATFORM) -p:SelfContained=true -p:PublishSingleFile=false -p:PublishReadyToRun=false
	endif
endif

all: ui

ui: InteropDLL/$(OBJFOLDER)/$(SHAREDLIB)
	mkdir -p $(OUTFOLDER)/Dependencies
	rm -fr $(OUTFOLDER)/Dependencies/*
	cp InteropDLL/$(OBJFOLDER)/$(SHAREDLIB) $(OUTFOLDER)/$(SHAREDLIB)
	#Called twice because the first call copies native libraries to the bin folder which need to be included in Dependencies.zip
	#Don't run with AOT flags the first time to reduce build duration
	cd UI && dotnet publish -c $(BUILD_TYPE) $(OPTIMIZEUI) -r $(MESENPLATFORM)
	cd UI && dotnet publish -c $(BUILD_TYPE) $(OPTIMIZEUI) $(PUBLISHFLAGS)

core: InteropDLL/$(OBJFOLDER)/$(SHAREDLIB)

pgohelper: InteropDLL/$(OBJFOLDER)/$(SHAREDLIB)
	mkdir -p PGOHelper/$(OBJFOLDER) && cd PGOHelper/$(OBJFOLDER) && $(CXX) $(CXXFLAGS) $(LINKCHECKUNRESOLVED) -o pgohelper ../PGOHelper.cpp ../../bin/pgohelperlib.so -pthread $(FSLIB) $(SDL2LIB) $(LIBEVDEVLIB) $(X11LIB)

%.o: %.c
	$(CC) $(CFLAGS) -MMD -MP -c $< -o $@

%.o: %.cpp
	$(CXX) $(CXXFLAGS) -MMD -MP -c $< -o $@

%.o: %.mm
	$(CXX) $(OBJCXXFLAGS) -MMD -MP -c $< -o $@

InteropDLL/$(OBJFOLDER)/$(SHAREDLIB): $(SEVENZIPOBJ) $(LUAOBJ) $(UTILOBJ) $(COREOBJ) $(SDLOBJ) $(LIBEVDEVOBJ) $(LINUXOBJ) $(DLLOBJ) $(MACOSOBJ) $(WINSOBJ)
	mkdir -p bin
	mkdir -p InteropDLL/$(OBJFOLDER)
	$(CXX) $(CXXFLAGS) $(LINKOPTIONS) $(LINKCHECKUNRESOLVED) -shared -o $(SHAREDLIB) $(DLLOBJ) $(SEVENZIPOBJ) $(LUAOBJ) $(LINUXOBJ) $(MACOSOBJ) $(WINSOBJ) $(LIBEVDEVOBJ) $(UTILOBJ) $(SDLOBJ) $(COREOBJ) $(SDL2INC) -pthread $(FSLIB) $(SDL2LIB) $(LIBEVDEVLIB) $(X11LIB)
	cp $(SHAREDLIB) bin/pgohelperlib.so
	mv $(SHAREDLIB) InteropDLL/$(OBJFOLDER)
	cp InteropDLL/$(OBJFOLDER)/$(SHAREDLIB) $(DEBUGFOLDER)/$(SHAREDLIB)

pgo:
	./buildPGO.sh

run:
	$(OUTFOLDER)/$(MESENPLATFORM)/publish/Mesen

clean:
	rm -r -f $(COREOBJ)
	rm -r -f $(UTILOBJ)
	rm -r -f $(LINUXOBJ) $(LIBEVDEVOBJ)
	rm -r -f $(WINSOBJ)
	rm -r -f $(SDLOBJ)
	rm -r -f $(SEVENZIPOBJ)
	rm -r -f $(LUAOBJ)
	rm -r -f $(MACOSOBJ)
	rm -r -f $(DLLOBJ)

-include $(COREOBJ:.o=.d) $(UTILOBJ:.o=.d) $(SDLOBJ:.o=.d) $(LUAOBJ:.o=.d) $(SEVENZIPOBJ:.o=.d) $(DLLOBJ:.o=.d) $(LINUXOBJ:.o=.d) $(LIBEVDEVOBJ:.o=.d) $(WINSOBJ:.o=.d)
