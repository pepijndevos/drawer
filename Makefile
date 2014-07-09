NUGET=mono tools/nuget.exe
FSC=fsharpc
CACHEDIR=deps
OUTDIR=build
SOURCES=main.fs

all: $(OUTDIR)/main.exe

$(CACHEDIR):
	cat requirements.txt | xargs -I % \
		$(NUGET) install "%" -OutputDirectory $(CACHEDIR)

$(OUTDIR): $(CACHEDIR)
	mkdir -p $(OUTDIR)
	cp $(CACHEDIR)/ServiceStack.*/lib/net40/*.dll $(OUTDIR)
	cp $(CACHEDIR)/Suave.*/lib/*.dll $(OUTDIR)
	cp $(CACHEDIR)/Npgsql.*/lib/net40/Npgsql.dll $(OUTDIR)
	cp $(CACHEDIR)/mustache-sharp.*/lib/net40/mustache-sharp.dll $(OUTDIR)
	cp $(CACHEDIR)/MarkdownSharp.*/lib/35/MarkdownSharp.dll $(OUTDIR)

$(OUTDIR)/main.exe: $(OUTDIR) $(SOURCES)
	$(FSC) $(SOURCES) -g -o $(OUTDIR)/main.exe -I $(OUTDIR) \
		$(patsubst %,-r %,$(notdir $(wildcard $(OUTDIR)/*.dll)))

clean:
	rm -rf $(OUTDIR) $(CACHEDIR)
