CACHEDIR=deps
OUTDIR=build
SOURCES=main.fs

all: $(OUTDIR)/main.exe

nuget.exe:
	curl -o nuget.exe -L http://nuget.org/nuget.exe

$(CACHEDIR): requirements.txt nuget.exe
	cat requirements.txt | xargs -I % \
		mono nuget.exe install "%" -OutputDirectory $(CACHEDIR)

$(OUTDIR): $(CACHEDIR)
	mkdir -p $(OUTDIR)
	cp $(CACHEDIR)/Suave.*/lib/*.dll $(OUTDIR)
	cp $(CACHEDIR)/Npgsql.*/lib/net40/Npgsql.dll $(OUTDIR)
	cp $(CACHEDIR)/mustache-sharp.*/lib/net40/mustache-sharp.dll $(OUTDIR)
	cp $(CACHEDIR)/MarkdownSharp.*/lib/35/MarkdownSharp.dll $(OUTDIR)

$(OUTDIR)/main.exe: $(OUTDIR) $(SOURCES)
	fsharpc $(SOURCES) -g -o $(OUTDIR)/main.exe -I $(OUTDIR) \
		$(patsubst %,-r %,$(notdir $(wildcard $(OUTDIR)/*.dll)))

clean:
	rm -rf $(OUTDIR) $(CACHEDIR)

standalone: $(OUTDIR)/main.exe
	mkbundle -L $(OUTDIR) --static $(OUTDIR)/main.exe $(OUTDIR)/*.dll -o $(OUTDIR)/standalone
