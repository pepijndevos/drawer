NUGET=mono tools/nuget.exe
FSC=fsharpc
CACHEDIR=deps
OUTDIR=build
SOURCES=main.fs

all: $(OUTDIR)/main.exe

$(CACHEDIR):
	xargs -I % -a requirements.txt $(NUGET) install "%" -OutputDirectory $(CACHEDIR)

$(OUTDIR): $(CACHEDIR)
	mkdir -p $(OUTDIR)
	cp $(CACHEDIR)/ServiceStack.*/lib/net40/*.dll $(OUTDIR)
	cp $(CACHEDIR)/Suave.*/lib/*.dll $(OUTDIR)
	cp $(CACHEDIR)/Npgsql.*/lib/net40/Npgsql.dll $(OUTDIR)
	cp $(CACHEDIR)/DotLiquid.*/lib/NET45/DotLiquid.dll $(OUTDIR)

$(OUTDIR)/main.exe: $(OUTDIR) $(SOURCES)
	$(FSC) $(SOURCES) -o $(OUTDIR)/main.exe -I $(OUTDIR) \
		$(patsubst %,-r %,$(notdir $(wildcard $(OUTDIR)/*.dll)))

clean:
	rm -rf $(OUTDIR) $(CACHEDIR)
