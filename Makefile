# build и start повторяют команды публикации на Render и работают без mise:
# студенту курса нужен только рантайм стека. Остальные задачи make проксирует
# в mise run, они живут в mise.toml.
build:
	dotnet publish src/PaasCsharpApp -c Release -o out

start: build
	dotnet out/PaasCsharpApp.dll

.PHONY: build start

# Тонкий прокси: make <задача> запускает mise run <задача>. Задачи живут в mise.toml.
%: FORCE
	@mise run $@

# Без этого правила make пытается собрать сам Makefile через прокси.
Makefile: ;
FORCE: ;
